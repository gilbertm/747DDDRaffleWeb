using RAFFLE.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using RAFFLE.BlazorWebAssembly.Client.Infrastructure.Auth;
using RAFFLE.BlazorWebAssembly.Client.Infrastructure.Common;
using RAFFLE.WebApi.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace RAFFLE.BlazorWebAssembly.Client.Shared;

public partial class NavMenu
{
    [CascadingParameter(Name = "AppDataService")]
    protected AppDataService AppDataService { get; set; } = default!;

    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;

    [Inject]
    protected IAuthorizationService AuthService { get; set; } = default!;

    private string? _hangfireUrl;
    private bool _canViewHangfire;
    private bool _canViewDashboard;
    private bool _canViewRoles;
    private bool _canViewUsers;
    private bool _canViewProducts;

    private bool _canViewAppUserProducts;
    private bool _canViewBrands;
    private bool _canViewCategories;
    private bool _canViewPackages;
    private bool _canViewTenants;
    private bool _canViewLoans;
    private bool _canViewInputOutputResources;
    private bool _isVerified;

    private bool _canCreateTenants;

    private bool CanViewAdministrationGroup => _canCreateTenants;

    protected override void OnInitialized()
    {
        Console.WriteLine("//////////////////////////////////////////////////////////////////////////////////////////////////");
        Console.WriteLine("---------------------------------- Logged - Navmenu loaded... ------------------------------------");
        Console.WriteLine("//////////////////////////////////////////////////////////////////////////////////////////////////");

    }

    protected override async Task OnParametersSetAsync()
    {

        _hangfireUrl = Config[ConfigNames.ApiBaseUrl] + "jobs";

        var user = (await AuthState).User;

        _canViewHangfire = await AuthService.HasPermissionAsync(user, RAFFLEAction.View, RAFFLEResource.Hangfire);
        _canViewDashboard = await AuthService.HasPermissionAsync(user, RAFFLEAction.View, RAFFLEResource.Dashboard);
        _canViewRoles = await AuthService.HasPermissionAsync(user, RAFFLEAction.View, RAFFLEResource.Roles);
        _canViewUsers = await AuthService.HasPermissionAsync(user, RAFFLEAction.View, RAFFLEResource.Users);
        _canViewTenants = await AuthService.HasPermissionAsync(user, RAFFLEAction.View, RAFFLEResource.Tenants);
        _canViewCategories = await AuthService.HasPermissionAsync(user, RAFFLEAction.View, RAFFLEResource.Categories);
        _canViewInputOutputResources = await AuthService.HasPermissionAsync(user, RAFFLEAction.View, RAFFLEResource.InputOutputResources);

        _canCreateTenants = await AuthService.HasPermissionAsync(user, RAFFLEAction.Create, RAFFLEResource.Tenants);

    }
}