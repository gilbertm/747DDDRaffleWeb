using RAFFLE.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using RAFFLE.BlazorWebAssembly.Client.Pages.Identity.Account;
using RAFFLE.BlazorWebAssembly.Client.Shared;
using Mapster;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System;
using System.Reflection;
using System.Text.Json;

namespace RAFFLE.BlazorWebAssembly.Client.Components.Common;

public partial class DynamicMapLoad
{
    [CascadingParameter(Name = "AppDataService")]
    public AppDataService AppDataService { get; set; } = default!;

    [Parameter]
    public bool IsMapOnly { get; set; }

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    protected IAppUsersClient AppUsersClient { get; set; } = default!;

    [Inject]
    protected IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    protected ILoggerFactory LoggerFactory { get; set; } = default!;

    private DotNetObjectReference<DynamicMapLoad>? _objRef;

    private List<string> JsonLoadedScripts { get; set; } = new();

    [JSInvokable]
    public void GetLoadedScriptsFromJS(string loadedScripts)
    {
        if (!string.IsNullOrEmpty(loadedScripts))
        {
            JsonLoadedScripts = JsonSerializer.Deserialize<List<string>>(loadedScripts) ?? new();
        }
    }

    [JSInvokable]
    public void ChangeAddressFromJS(string homeAddress, string homeCity, string homeCountry, string homeRegion, string latitude, string longitude)
    {
        if (AppDataService != default)
        {
            if (AppDataService.AppUser != default)
            {
                AppDataService.AppUser.HomeAddress = homeAddress;
                AppDataService.AppUser.HomeCity = homeCity;
                AppDataService.AppUser.HomeCountry = homeCountry;
                AppDataService.AppUser.HomeRegion = homeRegion;
                AppDataService.AppUser.Latitude = latitude;
                AppDataService.AppUser.Longitude = longitude;

            }

            AppDataService.City = homeCity;
            AppDataService.Country = homeCountry;
        }
    }

    public void Dispose()
    {
        _objRef?.Dispose();
    }

    protected override async Task OnInitializedAsync()
    {
        LoggerFactory.CreateLogger(Assembly.GetExecutingAssembly().Location).LogInformation("This is something");

        _objRef = DotNetObjectReference.Create(this);

        await JSRuntime.InvokeVoidAsync("loadScript", "https://api.mapbox.com/mapbox-gl-js/v2.8.2/mapbox-gl.js", "head");

        await JSRuntime.InvokeVoidAsync("getLoadedScript", _objRef);

        await JSRuntime.InvokeVoidAsync("loadScript", "https://api.mapbox.com/mapbox-gl-js/plugins/mapbox-gl-geocoder/v5.0.0/mapbox-gl-geocoder.min.js", "head");

        await JSRuntime.InvokeVoidAsync("getLoadedScript", _objRef);

        await JSRuntime.InvokeVoidAsync("loadScript", "js/mapBox.js");

        await JSRuntime.InvokeVoidAsync("getLoadedScript", _objRef);

        await JSRuntime.InvokeVoidAsync("loadCSS", "https://api.mapbox.com/mapbox-gl-js/v2.8.2/mapbox-gl.css");

        await JSRuntime.InvokeVoidAsync("loadCSS", "https://api.mapbox.com/mapbox-gl-js/plugins/mapbox-gl-geocoder/v5.0.0/mapbox-gl-geocoder.css");
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (AppDataService != default)
        {
            if (AppDataService.AppUser != default)
            {
                if (!string.IsNullOrEmpty(AppDataService.AppUser.Latitude) && !string.IsNullOrEmpty(AppDataService.AppUser.Longitude))
                {
                    var obj = new
                    {
                        Key = Config["MapBox:Key"],
                        MapContainer = Config["MapBox:MapContainer"],
                        Zoom = Config["MapBox:Zoom"],
                        Style = Config["MapBox:Style"],
                        AppDataService.AppUser.Longitude,
                        AppDataService.AppUser.Latitude
                    };

                    if (JsonLoadedScripts is { } && JsonLoadedScripts.Count > 0)
                    {
                        if (JsonLoadedScripts.Contains("js/mapBox.js"))
                        {
                            await JSRuntime.InvokeVoidAsync("dotNetJSMapBox.initMap", obj);
                        }
                    }
                }

                // else
                // {
                //    // await AppDataService.UpdateLocationAsync();
                // }
            }
            else
            {
                var position = AppDataService.GetGeolocationPosition();

                if (position != default)
                {
                    if (position.Coords != default)
                    {
                        var obj = new
                        {
                            Key = Config["MapBox:Key"],
                            MapContainer = Config["MapBox:MapContainer"],
                            Zoom = Config["MapBox:Zoom"],
                            Style = Config["MapBox:Style"],
                            Longitude = position.Coords.Longitude.ToString(),
                            Latitude = position.Coords.Latitude.ToString()
                        };

                        if (JsonLoadedScripts is { } && JsonLoadedScripts.Count > 0)
                        {
                            if (JsonLoadedScripts.Contains("js/mapBox.js"))
                            {
                                await JSRuntime.InvokeVoidAsync("dotNetJSMapBox.initMap", obj);
                            }
                        }
                    }
                }

                await AppDataService.UpdateLocationAsync();

            }
        }

    }

    private async Task UpdateAppUserAddress()
    {
        if (_objRef == default)
            _objRef = DotNetObjectReference.Create(this);

        await JSRuntime.InvokeVoidAsync("dotNetJSMapBox.updateAddressDtoFromJS", _objRef);

        if (AppDataService != default)
        {
            if (AppDataService.AppUser != default)
            {
                var updateAppUserRequest = new UpdateAppUserRequest
                {
                    ApplicationUserId = AppDataService.AppUser.ApplicationUserId,
                    HomeAddress = AppDataService.AppUser.HomeAddress,
                    HomeCity = AppDataService.AppUser.HomeCity,
                    HomeCountry = AppDataService.AppUser.HomeCountry,
                    HomeRegion = AppDataService.AppUser.HomeRegion,
                    Id = AppDataService.AppUser.Id,
                    IsVerified = AppDataService.AppUser.IsVerified,
                    Latitude = AppDataService.AppUser.Latitude,
                    Longitude = AppDataService.AppUser.Longitude,

                    // significant statuses
                    AddressStatus = VerificationStatus.Verified,

                    DocumentsStatus = AppDataService.AppUser.DocumentsStatus,
                    RolePackageStatus = AppDataService.AppUser.RolePackageStatus
                };

                await AppUsersClient.UpdateAsync(AppDataService.AppUser.Id, updateAppUserRequest);

                await AppDataService.RevalidateVerification();
            }
        }

        NavigationManager.NavigateTo("/account/role-subscription", true);
    }
}
