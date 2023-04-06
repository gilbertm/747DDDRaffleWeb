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

    private IGeolocationService GeolocationService { get; set; } = default!;

    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    private IHttpClientFactory HttpClientFactory { get; set; } = default!;

    private IConfiguration Configuration { get; set; } = default!;

    private IAppUsersClient AppUsersClient { get; set; } = default!;
        private IRolesClient RolesClient { get; set; } = default!;

    private IUsersClient UsersClient { get; set; } = default!;

        private ILocalStorageService LocalStorageService { get; set; } = default!;

    private ISnackbar Snackbar { get; set; } = default!;

    public AppDataService(ILoggerFactory loggerFactory, ILocalStorageService localStorageService, IGeolocationService geolocationService, AuthenticationStateProvider authenticationStateProvider, ISnackbar snackbar,  IConfiguration configuration, IAppUsersClient appUsersClient, IUsersClient usersClient, IRolesClient rolesClient, IHttpClientFactory httpClientFactory)
    {
        Logger = loggerFactory.CreateLogger($"RaffleConsoleWriteLine - {nameof(AppDataService)}");

        LocalStorageService = localStorageService;

        GeolocationService = geolocationService;

        HttpClientFactory = httpClientFactory;

        Snackbar = snackbar;

        Configuration = configuration;

        AppUsersClient = appUsersClient;

        UsersClient = usersClient;

        RolesClient = rolesClient;

        AuthenticationStateProvider = authenticationStateProvider;
        GeolocationService.GetCurrentPosition(
           component: this,
           onSuccessCallbackMethodName: nameof(OnPositionRecieved),
           onErrorCallbackMethodName: nameof(OnPositionError),
           options: _options);

        ShowValuesAppDto();
    }

    private readonly PositionOptions _options = new()
    {
        EnableHighAccuracy = true,
        MaximumAge = null,
        Timeout = 15_000
    };

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

    public string? City { get; set; }
    public string? Country { get; set; }
    public string? CountryCurrency { get; set; }
    public bool ErrorPopupProfile { get; set; } = false; // use this for error, if true make an error popup

    private bool Changed { get; set; } = false;

    private GeolocationPosition? _position;

    private GeolocationPositionError? _positionError;

    public void ShowValuesAppDto()
    {
        if (Logger.IsEnabled(LogLevel.Information))
        {
            var st = new StackTrace(new StackFrame(1));

            if (st != default)
            {
                if (st.GetFrame(0) != default)
                {
                    Logger.LogInformation($"RaffleConsoleWriteLine {st?.GetFrame(0)?.GetMethod()?.Name} - Country: {JsonSerializer.Serialize(Country)}");
                    Logger.LogInformation($"RaffleConsoleWriteLine {st?.GetFrame(0)?.GetMethod()?.Name} - City: {JsonSerializer.Serialize(City)}");
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

        // if not found / not found exception
        // create a silent custom helper
        // the app user should be present here
        AppUser = await SetupAppUser(userId);

        if (AppUser != default)
        {
            if (string.IsNullOrEmpty(AppUser.Email))
            {
                AppUser.Email = email;
                AppUser.FirstName = firstName;
                AppUser.LastName = lastName;
                AppUser.PhoneNumber = phoneNumber;
                AppUser.ImageUrl = imageUrl;

                Changed = true;
            }

            if (string.IsNullOrEmpty(AppUser.RoleId))
            {
                var userRoles = await UsersClient.GetRolesAsync(userId);

                var lenderOrLessee = userRoles.Where(r => (new string[] { "Lender", "Lessee" }).Contains(r.RoleName) && r.Enabled).ToList();
                if (lenderOrLessee.Count == 1)
                {
                    // assigned properly with one application role
                    AppUser.RoleId = lenderOrLessee[0].RoleId;
                    AppUser.RoleName = lenderOrLessee[0].RoleName;
                }

                var basic = userRoles.Where(r => (new string[] { "Basic" }).Contains(r.RoleName) && r.Enabled).ToList();
                if (basic.Count > 0)
                {
                    AppUser.RoleId = basic[0].RoleId;
                    AppUser.RoleName = basic[0].RoleName;
                }

                var admin = userRoles.Where(r => (new string[] { "Admin" }).Contains(r.RoleName) && r.Enabled).ToList();
                if (admin.Count > 0)
                {
                    AppUser.RoleId = admin[0].RoleId;
                    AppUser.RoleName = admin[0].RoleName;
                }

                Changed = true;
            }

            if (Changed)
            {
                if (await ApiHelper.ExecuteCallGuardedAsync(async () => await AppUsersClient.UpdateAsync(AppUser.Id, AppUser.Adapt<UpdateAppUserRequest>()), Snackbar, null) is Guid guidUpdate)
                {
                    if (guidUpdate != default && guidUpdate != Guid.Empty)
                    {
                        if (await ApiHelper.ExecuteCallGuardedAsync(async () => await AppUsersClient.GetApplicationUserAsync(userId), Snackbar, null) is AppUserDto appUserUpdated)
                        {
                            AppUser = appUserUpdated;

                            Changed = false;
                        }
                    }
                }
            }
        }

        // if (_position == default)
           //  await UpdateLocationAsync();
    }

    private async Task<AppUserDto> SetupAppUser(string userId)
    {
        try
        {
            var appUserCheck = await AppUsersClient.GetApplicationUserAsync(userId);

        }
        catch (Exception)
        {

            var guid = await AppUsersClient.CreateAsync(new CreateAppUserRequest
            {
                ApplicationUserId = userId
            });

        }

        if (await ApiHelper.ExecuteCallGuardedAsync(async () => await AppUsersClient.GetApplicationUserAsync(userId), Snackbar, default) is AppUserDto appUser)
        {
            if (appUser != default)
            {
                return appUser;
            }

        }

        return default!;
    }

    /// <summary>
    /// Updates the location of the app user
    /// OnAfterRenderAsync is the most recommended
    /// </summary>
    /// <returns></returns>
    [Obsolete("UpdateLocationAsync looks unnecessary.")]
    public async Task UpdateLocationAsync()
    {
        if (Logger.IsEnabled(LogLevel.Information))
        {
            var st = new StackTrace(new StackFrame(1));

            if (st != default)
            {
                if (st.GetFrame(0) != default)
                {
                    Logger.LogInformation($"RaffleConsoleWriteLine {st?.GetFrame(0)?.GetMethod()?.Name} - {JsonSerializer.Serialize(AppUser)} - Update Location");
                }
            }
        }

        if (_position != default)
        {
            if (_position.Coords != default)
            {
                if (AppUser != default)
                {
                    if (AppUser.Longitude == default && AppUser.Latitude == default)
                    {
                        AppUser.Longitude = _position.Coords.Longitude.ToString();
                        AppUser.Latitude = _position.Coords.Latitude.ToString();

                    }

                    var updateAppUserRequest = new UpdateAppUserRequest
                    {
                        Id = AppUser.Id,
                        ApplicationUserId = AppUser.ApplicationUserId,
                        HomeAddress = AppUser.HomeAddress,
                        HomeCity = AppUser.HomeCity,
                        HomeCountry = AppUser.HomeCountry,
                        HomeRegion = AppUser.HomeRegion,
                        Latitude = AppUser.Latitude,
                        Longitude = AppUser.Longitude,
                    };

                    if (await ApiHelper.ExecuteCallGuardedAsync(async () => await AppUsersClient.UpdateAsync(AppUser.Id, updateAppUserRequest), Snackbar, null) is Guid guidUpdate)
                    {
                        if (guidUpdate != default && guidUpdate != Guid.Empty)
                        {
                            if (await ApiHelper.ExecuteCallGuardedAsync(async () => await AppUsersClient.GetApplicationUserAsync(AppUser.ApplicationUserId), Snackbar, null) is AppUserDto appUserUpdated)
                            {
                                AppUser = appUserUpdated;
                            }
                        }
                    }
                }
            }

        }
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

    public string GetCurrencyAppUser()
    {
        var countryProvider = new CountryProvider();

        var countryInfo = countryProvider.GetCountryByName(AppUser.HomeCountry);

        if (AppUser != default)
        {
            if (countryInfo is { })
            {
                if (countryInfo.Currencies.Count() > 0)
                {
                    return countryInfo.Currencies.FirstOrDefault()?.IsoCode ?? string.Empty;
                }

            }
        }

        return string.Empty;
    }

    public event Action? OnChange;

    public GeolocationPosition GetGeolocationPosition()
    {
        return _position ?? default!;
    }

    public GeolocationPositionError GetGeolocationPositionError()
    {
        return _positionError ?? default!;
    }

    #region business logics

    /// <summary>
    /// Applicable: Lender
    /// Check the current user's package
    ///     against lent or provided running loans (draft, published, running payment)
    /// </summary>
    /// <returns>bool</returns>
    public async Task<bool> CanCreateActionAsync()
    {
        if (AppUser != default)
        {
            if (AppUser.RoleName != default)
            {
            }
        }

        return false;
    }

    /// <summary>
    /// Applicable: Lessee
    /// Check the current user's package
    ///     against applied loans (awarded, running)
    /// </summary>
    /// <returns>bool</returns>
    public async Task<bool> CanApplyActionAsync()
    {
       return false;
    }

    #endregion

    #region helpers
    #endregion

    [JSInvokable]
    public void OnPositionRecieved(GeolocationPosition position)
    {
        _position = position;
        NotifyDataChanged();
    }

    [JSInvokable]
    public void OnPositionError(GeolocationPositionError positionError)
    {
        _positionError = positionError;
        NotifyDataChanged();

    }

    private void NotifyDataChanged() => OnChange?.Invoke();
}