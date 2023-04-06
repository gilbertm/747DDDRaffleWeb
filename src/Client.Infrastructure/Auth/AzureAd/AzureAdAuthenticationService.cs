using RAFFLE.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace RAFFLE.BlazorWebAssembly.Client.Infrastructure.Auth.AzureAd;

internal class AzureAdAuthenticationService : IAuthenticationService
{
    private readonly NavigationManager _navigation;

    public AzureAdAuthenticationService(NavigationManager navigation) => _navigation = navigation;

    public AuthProvider ProviderType => AuthProvider.AzureAd;

    public void NavigateToExternalLogin(string returnUrl) =>
        _navigation.NavigateTo($"authentication/login?returnUrl={Uri.EscapeDataString(returnUrl)}");

    public Task<bool> LoginAsync(string tenantId, TokenRequest request) =>
        throw new NotImplementedException();

    public Task LogoutAsync()
    {
        _navigation.NavigateToLogout("authentication/logout");
        return Task.CompletedTask;
    }

    public Task ReLoginAsync(string returnUrl)
    {
        NavigateToExternalLogin(returnUrl);
        return Task.CompletedTask;
    }
}