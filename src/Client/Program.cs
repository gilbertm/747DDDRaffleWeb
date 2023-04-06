using System.Globalization;
using RAFFLE.BlazorWebAssembly.Client;
using RAFFLE.BlazorWebAssembly.Client.Infrastructure;
using RAFFLE.BlazorWebAssembly.Client.Infrastructure.Common;
using RAFFLE.BlazorWebAssembly.Client.Infrastructure.Preferences;
using RAFFLE.BlazorWebAssembly.Client.Shared;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddClientServices(builder.Configuration);
builder.Services.AddGeolocationServices();
builder.Services.AddScoped<IAppDataService, AppDataService>();
builder.Services.AddScoped<AppDataService>();

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