using Xunit;
using Bunit;
using Bunit.TestDoubles;
using RAFFLE.BlazorWebAssembly.Client.Infrastructure.Auth;
using RAFFLE.BlazorWebAssembly.Client.Pages.Identity.Account;
using Microsoft.Extensions.DependencyInjection;
using RAFFLE.BlazorWebAssembly.Client.Infrastructure.Preferences;
using Blazored.LocalStorage;
using MudBlazor;
using MudBlazor.Services;
using RAFFLE.BlazorWebAssembly.Client.Infrastructure.Auth.AzureAd;
using RAFFLE.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using Microsoft.JSInterop;
using RAFFLE.BlazorWebAssembly.Client.Components.Common;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RAFFLE.BlazorWebAssembly.Client.Infrastructure;
using System.Collections;
using Microsoft.AspNetCore.Components;
using Xunit.Abstractions;
using Xunit.Sdk;
using Client.Tests;

namespace RAFFLE.BlazorWebAssembly.Client.Tests;

public class AccountSampleBUnitTest
{

    [Fact]
    public void AccountDummyRendersCorrectly()
    {
        // Arrange
        IConfiguration config = new ConfigurationBuilder()
            .Build();

        using var ctx = new TestContext();
        ctx.Services.AddBlazoredLocalStorage();
        ctx.Services.AddSingleton<IConfiguration>(config);
        ctx.Services.AddMudServices();
        ctx.Services.AddLocalization();
        ctx.Services.AddMudBlazorDialog();
        ctx.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
        ctx.Services.AddSingleton<IPersonalClient, PersonalClient>();
        ctx.Services.AddSingleton<IInputOutputResourceClient, InputOutputResourceClient>();
        ctx.Services.AddLocalization();
        ctx.Services.AddScoped<IClientPreferenceManager, ClientPreferenceManager>();

        var authContext = ctx.AddTestAuthorization();
        authContext.SetAuthorized("Admin");
        authContext.SetRoles("Admin");
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        // Act
        var cut = ctx.RenderComponent<AccountDummy>();

        // Assert
        cut.MarkupMatches("<h3>AccountDummy</h3>");
    }

    private IConfiguration? _config;

    public IConfiguration Configuration
    {
        get
        {
            var myConfiguration = new Dictionary<string, string>
            {
                { "Key1", "Value1" },
                { "Nested:Key1", "NestedValue1" },
                { "Nested:Key2", "NestedValue2" }
            };

            if (_config == null)
            {
                var builder = new ConfigurationBuilder();

                // .AddJsonFile($"testsettings.json", optional: false);

                builder.AddInMemoryCollection(myConfiguration!);

                _config = builder.Build();
            }

            return _config;
        }
    }
}

public class AuthenticationService : IAuthenticationService
{
    public AuthProvider ProviderType => AuthProvider.AzureAd;

    public Task<bool> LoginAsync(string tenantId, TokenRequest request)
    {
        throw new NotImplementedException();
    }

    public Task LogoutAsync()
    {
        throw new NotImplementedException();
    }

    public void NavigateToExternalLogin(string returnUrl)
    {
        throw new NotImplementedException();
    }

    public Task ReLoginAsync(string returnUrl)
    {
        throw new NotImplementedException();
    }
}