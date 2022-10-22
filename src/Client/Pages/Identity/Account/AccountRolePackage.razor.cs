using EHULOG.BlazorWebAssembly.Client.Components.Common;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Identity.Account;

public partial class AccountRolePackage
{
    [Inject]
    public AppDataService AppDataService { get; set; } = default!;

    public UserRolesRequest UserRolesRequest { get; set; } = default!;

    public CreateAppUserRequest CreateAppUserRequest { get; set; } = default!;

    public UpdateAppUserRequest UpdateAppUserRequest { get; set; } = default!;

    [Inject]
    protected IInputOutputResourceClient InputOutputResourceClient { get; set; } = default!;

    [Inject]
    protected IPackagesClient PackagesClient { get; set; } = default!;
    [Inject]
    private IUsersClient UsersClient { get; set; } = default!;
    [Inject]
    private IAppUsersClient AppUsersClient { get; set; } = default!;

    private readonly List<ExtendedPackageDto> _runningPackages = new();

    private readonly List<ExtendedRoleDto> _runningRoles = new();

    private List<UserRoleDto> _userRolesList = default!;

    private List<PackageDto> _packages = new();

    private CustomValidation? _customValidation;

    private Guid OldPackage { get; set; }

    private bool LockRole { get; set; }

    private bool LockPackage { get; set; }

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

    protected override async Task OnInitializedAsync()
    {
        await AppDataService.InitializationAsync();

        LockPackage = false;
        LockRole = false;

        if (AppDataService != default)
        {
            if (AppDataService.AppUserDataTransferObject != default)
            {
                // the current package
                // TODOL: this will handle changes of subscription
                OldPackage = AppDataService.AppUserDataTransferObject.PackageId;

                if (!AppDataService.AppUserDataTransferObject.PackageId.Equals(default!))
                {
                    // selecting a different package
                    // TODO: to be handled
                    //       payment subscription handlers
                    IsForSubmission = true;
                }

                // process
                if (AppDataService.AppUserDataTransferObject.Id != default)
                {
                    // package
                    if (await ApiHelper.ExecuteCallGuardedAsync(
                            () => PackagesClient.GetAsync(null), Snackbar)
                        is List<PackageDto> responsePackages)
                    {
                        _packages = responsePackages;

                        foreach (var package in _packages)
                        {
                            // add image
                            var image = await InputOutputResourceClient.GetAsync(package.Id);

                            if (image.Count > 0)
                            {
                                package.Image = image.First();
                            }

                            // application user package id match entry
                            // mark selected and visible
                            bool selected = AppDataService.AppUserDataTransferObject.PackageId.Equals(package.Id);

                            _runningPackages.Add(new ExtendedPackageDto()
                            {
                                PackageDto = package,
                                IsSelected = selected,
                                IsVisible = selected
                            });
                        }
                    }

                    // role
                    if (await ApiHelper.ExecuteCallGuardedAsync(
                            () => UsersClient.GetRolesAsync(AppDataService.AppUserDataTransferObject.ApplicationUserId), Snackbar)
                        is ICollection<UserRoleDto> responseRoles)
                    {
                        _userRolesList = responseRoles.ToList();

                        // look for the ehulog roles (not admin or basic) that are disabled
                        var usableRoles = _userRolesList.Where(r => !(new string[] { "Basic", "Admin" }).Contains(r.RoleName)).ToList();

                        // TODO:// implement a unique scenario
                        var superUsers = _userRolesList.Where(r => (new string[] { "Admin", "Administrator", "SuperUser" }).Contains(r.RoleName) && r.Enabled).ToList();

                        if (superUsers.Count > 0)
                        {
                            // TODO:// handle accordingly, nothing to do yet
                        }

                        // make visible and selectable options
                        // these usable roles
                        // TODO:// this is only available on first sign in
                        //      all usable roles will be displayed
                        //      one the application user selects, next block should always clean the unnecessary
                        foreach (var role in usableRoles.ToList())
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

                        // is usable role and enabled
                        var currentUserUsableEnabledRoles = usableRoles.Where(r => r.Enabled).ToList();

                        // application user already assigned with specific applicaiton role (i.e. lender or lessee)
                        // this role is non-changable
                        if (currentUserUsableEnabledRoles.Count == 1)
                        {
                            if (AppDataService.AppUserDataTransferObject.RoleId != default! && !string.IsNullOrEmpty(AppDataService.AppUserDataTransferObject.RoleId))
                            {
                                if (!AppDataService.AppUserDataTransferObject.RoleId.Equals(currentUserUsableEnabledRoles[0].RoleId))
                                {
                                    AppDataService.AppUserDataTransferObject.RoleId = currentUserUsableEnabledRoles[0].RoleId;
                                    AppDataService.AppUserDataTransferObject.RoleName = currentUserUsableEnabledRoles[0].RoleName;
                                }
                            }

                            // clear, we don't want this changed anymore
                            // not unless deem super necessary
                            _runningRoles.Clear();

                            // the selectable role
                            // is only a singular role
                            // that has been assigned to the application user
                            // all other roles must non-existent
                            _runningRoles.Add(new ExtendedRoleDto()
                            {
                                RoleDto = new RoleDto()
                                {
                                    Id = currentUserUsableEnabledRoles[0].RoleId ?? default!,
                                    Name = currentUserUsableEnabledRoles[0].RoleName ?? default!,
                                    Description = currentUserUsableEnabledRoles[0].Description,
                                },
                                IsSelected = true,
                                IsVisible = true
                            });

                            LockRole = true;

                            if (AppDataService.AppUserDataTransferObject.PackageId != Guid.Empty && AppDataService.AppUserDataTransferObject.PackageId != default)
                            {
                                // if package is already propagated
                                // lock the package
                                // but since package can be selected or resubscribed
                                // the package can be updated and upgraded to the selected subscription
                                if (_runningPackages.Find(rp => rp.PackageDto.Id.Equals(AppDataService.AppUserDataTransferObject.PackageId)) != default!)
                                {
                                    _runningPackages.First(rp => rp.PackageDto.Id.Equals(AppDataService.AppUserDataTransferObject.PackageId)).IsSelected = true;
                                    _runningPackages.First(rp => rp.PackageDto.Id.Equals(AppDataService.AppUserDataTransferObject.PackageId)).IsVisible = true;

                                    LockPackage = true;
                                }
                            }
                            else
                            {
                                if ((AppDataService.AppUserDataTransferObject.RoleName is { }) && AppDataService.AppUserDataTransferObject.RoleName.Equals("Lender"))
                                {
                                    ExtendedPackageDto defaultRecord = default!;

                                    PackagesForRole(true, out defaultRecord);

                                    if (defaultRecord != default!)
                                        UpdatePackage(defaultRecord);
                                }
                                else
                                {
                                    ExtendedPackageDto defaultRecord = default!;

                                    PackagesForRole(false, out defaultRecord);

                                    if (defaultRecord != default!)
                                        UpdatePackage(defaultRecord);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    // helper functions
    // out is the default package for the specified role
    private void PackagesForRole(bool isLender, out ExtendedPackageDto defaultRecord)
    {
        defaultRecord = default!;

        foreach (var package in _runningPackages)
        {
            package.IsSelected = false;
            package.IsHovered = false;
            package.IsVisible = false;

            // lender
            if (isLender && (package.PackageDto.IsLender == isLender))
            {
                package.IsVisible = true;

                if (package.PackageDto.IsDefault)
                    defaultRecord = package;
            }

            // non-lender
            if (!isLender && (package.PackageDto.IsLender == isLender))
            {
                package.IsVisible = true;

                if (package.PackageDto.IsDefault)
                    defaultRecord = package;
            }
        }
    }

    private void ClearRole()
    {
        if (AppDataService.AppUserDataTransferObject is not null)
        {
            AppDataService.AppUserDataTransferObject.RoleId = default!;
            AppDataService.AppUserDataTransferObject.RoleName = string.Empty;
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
            if (_runningRoles is not null && _runningRoles.First(rr => rr.Equals(extendedRoleDto)) is { })
            {
                _runningRoles.First(rr => rr.Equals(extendedRoleDto)).IsHovered = true;
            }

            if ((extendedRoleDto.RoleDto is not null) && extendedRoleDto.RoleDto.Name.Equals("Lender"))
            {
                PackagesForRole(true, out _);
            }
            else
            {
                PackagesForRole(false, out _);
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

        if (extendedRoleDto is { })
        {
            if (_runningRoles is { } && _runningRoles.Find(rr => rr.Equals(extendedRoleDto)) is { })
            {
                if (AppDataService.AppUserDataTransferObject is { } && extendedRoleDto.RoleDto is { })
                {
                    AppDataService.AppUserDataTransferObject.RoleId = extendedRoleDto.RoleDto.Id;
                    AppDataService.AppUserDataTransferObject.RoleName = extendedRoleDto.RoleDto.Name;
                }

                IsForSubmission = true;

                _runningRoles.First(rr => rr.Equals(extendedRoleDto)).IsSelected = true;
                _runningRoles.First(rr => rr.Equals(extendedRoleDto)).IsVisible = true;
            }

            if ((extendedRoleDto.RoleDto is { }) && extendedRoleDto.RoleDto.Name.Equals("Lender"))
            {
                PackagesForRole(true, out _);
            }
            else
            {
                PackagesForRole(false, out _);
            }

            if (AppDataService.AppUserDataTransferObject is { })
            {
                // make default
                // make it the default value of appuser's package id
                if (_runningPackages is { } && _runningPackages.Find(p => p.IsVisible && p.PackageDto.IsDefault) is { })
                {
                    AppDataService.AppUserDataTransferObject.PackageId = _runningPackages.First(p => p.IsVisible && p.PackageDto.IsDefault).PackageDto.Id;
                }
            }
        }

        LockRole = true;

        SelectDefaultPackage();

        StateHasChanged();
    }

    private void SelectDefaultPackage()
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
        LockPackage = false;

        if (AppDataService.AppUserDataTransferObject is not null)
        {
            AppDataService.AppUserDataTransferObject.PackageId = default!;
        }

        foreach (var package in _runningPackages)
        {
            package.IsSelected = false;
            package.IsHovered = false;
            package.IsVisible = false;
        }

        SelectDefaultPackage();

        // can only clear the role selection
        // if it is not locked and saved yet
        if (!LockRole)
        {
            ClearRole();
        }
        else
        {
            if (_runningRoles is not null)
            {
                var role = _runningRoles[0];

                if (role?.RoleDto != null)
                {
                    if (role.RoleDto.Name.Equals("Lender"))
                    {
                        PackagesForRole(true, out _);
                    }
                    else
                    {
                        PackagesForRole(false, out _);
                    }
                }
            }
        }
    }

    private void HoverPackage(ExtendedPackageDto extendedPackageDto)
    {
        foreach (var package in _runningPackages)
        {
            package.IsHovered = false;
        }

        if (extendedPackageDto is not null)
        {
            if (_runningPackages is not null && _runningPackages.First(rr => rr.Equals(extendedPackageDto)) is { })
            {
                _runningPackages.First(rr => rr.Equals(extendedPackageDto)).IsHovered = true;
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

            if (AppDataService.AppUserDataTransferObject is not null && extendedPackageDto.PackageDto is not null)
            {
                AppDataService.AppUserDataTransferObject.PackageId = extendedPackageDto.PackageDto.Id;
            }
        }

        IsForSubmission = true;

        StateHasChanged();
    }

    private void ClearReload()
    {
        Navigation.NavigateTo("/account/rolepackage", true);
    }

    private bool IsForSubmission { get; set; } = false;

    private async Task UpdateAppUserRoleAndPackage()
    {
        if (AppDataService.AppUserDataTransferObject is not null)
        {
            /*
       * role
       * _runningRoles is greater than 1, if lessee or lender is not yet selected
       */
            if (!string.IsNullOrEmpty(AppDataService.AppUserDataTransferObject.RoleId) && _runningRoles.Count > 1)
            {
                if (await ApiHelper.ExecuteCallGuardedAsync(
                    () => UsersClient.GetRolesAsync(AppDataService.AppUserDataTransferObject.ApplicationUserId), Snackbar)
                is ICollection<UserRoleDto> response)
                {
                    _userRolesList = response.ToList();

                    _userRolesList.ForEach(userRole =>
                    {
                        if (!string.IsNullOrEmpty(userRole.RoleId) && userRole.RoleId.Equals(AppDataService.AppUserDataTransferObject.RoleId))
                        {
                            userRole.Enabled = true;
                            AppDataService.AppUserDataTransferObject.RoleId = userRole.RoleId;
                        }
                        else
                        {
                            userRole.Enabled = false;
                        }
                    });

                    if (await ApiHelper.ExecuteCallGuardedAsync(
                        () => UsersClient.AssignRolesAsync(AppDataService.AppUserDataTransferObject.ApplicationUserId, new UserRolesRequest
                        {
                            UserRoles = _userRolesList
                        }),
                        Snackbar,
                        successMessage: L["Updated User Roles."]) is not null)
                    {
                    }
                }
            }

            if (Guid.TryParse(AppDataService.AppUserDataTransferObject.PackageId.ToString(), out _) && _runningPackages.Count(p => p.IsSelected) == 1)
            {
                /******************************************************************************/
                /******************************************************************************/

                //TODO:// This needs to be IMPLEMENTED, MONETIZATION

                /******************************************************************************/
                /******************************************************************************/
                /******************************************************************************/
                /******************************************************************************/

                /* create appuser */
                UpdateAppUserRequest = new()
                {
                    Id = AppDataService.AppUserDataTransferObject.Id,
                    ApplicationUserId = AppDataService.AppUserDataTransferObject.ApplicationUserId,
                    PackageId = AppDataService.AppUserDataTransferObject.PackageId
                };

                if (await ApiHelper.ExecuteCallGuardedAsync(
                    () => AppUsersClient.UpdateAsync(AppDataService.AppUserDataTransferObject.Id, UpdateAppUserRequest), Snackbar, _customValidation, L["Package updated. "]) is Guid guid)
                {
                    // Snackbar.Add(L["User data found. Propagating... {0}", guid], Severity.Success);
                    // Snackbar.Add(L["Your Profile has been updated. Please Login again to Continue."], Severity.Success);

                    // DialogOptions noHeader = new DialogOptions() { NoHeader = true };
                    // Dialog.Show<TimerReloginDialog>("Relogin", noHeader);

                    Snackbar.Add(L["Package updated. If paid subscription, ensure that you have fulfilled payment."], Severity.Success);

                    Navigation.NavigateTo("/account/rolepackage", true);

                    return;
                }
            }
        }

        Navigation.NavigateTo("/account", true);
    }
}