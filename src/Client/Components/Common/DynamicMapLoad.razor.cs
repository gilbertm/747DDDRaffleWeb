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
    [Parameter]
    public bool IsMapOnly { get; set; }

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;
    [Inject]
    public AppDataService AppDataService { get; set; } = default!;
    [Inject]
    protected IAppUsersClient AppUsersClient { get; set; } = default!;
    [Inject]
    protected IJSRuntime JSRuntime { get; set; } = default!;

    private AppUserDto? AppUserDto { get; set; }

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
        if (AppUserDto is not null)
        {
            AppUserDto.HomeAddress = homeAddress;
            AppUserDto.HomeCity = homeCity;
            AppUserDto.HomeCountry = homeCountry;
            AppUserDto.HomeRegion = homeRegion;
            AppUserDto.Latitude = latitude;
            AppUserDto.Longitude = longitude;
        }
    }

    public void Dispose()
    {
        _objRef?.Dispose();
    }
    protected override async Task OnInitializedAsync()
    {
        _objRef = DotNetObjectReference.Create(this);

        AppUserDto = AppDataService.GetAppUserDataTransferObject();

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
        if (AppUserDto is not null)
        {
            if (!string.IsNullOrEmpty(AppUserDto.Latitude) && !string.IsNullOrEmpty(AppUserDto.Longitude))
            {
                var obj = new
                {
                    Key = Config["MapBox:Key"],
                    MapContainer = Config["MapBox:MapContainer"],
                    Zoom = Config["MapBox:Zoom"],
                    Style = Config["MapBox:Style"],
                    AppUserDto.Longitude,
                    AppUserDto.Latitude
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
        else
        {
            var position = AppDataService.GetGeolocationPosition();

            var obj = new
            {
                Key = Config["MapBox:Key"],
                MapContainer = Config["MapBox:MapContainer"],
                Zoom = Config["MapBox:Zoom"],
                Style = Config["MapBox:Style"],
                Longitude = position is { } && !string.IsNullOrEmpty(position.Coords.Longitude.ToString()) ? position.Coords.Longitude.ToString() : "0",
                Latitude = position is { } && !string.IsNullOrEmpty(position.Coords.ToString()) ? position.Coords.Latitude.ToString() : "0"
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

    private async Task UpdateAppUserAddress()
    {
        _objRef = DotNetObjectReference.Create(this);

        await JSRuntime.InvokeVoidAsync("dotNetJSMapBox.updateAddressDtoFromJS", _objRef);

        if (AppUserDto is not null)
        {
            var updateAppUserRequest = new UpdateAppUserRequest
            {
                ApplicationUserId = AppUserDto.ApplicationUserId,
                HomeAddress = AppUserDto.HomeAddress,
                HomeCity = AppUserDto.HomeCity,
                HomeCountry = AppUserDto.HomeCountry,
                HomeRegion = AppUserDto.HomeRegion,
                Id = AppUserDto.Id,
                IsVerified = AppUserDto.IsVerified,
                Latitude = AppUserDto.Latitude,
                Longitude = AppUserDto.Longitude,
                PackageId = AppUserDto.PackageId
            };

            _ = await AppUsersClient.UpdateAsync(AppUserDto.Id, updateAppUserRequest);
        }

        NavigationManager.NavigateTo("/account/rolepackage");
        return;
    }
}
