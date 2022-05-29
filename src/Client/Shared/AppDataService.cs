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

namespace EHULOG.BlazorWebAssembly.Client.Shared;

public class AppDataService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    private IJSRuntime _jsRuntime { get; set; }

    private IHttpClientFactory _httpClientFactory { get; set; }

    protected IAppUsersClient _appUsersClient { get; set; } = default!;

    protected IPackagesClient PackagesClient { get; set; } = default!;

    private IRolesClient RolesClient { get; set; } = default!;

    private IUsersClient UsersClient { get; set; } = default!;

    private LocationService _locationService { get; set; } = default!;

    public AppDataService(AuthenticationStateProvider authenticationStateProvider, IAppUsersClient appUsersClient, IJSRuntime jsRuntime, IHttpClientFactory httpClientFactory, LocationService locationService)
    {
        _jsRuntime = jsRuntime;

        _httpClientFactory = httpClientFactory;

        _appUsersClient = appUsersClient;

        _locationService = locationService;

        _authenticationStateProvider = authenticationStateProvider;
    }

    private AppUserDto _appUserDto { get; set; } = new();

    public AppUserDto AppUserDto
    {
        get
        {
            return _appUserDto ?? default!;
        }

        set
        {
            _appUserDto = value;
            NotifyDataChanged();
        }
    }

    private bool _isNewUser { get; set; } = false;

    public async Task Start()
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
                    var obj = new
                    {
                        Key = "pk.bf547d628289a729866c964e450f6beb",
                        Longitude = location.Longitude.ToString(),
                        Latitude = location.Latitude.ToString(),
                    };

                    _appUserDto.Longitude = location.Longitude.ToString();
                    _appUserDto.Latitude = location.Latitude.ToString();

                    var request = new HttpRequestMessage(
                        HttpMethod.Get,
                        $"https://us1.locationiq.com/v1/reverse.php?key={obj.Key}&lat={obj.Latitude}&lon={obj.Longitude}&format=json");

                    var client = _httpClientFactory.CreateClient();

                    var response = await client.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        using var responseStream = await response.Content.ReadAsStreamAsync();
                        var locations = await JsonSerializer.DeserializeAsync<LocationIQGeoCoding>(responseStream);

                        if (locations is not null)
                        {
                            if (!string.IsNullOrEmpty(locations.Address.Country))
                            {
                                _appUserDto.HomeCountry = locations.Address.Country;
                            }

                            if (!string.IsNullOrEmpty(locations.Address.County) || !string.IsNullOrEmpty(locations.Address.State))
                            {
                                _appUserDto.HomeCity = locations.Address.County ?? locations.Address.State;
                            }

                            if (!string.IsNullOrEmpty(locations.Address.Region))
                            {
                                _appUserDto.HomeRegion = locations.Address.Region;
                            }

                            if (!string.IsNullOrEmpty(locations.Address.Road))
                            {
                                _appUserDto.HomeAddress = string.Concat(locations.Address.Suburb, " ", locations.Address.Road);
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
        }
    }

    public event Action? OnChange;

    private void NotifyDataChanged() => OnChange?.Invoke();

    public class LocationIQGeoCoding
    {
        /// <summary>
        /// Gets or Sets Distance.
        /// </summary>
        [JsonPropertyName("distance")]
        public decimal Distance { get; set; }

        /// <summary>
        /// Gets or Sets PlaceId.
        /// </summary>
        [JsonPropertyName("place_id")]
        public string PlaceId { get; set; } = default!;

        /// <summary>
        /// Gets or Sets Licence.
        /// </summary>
        [JsonPropertyName("licence")]
        public string Licence { get; set; } = default!;

        /// <summary>
        /// Gets or Sets Osm_type.
        /// </summary>
        [JsonPropertyName("osm_type")]
        public string Osm_type { get; set; } = default!;

        /// <summary>
        /// Gets or Sets Osm_id.
        /// </summary>
        [JsonPropertyName("osm_id")]
        public string Osm_id { get; set; } = default!;

        /// <summary>
        /// Gets or Sets Longitude.
        /// </summary>
        [JsonPropertyName("lon")]
        public string Longitude { get; set; } = default!;

        /// <summary>
        /// Gets or Sets Latitude.
        /// </summary>
        [JsonPropertyName("lat")]
        public string Latitude { get; set; } = default!;

        /// <summary>
        /// Gets or Sets Display_name.
        /// </summary>
        [JsonPropertyName("display_name")]
        public string Display_name { get; set; } = default!;

        /// <summary>
        /// Gets or Sets Address.
        /// </summary>
        [JsonPropertyName("address")]
        public LocationIQGeoCodingAddress Address { get; set; } = default!;


    }


    public class LocationIQGeoCodingAddress
    {
        /// <summary>
        /// Gets or Sets Cafe.
        /// </summary>
        [JsonPropertyName("cafe")]
        public string Cafe { get; set; } = default!;

        /// <summary>
        /// Gets or Sets Road.
        /// </summary>
        [JsonPropertyName("road")]
        public string Road { get; set; } = default!;

        /// <summary>
        /// Gets or Sets Suburb.
        /// </summary>
        [JsonPropertyName("suburb")]
        public string Suburb { get; set; } = default!;

        /// <summary>
        /// Gets or Sets County.
        /// </summary>
        [JsonPropertyName("county")]
        public string County { get; set; } = default!;

        /// <summary>
        /// Gets or Sets Region.
        /// </summary>
        [JsonPropertyName("region")]
        public string Region { get; set; } = default!;

        /// <summary>
        /// Gets or Sets State.
        /// </summary>
        [JsonPropertyName("state")]
        public string State { get; set; } = default!;

        /// <summary>
        /// Gets or Sets Postal_code.
        /// </summary>
        [JsonPropertyName("postcode")]
        public string Postal_code { get; set; } = default!;

        /// <summary>
        /// Gets or Sets Country.
        /// </summary>
        [JsonPropertyName("country")]
        public string Country { get; set; } = default!;

        /// <summary>
        /// Gets or Sets Country_code.
        /// </summary>
        [JsonPropertyName("country_code")]
        public string Country_code { get; set; } = default!;
    }
}
