using Blazored.LocalStorage;
using RAFFLE.BlazorWebAssembly.Client.Components.EntityTable;
using RAFFLE.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using RAFFLE.BlazorWebAssembly.Client.Infrastructure.Common;
using Mapster;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using MudBlazor;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using Nager.Country;

namespace RAFFLE.BlazorWebAssembly.Client.Shared;

public class AppDataService : IAppDataService
{
    private ILogger Logger { get; set; } = default!;

    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    private IHttpClientFactory HttpClientFactory { get; set; } = default!;

    private IConfiguration Configuration { get; set; } = default!;

    private IAppUsersClient AppUsersClient { get; set; } = default!;

    private IRolesClient RolesClient { get; set; } = default!;

    private IUsersClient UsersClient { get; set; } = default!;

    private ILocalStorageService LocalStorageService { get; set; } = default!;

    private ISnackbar Snackbar { get; set; } = default!;

    public AppDataService(ILoggerFactory loggerFactory, ILocalStorageService localStorageService, IGeolocationService geolocationService, AuthenticationStateProvider authenticationStateProvider, ISnackbar snackbar, IConfiguration configuration, IAppUsersClient appUsersClient, IUsersClient usersClient, IRolesClient rolesClient, IHttpClientFactory httpClientFactory)
    {
        Logger = loggerFactory.CreateLogger($"RaffleConsoleWriteLine - {nameof(AppDataService)}");

        LocalStorageService = localStorageService;

        HttpClientFactory = httpClientFactory;

        Snackbar = snackbar;

        Configuration = configuration;

        AppUsersClient = appUsersClient;

        UsersClient = usersClient;

        RolesClient = rolesClient;

        AuthenticationStateProvider = authenticationStateProvider;
    }

    private AppUserDto? _appUserDto;

    public AppUserDto AppUser
    {
        get
        {
            return _appUserDto ?? default!;
        }

        private set
        {
            if (value != default!)
            {
                if (!AppUserDto.Equals(_appUserDto, value))
                {
                    _appUserDto = value;
                }
            }
        }
    }


    public void ShowValuesAppDto()
    {
        if (Logger.IsEnabled(LogLevel.Information))
        {
            var st = new StackTrace(new StackFrame(1));

            if (st != default)
            {
                if (st.GetFrame(0) != default)
                {
                    Logger.LogInformation($"RaffleConsoleWriteLine {st?.GetFrame(0)?.GetMethod()?.Name} - AppUser: {JsonSerializer.Serialize(AppUser)}");
                }
            }
        }
    }

    public async Task InitializationAsync()
    {
        if (Logger.IsEnabled(LogLevel.Information))
        {
            var st = new StackTrace(new StackFrame(1));

            if (st != default)
            {
                if (st.GetFrame(0) != default)
                {
                    Logger.LogInformation($"RaffleConsoleWriteLine {st?.GetFrame(0)?.GetMethod()?.Name} - InitializationAsync");
                }
            }
        }

        var userClaimsPrincipal = await IsAuthenticated();

        if (userClaimsPrincipal == default)
        {
            return;
        }

        string userId = userClaimsPrincipal.GetUserId() ?? string.Empty;
        string email = userClaimsPrincipal.GetEmail() ?? string.Empty;
        string firstName = userClaimsPrincipal.GetFirstName() ?? string.Empty;
        string lastName = userClaimsPrincipal.GetSurname() ?? string.Empty;
        string phoneNumber = userClaimsPrincipal.GetPhoneNumber() ?? string.Empty;
        string imageUrl = string.IsNullOrEmpty(userClaimsPrincipal?.GetImageUrl()) ? string.Empty : (Configuration[ConfigNames.ApiBaseUrl] + userClaimsPrincipal?.GetImageUrl());
    }

    public async Task<ClaimsPrincipal> IsAuthenticated()
    {
        // note: front anon requests has bypass on the jwt handler
        // Client.Infrastructure\Auth\Jwt\JwtAuthenticationHeaderHandler.cs
        // this allows custom passthrough
        // equivalent
        // var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        // var user = authState.User;
        // IsAuthenticated = user.Identity?.IsAuthenticated ?? false;

        var getAuthenticationStateAsync = await AuthenticationStateProvider.GetAuthenticationStateAsync();

        if (getAuthenticationStateAsync != default)
        {
            if ((await AuthenticationStateProvider.GetAuthenticationStateAsync()).User is ClaimsPrincipal userClaimsPrincipal)
            {
                if (userClaimsPrincipal.Identity != default)
                {
                    if (userClaimsPrincipal.Identity.IsAuthenticated)
                    {
                        return userClaimsPrincipal;
                    }
                    else
                    {
                        return default!;
                    }
                }
            }
        }

        return default!;
    }
}