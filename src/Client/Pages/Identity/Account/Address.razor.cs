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

    public AppUserDto AppUserDto = new();

    private CustomValidation? _customValidation;

    private DotNetObjectReference<Address>? _objRef;

    protected override async Task OnInitializedAsync()
    {
        await AppDataService.Start();

        AppUserDto = AppDataService.AppUserDto;

    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // data is not loading
        // rerun service
        if (string.IsNullOrEmpty(AppDataService.AppUserDto.Latitude) && string.IsNullOrEmpty(AppDataService.AppUserDto.Longitude))
        {
            await AppDataService.Start();
            AppUserDto = AppDataService.AppUserDto;
        }

        var obj = new
        {
            Key = "pk.bf547d628289a729866c964e450f6beb",
            MapContainer = "InitialRegisterMap",
            Zoom = 13,
            Longitude = AppDataService.AppUserDto.Longitude,
            Latitude = AppDataService.AppUserDto.Latitude,
        };

        await JS.InvokeVoidAsync("dotNetToJsMapInitialize.displayInitMap", obj);
    }

    private async Task UpdateAppUserAddress()
    {
        _objRef = DotNetObjectReference.Create(this);

        await JS.InvokeVoidAsync("dotNetToJsMapInitialize.updateAddressDtoFromJS", _objRef);

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

        var guid = await AppUsersClient.UpdateAsync(AppUserDto.Id, updateAppUserRequest);
    }

    [JSInvokable]
    public void ChangeAddressFromJS(string homeAddress, string homeCity, string homeCountry, string homeRegion, string latitude, string longitude)
    {
        AppUserDto.HomeAddress = homeAddress;
        AppUserDto.HomeCity = homeCity;
        AppUserDto.HomeCountry = homeCountry;
        AppUserDto.HomeRegion = homeRegion;
        AppUserDto.Latitude = latitude;
        AppUserDto.Longitude = longitude;
    }

    public void Dispose()
    {
        _objRef?.Dispose();
    }

}
