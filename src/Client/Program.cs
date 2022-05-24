using System.Globalization;
using AspNetMonsters.Blazor.Geolocation.Custom;
using EHULOG.BlazorWebAssembly.Client;
using EHULOG.BlazorWebAssembly.Client.Infrastructure;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Common;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Preferences;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddClientServices(builder.Configuration);
builder.Services.AddSingleton<LocationService>();

var host = builder.Build();

var storageService = host.Services.GetRequiredService<IClientPreferenceManager>();
if (storageService != null)
{
    CultureInfo culture;
    if (await storageService.GetPreference() is ClientPreference preference)
        culture = new CultureInfo(preference.LanguageCode);
    else
        culture = new CultureInfo(LocalizationConstants.SupportedLanguages.FirstOrDefault()?.Code ?? "en-US");
    CultureInfo.DefaultThreadCurrentCulture = culture;
    CultureInfo.DefaultThreadCurrentUICulture = culture;
}

await host.RunAsync();