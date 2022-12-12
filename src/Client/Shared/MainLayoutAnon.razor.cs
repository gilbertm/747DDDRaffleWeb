using Microsoft.AspNetCore.Components;

namespace EHULOG.BlazorWebAssembly.Client.Shared;

public partial class MainLayoutAnon
{
    [Parameter]
    public RenderFragment ChildContent { get; set; } = default!;
    [Parameter]
    public EventCallback OnDarkModeToggle { get; set; }
    [Parameter]
    public EventCallback<bool> OnRightToLeftToggle { get; set; }

    private bool _rightToLeft = default!;
    public async Task ToggleDarkMode()
    {
        await OnDarkModeToggle.InvokeAsync();
    }

    private async Task RightToLeftToggle()
    {
        bool isRtl = true;

        _rightToLeft = isRtl;

        await OnRightToLeftToggle.InvokeAsync(isRtl);
    }

    [Inject]
    protected AppDataService AppDataService { get; set; } = default!;

    private string _currentUrl = default!;
    private Uri _uri = default!;
    private string _cssClasses = default!;

    protected override async Task OnInitializedAsync()
    {
        Console.WriteLine("$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$");
        Console.WriteLine("------------------------------------ Main Layout Anonymous   ------------------------------------");
        Console.WriteLine("$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$");

        await AppDataService.InitializationAsync();
    }

    protected override void OnParametersSet()
    {
        _currentUrl = Navigation.Uri;

        _uri = new Uri(_currentUrl);

        if (_uri is { } && _uri.Segments.Count() > 1)
        {

            _cssClasses = _uri.Segments.ToList().ElementAt(1).ToString().ToLower();

            _cssClasses = _cssClasses.Replace("/", "");

        }
        else
        {
            _cssClasses = "home";
        }
    }

    public void UpdateCity(string city)
    {
        AppDataService.City = city;
        StateHasChanged();
    }

    public void UpdateCountry(string country)
    {
        AppDataService.Country = country;
        StateHasChanged();
    }

    public void UpdateCountryCurrency(string countryCurrency)
    {
        AppDataService.CountryCurrency = countryCurrency;
        StateHasChanged();
    }

    /*protected async Task TestOnAfterRenderAsync(bool firstRender)
        {
        await EatCookies();

        if (!firstRender)
            {
            Country = await AppDataService.GetCountryOnAnonymousStateAsync();

            if (!string.IsNullOrEmpty(Country))
                {
                var CountryCurrency = AppDataService.GetCurrencyAnonymous(Country);
            }


            if (AppDataService.Country == default)
                {
                AppDataService.Country = await AppDataService.GetCountryOnAnonymousStateAsync();

                if (string.IsNullOrEmpty(AppDataService.CountryCurrency))
                    {
                    AppDataService.CountryCurrency = AppDataService.GetCurrencyAnonymous(AppDataService.Country);
                }

                await StoreCookies();


             }
             StateHasChanged();
    }
    }

    public async Task EatCookies()
         {
         check and get cookie for country location
        string? countryLocation = await LocalStorage.GetItemAsStringAsync("CountryLocation");

        if (countryLocation != default)
            {
            AppDataService.Country = countryLocation;
        }

        string? countryLocationCurrency = await LocalStorage.GetItemAsStringAsync("CountryLocationCurrency");

        if (countryLocationCurrency != default)
            {
            AppDataService.CountryCurrency = countryLocationCurrency;
    }
    }

    public async Task StoreCookies()
        {
        if (!string.IsNullOrEmpty(AppDataService.Country))
            {
            await LocalStorage.SetItemAsStringAsync("CountryLocation", AppDataService.Country);
        }

        if (!string.IsNullOrEmpty(AppDataService.CountryCurrency))
            {
            await LocalStorage.SetItemAsStringAsync("CountryLocationCurrency", AppDataService.Country);
    }
    }
    */

}