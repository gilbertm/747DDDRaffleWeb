using System.Security.Claims;
using EHULOG.BlazorWebAssembly.Client.Components.Common;
using EHULOG.BlazorWebAssembly.Client.Components.Dialogs;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Auth;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Common;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Identity.Account;

public partial class Account
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthenticationService AuthService { get; set; } = default!;
    [Inject]
    protected IAppUsersClient AppUsersClient { get; set; } = default!;

    private bool SecurityTabHidden { get; set; } = false;

    protected AppUserDto AppUserDto = new();

    private string? _userId;

    private CustomValidation? _customValidation;

    protected override async Task OnInitializedAsync()
    {
        if (AuthService.ProviderType == AuthProvider.AzureAd)
        {
            SecurityTabHidden = true;
        }

        /* CreateAppUserRequest _appUserCreateModel = new();
        UpdateAppUserRequest _appUserUpdateModel = new();; */

        if ((await AuthState).User is { } user)
        {
            _userId = user.GetUserId();

            if (_userId is not null)
            {

                if (await ApiHelper.ExecuteCallGuardedAsync(
                    () => AppUsersClient.GetAsync(_userId), Snackbar) is AppUserDto appUserDto)
                {
                    if (string.IsNullOrEmpty(appUserDto.ApplicationUserId))
                    {

                        AppUserDto = appUserDto;

                        AppUserDto.ApplicationUserId = _userId;

                        Snackbar.Add(L["User data not found. Propagating..."], Severity.Success);
                    }
                    else
                    {
                        AppUserDto = appUserDto;

                        Snackbar.Add(L["User data found. Propagating..."], Severity.Success);
                    }
                }

            }

        }
    }
}