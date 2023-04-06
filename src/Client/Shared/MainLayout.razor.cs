using RAFFLE.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using RAFFLE.BlazorWebAssembly.Client.Infrastructure.Common;
using RAFFLE.BlazorWebAssembly.Client.Infrastructure.Preferences;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Diagnostics;

namespace RAFFLE.BlazorWebAssembly.Client.Shared;

public partial class MainLayout
{
    [Inject]
    protected AppDataService AppDataService { get; set; } = default!;
    [Parameter]
    public RenderFragment ChildContent { get; set; } = default!;
    [Parameter]
    public EventCallback OnDarkModeToggle { get; set; }
    [Parameter]
    public EventCallback<bool> OnRightToLeftToggle { get; set; }

    [Inject]
    protected ILoggerFactory LoggerFactory { get; set; } = default!;

    private ILogger Logger { get; set; } = default!;

    private bool _drawerOpen;
    private bool _rightToLeft;

    protected override async Task OnInitializedAsync()
    {
        Logger = LoggerFactory.CreateLogger($"RaffleConsoleWriteLine - {nameof(MainLayout)}");

        if (Logger.IsEnabled(LogLevel.Information))
        {
            var st = new StackTrace(new StackFrame(1));

            if (st != default)
            {
                if (st.GetFrame(0) != default)
                {
                    Logger.LogInformation($"RaffleConsoleWriteLine {st?.GetFrame(0)?.GetMethod()?.Name}");
                }
            }
        }

        if (AppDataService != default)
        {
            await AppDataService.InitializationAsync();

            if (AppDataService.AppUser != default)
            {
                AppDataService.ShowValuesAppDto();

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