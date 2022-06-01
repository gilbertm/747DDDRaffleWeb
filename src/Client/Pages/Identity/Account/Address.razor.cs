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
    [Inject]
    public AppDataService AppDataService { get; set; } = default!;
    [Inject]
    protected IAppUsersClient AppUsersClient { get; set; } = default!;

    private AppUserDto _appUserDto;

    private CustomValidation? _customValidation;

    private DotNetObjectReference<Address>? _objRef;

    protected override async Task OnInitializedAsync()
    {
        _appUserDto = await AppDataService.Start();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // data is not loading
        // rerun service
        if (string.IsNullOrEmpty(_appUserDto.Latitude) && string.IsNullOrEmpty(_appUserDto.Longitude))
        {
            _appUserDto = await AppDataService.Start();
        }

        var obj = new
        {
            Key = "pk.bf547d628289a729866c964e450f6beb",
            MapContainer = "InitialRegisterMap",
            Zoom = 13,
            Longitude = _appUserDto.Longitude,
            Latitude = _appUserDto.Latitude,
        };

        await JS.InvokeVoidAsync("dotNetToJsMapInitialize.displayInitMap", obj);
    }

    private async Task UpdateAppUserAddress()
    {
        _objRef = DotNetObjectReference.Create(this);

        await JS.InvokeVoidAsync("dotNetToJsMapInitialize.updateAddressDtoFromJS", _objRef);

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

    [JSInvokable]
    public void ChangeAddressFromJS(string homeAddress, string homeCity, string homeCountry, string homeRegion, string latitude, string longitude)
    {
        _appUserDto.HomeAddress = homeAddress;
        _appUserDto.HomeCity = homeCity;
        _appUserDto.HomeCountry = homeCountry;
        _appUserDto.HomeRegion = homeRegion;
        _appUserDto.Latitude = latitude;
        _appUserDto.Longitude = longitude;
    }

    public void Dispose()
    {
        _objRef?.Dispose();
    }

}
