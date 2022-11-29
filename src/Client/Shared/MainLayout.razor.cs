using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Common;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Preferences;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace EHULOG.BlazorWebAssembly.Client.Shared;

public partial class MainLayout
{
    [Parameter]
    public RenderFragment ChildContent { get; set; } = default!;
    [Parameter]
    public EventCallback OnDarkModeToggle { get; set; }
    [Parameter]
    public EventCallback<bool> OnRightToLeftToggle { get; set; }

    [Inject]
    protected AppDataService AppDataService { get; set; } = default!;

    private bool _drawerOpen;
    private bool _rightToLeft;
    private PackageDto package = new();
    private List<LoanDto> loans = new();

    protected override async Task OnInitializedAsync()
    {
        await AppDataService.InitializationAsync();

        if (AppDataService != default)
        {
            if (AppDataService.AppUser != default)
            {
                loans = await AppDataService.GetCurrentUserLoansAsync();
                package = await AppDataService.GetCurrentUserPackageAsync();

                AppDataService.City = AppDataService.AppUser.HomeCity;
                AppDataService.Country = AppDataService.AppUser.HomeCountry;
            }
        }

        if (await ClientPreferences.GetPreference() is ClientPreference preference)
        {
            _rightToLeft = preference.IsRTL;
            _drawerOpen = preference.IsDrawerOpen;
        }
    }

    private async Task RightToLeftToggle()
    {
        bool isRtl = await ClientPreferences.ToggleLayoutDirectionAsync();
        _rightToLeft = isRtl;

        await OnRightToLeftToggle.InvokeAsync(isRtl);
    }

    public async Task ToggleDarkMode()
    {
        await OnDarkModeToggle.InvokeAsync();
    }

    private async Task DrawerToggle()
    {
        _drawerOpen = await ClientPreferences.ToggleDrawerAsync();
    }

    private void Logout()
    {
        var parameters = new DialogParameters
            {
                { nameof(Dialogs.Logout.ContentText), $"{L["Logout Confirmation"]}"},
                { nameof(Dialogs.Logout.ButtonText), $"{L["Logout"]}"},
                { nameof(Dialogs.Logout.Color), Color.Error}
            };

        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        DialogService.Show<Dialogs.Logout>(L["Logout"], parameters, options);
    }

    private void Profile()
    {
        Navigation.NavigateTo("/account");
    }
}