using System.Security.Claims;
using RAFFLE.BlazorWebAssembly.Client.Components.Common;
using RAFFLE.BlazorWebAssembly.Client.Components.Dialogs;
using RAFFLE.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using RAFFLE.BlazorWebAssembly.Client.Infrastructure.Auth;
using RAFFLE.BlazorWebAssembly.Client.Infrastructure.Common;
using RAFFLE.BlazorWebAssembly.Client.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace RAFFLE.BlazorWebAssembly.Client.Pages.Identity.Account;

public partial class Account
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthenticationService AuthService { get; set; } = default!;

    private bool SecurityTabHidden { get; set; } = false;

    protected override void OnInitialized()
    {
        if (AuthService.ProviderType == AuthProvider.AzureAd)
        {
            SecurityTabHidden = true;
        }
    }
}