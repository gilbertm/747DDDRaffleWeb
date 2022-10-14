using Microsoft.JSInterop;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using Microsoft.AspNetCore.Components.Authorization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Geo.MapBox.Abstractions;
using MudBlazor;
using Microsoft.AspNetCore.Components;

namespace EHULOG.BlazorWebAssembly.Client.Shared;

public class AppDataService : IAppDataService
{
    private IGeolocationService _geolocationService { get; set; } = default!;

    private AuthenticationStateProvider _authenticationStateProvider { get; set; } = default!;

    private IHttpClientFactory _httpClientFactory { get; set; } = default!;

    private IConfiguration _configuration { get; set; } = default!;

    private IAppUsersClient _appUsersClient { get; set; } = default!;

    private IPackagesClient _packagesClient { get; set; } = default!;

    private IRolesClient _rolesClient { get; set; } = default!;

    private IUsersClient _usersClient { get; set; } = default!;

    private IMapBoxGeocoding _mapBoxGeocoding { get; set; } = default!;

    public AppDataService(IGeolocationService geolocationService, AuthenticationStateProvider authenticationStateProvider, IMapBoxGeocoding mapBoxGeocoding, IConfiguration configuration, IAppUsersClient appUsersClient, IPackagesClient packagesClient, IUsersClient usersClient, IRolesClient rolesClient, IHttpClientFactory httpClientFactory)
    {
        _geolocationService = geolocationService;

        _httpClientFactory = httpClientFactory;

        _configuration = configuration;

        _mapBoxGeocoding = mapBoxGeocoding;

        _appUsersClient = appUsersClient;

        _usersClient = usersClient;

        _packagesClient = packagesClient;

        _rolesClient = rolesClient;

        _authenticationStateProvider = authenticationStateProvider;

        Initialization = InitializationAsync();
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

    private bool _isNewUser { get; set; } = false;

    private GeolocationPosition? _position;

    private GeolocationPositionError? _positionError;

    public AppUserDto GetAppUserDataTransferObject()
    {
        if (_appUserDto is { })
        {
            return _appUserDto;
        }

        return default!;
    }

    public async Task<AppUserDto> InitializationAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        _geolocationService.GetCurrentPosition(
                   component: this,
                   onSuccessCallbackMethodName: nameof(OnPositionRecieved),
                   onErrorCallbackMethodName: nameof(OnPositionError),
                   options: _options);

        bool isAuthenticated = user.Identity?.IsAuthenticated ?? false;

        if (isAuthenticated)
        {
            string? userId = user.FindFirst(c => c.Type.Contains("nameidentifier"))?.Value;

            AppUserDataTransferObject = await _appUsersClient.GetAsync(userId);

            if (AppUserDataTransferObject.Id == Guid.Empty || string.IsNullOrEmpty(AppUserDataTransferObject.ApplicationUserId))
            {
                _isNewUser = true;

                // create the app user defaults
                var createAppUserRequest = new CreateAppUserRequest
                {
                    ApplicationUserId = userId ?? default!
                };

                var guid = await _appUsersClient.CreateAsync(createAppUserRequest);

                /* NOT applicable: assign default role
                   role is basic at this stage, so this part is complete
                   the user will be opted to select between lesser and lessee on the role and packages dashboard

                   NOT applicable: assign default package
                   as the package is subject to the role selection, this can be done during the dashboard selection/completion states */

                AppUserDataTransferObject = await _appUsersClient.GetAsync(userId);
            }

            if (_isNewUser || (string.IsNullOrEmpty(AppUserDataTransferObject.Latitude) && string.IsNullOrEmpty(AppUserDataTransferObject.Longitude)))
            {
                if (_position is { })
                {
                    AppUserDataTransferObject.Longitude = _position.Coords.Longitude.ToString();
                    AppUserDataTransferObject.Latitude = _position.Coords.Latitude.ToString();

                    var responseReverseGeocoding = await _mapBoxGeocoding.ReverseGeocodingAsync(new()
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
                        if (responseReverseGeocoding.Features.Count() > 0)
                        {
                            foreach (var f in responseReverseGeocoding.Features)
                            {
                                if (f.Contexts.Count() > 0)
                                {
                                    foreach (var c in f.Contexts)
                                    {
                                        System.Diagnostics.Debug.Write(c.Id.Contains("Home"));
                                        System.Diagnostics.Debug.Write(c.ContextText);

                                        if (!string.IsNullOrEmpty(c.Id))
                                        {
                                            switch (c.Id)
                                            {
                                                case string s when s.Contains("country"):
                                                    AppUserDataTransferObject.HomeCountry = c.ContextText.First().Text;
                                                    break;
                                                case string s when s.Contains("region"):
                                                    AppUserDataTransferObject.HomeRegion = c.ContextText.First().Text;
                                                    break;
                                                case string s when s.Contains("postcode"):
                                                    AppUserDataTransferObject.HomeAddress += c.ContextText.First().Text;
                                                    break;
                                                case string s when s.Contains("district"):
                                                    AppUserDataTransferObject.HomeCountry += c.ContextText.First().Text;
                                                    break;
                                                case string s when s.Contains("place"):
                                                    AppUserDataTransferObject.HomeCity = c.ContextText.First().Text;
                                                    break;
                                                case string s when s.Contains("locality"):
                                                    AppUserDataTransferObject.HomeAddress += c.ContextText.First().Text;
                                                    break;
                                                case string s when s.Contains("neighborhood"):
                                                    AppUserDataTransferObject.HomeAddress += c.ContextText.First().Text;
                                                    break;
                                                case string s when s.Contains("address"):
                                                    AppUserDataTransferObject.HomeAddress += c.ContextText.First().Text;
                                                    break;
                                                case string s when s.Contains("poi"):
                                                    AppUserDataTransferObject.HomeAddress += c.ContextText.First().Text;
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

                var guid = await _appUsersClient.UpdateAsync(AppUserDataTransferObject.Id, updateAppUserRequest);
            }

            // get application user role, the selected if exists
            // if not just assign the other roles
            // this is used for checking on some parts of the system.
            var userRoles = await _usersClient.GetRolesAsync(userId);
            if (userRoles != null)
            {
                if (!string.IsNullOrEmpty(AppUserDataTransferObject.RoleId))
                {

                    var userRole = userRoles.Where(ur => (ur.RoleId is not null) && ur.RoleId.Equals(_appUserDto.RoleId) && ur.Enabled).FirstOrDefault();

                    if (userRole is not null)
                    {
                        AppUserDataTransferObject.RoleId = userRole.RoleId;
                        AppUserDataTransferObject.RoleName = userRole.RoleName;
                    }
                }
                else
                {
                    var lenderOrLessee = userRoles.Where(r => (new string[] { "Lender", "Lessee" }).Contains(r.RoleName) && r.Enabled).ToList();
                    if (lenderOrLessee.Count() == 1)
                    {
                        // assigned properly with one application role
                        AppUserDataTransferObject.RoleId = lenderOrLessee.First().RoleId;
                        AppUserDataTransferObject.RoleName = lenderOrLessee.First().RoleName;
                    }

                    var basic = userRoles.Where(r => (new string[] { "Basic" }).Contains(r.RoleName) && r.Enabled).ToList();
                    if (basic.Count() > 0)
                    {
                        AppUserDataTransferObject.RoleId = basic.First().RoleId;
                        AppUserDataTransferObject.RoleName = basic.First().RoleName;
                    }

                    var admin = userRoles.Where(r => (new string[] { "Admin" }).Contains(r.RoleName) && r.Enabled).ToList();
                    if (admin.Count() > 0)
                    {
                        AppUserDataTransferObject.RoleId = admin.First().RoleId;
                        AppUserDataTransferObject.RoleName = admin.First().RoleName;
                    }

                }
            }
        }

        return AppUserDataTransferObject;
    }

    public event Action? OnChange;

    private void NotifyDataChanged() => OnChange?.Invoke();

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
}
