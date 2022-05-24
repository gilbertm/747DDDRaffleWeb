using System.Security.Claims;
using EHULOG.BlazorWebAssembly.Client.Components.Common;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Auth;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Identity.Account;

public partial class AccountAddress
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthenticationService AuthService { get; set; } = default!;
    [Inject]
    protected IAppUsersClient AppUsersClient { get; set; } = default!;
    
    protected AppUserDto appUserDto = new();

    protected Guid appUserId { get; set; }

    private string? _userId;


    private CustomValidation? _customValidation;

    protected override async Task OnInitializedAsync()
    {
        if ((await AuthState).User is { } user)
        {
            _userId = user.GetUserId();

            if (_userId is not null)
            {

                if (await ApiHelper.ExecuteCallGuardedAsync(
                    () => AppUsersClient.GetAsync(_userId), Snackbar, _customValidation) is AppUserDto resultAppUserDto)
                {
                    if (string.IsNullOrEmpty(appUserDto.ApplicationUserId) && string.IsNullOrEmpty(resultAppUserDto.ApplicationUserId))
                    {
                        // update the empty object
                        appUserDto.ApplicationUserId = _userId;

                        var createAppUserRequest = new CreateAppUserRequest
                        {
                            ApplicationUserId = appUserDto.ApplicationUserId
                        };

                        if (await ApiHelper.ExecuteCallGuardedAsync(
                            () => AppUsersClient.CreateAsync(createAppUserRequest), Snackbar, _customValidation) is Guid guid)
                        {
                            appUserId = guid;
                            Snackbar.Add(L["AppUser data initialized. Propagating... {0}", guid], Severity.Success);
                        }

                    }
                    else
                    {
                        // assign the result object with values
                        appUserDto = resultAppUserDto;
                        appUserId = resultAppUserDto.Id;

                        Snackbar.Add(L["User data found. Propagating..."], Severity.Success);

                    }
                }

            }

        }

        StateHasChanged();
    }
}