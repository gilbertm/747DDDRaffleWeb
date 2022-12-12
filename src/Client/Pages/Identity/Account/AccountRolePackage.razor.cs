using EHULOG.BlazorWebAssembly.Client.Components.Common;
using EHULOG.BlazorWebAssembly.Client.Components.Dialogs;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Shared;
using EHULOG.BlazorWebAssembly.Client.Shared.Dialogs;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Globalization;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Identity.Account;

public partial class AccountRolePackage
{
    [CascadingParameter(Name = "AppDataService")]
    public AppDataService AppDataService { get; set; } = default!;

    [Inject]
    protected IDialogService Dialog { get; set; } = default!;

    [Inject]
    protected IInputOutputResourceClient InputOutputResourceClient { get; set; } = default!;

    [Inject]
    protected ISubscriptionsClient SubscriptionsClient { get; set; } = default!;

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

    private Guid CurrentPackageId { get; set; } = default!;

    private Guid NewPackageId { get; set; } = default!;

    private bool LockRole { get; set; }

    private bool LockPackage { get; set; }

    private bool CancelCurrentPlan { get; set; }

    private int TotalCountPackagesForCurrentRole { get; set; }

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

    private bool _isChangePackageOverlayButtonHover = false;

    protected override async Task OnInitializedAsync()
    {
        LockPackage = false;
        LockRole = false;

        if (AppDataService != default)
        {
            Console.WriteLine("EhulogConsoleWriteLine - OnInitializedAsync");

            // popup might loose state
            // reload the user if its gone
            if (AppDataService.AppUser == default)
            {
                await AppDataService.InitializationAsync();
            }

            if (AppDataService.AppUser != default)
            {
                if (AppDataService.AppUser.Subscription != default)
                {
                    CurrentPackageId = AppDataService.AppUser.Subscription.PackageId ?? default!;
                    IsForSubmission = true;
                }

                // initialize running packages
                if (CurrentPackageId != default)
                {
                    if (await ApiHelper.ExecuteCallGuardedAsync(
                            () => PackagesClient.GetAsync(null), Snackbar)
                        is List<PackageDto> responsePackages)
                    {
                        _packages = responsePackages;

                        foreach (var package in _packages)
                        {
                            var image = await InputOutputResourceClient.GetAsync(package.Id);

                            if (image.Count > 0)
                            {
                                package.Image = image.First();
                            }

                            _runningPackages.Add(new ExtendedPackageDto()
                            {
                                PackageDto = package,

                                // set selected and visibility, if matched
                                IsSelected = package.Id.Equals(CurrentPackageId),
                                IsVisible = package.Id.Equals(CurrentPackageId)
                            });
                        }
                    }
                }

                // initialize running roles
                if (await ApiHelper.ExecuteCallGuardedAsync(
                        () => UsersClient.GetRolesAsync(AppDataService.AppUser.ApplicationUserId), Snackbar)
                    is ICollection<UserRoleDto> responseRoles)
                {
                    _userRolesList = responseRoles.ToList();

                    // look for the ehulog roles (not admin or basic) that are disabled
                    var usableRoles = _userRolesList.Where(r => !(new string[] { "Basic", "Admin" }).Contains(r.RoleName)).ToList();

                    // TODO:// implement a unique scenario to handle super users
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
                        if (AppDataService.AppUser.RoleId != default! && !string.IsNullOrEmpty(AppDataService.AppUser.RoleId))
                        {
                            if (!AppDataService.AppUser.RoleId.Equals(currentUserUsableEnabledRoles[0].RoleId))
                            {
                                AppDataService.AppUser.RoleId = currentUserUsableEnabledRoles[0].RoleId;
                                AppDataService.AppUser.RoleName = currentUserUsableEnabledRoles[0].RoleName;
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

                        if (CurrentPackageId != default)
                        {
                            // if package is already propagated
                            // lock the package
                            // but since package can be selected or resubscribed
                            // the package can be updated and upgraded to the selected subscription
                            if (_runningPackages.Find(rp => rp.PackageDto.Id.Equals(CurrentPackageId)) != default!)
                            {
                                _runningPackages.First(rp => rp.PackageDto.Id.Equals(CurrentPackageId)).IsSelected = true;
                                _runningPackages.First(rp => rp.PackageDto.Id.Equals(CurrentPackageId)).IsVisible = true;

                                LockPackage = true;
                            }
                        }
                        else
                        {
                            if ((AppDataService.AppUser.RoleName is { }) && AppDataService.AppUser.RoleName.Equals("Lender"))
                            {
                                ExtendedPackageDto defaultRecord = default!;

                                PackagesForRole(true, out defaultRecord);

                                if (defaultRecord != default!)
                                    SelectPackage(defaultRecord);
                            }
                            else
                            {
                                ExtendedPackageDto defaultRecord = default!;

                                PackagesForRole(false, out defaultRecord);

                                if (defaultRecord != default!)
                                    SelectPackage(defaultRecord);
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
        TotalCountPackagesForCurrentRole = 0;

        foreach (var package in _runningPackages)
        {
            package.IsSelected = false;
            package.IsHovered = false;
            package.IsVisible = false;

            // lender
            if (isLender && (package.PackageDto.IsLender == isLender))
            {
                package.IsVisible = true;
                TotalCountPackagesForCurrentRole++;

                if (package.PackageDto.IsDefault)
                    defaultRecord = package;
            }

            // non-lender
            if (!isLender && (package.PackageDto.IsLender == isLender))
            {
                package.IsVisible = true;
                TotalCountPackagesForCurrentRole++;

                if (package.PackageDto.IsDefault)
                    defaultRecord = package;
            }
        }
    }

    private void ClearRole()
    {
        if (AppDataService.AppUser is not null)
        {
            AppDataService.AppUser.RoleId = default!;
            AppDataService.AppUser.RoleName = string.Empty;
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
                if (AppDataService.AppUser is { } && extendedRoleDto.RoleDto is { })
                {
                    AppDataService.AppUser.RoleId = extendedRoleDto.RoleDto.Id;
                    AppDataService.AppUser.RoleName = extendedRoleDto.RoleDto.Name;
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

            // make default
            // make it the default value of appuser's package id
            if (_runningPackages is { } && _runningPackages.Find(p => p.IsVisible && p.PackageDto.IsDefault) is { })
            {
                NewPackageId = _runningPackages.First(p => p.IsVisible && p.PackageDto.IsDefault).PackageDto.Id;
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
        CancelCurrentPlan = true;

        NewPackageId = CurrentPackageId;

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

    private void ClearReload()
    {
        Navigation.NavigateTo(Navigation.Uri, forceLoad: true);

        // var timer = new Timer(
        //    new TimerCallback(_ =>
        //        {
        //            Navigation.NavigateTo(Navigation.Uri, forceLoad: true);
        //        }),
        //    null,
        //    2000,
        //    2000);
    }

    private void SelectPackage(ExtendedPackageDto extendedPackageDto)
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

            NewPackageId = extendedPackageDto.PackageDto.Id;
        }

        IsForSubmission = true;
        CancelCurrentPlan = true;
        _isChangePackageOverlayButtonHover = false;

        StateHasChanged();
    }

    private async Task CancelPlanAsync()
    {
        //TODO:// make sure cancellation is valid and possible

        _infoMessageWrapper = AddInfoWrapperContent();

        var noHeader = new DialogOptions() { MaxWidth = MaxWidth.Large, CloseButton = true };

        var parameters = new DialogParameters { ["ContentText"] = _infoMessageWrapper, ["IsCancelButton"] = true, ["IsWarning"] = true };

        var dialog = Dialog.Show<Info>("Change plan", parameters, noHeader);

        var resultDialog = await dialog.Result;

        if (!resultDialog.Cancelled)
        {
            if (resultDialog.Data is bool result)
            {
                if (result)
                    ClearPackage();
            }
        }
    }

    private async Task<bool> IsChangePlanAsync()
    {
        //TODO:// make sure cancellation is valid and possible

        _infoMessageWrapper = AddInfoWrapperContent();

        var noHeader = new DialogOptions() { MaxWidth = MaxWidth.Large, CloseButton = true };

        var parameters = new DialogParameters { ["ContentText"] = _infoMessageWrapper, ["IsCancelButton"] = true, ["IsWarning"] = true };

        var dialog = Dialog.Show<Info>("Plan cancellation", parameters, noHeader);

        var resultDialog = await dialog.Result;

        if (!resultDialog.Cancelled)
        {
            if (resultDialog.Data is bool result)
            {
                if (result)
                    return true;
            }
        }

        return false;
    }

    private bool IsForSubmission { get; set; } = false;

    private async Task UpdateAppUserRoleAndPackage()
    {
        if (AppDataService != default)
        {
            if (AppDataService.AppUser != default)
            {
                /*
           * role
           * _runningRoles is greater than 1, if lessee or lender is not yet selected
           */
                if (!string.IsNullOrEmpty(AppDataService.AppUser.RoleId) && _runningRoles.Count > 1)
                {
                    if (await ApiHelper.ExecuteCallGuardedAsync(
                        () => UsersClient.GetRolesAsync(AppDataService.AppUser.ApplicationUserId), Snackbar)
                    is ICollection<UserRoleDto> response)
                    {
                        _userRolesList = response.ToList();

                        _userRolesList.ForEach(userRole =>
                        {
                            if (!string.IsNullOrEmpty(userRole.RoleId) && userRole.RoleId.Equals(AppDataService.AppUser.RoleId))
                            {
                                userRole.Enabled = true;
                                AppDataService.AppUser.RoleId = userRole.RoleId;
                            }
                            else
                            {
                                userRole.Enabled = false;
                            }
                        });

                        if (await ApiHelper.ExecuteCallGuardedAsync(
                            () => UsersClient.AssignRolesAsync(AppDataService.AppUser.ApplicationUserId, new UserRolesRequest
                            {
                                UserRoles = _userRolesList
                            }),
                            Snackbar,
                            successMessage: L["Updated User Roles."]) is not null)
                        {
                        }
                    }
                }
                
                if (AppDataService.AppUser.Subscription != default)
                {
                    if (NewPackageId != default && _runningPackages.Count(p => p.IsSelected) == 1)
                    {
                        // change of package popup
                        // check and confirmed

                        // if old package is default then new package is paid subscription
                        // allow change immediately

                        // if old package is lower than current, warn if there's existing loans
                        // possibily disallow to avoid system conflicts

                        // if new package is greater
                        // allow without any issues

                        // popup ok
                        // create the shopping cart entry
                        // proceed to payment page

                        // if cancelled but change has been made, always remind

                        if (await IsChangePlanAsync())
                        {
                            Navigation.NavigateTo("/payment/gateways?subscription=true", true);
                        }

                        /* update subscription */
                        //UpdateSubscriptionRequest updateSubscriptionRequest = new()
                        //{
                        //    AppUserId = AppDataService.AppUser.Id,
                        //    PackageId = NewPackageId,
                        //};

                        //if (await ApiHelper.ExecuteCallGuardedAsync(
                        //    () => SubscriptionsClient.UpdateAsync(AppDataService.AppUser.Id, updateSubscriptionRequest), Snackbar, _customValidation, L["Subscription updated. "]) is Guid guid)
                        //{

                        //    // Snackbar.Add(L["User data found. Propagating... {0}", guid], Severity.Success);
                        //    // Snackbar.Add(L["Your Profile has been updated. Please Login again to Continue."], Severity.Success);

                        //    // DialogOptions noHeader = new DialogOptions() { NoHeader = true };
                        //    // Dialog.Show<TimerReloginDialog>("Relogin", noHeader);

                        //    Snackbar.Add(L["Role and package submitted. If paid subscription, ensure that you have fulfilled payment."], Severity.Success);

                        //    var timer = new Timer(
                        //        new TimerCallback(_ =>
                        //        {
                        //            Navigation.NavigateTo(Navigation.Uri, forceLoad: true);
                        //        }),
                        //        null,
                        //        2000,
                        //        2000);
                        //}
                    }
                }

            }
        }        
    }

    /*
     * the payment verifies
     * make the user capable of transactions in the system
     * defaults: always verified
     * subscriptions:
     * payment IS A MUST
     */
    private async Task PaymentMock()
    {
        // payment
        // subscriptions
        // update shopping cart

        if (AppDataService != default)
        {
            if (AppDataService.AppUser != default)
            {
                if (AppDataService.AppUser.ApplicationUserId != default)
                {
                    UpdateAppUserRequest updateAppUserRequest = new()
                    {
                        Id = AppDataService.AppUser.Id,
                        ApplicationUserId = AppDataService.AppUser.ApplicationUserId,
                        RolePackageStatus = VerificationStatus.Verified
                    };

                    // appuser update uses the extended app user mapping id
                    if (await ApiHelper.ExecuteCallGuardedAsync(
                        () => AppUsersClient.UpdateAsync(AppDataService.AppUser.Id, updateAppUserRequest), Snackbar, _customValidation, L["Package updated. Subscriptions verified."]) is Guid guid)
                    {
                        if (guid != default)
                        {
                            await AppDataService.RevalidateVerification();
                        }

                        Navigation.NavigateTo(Navigation.Uri, true);
                    }

                }
            }
        }

    }
}
