using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Auth;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace EHULOG.BlazorWebAssembly.Client.Pages;

public partial class Index
{

    [CascadingParameter(Name = "AppDataService")]
    protected AppDataService AppDataService { get; set; } = default!;

    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthenticationService AuthService { get; set; } = default!;

    [Inject]
    protected ILoggerFactory LoggerFactory { get; set; } = default!;

    private ILogger Logger { get; set; } = default!;

    private bool _isAuthenticated = false;

    protected override async Task OnInitializedAsync()
    {
        Logger = LoggerFactory.CreateLogger($"EhulogConsoleWriteLine - {nameof(Index)}");

        if (Logger.IsEnabled(LogLevel.Information))
        {
            var st = new StackTrace(new StackFrame(1));

            if (st != default)
            {
                if (st.GetFrame(0) != default)
                {
                    Logger.LogInformation($"EhulogConsoleWriteLine {st?.GetFrame(0)?.GetMethod()?.Name}");
                }
            }
        }

        if ((await AuthState).User is { } user)
        {
            if (user != default)
            {
                if (user.Identity != default)
                {
                    if (user.Identity.IsAuthenticated)
                    {
                        _isAuthenticated = true;
                    }
                }
            }
        }

        if (AppDataService != default)
        {
            if (await AppDataService.IsAuthenticated() is { } authenticatedUser)
            {
                if (AppDataService.AppUser != default)
                {
                    if (!string.IsNullOrEmpty(AppDataService.AppUser.RoleName) && AppDataService.AppUser.RoleName.Equals("Admin"))
                    {
                        Navigation.NavigateTo("/users");
                    }
                    else if (!string.IsNullOrEmpty(AppDataService.AppUser.RoleName) && AppDataService.AppUser.RoleName.Equals("Lender"))
                    {
                        Navigation.NavigateTo("/loans/lender");
                    }
                    else
                    {
                        if (AppDataService.AppUser.RolePackageStatus != default)
                        {
                            if (AppDataService.AppUser.RolePackageStatus < VerificationStatus.Submitted)
                            {
                                Navigation.NavigateTo("/account/role-subscription");
                            }
                            else
                            {
                                Navigation.NavigateTo("/account");
                            }
                        }
                    }
                }
            }
        }
    }
}