using AspNetMonsters.Blazor.Geolocation.Custom;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Auth;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using Microsoft.AspNetCore.Components.Authorization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Geo.MapBox.DependencyInjection;
using Geo.MapBox.Abstractions;
using System.Globalization;

namespace EHULOG.BlazorWebAssembly.Client.Shared;

public class AppDataService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    private IJSRuntime _jsRuntime { get; set; }

    private IHttpClientFactory _httpClientFactory { get; set; }

    private IConfiguration _configuration { get; set; }

    protected IAppUsersClient _appUsersClient { get; set; } = default!;

    protected IPackagesClient _packagesClient { get; set; } = default!;

    private IRolesClient _rolesClient { get; set; } = default!;

    private IUsersClient _usersClient { get; set; } = default!;

    private IMapBoxGeocoding _mapBoxGeocoding { get; set; } = default!;

    private LocationService _locationService { get; set; } = default!;

    public AppDataService(AuthenticationStateProvider authenticationStateProvider, IMapBoxGeocoding mapBoxGeocoding, IConfiguration configuration, IAppUsersClient appUsersClient, IPackagesClient packagesClient, IUsersClient usersClient, IRolesClient rolesClient, IJSRuntime jsRuntime, IHttpClientFactory httpClientFactory, LocationService locationService)
    {
        _jsRuntime = jsRuntime;

        _httpClientFactory = httpClientFactory;

        _configuration = configuration;

        _mapBoxGeocoding = mapBoxGeocoding;

        _appUsersClient = appUsersClient;

        _usersClient = usersClient;

        _packagesClient = packagesClient;

        _rolesClient = rolesClient;

        _locationService = locationService;

        _authenticationStateProvider = authenticationStateProvider;
    }

    private AppUserDto? _appUserDto { get; set; }

    private bool _isNewUser { get; set; } = false;

    public async Task<AppUserDto> Start()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        bool isAuthenticated = user.Identity?.IsAuthenticated ?? false;

        if (isAuthenticated)
        {
            string? userId = user.FindFirst(c => c.Type.Contains("nameidentifier"))?.Value;

            _appUserDto = await _appUsersClient.GetAsync(userId);

            if (_appUserDto.Id == Guid.Empty || string.IsNullOrEmpty(_appUserDto.ApplicationUserId))
            {
                _isNewUser = true;

                /* create the app user defaults */
                var createAppUserRequest = new CreateAppUserRequest
                {
                    ApplicationUserId = userId ?? default!
                };

                var guid = await _appUsersClient.CreateAsync(createAppUserRequest);

                /* NOT applicable: assign default role */
                /* role is basic at this stage, so this part is complete */
                /* the user will be opted to select between lesser and lessee on the role and packages dashboard */

                /* NOT applicable: assign default package */
                /* as the package is subject to the role selection, this can be done during the dashboard selection/completion states */

                _appUserDto = await _appUsersClient.GetAsync(userId);
            }

            if (_isNewUser || (string.IsNullOrEmpty(_appUserDto.Latitude) && string.IsNullOrEmpty(_appUserDto.Longitude)))
            {
                var location = await _locationService.GetLocationAsync();

                if (location is not null)
                {
                    _appUserDto.Longitude = location.Longitude.ToString();
                    _appUserDto.Latitude = location.Latitude.ToString();

                    var responseReverseGeocoding = await _mapBoxGeocoding.ReverseGeocodingAsync(new()
                    {
                        Coordinate = new Geo.MapBox.Models.Coordinate()
                        {
                            Latitude = Convert.ToDouble(_appUserDto.Latitude),
                            Longitude = Convert.ToDouble(_appUserDto.Longitude)
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
                                                    _appUserDto.HomeCountry = c.ContextText.First().Text;
                                                    break;
                                                case string s when s.Contains("region"):
                                                    _appUserDto.HomeRegion = c.ContextText.First().Text;
                                                    break;
                                                case string s when s.Contains("postcode"):
                                                    _appUserDto.HomeAddress += c.ContextText.First().Text;
                                                    break;
                                                case string s when s.Contains("district"):
                                                    _appUserDto.HomeCountry += c.ContextText.First().Text;
                                                    break;
                                                case string s when s.Contains("place"):
                                                    _appUserDto.HomeCity = c.ContextText.First().Text;
                                                    break;
                                                case string s when s.Contains("locality"):
                                                    _appUserDto.HomeAddress += c.ContextText.First().Text;
                                                    break;
                                                case string s when s.Contains("neighborhood"):
                                                    _appUserDto.HomeAddress += c.ContextText.First().Text;
                                                    break;
                                                case string s when s.Contains("address"):
                                                    _appUserDto.HomeAddress += c.ContextText.First().Text;
                                                    break;
                                                case string s when s.Contains("poi"):
                                                    _appUserDto.HomeAddress += c.ContextText.First().Text;
                                                    break;
                                            }
                                        }
                                    }
                                }

                                if (f.Center is { })
                                {
                                    _appUserDto.Latitude = f.Center.Latitude.ToString();
                                    _appUserDto.Longitude = f.Center.Longitude.ToString();
                                }

                                if (f.Properties.Address is { })
                                {
                                    _appUserDto.HomeAddress += f.Properties.Address;
                                }
                            }
                        }
                    }

                }

                var updateAppUserRequest = new UpdateAppUserRequest
                {
                    ApplicationUserId = _appUserDto.ApplicationUserId,
                    HomeAddress = _appUserDto.HomeAddress,
                    HomeCity = _appUserDto.HomeCity,
                    HomeCountry = _appUserDto.HomeCountry,
                    HomeRegion = _appUserDto.HomeRegion,
                    Id = _appUserDto.Id,
                    IsVerified = _appUserDto.IsVerified,
                    Latitude = _appUserDto.Latitude,
                    Longitude = _appUserDto.Longitude,
                    PackageId = _appUserDto.PackageId

                };

                var guid = await _appUsersClient.UpdateAsync(_appUserDto.Id, updateAppUserRequest);
            }

            // get application user role, the selected if exists
            // if not just assign the other roles
            // this is used for checking on some parts of the system.
            var userRoles = await _usersClient.GetRolesAsync(userId);
            if (userRoles != null)
            {
                if (!string.IsNullOrEmpty(_appUserDto.RoleId))
                {

                    var userRole = userRoles.Where(ur => (ur.RoleId is not null) && ur.RoleId.Equals(_appUserDto.RoleId) && ur.Enabled).FirstOrDefault();

                    if (userRole is not null)
                    {
                        _appUserDto.RoleId = userRole.RoleId;
                        _appUserDto.RoleName = userRole.RoleName;
                    }
                }
                else
                {
                    var lenderOrLessee = userRoles.Where(r => (new string[] { "Lender", "Lessee" }).Contains(r.RoleName) && r.Enabled).ToList();
                    if (lenderOrLessee.Count() == 1)
                    {
                        // assigned properly with one application role
                        _appUserDto.RoleId = lenderOrLessee.First().RoleId;
                        _appUserDto.RoleName = lenderOrLessee.First().RoleName;
                    }

                    var basic = userRoles.Where(r => (new string[] { "Basic" }).Contains(r.RoleName) && r.Enabled).ToList();
                    if (basic.Count() > 0)
                    {
                        _appUserDto.RoleId = basic.First().RoleId;
                        _appUserDto.RoleName = basic.First().RoleName;
                    }

                    var admin = userRoles.Where(r => (new string[] { "Admin" }).Contains(r.RoleName) && r.Enabled).ToList();
                    if (admin.Count() > 0)
                    {
                        _appUserDto.RoleId = admin.First().RoleId;
                        _appUserDto.RoleName = admin.First().RoleName;
                    }

                }
            }
        }

        return _appUserDto;
    }

    public event Action? OnChange;

    private void NotifyDataChanged() => OnChange?.Invoke();
}
