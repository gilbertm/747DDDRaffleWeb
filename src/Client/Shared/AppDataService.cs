using Microsoft.JSInterop;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using Microsoft.AspNetCore.Components.Authorization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Geo.MapBox.Abstractions;
using MudBlazor;
using Microsoft.AspNetCore.Components;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Common;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Auth;
using EHULOG.BlazorWebAssembly.Client.Pages.Identity.Users;
using System.Security.Claims;

namespace EHULOG.BlazorWebAssembly.Client.Shared;

public class AppDataService : IAppDataService
{
    private IGeolocationService GeolocationService { get; set; } = default!;

    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    private IHttpClientFactory HttpClientFactory { get; set; } = default!;

    private IConfiguration Configuration { get; set; } = default!;

    private IAppUsersClient AppUsersClient { get; set; } = default!;

    private IPackagesClient PackagesClient { get; set; } = default!;

    private IRolesClient RolesClient { get; set; } = default!;

    private IUsersClient UsersClient { get; set; } = default!;

    private IMapBoxGeocoding MapBoxGeocoding { get; set; } = default!;

    public AppDataService(IGeolocationService geolocationService, AuthenticationStateProvider authenticationStateProvider, IMapBoxGeocoding mapBoxGeocoding, IConfiguration configuration, IAppUsersClient appUsersClient, IPackagesClient packagesClient, IUsersClient usersClient, IRolesClient rolesClient, IHttpClientFactory httpClientFactory)
    {
        GeolocationService = geolocationService;

        HttpClientFactory = httpClientFactory;

        Configuration = configuration;

        MapBoxGeocoding = mapBoxGeocoding;

        AppUsersClient = appUsersClient;

        UsersClient = usersClient;

        PackagesClient = packagesClient;

        RolesClient = rolesClient;

        AuthenticationStateProvider = authenticationStateProvider;

    }

    public Task Initialization { get; private set; }

    private readonly PositionOptions _options = new()
    {
        EnableHighAccuracy = true,
        MaximumAge = null,
        Timeout = 15_000
    };

    private AppUserDto? _appUserDto;

    public AppUserDto AppUserDataTransferObject
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

    private bool IsNewUser { get; set; } = false;

    private GeolocationPosition? _position;

    private GeolocationPositionError? _positionError;

    public async Task InitializationAsync()
    {
        if ((await AuthenticationStateProvider.GetAuthenticationStateAsync()).User is { } userClaimsPrincipal)
        {
            GeolocationService.GetCurrentPosition(
                   component: this,
                   onSuccessCallbackMethodName: nameof(OnPositionRecieved),
                   onErrorCallbackMethodName: nameof(OnPositionError),
                   options: _options);

            Console.WriteLine("------------------------ AppDataService - InitializationAsync");

            string? userId = userClaimsPrincipal.GetUserId() ?? string.Empty;

            AppUserDataTransferObject = await AppUsersClient.GetAsync(userId);

            if (AppUserDataTransferObject != default)
            {
                Console.WriteLine("------------------------ AppDataService - AppUserDataTransferObject");

                if (AppUserDataTransferObject.Id == Guid.Empty || string.IsNullOrEmpty(AppUserDataTransferObject.ApplicationUserId))
                {
                    IsNewUser = true;

                    // create the app user defaults
                    var createAppUserRequest = new CreateAppUserRequest
                    {
                        ApplicationUserId = userId ?? default!
                    };

                    var guid = await AppUsersClient.CreateAsync(createAppUserRequest);

                    /* NOT applicable: assign default role
                       role is basic at this stage, so this part is complete
                       the user will be opted to select between lesser and lessee on the role and packages dashboard

                       NOT applicable: assign default package
                       as the package is subject to the role selection, this can be done during the dashboard selection/completion states */

                    AppUserDataTransferObject = await AppUsersClient.GetAsync(userId);
                }

                if (IsNewUser || (string.IsNullOrEmpty(AppUserDataTransferObject.Latitude) && string.IsNullOrEmpty(AppUserDataTransferObject.Longitude)))
                {
                    if (_position is { })
                    {
                        AppUserDataTransferObject.Longitude = _position.Coords.Longitude.ToString();
                        AppUserDataTransferObject.Latitude = _position.Coords.Latitude.ToString();

                        var responseReverseGeocoding = await MapBoxGeocoding.ReverseGeocodingAsync(new()
                        {
                            Coordinate = new Geo.MapBox.Models.Coordinate()
                            {
                                Latitude = Convert.ToDouble(AppUserDataTransferObject.Latitude),
                                Longitude = Convert.ToDouble(AppUserDataTransferObject.Longitude)
                            },
                            EndpointType = Geo.MapBox.Enums.EndpointType.Places
                        });

                        if (responseReverseGeocoding is { })
                        {
                            if (responseReverseGeocoding.Features.Count > 0)
                            {
                                foreach (var f in responseReverseGeocoding.Features)
                                {
                                    if (f.Contexts.Count > 0)
                                    {
                                        foreach (var c in f.Contexts)
                                        {
                                            // System.Diagnostics.Debug.Write(c.Id.Contains("Home"));
                                            // System.Diagnostics.Debug.Write(c.ContextText);

                                            if (!string.IsNullOrEmpty(c.Id))
                                            {
                                                switch (c.Id)
                                                {
                                                    case string s when s.Contains("country"):
                                                        AppUserDataTransferObject.HomeCountry = c.ContextText[0].Text;
                                                        break;
                                                    case string s when s.Contains("region"):
                                                        AppUserDataTransferObject.HomeRegion = c.ContextText[0].Text;
                                                        break;
                                                    case string s when s.Contains("postcode"):
                                                        AppUserDataTransferObject.HomeAddress += c.ContextText[0].Text;
                                                        break;
                                                    case string s when s.Contains("district"):
                                                        AppUserDataTransferObject.HomeCountry += c.ContextText[0].Text;
                                                        break;
                                                    case string s when s.Contains("place"):
                                                        AppUserDataTransferObject.HomeCity = c.ContextText[0].Text;
                                                        break;
                                                    case string s when s.Contains("locality"):
                                                        AppUserDataTransferObject.HomeAddress += c.ContextText[0].Text;
                                                        break;
                                                    case string s when s.Contains("neighborhood"):
                                                        AppUserDataTransferObject.HomeAddress += c.ContextText[0].Text;
                                                        break;
                                                    case string s when s.Contains("address"):
                                                        AppUserDataTransferObject.HomeAddress += c.ContextText[0].Text;
                                                        break;
                                                    case string s when s.Contains("poi"):
                                                        AppUserDataTransferObject.HomeAddress += c.ContextText[0].Text;
                                                        break;
                                                }
                                            }
                                        }
                                    }

                                    if (f.Center is { })
                                    {
                                        AppUserDataTransferObject.Latitude = f.Center.Latitude.ToString();
                                        AppUserDataTransferObject.Longitude = f.Center.Longitude.ToString();
                                    }

                                    if (f.Properties.Address is { })
                                    {
                                        AppUserDataTransferObject.HomeAddress += f.Properties.Address;
                                    }
                                }
                            }
                        }
                    }

                    var updateAppUserRequest = new UpdateAppUserRequest
                    {
                        ApplicationUserId = AppUserDataTransferObject.ApplicationUserId,
                        HomeAddress = AppUserDataTransferObject.HomeAddress,
                        HomeCity = AppUserDataTransferObject.HomeCity,
                        HomeCountry = AppUserDataTransferObject.HomeCountry,
                        HomeRegion = AppUserDataTransferObject.HomeRegion,
                        Id = AppUserDataTransferObject.Id,
                        IsVerified = AppUserDataTransferObject.IsVerified,
                        Latitude = AppUserDataTransferObject.Latitude,
                        Longitude = AppUserDataTransferObject.Longitude,
                        PackageId = AppUserDataTransferObject.PackageId
                    };

                    var guid = await AppUsersClient.UpdateAsync(AppUserDataTransferObject.Id, updateAppUserRequest);
                }

                // get application user role, the selected if exists
                // if not just assign the other roles
                // this is used for checking on some parts of the system.
                var userRoles = await UsersClient.GetRolesAsync(userId);
                if (userRoles != null)
                {
                    if (!string.IsNullOrEmpty(AppUserDataTransferObject.RoleId))
                    {
                        if (_appUserDto is { })
                        {
                            var userRole = userRoles.FirstOrDefault(ur => (ur.RoleId is not null) && ur.RoleId.Equals(_appUserDto.RoleId) && ur.Enabled);

                            if (userRole is not null)
                            {
                                AppUserDataTransferObject.RoleId = userRole.RoleId;
                                AppUserDataTransferObject.RoleName = userRole.RoleName;
                            }
                        }
                    }
                    else
                    {
                        var lenderOrLessee = userRoles.Where(r => (new string[] { "Lender", "Lessee" }).Contains(r.RoleName) && r.Enabled).ToList();
                        if (lenderOrLessee.Count == 1)
                        {
                            // assigned properly with one application role
                            AppUserDataTransferObject.RoleId = lenderOrLessee[0].RoleId;
                            AppUserDataTransferObject.RoleName = lenderOrLessee[0].RoleName;
                        }

                        var basic = userRoles.Where(r => (new string[] { "Basic" }).Contains(r.RoleName) && r.Enabled).ToList();
                        if (basic.Count > 0)
                        {
                            AppUserDataTransferObject.RoleId = basic[0].RoleId;
                            AppUserDataTransferObject.RoleName = basic[0].RoleName;
                        }

                        var admin = userRoles.Where(r => (new string[] { "Admin" }).Contains(r.RoleName) && r.Enabled).ToList();
                        if (admin.Count > 0)
                        {
                            AppUserDataTransferObject.RoleId = admin[0].RoleId;
                            AppUserDataTransferObject.RoleName = admin[0].RoleName;
                        }
                    }
                }

            }
        }
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
