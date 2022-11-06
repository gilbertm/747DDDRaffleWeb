﻿using EHULOG.BlazorWebAssembly.Client.Components.Common;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Auth;
using EHULOG.BlazorWebAssembly.Client.Shared;
using EHULOG.WebApi.Shared.Multitenancy;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using MudBlazor;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Authentication;

public partial class Login
{
    [CascadingParameter]
    public Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    public IAuthenticationService AuthService { get; set; } = default!;

    private CustomValidation? _customValidation;

    public bool BusySubmitting { get; set; }

    private readonly TokenRequest _tokenRequest = new();
    private string TenantId { get; set; } = string.Empty;
    private bool _passwordVisibility;
    private InputType _passwordInput = InputType.Password;
    private string _passwordInputIcon = Icons.Material.Filled.VisibilityOff;

    protected override async Task OnInitializedAsync()
    {
        if (AuthService.ProviderType == AuthProvider.AzureAd)
        {
            AuthService.NavigateToExternalLogin(Navigation.Uri);
            return;
        }

        var authState = await AuthState;
        if (authState.User.Identity?.IsAuthenticated is true)
        {
            Navigation.NavigateTo("/");
        }
    }

    private void TogglePasswordVisibility()
    {
        if (_passwordVisibility)
        {
            _passwordVisibility = false;
            _passwordInputIcon = Icons.Material.Filled.VisibilityOff;
            _passwordInput = InputType.Password;
        }
        else
        {
            _passwordVisibility = true;
            _passwordInputIcon = Icons.Material.Filled.Visibility;
            _passwordInput = InputType.Text;
        }
    }

    private void FillAdministratorCredentials()
    {
        _tokenRequest.Email = MultitenancyConstants.Root.EmailAddress;
        _tokenRequest.Password = MultitenancyConstants.DefaultPassword;

        // TenantId = MultitenancyConstants.Root.Id;
    }

    private async Task SubmitAsync()
    {
        TenantId = MultitenancyConstants.Root.Id;

        BusySubmitting = true;

        if (await ApiHelper.ExecuteCallGuardedAsync(
            () => AuthService.LoginAsync(TenantId, _tokenRequest),
            Snackbar,
            _customValidation))
        {
            Snackbar.Add($"Logged in as {_tokenRequest.Email}", Severity.Info);

            // get our URI
            var uri = Navigation.ToAbsoluteUri(Navigation.Uri);

            bool foundQueryParameter = QueryHelpers.ParseQuery(uri.Query).TryGetValue("redirect_url", out var valueFromQueryString);

            if (foundQueryParameter)
            {
                string redirect_url = valueFromQueryString.FirstOrDefault() ?? string.Empty;

                Navigation.NavigateTo(redirect_url);
            }
        }

        BusySubmitting = false;
    }
}