using EHULOG.BlazorWebAssembly.Client.Components.Common;
using EHULOG.BlazorWebAssembly.Client.Components.Dialogs;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Auth;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using MudBlazor;
using System.Security.Claims;
using static EHULOG.BlazorWebAssembly.Client.Pages.Identity.Account.AccountRolePackage;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Identity.Account;

public partial class AccountRolePackage
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthenticationService AuthService { get; set; } = default!;
    [Inject]
    protected IInputOutputResourceClient InputOutputResourceClient { get; set; } = default!;

    [Inject]
    protected IPackagesClient PackagesClient { get; set; } = default!;
    [Inject]
    private IRolesClient RolesClient { get; set; } = default!;
    [Inject]
    private IUsersClient UsersClient { get; set; } = default!;
    [Inject]
    private IAppUsersClient AppUsersClient { get; set; } = default!;

    [Inject]
    public AppDataService AppDataService { get; set; } = default!;

    private AppUserDto _appUserDto { get; set; } = default!;

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

    private bool _hideRolePackage { get; set; }

    private bool _selectedRoleIsOpen { get; set; }

    private Guid _oldPackage { get; set; }

    private bool _lockRolePackage { get; set; }

    public class SelectedHoveredVisible
    {
        public bool IsSelected { get; set; }

        public bool IsHovered { get; set; }

        public bool IsVisible { get; set; }
    }

    public class ExtendedPackageDto : SelectedHoveredVisible
    {
        public PackageDto PackageDto { get; set; } = default!;
    }

    public class ExtendedRoleDto : SelectedHoveredVisible
    {
        public RoleDto? RoleDto { get; set; }
    }

    private void AddSelectedRoleIsOpenClass() => _selectedRoleIsOpen = !_selectedRoleIsOpen;

    protected override async Task OnInitializedAsync()
    {
        _lockRolePackage = false;

        _appUserDto = AppDataService.GetAppUserDataTransferObject();

        if (_appUserDto is not null)
        {
            _oldPackage = _appUserDto.PackageId;

            if (!_appUserDto.PackageId.Equals(default!))
            {
                isForSubmission = true;
            }

            /* role */
            if (await ApiHelper.ExecuteCallGuardedAsync(
                     () => UsersClient.GetRolesAsync(_appUserDto.ApplicationUserId), Snackbar)
                 is ICollection<UserRoleDto> response)
            {
                _userRolesList = response.ToList();

                /*
                 * look for the ehulog roles (not admin or basic) that are disabled
                 */
                var lenderOrLessee = _userRolesList.Where(r => (new string[] { "Lender", "Lessee" }).Contains(r.RoleName) && r.Enabled).ToList();

                /*
                 * look for the ehulog roles (not admin or basic) that are disabled
                 */
                var superUsers = _userRolesList.Where(r => (new string[] { "Admin", "Administrator", "SuperUser" }).Contains(r.RoleName) && r.Enabled).ToList();

                if (superUsers.Count() > 0)
                {
                    _hideRolePackage = true;
                }
            }

            /* process */
            if (_appUserDto is not null && _appUserDto.Id != default)
            {
                if (await ApiHelper.ExecuteCallGuardedAsync(
                        () => PackagesClient.GetAsync(null), Snackbar)
                    is List<PackageDto> responsePackages)
                {
                    _packages = responsePackages;

                    foreach (var package in _packages)
                    {
                        var image = await InputOutputResourceClient.GetAsync(package.Id);

                        if (image.Count() > 0)
                        {
                            package.Image = image.First();
                        }
                    }
                }

                if (_packages is not null)
                {
                    foreach (var package in _packages)
                    {
                        bool selected = _appUserDto.PackageId.Equals(package.Id) ? true : false;
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
                        () => UsersClient.GetRolesAsync(_appUserDto.ApplicationUserId), Snackbar)
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

                    // handle already
                    // have role and package
                    lenderOrLessee = lenderOrLessee.Where(r => r.Enabled).ToList();

                    if (lenderOrLessee.Count() == 1)
                    {
                        _appUserDto.RoleId = lenderOrLessee.First().RoleId;

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

                        if (_runningPackages.Where(rp => rp.PackageDto.Id.Equals(_appUserDto.PackageId)).Count() == 1)
                        {
                            _runningPackages.Where(rp => rp.PackageDto.Id.Equals(_appUserDto.PackageId)).First().IsSelected = true;
                            _runningPackages.Where(rp => rp.PackageDto.Id.Equals(_appUserDto.PackageId)).First().IsVisible= true;

                        }

                        _lockRolePackage = true;

                    }
                }
            }
        }
    }

    /* helper functions */
    private void PackagesForRole(bool isLender)
    {
        foreach (var package in _runningPackages)
        {
            package.IsSelected = false;
            package.IsHovered = false;
            package.IsVisible = false;

            // lender
            if (isLender && (package.PackageDto.IsLender == isLender))
            {
                package.IsVisible = true;
            }

            // non-lender
            if (!isLender && (package.PackageDto.IsLender == isLender))
            {
                package.IsVisible = true;
            }
        }
    }

    private void ClearRole()
    {
        if (_appUserDto is not null)
        {
            _appUserDto.RoleId = default!;
            _appUserDto.RoleName = string.Empty;
        }

        /* cancel selection */
        foreach (var role in _runningRoles)
        {
            role.IsSelected = false;
            role.IsHovered = false;
            role.IsVisible = true;
        }

        StateHasChanged();
    }

    private void HoverRole(ExtendedRoleDto extendedRoleDto)
    {
        foreach (var role in _runningRoles)
        {
            role.IsHovered = false;
        }

        if (extendedRoleDto is not null)
        {
            if (_runningRoles is not null && _runningRoles.Where(rr => rr.Equals(extendedRoleDto)).Count() > 0)
            {
                _runningRoles.Where(rr => rr.Equals(extendedRoleDto)).First().IsHovered = true;
            }

            if ((extendedRoleDto.RoleDto is not null) && extendedRoleDto.RoleDto.Name.Equals("Lender"))
            {
                PackagesForRole(true);
            }
            else
            {
                PackagesForRole(false);
            }
        }

        SelectDefaultPackage();

        StateHasChanged();
    }

    private void UpdateRole(ExtendedRoleDto extendedRoleDto)
    {
        foreach (var role in _runningRoles)
        {
            role.IsSelected = false;
            role.IsVisible = false;
        }

        if (extendedRoleDto is not null)
        {
            if (_runningRoles is not null && _runningRoles.Where(rr => rr.Equals(extendedRoleDto)).Count() > 0)
            {
                if (_appUserDto is not null && extendedRoleDto.RoleDto is not null)
                {
                    _appUserDto.RoleId = extendedRoleDto.RoleDto.Id;
                    _appUserDto.RoleName = extendedRoleDto.RoleDto.Name;
                }

                isForSubmission = true;
                _runningRoles.Where(rr => rr.Equals(extendedRoleDto)).First().IsSelected = true;
                _runningRoles.Where(rr => rr.Equals(extendedRoleDto)).First().IsVisible = true;
            }

            if ((extendedRoleDto.RoleDto is not null) && extendedRoleDto.RoleDto.Name.Equals("Lender"))
            {
                PackagesForRole(true);
            }
            else
            {
                PackagesForRole(false);
            }

            if (_appUserDto is not null)
            {
                // make default
                // make it the default value of appuser's package id
                if (_runningPackages is not null && _runningPackages.Where(p => p.IsVisible && p.PackageDto.IsDefault).Count() == 1)
                {
                    _appUserDto.PackageId = _runningPackages.Where(p => p.IsVisible && p.PackageDto.IsDefault).First().PackageDto.Id;
                }
            }
        }

        SelectDefaultPackage();

        StateHasChanged();
    }

    public void SelectDefaultPackage()
    {
        foreach (var package in _runningPackages)
        {
            if (package.IsVisible && package.PackageDto.IsDefault)
            {
                package.IsSelected = true;
            }
        }
    }

    private void ClearPackage()
    {

        if (_appUserDto is not null)
        {
            _appUserDto.PackageId = default!;
        }

        foreach (var package in _runningPackages)
        {
            package.IsSelected = false;
            package.IsHovered = false;
            package.IsVisible = false;
        }

        SelectDefaultPackage();

        ClearRole();
    }

    private void HoverPackage(ExtendedPackageDto extendedPackageDto)
    {
        foreach (var package in _runningPackages)
        {
            package.IsHovered = false;
        }

        if (extendedPackageDto is not null)
        {
            if (_runningPackages is not null && _runningPackages.Where(rr => rr.Equals(extendedPackageDto)).Count() > 0)
            {
                _runningPackages.Where(rr => rr.Equals(extendedPackageDto)).First().IsHovered = true;
            }
        }

        StateHasChanged();
    }

    private void UpdatePackage(ExtendedPackageDto extendedPackageDto)
    {
        /* selected and updated the model */
        foreach (var package in _runningPackages)
        {
            package.IsSelected = false;
            package.IsVisible = false;
        }

        if (extendedPackageDto is not null)
        {
            extendedPackageDto.IsSelected = true;
            extendedPackageDto.IsVisible = true;

            if (_appUserDto is not null && extendedPackageDto.PackageDto is not null)
            {
                _appUserDto.PackageId = extendedPackageDto.PackageDto.Id;
            }
        }

        isForSubmission = true;

        StateHasChanged();
    }

    private void ClearAppUserDto()
    {
        ClearRole();
        ClearPackage();

    }

    private bool isForSubmission { get; set; } = false;

    private async Task UpdateAppUserRoleAndPackage()
    {
        if (_appUserDto is not null)
        {
            /*
       * role
       * _runningRoles is greater than 1, if lessee or lender is not yet selected
       */
            if (!string.IsNullOrEmpty(_appUserDto.RoleId) && _runningRoles.Count() > 1)
            {
                if (await ApiHelper.ExecuteCallGuardedAsync(
                    () => UsersClient.GetRolesAsync(_appUserDto.ApplicationUserId), Snackbar)
                is ICollection<UserRoleDto> response)
                {
                    _userRolesList = response.ToList();

                    _userRolesList.ForEach(userRole =>
                    {
                        if (!string.IsNullOrEmpty(userRole.RoleId) && userRole.RoleId.Equals(_appUserDto.RoleId))
                        {
                            userRole.Enabled = true;
                            _appUserDto.RoleId = userRole.RoleId;
                        }
                        else
                        {
                            userRole.Enabled = false;
                        }

                    });

                    if (await ApiHelper.ExecuteCallGuardedAsync(
                        () => UsersClient.AssignRolesAsync(_appUserDto.ApplicationUserId, new UserRolesRequest
                        {
                            UserRoles = _userRolesList
                        }),
                        Snackbar,
                        successMessage: L["Updated User Roles."]) is not null)
                    {
                    }
                }
            }

            if (Guid.TryParse(_appUserDto.PackageId.ToString(), out _) && _runningPackages.Where(p => p.IsSelected).Count() == 1)
            {
                /* create appuser */
                UpdateAppUserRequest = new()
                {
                    Id = _appUserDto.Id,
                    ApplicationUserId = _appUserDto.ApplicationUserId,
                    PackageId = _appUserDto.PackageId
                };

                if (await ApiHelper.ExecuteCallGuardedAsync(
                    () => AppUsersClient.UpdateAsync(_appUserDto.Id, UpdateAppUserRequest), Snackbar, _customValidation, L["Package updated. "]) is Guid guid)
                {
                    // Snackbar.Add(L["User data found. Propagating... {0}", guid], Severity.Success);
                    Snackbar.Add(L["Your Profile has been updated. Please Login again to Continue."], Severity.Success);

                    DialogOptions noHeader = new DialogOptions() { NoHeader = true };
                    Dialog.Show<TimerReloginDialog>("Relogin", noHeader);

                    return;
                }
            }
        }

        Navigation.NavigateTo($"/account", true);
    }
}