using EHULOG.BlazorWebAssembly.Client.Pages.Catalog.Anons;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Common;
using EHULOG.BlazorWebAssembly.Client.Pages.Multitenancy;
using EHULOG.BlazorWebAssembly.Client.Shared;
using EHULOG.WebApi.Shared.Multitenancy;
using Mapster;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using System.Runtime.CompilerServices;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog.Anons;

public partial class FrontAnonymousLoans
{
    [CascadingParameter(Name = "AppDataService")]
    public AppDataService AppDataService { get; set; } = default!;

    [Inject]
    private HttpClient HttpClient { get; set; } = default!;

    [Inject]
    private ILoansClient LoansClient { get; set; } = default!;

    public bool Loading { get; set; }

    public bool IsLoaded { get; set; } = false;

    private IEnumerable<LoanDto>? _entityList;
    private int _totalItems;
    private int _pageSize;
    private int _currentPage;
    private IDictionary<Guid, bool> _dictOverlayVisibility = new Dictionary<Guid, bool>();

    private float _borrowing = 10000f;
    private int _borrowDuration = 6;
    private int _loaning = 500;
    private int _loaningDuration = 1;

    protected override async Task OnInitializedAsync()
    {
        // check and get cookie for country location
        /* string? countryLocationStorage = LocalStorage.GetItem<string>("CountryLocation");

        if (countryLocationStorage != default)
        {
            Console.WriteLine(countryLocationStorage);

            LocalStorage.SetItem("CountryLocation", $"This is a sample data {DateTime.Now}");
        } */

        if ((await AppDataService.IsAuthenticated()) != default)
        {
            // navigate to home (/)
            // the front component will do the routing checks
            //
            // if logged in, the role will be used to navigate
            // to the user's account type listing
            Navigation.NavigateTo("/");

            return;
        }

        await LoadDataAsync();
    }

    private async Task LoadDataAsync(float? amount = default, int? month = default)
    {

        var filter = GetPaginationFilter();

        var loanFilter = filter.Adapt<SearchLoansAnonRequest>();

        _dictOverlayVisibility.Clear();


        if (AppDataService != default)
        {
            if (AppDataService.Country != default)
                loanFilter.HomeCountry = AppDataService.Country;

            if (AppDataService.City != default)
                loanFilter.HomeCity = AppDataService.City;

            if (amount.HasValue)
            {
                loanFilter.Amount = amount.Value;
                _borrowing = amount.Value;
            } else
            {
                loanFilter.Amount = _borrowing;
            }

            if (month.HasValue)
            {
                loanFilter.Month = month.Value;
                _borrowDuration = month.Value;
            } else
            {
                loanFilter.Month = _borrowDuration;
            }
        }

        if (await ApiHelper.ExecuteCallGuardedAsync(
               async () => await GetAnonLoans(loanFilter), Snackbar) is { } result)
        {
            
            if (result.Data is { } && result.Data.Count > 0)
            {
                foreach (var item in result.Data)
                {
                    if (item.LoanLenders != default)
                    {
                        _dictOverlayVisibility.Add(item.Id, false);

                        var loanLender = item.LoanLenders.Where(l => l.LoanId.Equals(item.Id)).FirstOrDefault();

                        if (loanLender is { })
                        {
                            var image = await GetImage(loanLender.ProductId);

                            if (loanLender.Product is { } && image is { })
                            {
                                loanLender.Product.Image = image;
                            }
                        }
                    }
                }
            }

            _totalItems = result.TotalCount;
            _entityList = result.Data;
            _pageSize = result.PageSize;
        }
    }

    private async Task OnValueChangedUpdateAnonymousListingAmount(float amount)
    {
        await LoadDataAsync(amount, null);

        StateHasChanged();
    }

    private async Task OnValueChangedUpdateAnonymousListingMonth(int month)
    {
        await LoadDataAsync(null, month);

        StateHasChanged();
    }

    private PaginationFilter GetPaginationFilter()
    {
        StringValues pageCount;

        var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
        if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("page", out pageCount))
        {
            _currentPage = Convert.ToInt32(pageCount);
        }

        _currentPage = (_currentPage <= 1) ? 1 : _currentPage;

        var filter = new PaginationFilter
        {
            PageNumber = _currentPage,
            PageSize = 5
        };

        return filter;
    }

    private async Task<InputOutputResourceDto> GetImage(Guid productId)
    {
        HttpRequestMessage request;
        HttpResponseMessage response;

        request = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{Config[ConfigNames.ApiBaseUrl]}api/tokens/"),
            Content = new StringContent(JsonSerializer.Serialize(new TokenRequest()
            {
                Email = MultitenancyConstants.ServiceLessee.EmailAddress,
                Password = MultitenancyConstants.DefaultPasswordServiceLessee
            })),
        };

        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Content.Headers.Add("tenant", "ehulog");

        response = await HttpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (tokenResponse is not null)
            {
                using var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri($"{Config[ConfigNames.ApiBaseUrl]}api/v1/inputoutputresource/anon/{productId}"));
                requestMessage.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", tokenResponse.Token);

                response = await HttpClient.SendAsync(requestMessage);

                if (response.IsSuccessStatusCode)
                {
                    var images = JsonSerializer.Deserialize<List<InputOutputResourceDto>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (images is not null)
                    {
                        if (images.Count > 0)
                        {
                            return images[0];
                        }
                    }
                }
            }
        }

        return default!;
    }

    private async Task<GridContainerPaginationResponse<LoanDto>> GetAnonLoans(SearchLoansAnonRequest search)
    {
        HttpRequestMessage request;
        HttpResponseMessage response;

        request = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{Config[ConfigNames.ApiBaseUrl]}api/tokens/"),
            Content = new StringContent(JsonSerializer.Serialize(new TokenRequest()
            {
                Email = MultitenancyConstants.ServiceLessee.EmailAddress,
                Password = MultitenancyConstants.DefaultPasswordServiceLessee
            })),
        };

        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Content.Headers.Add("tenant", "ehulog");

        response = await HttpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (tokenResponse is not null)
            {
                request = new HttpRequestMessage()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri($"{Config[ConfigNames.ApiBaseUrl]}api/v1/loans/search/anon"),
                    Content = new StringContent(JsonSerializer.Serialize(search)),
                };

                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.Token);

                request.Content.Headers.Add("tenant", "ehulog");

                response = await HttpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var dataLoans = JsonSerializer.Deserialize<GridContainerPaginationResponse<LoanDto>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (dataLoans is not null)
                        return dataLoans;
                }
            }
        }

        return default!;
    }

}