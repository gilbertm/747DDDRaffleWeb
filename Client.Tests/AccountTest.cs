using Xunit;
using Bunit;
using Bunit.TestDoubles;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Auth;
using EHULOG.BlazorWebAssembly.Client.Pages.Identity.Account;
using Microsoft.Extensions.DependencyInjection;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Preferences;
using Blazored.LocalStorage;
using MudBlazor;
using MudBlazor.Services;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Auth.AzureAd;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using Microsoft.JSInterop;
using Moq;
using EHULOG.BlazorWebAssembly.Client.Components.Common;

namespace EHULOG.BlazorWebAssembly.Client.Tests;

public class AccountTest
{
    [Fact]
    public void HelloWorld_Renders_Correctly()
    {

        // Arrange
        IConfiguration config = new ConfigurationBuilder()
            .Build();

        using var ctx = new TestContext();
        ctx.Services.AddBlazoredLocalStorage();
        ctx.Services.AddMudServices();
        ctx.Services.AddMudBlazorDialog();
        ctx.Services.AddSingleton<IConfiguration>(config);
        ctx.Services.AddScoped<IClientPreferenceManager, ClientPreferenceManager>();

        var authContext = ctx.AddTestAuthorization();
        authContext.SetAuthorized("Admin");
        authContext.SetRoles("Admin");

        // Act
        var cut = ctx.RenderComponent<Account>();

        // Assert
        cut.MarkupMatches("<h1>Hello world from Blazor</h1>");
    }

    [Fact]
    public void Address_Renders_Properly()
    {

        // Arrange
        IConfiguration config = new ConfigurationBuilder()
            .Build();

        using var ctx = new TestContext();
        ctx.Services.AddBlazoredLocalStorage();
        ctx.Services.AddLocalization();
        ctx.Services.AddMudServices();
        ctx.Services.AddMudBlazorDialog();
        ctx.Services.AddSingleton<IConfiguration>(config);
        ctx.Services.AddScoped<IClientPreferenceManager, ClientPreferenceManager>();

        var authContext = ctx.AddTestAuthorization();
        authContext.SetAuthorized("Admin");
        authContext.SetRoles("Admin");

        // Act
        IRenderedComponent<DynamicMapLoad>? cut = ctx.RenderComponent<DynamicMapLoad>();

        // Assert
        string? p = cut.Find("p").TextContent;

        p.MarkupMatches("Your address and maplocation. You will get ehulog resouces from this");
    }

    [Fact]
    public void Account_Access_Component_Test()
    {
        /* https://bunit.dev/docs/test-doubles/faking-auth.html */

        // Arrange
        using var ctx = new TestContext();
        ctx.Services.AddBlazoredLocalStorage();
        ctx.Services.AddMudServices();
        ctx.Services.AddLocalization();
        ctx.Services.AddMudBlazorDialog();
        ctx.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
        ctx.Services.AddSingleton<IAppUsersClient, AppUsersClient>();
        ctx.Services.AddSingleton<IPersonalClient, PersonalClient>();

        ctx.Services.AddScoped<IClientPreferenceManager, ClientPreferenceManager>();
        ctx.Services.AddSingleton<IConfiguration>(Configuration);

        var authContext = ctx.AddTestAuthorization();
        // authContext.SetAuthorized("Admin");
        // authContext.SetRoles("Admin");

        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        // Act
        var cut = ctx.RenderComponent<Account>();

        // Assert
        /* cut.MarkupMatches(@"<h1>Hi TEST USER, you have these claims and rights:</h1>
                    <ul>
                      <li>You have the role SUPER USER</li>
                    </ul>"); */

        /* var parElement = component.Find("p");

   component.Find("button").Click();

   var elementResult = parElement.TextContent;

   parElement.MarkupMatches("<p>Current count: 1</p>"); */
    }

    private IConfiguration _config;

    public IConfiguration Configuration
    {
        get
        {
            var myConfiguration = new Dictionary<string, string>
            {
                {"Key1", "Value1"},
                {"Nested:Key1", "NestedValue1"},
                {"Nested:Key2", "NestedValue2"}
            };

            if (_config == null)
            {
                var builder = new ConfigurationBuilder();
                // .AddJsonFile($"testsettings.json", optional: false);

                builder.AddInMemoryCollection(myConfiguration);

                _config = builder.Build();
            }

            return _config;
        }
    }
}

public class AuthenticationService : IAuthenticationService
{
    public AuthProvider ProviderType => throw new NotImplementedException();

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

