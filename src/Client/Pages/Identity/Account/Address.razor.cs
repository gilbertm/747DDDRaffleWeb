using System.Security.Claims;
using EHULOG.BlazorWebAssembly.Client.Components.Common;
using EHULOG.BlazorWebAssembly.Client.Components.Dialogs;
using EHULOG.BlazorWebAssembly.Client.Components.EntityTable;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Auth;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Common;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using Microsoft.JSInterop;
using AspNetMonsters.Blazor.Geolocation.Custom;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.Serialization;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Identity.Account;

public partial class Address
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthenticationService AuthService { get; set; } = default!;
    [Inject]
    protected IAppUsersClient AppUsersClient { get; set; } = default!;
    [Inject]
    protected IPackagesClient PackagesClient { get; set; } = default!;
    [Inject]
    private IRolesClient RolesClient { get; set; } = default!;
    [Inject]
    private IUsersClient UsersClient { get; set; } = default!;
    [Inject]
    private LocationService LocationService { get; set; } = default!;
    [Inject]
    private IHttpClientFactory ClientFactory { get; set; } = default!;

    [Parameter]
    public AppUserDto AppUserDto { get; set; } = default!;

    [Parameter]
    public Guid AppUserId { get; set; } = default!;

    public Location Location { get; set; } = default!;

    private CustomValidation? _customValidation;

    private bool _shouldRender;

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

    protected override async Task OnInitializedAsync()
    {
        if (Location is null)
        {
            Location = await LocationService.GetLocationAsync();
        }

        var obj = new
        {
            Key = "pk.bf547d628289a729866c964e450f6beb",
            MapContainer = "InitialRegisterMap",
            Zoom = 12,
            Longitude = Location.Longitude.ToString(),
            Latitude = Location.Latitude.ToString(),
        };

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://us1.locationiq.com/v1/reverse.php?key={obj.Key}&lat={obj.Latitude}&lon={obj.Longitude}&format=json");

        var client = ClientFactory.CreateClient();

        var response = await client.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            using var responseStream = await response.Content.ReadAsStreamAsync();
            var locations = await JsonSerializer.DeserializeAsync<LocationIQGeoCoding>(responseStream);

            if (locations is not null)
            {
                if (!string.IsNullOrEmpty(locations.Address.Country))
                {
                    AppUserDto.HomeCountry = locations.Address.Country;
                }

                if (!string.IsNullOrEmpty(locations.Address.County) || !string.IsNullOrEmpty(locations.Address.State))
                {
                    AppUserDto.HomeCity = locations.Address.County ?? locations.Address.State;
                }

                if (!string.IsNullOrEmpty(locations.Address.Region))
                {
                    AppUserDto.HomeRegion = locations.Address.Region;
                }

                if (!string.IsNullOrEmpty(locations.Address.Road))
                {
                    AppUserDto.HomeAddress = string.Concat(locations.Address.Suburb, " ", locations.Address.Road);
                }
            }
        }

        _shouldRender = true;
    }

    protected override bool ShouldRender() => _shouldRender;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {

        if (Location is null)
        {
            Location = await LocationService.GetLocationAsync();
        }

        var obj = new
        {
            Key = "pk.bf547d628289a729866c964e450f6beb",
            MapContainer = "InitialRegisterMap",
            Zoom = 12,
            Longitude = Location.Longitude.ToString(),
            Latitude = Location.Latitude.ToString(),
        };

        await JS.InvokeVoidAsync("dotNetToJsMapInitialize.displayInitMap", obj);
    }
}
