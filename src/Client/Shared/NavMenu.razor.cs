using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Auth;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Common;
using EHULOG.WebApi.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace EHULOG.BlazorWebAssembly.Client.Shared;

public partial class NavMenu
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthorizationService AuthService { get; set; } = default!;
    [Inject]
    public AppDataService AppDataService { get; set; } = default!;

    private AppUserDto _appUserDto { get; set; } = default!;

    private string? _hangfireUrl;
    private bool _canViewHangfire;
    private bool _canViewDashboard;
    private bool _canViewRoles;
    private bool _canViewUsers;
    private bool _canViewProducts;
    private bool _canViewBrands;
    private bool _canViewCategories;
    private bool _canViewPackages;
    private bool _canViewTenants;
    private bool _canViewLoans;
    private bool CanViewAdministrationGroup => _canViewUsers || _canViewRoles || _canViewTenants;

    protected override async Task OnParametersSetAsync()
    {
        _appUserDto = await AppDataService.Start();

        _hangfireUrl = Config[ConfigNames.ApiBaseUrl] + "jobs";
        var user = (await AuthState).User;
        _canViewHangfire = await AuthService.HasPermissionAsync(user, EHULOGAction.View, EHULOGResource.Hangfire);
        _canViewDashboard = await AuthService.HasPermissionAsync(user, EHULOGAction.View, EHULOGResource.Dashboard);
        _canViewRoles = await AuthService.HasPermissionAsync(user, EHULOGAction.View, EHULOGResource.Roles);
        _canViewUsers = await AuthService.HasPermissionAsync(user, EHULOGAction.View, EHULOGResource.Users);
        _canViewProducts = await AuthService.HasPermissionAsync(user, EHULOGAction.View, EHULOGResource.Products);
        _canViewBrands = await AuthService.HasPermissionAsync(user, EHULOGAction.View, EHULOGResource.Brands);
        _canViewTenants = await AuthService.HasPermissionAsync(user, EHULOGAction.View, EHULOGResource.Tenants);
        _canViewCategories = await AuthService.HasPermissionAsync(user, EHULOGAction.View, EHULOGResource.Categories);
        _canViewPackages = await AuthService.HasPermissionAsync(user, EHULOGAction.View, EHULOGResource.Packages);
        _canViewLoans = await AuthService.HasPermissionAsync(user, EHULOGAction.View, EHULOGResource.Loans);
    }
}