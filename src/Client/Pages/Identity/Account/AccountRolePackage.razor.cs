using System.Security.Claims;
using EHULOG.BlazorWebAssembly.Client.Components.Common;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Auth;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Identity.Account;

public partial class AccountRolePackage
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthenticationService AuthService { get; set; } = default!;
    [Inject]
    protected IAppUsersClient AppUsersClient { get; set; } = default!;
    [Inject]
    protected IPackagesClient PackagesClient { get; set; } = default!;
    [Inject]
    private IRolesClient RolesClient { get; set; } = default!;
    [Inject]
    private IUsersClient UsersClient { get; set; } = default!;

    protected AppUserDto appUserDto { get; set; } = new ();

    public UserRolesRequest UserRolesRequest { get; set; } = default!;

    public CreateAppUserRequest CreateAppUserRequest { get; set; } = default!;

    public UpdateAppUserRequest UpdateAppUserRequest { get; set; } = default!;

    private string _userId { get; set; } = default!;

    private List<UserRoleDto> _userRolesList = default!;

    private List<PackageDto> _packages = new();

    private List<ExtendedPackageDto> _runningPackages = new();

    private List<RoleDto> _roles = new();

    private List<ExtendedRoleDto> _runningRoles = new();

    private CustomValidation? _customValidation;

    private string _styleRolePackage { get; set; } = default!;

    private bool _hideRolePackage { get; set; } = false;

    public class SelectedVisible
    {
        public bool IsSelected { get; set; } = false;

        public bool IsVisible { get; set; } = false;
    }

    public class ExtendedPackageDto : SelectedVisible
    {
        public PackageDto PackageDto { get; set; } = default!;
    }

    public class ExtendedRoleDto : SelectedVisible
    {
        public RoleDto? RoleDto { get; set; }
    }

    protected override async Task OnInitializedAsync()
    {
        if ((await AuthState).User is { } user)
        {
            _userId = user.GetUserId() ?? default!;

            if (!string.IsNullOrEmpty(_userId))
            {
                appUserDto.ApplicationUserId = _userId;

                if (await ApiHelper.ExecuteCallGuardedAsync(
                    () => AppUsersClient.GetAsync(appUserDto.ApplicationUserId), Snackbar, _customValidation) is AppUserDto resultAppUserDto)
                {
                    if (string.IsNullOrEmpty(resultAppUserDto.ApplicationUserId))
                    {
                        var createAppUserRequest = new CreateAppUserRequest
                        {
                            ApplicationUserId = appUserDto.ApplicationUserId
                        };

                        if (await ApiHelper.ExecuteCallGuardedAsync(
                            () => AppUsersClient.CreateAsync(createAppUserRequest), Snackbar, _customValidation) is Guid guid)
                        {
                            appUserDto.Id = guid;
                            Snackbar.Add(L["AppUser data initialized. Propagating... {0}", guid], Severity.Success);
                        }
                    }

                    if (resultAppUserDto.Id != default!)
                    {
                        appUserDto.Id = resultAppUserDto.Id;
                    }

                    if (resultAppUserDto.RoleId != default!)
                    {
                        appUserDto.RoleId = resultAppUserDto.RoleId;
                    }

                    if (resultAppUserDto.PackageId != default!)
                    {
                        appUserDto.PackageId = resultAppUserDto.PackageId;
                    }

                    /* role */
                    if (await ApiHelper.ExecuteCallGuardedAsync(
                             () => UsersClient.GetRolesAsync(appUserDto.ApplicationUserId), Snackbar)
                         is ICollection<UserRoleDto> response)
                    {
                        _userRolesList = response.ToList();

                        /*
                         * look for the ehulog roles (not admin or basic) that are disabled
                         */
                        var lenderOrLessee = _userRolesList.Where(r => (new string[] { "Lender", "Lessee" }).Contains(r.RoleName) && r.Enabled).ToList();

                        if (lenderOrLessee.Count() == 1)
                        {
                        }
                        else
                        {
                            _styleRolePackage = "color:#ff0000";
                        }

                        /*
                         * look for the ehulog roles (not admin or basic) that are disabled
                         */
                        var superUsers = _userRolesList.Where(r => (new string[] { "Admin", "Administrator", "SuperUser" }).Contains(r.RoleName) && r.Enabled).ToList();

                        if (superUsers.Count() > 0)
                        {
                            _hideRolePackage = true;
                        }
                    }

                    Snackbar.Add(L["User data found. Propagating..."], Severity.Success);
                }


                /* process */
                if (appUserDto is not null && appUserDto.Id != default)
                {
                    if (await ApiHelper.ExecuteCallGuardedAsync(
                            () => PackagesClient.GetAsync(null), Snackbar)
                        is List<PackageDto> responsePackages)
                    {
                        _packages = responsePackages;
                    }

                    if (_packages is not null)
                    {
                        foreach (var package in _packages)
                        {
                            bool selected = appUserDto.PackageId.Equals(package.Id) ? true : false;
                            _runningPackages.Add(new ExtendedPackageDto()
                            {
                                PackageDto = package,
                                IsSelected = selected,
                                IsVisible = selected
                            });
                        }
                    }

                    /* role */
                    if (await ApiHelper.ExecuteCallGuardedAsync(
                            () => UsersClient.GetRolesAsync(appUserDto.ApplicationUserId), Snackbar)
                        is ICollection<UserRoleDto> responseRoles)
                    {
                        _userRolesList = responseRoles.ToList();

                        /*
                         * look for the ehulog roles (not admin or basic) that are disabled
                         */
                        var lenderOrLessee = _userRolesList.Where(r => !(new string[] { "Basic", "Admin" }).Contains(r.RoleName)).ToList();

                        foreach (var role in lenderOrLessee.Where(r => !r.Enabled).ToList())
                        {
                            _runningRoles.Add(new ExtendedRoleDto()
                            {
                                RoleDto = new RoleDto()
                                {
                                    Id = role.RoleId ?? default!,
                                    Name = role.RoleName ?? default!,
                                    Description = role.Description,
                                },
                                IsSelected = false,
                                IsVisible = true
                            });
                        }

                        lenderOrLessee = lenderOrLessee.Where(r => r.Enabled).ToList();

                        if (lenderOrLessee.Count() == 1)
                        {
                            appUserDto.RoleId = lenderOrLessee.First().RoleId;

                            _runningRoles.Clear();

                            _runningRoles.Add(new ExtendedRoleDto()
                            {
                                RoleDto = new RoleDto()
                                {
                                    Id = lenderOrLessee.First().RoleId ?? default!,
                                    Name = lenderOrLessee.First().RoleName ?? default!,
                                    Description = lenderOrLessee.First().Description,
                                },
                                IsSelected = true,
                                IsVisible = true
                            });

                            string? lenderOrLesseRoleName = lenderOrLessee.First().RoleName ?? default!;
                            if (lenderOrLesseRoleName is not null && lenderOrLesseRoleName.Equals("Lender"))
                            {
                                PackagesForLenderRole();

                                if (_runningPackages.Where(rp => rp.PackageDto.Id.Equals(appUserDto.PackageId)).Count() == 1)
                                {
                                    _runningPackages = _runningPackages.Where(rp => rp.PackageDto.Id.Equals(appUserDto.PackageId)).ToList();

                                }
                            }
                            else
                            {
                                PackagesForNonLenderRole();
                            }

                        }
                    }
                }
            }

        }
    }

    /* helper functions */
    private void PackagesForLenderRole()
    {
        foreach (var package in _runningPackages)
        {
            package.IsSelected = false;

            if (package.PackageDto.IsLender)
            {
                package.IsVisible = true;
            }
            else
            {
                package.IsVisible = false;
            }
        }
    }

    private void PackagesForNonLenderRole()
    {
        foreach (var package in _runningPackages)
        {
            package.IsSelected = false;

            if (package.PackageDto.IsLender)
            {
                package.IsVisible = false;
            }
            else
            {
                package.IsVisible = true;
            }
        }
    }

    private void UpdateRole(ExtendedRoleDto extendedRoleDto)
    {
        foreach (var role in _runningRoles)
        {
            role.IsSelected = false;
        }

        if (extendedRoleDto is not null)
        {
            extendedRoleDto.IsSelected = true;

            if ((extendedRoleDto.RoleDto is not null) && extendedRoleDto.RoleDto.Name.Equals("Lender"))
            {
                PackagesForLenderRole();
            }
            else
            {
                PackagesForNonLenderRole();
            }
        }

        appUserDto.RoleId = extendedRoleDto?.RoleDto?.Id;
        System.Console.Write("RoleId: ");
        System.Console.WriteLine(extendedRoleDto?.RoleDto?.Id);
        StateHasChanged();
    }

    private void UpdatePackage(ExtendedPackageDto extendedPackageDto)
    {
        foreach (var package in _runningPackages)
        {
            package.IsSelected = false;
        }

        extendedPackageDto.IsSelected = true;

        appUserDto.PackageId = extendedPackageDto.PackageDto.Id;
        System.Console.Write("PackageId: ");
        System.Console.WriteLine(extendedPackageDto.PackageDto.Id);
        StateHasChanged();
    }

    private void ClearAppUserDto()
    {
        foreach (var role in _runningRoles)
        {
            role.IsSelected = false;
        }

        foreach (var package in _runningPackages)
        {
            package.IsSelected = false;
        }

        appUserDto.RoleId = null;
        appUserDto.PackageId = default!;
        StateHasChanged();
    }

    private async Task UpdateAppUserRoleAndPackage()
    {
        if (await ApiHelper.ExecuteCallGuardedAsync(
            () => UsersClient.GetByIdAsync(appUserDto.ApplicationUserId), Snackbar)
            is UserDetailsDto user)
        {
            /*
             * role
             * _runningRoles is greater than 1, if lessee or lender is not yet selected
             */
            if (!string.IsNullOrEmpty(appUserDto.RoleId) && _runningRoles.Count() > 1)
            {
                if (await ApiHelper.ExecuteCallGuardedAsync(
                    () => UsersClient.GetRolesAsync(user.Id.ToString()), Snackbar)
                is ICollection<UserRoleDto> response)
                {
                    _userRolesList = response.ToList();

                    _userRolesList.ForEach(userRole =>
                    {
                        if (!string.IsNullOrEmpty(userRole.RoleId) && userRole.RoleId.Equals(appUserDto.RoleId))
                        {
                            userRole.Enabled = true;
                            appUserDto.RoleId = userRole.RoleId;
                        }
                        else
                        {
                            userRole.Enabled = false;
                        }

                    });

                    if (await ApiHelper.ExecuteCallGuardedAsync(
                        () => UsersClient.AssignRolesAsync(user.Id.ToString(), new UserRolesRequest
                        {
                            UserRoles = _userRolesList
                        }),
                        Snackbar,
                        successMessage: L["Updated User Roles."]) is not null)
                    {
                    }
                }
            }

            if (Guid.TryParse(appUserDto.PackageId.ToString(), out _) && _runningPackages.Count() == 1)
            {
                /* create appuser */
                UpdateAppUserRequest = new()
                {
                    Id = appUserDto.Id,
                    ApplicationUserId = appUserDto.ApplicationUserId,
                    PackageId = appUserDto.PackageId
                };

                if (await ApiHelper.ExecuteCallGuardedAsync(
                    () => AppUsersClient.UpdateAsync(appUserDto.Id, UpdateAppUserRequest), Snackbar, _customValidation, L["Package updated. "]) is Guid guid)
                {
                    Snackbar.Add(L["User data found. Propagating... {0}", guid], Severity.Success);
                }
            }

        }

        Navigation.NavigateTo($"/account", true);
    }
}