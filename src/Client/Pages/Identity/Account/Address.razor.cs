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

    public AppUserDto AppUserDto = new();

    private CustomValidation? _customValidation;

    protected override async Task OnInitializedAsync()
    {
        await AppDataService.Start();

        AppUserDto = AppDataService.AppUserDto;

    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {

        var obj = new
        {
            Key = "pk.bf547d628289a729866c964e450f6beb",
            MapContainer = "InitialRegisterMap",
            Zoom = 12,
            Longitude = AppUserDto.Longitude,
            Latitude = AppUserDto.Latitude,
        };

        await JS.InvokeVoidAsync("dotNetToJsMapInitialize.displayInitMap", obj);
    }
}
