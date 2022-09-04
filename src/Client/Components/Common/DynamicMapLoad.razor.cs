using AspNetMonsters.Blazor.Geolocation.Custom;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Pages.Identity.Account;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Text.Json;

namespace EHULOG.BlazorWebAssembly.Client.Components.Common;

public partial class DynamicMapLoad
{
    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;
    [Inject]
    public AppDataService AppDataService { get; set; } = default!;
    [Inject]
    protected IAppUsersClient AppUsersClient { get; set; } = default!;
    [Inject]
    protected IJSRuntime JSRuntime { get; set; } = default!;
    [Inject]
    private LocationService LocationService { get; set; } = default!;

    private AppUserDto? _appUserDto { get; set; } = default!;

    private CustomValidation? _customValidation;

    private DotNetObjectReference<DynamicMapLoad>? _objRef;

    private List<string> _jsonLoadedScripts { get; set; } = new();

    [Parameter]
    public bool IsMapOnly { get; set; }

    protected override async Task OnInitializedAsync()
    {
        _objRef = DotNetObjectReference.Create(this);

        _appUserDto = await AppDataService.Start();

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
        if (_appUserDto is not null)
        {
            if (!string.IsNullOrEmpty(_appUserDto.Latitude) && !string.IsNullOrEmpty(_appUserDto.Longitude))
            {

                var obj = new
                {
                    Key = Config["MapBox:Key"],
                    MapContainer = Config["MapBox:MapContainer"],
                    Zoom = Config["MapBox:Zoom"],
                    Style = Config["MapBox:Style"],
                    Longitude = _appUserDto.Longitude,
                    Latitude = _appUserDto.Latitude
                };

                if (_jsonLoadedScripts is { } && _jsonLoadedScripts.Count() > 0)
                {
                    if (_jsonLoadedScripts.Contains("js/mapBox.js"))
                    {
                        await JSRuntime.InvokeVoidAsync("dotNetJSMapBox.initMap", obj);
                    }
                }
            }
        }
        else
        {
            var location = await LocationService.GetLocationAsync();

            var obj = new
            {
                Key = Config["MapBox:Key"],
                MapContainer = Config["MapBox:MapContainer"],
                Zoom = Config["MapBox:Zoom"],
                Style = Config["MapBox:Style"],
                Longitude = location is { } && !string.IsNullOrEmpty(location.Longitude.ToString()) ? location.Longitude.ToString() : "0",
                Latitude = location is { } && !string.IsNullOrEmpty(location.Latitude.ToString()) ? location.Latitude.ToString() : "0"
            };

            if (_jsonLoadedScripts is { } && _jsonLoadedScripts.Count() > 0)
            {
                if (_jsonLoadedScripts.Contains("js/mapBox.js"))
                {
                    await JSRuntime.InvokeVoidAsync("dotNetJSMapBox.initMap", obj);
                }
            }

        }
    }

    private async Task UpdateAppUserAddress()
    {
        _objRef = DotNetObjectReference.Create(this);

        await JSRuntime.InvokeVoidAsync("dotNetJSMapBox.updateAddressDtoFromJS", _objRef);

        if (_appUserDto is not null)
        {
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

            var guid = await AppUsersClient.UpdateAsync(_appUserDto.Id, updateAppUserRequest);
        }

        NavigationManager.NavigateTo("/account/rolepackage");
        return;
    }

    [JSInvokable]
    public void GetLoadedScriptsFromJS(string loadedScripts)
    {
        if (!string.IsNullOrEmpty(loadedScripts))
        {
            _jsonLoadedScripts = JsonSerializer.Deserialize<List<string>>(loadedScripts) ?? new();
        }
    }

    [JSInvokable]
    public void ChangeAddressFromJS(string homeAddress, string homeCity, string homeCountry, string homeRegion, string latitude, string longitude)
    {
        if (_appUserDto is not null)
        {
            _appUserDto.HomeAddress = homeAddress;
            _appUserDto.HomeCity = homeCity;
            _appUserDto.HomeCountry = homeCountry;
            _appUserDto.HomeRegion = homeRegion;
            _appUserDto.Latitude = latitude;
            _appUserDto.Longitude = longitude;
        }
    }

    public void Dispose()
    {
        _objRef?.Dispose();
    }
}
