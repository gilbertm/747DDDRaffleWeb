using EHULOG.BlazorWebAssembly.Client.Components.Dialogs;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Auth;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Common;
using EHULOG.BlazorWebAssembly.Client.Shared;
using EHULOG.WebApi.Shared.Multitenancy;
using Mapster;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using MudBlazor;
using System.Net.Http.Headers;
using System.Text.Json;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog.Anons;

public partial class FrontAnonymousLoans
{
    [CascadingParameter(Name = "AppDataService")]
    protected AppDataService AppDataService { get; set; } = default!;

    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthenticationService AuthService { get; set; } = default!;

    [Inject]
    private HttpClient HttpClient { get; set; } = default!;

    [Inject]
    private ILoansClient LoansClient { get; set; } = default!;

    [Inject]
    private ILoanLedgersClient LoanLedgersClient { get; set; } = default!;

    [Inject]
    protected IDialogService Dialog { get; set; } = default!;

    private IEnumerable<LoanDto>? _entityList;
    private int _totalItems;
    private int _pageSize;
    private int _currentPage;
    private IDictionary<Guid, bool> _dictOverlayVisibility = new Dictionary<Guid, bool>();

    private float _borrowing = 10000f;
    private int _borrowDuration = 6;
    private float _loaning = 500f;
    private int _loaningDuration = 1;
    private float _loaningInterest = 1f;
    private float _loaningTotal = 0f;
    private bool _isAuthenticated = false;

    protected override async Task OnInitializedAsync()
    {
        if ((await AuthState).User is { } user)
        {
            if (user != default)
            {
                if (user.Identity != default)
                {
                    if (user.Identity.IsAuthenticated)
                    {
                        _isAuthenticated = true;
                    }
                }
            }
        }

        // if (AppDataService.IsAuthenticated() != default)
        // {
        //    _isAuthenticated = true;
        // }

        // check and get cookie for country location
        /* string? countryLocationStorage = LocalStorage.GetItem<string>("CountryLocation");

        if (countryLocationStorage != default)
        {
            Console.WriteLine(countryLocationStorage);

            LocalStorage.SetItem("CountryLocation", $"This is a sample data {DateTime.Now}");
        } */

        await LoadDataAsync();

        await Calculator();

        _loaningTotal = temporaryLedgerTable.Sum(x => x.Amount);
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
            }
            else
            {
                loanFilter.Amount = _borrowing;
            }

            if (month.HasValue)
            {
                loanFilter.Month = month.Value;
                _borrowDuration = month.Value;
            }
            else
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

    private async Task OnValueChangedUpdateAnonymousListingLoanAmount(float amount)
    {
        _loaning = amount;

        if (await Calculator())
        {
            _loaningTotal = temporaryLedgerTable.Sum(x => x.Amount);

            StateHasChanged();
        }

    }

    private async Task OnValueChangedUpdateAnonymousListingLoanDuration(int duration)
    {
        _loaningDuration = duration;

        if (await Calculator())
        {
            _loaningTotal = temporaryLedgerTable.Sum(x => x.Amount);

            StateHasChanged();
        }
    }

    private async Task OnValueChangedUpdateAnonymousListingLoanInterest(float interest)
    {
        _loaningInterest = interest;

        if (await Calculator())
        {
            _loaningTotal = temporaryLedgerTable.Sum(x => x.Amount);

            StateHasChanged();

        }

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
        // Note: bypass
        // D:\Docker\DotNet\eHulogLocal\eHulogDDDWasm\src\Client.Infrastructure\Auth\Jwt\JwtAuthenticationHeaderHandler.cs

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
        // Note: bypass
        // D:\Docker\DotNet\eHulogLocal\eHulogDDDWasm\src\Client.Infrastructure\Auth\Jwt\JwtAuthenticationHeaderHandler.cs

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

    public class TemporaryLedgerTableElement
    {
        public int Position { get; set; }
        public DateTime Due { get; set; }
        public float Amount { get; set; }
        public float Balance { get; set; }
    }

    private List<TemporaryLedgerTableElement> temporaryLedgerTable = new List<TemporaryLedgerTableElement>();

    private async Task<bool> Calculator()
    {
        temporaryLedgerTable.Clear();

        // Note: bypass
        // D:\Docker\DotNet\eHulogLocal\eHulogDDDWasm\src\Client.Infrastructure\Auth\Jwt\JwtAuthenticationHeaderHandler.cs

        GetLoanLedgerMemRequest getLoanLedgerMemRequest = new()
        {
            Amount = _loaning,
            Interest = _loaningInterest,
            InterestType = InterestType.Compound,
            Month = _loaningDuration,
            StartOfPayment = DateTime.Now
        };

        HttpRequestMessage request;
        HttpResponseMessage response;

        request = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{Config[ConfigNames.ApiBaseUrl]}api/tokens/"),
            Content = new StringContent(JsonSerializer.Serialize(new TokenRequest()
            {
                Email = MultitenancyConstants.Root.EmailAddress,
                Password = MultitenancyConstants.DefaultPassword
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
                    RequestUri = new Uri($"{Config[ConfigNames.ApiBaseUrl]}api/v1/loanledgers/calculator"),
                    Content = new StringContent(JsonSerializer.Serialize(getLoanLedgerMemRequest)),
                };

                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.Token);

                request.Content.Headers.Add("tenant", "ehulog");

                response = await HttpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var dictionary = JsonSerializer.Deserialize<Dictionary<int, Dictionary<string, object>>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (dictionary is not null && dictionary.Count > 0)
                    {
                        foreach (var item in dictionary)
                        {
                            float amountDue = 0.00f;
                            float balance = 0.00f;
                            DateTime dateDue = DateTime.UtcNow;

                            foreach (var kv in item.Value)
                            {
                                switch (kv.Key)
                                {
                                    case "AmountDue":
                                        amountDue = Convert.ToSingle(kv.Value.ToString());
                                        break;
                                    case "Balance":
                                        balance = Convert.ToSingle(kv.Value.ToString());
                                        break;
                                    case "DateDue":
                                        dateDue = Convert.ToDateTime(kv.Value.ToString());
                                        break;
                                }
                            }

                            temporaryLedgerTable.Add(new TemporaryLedgerTableElement()
                            {
                                Position = item.Key,
                                Due = dateDue,
                                Amount = amountDue,
                                Balance = balance
                            });
                        }
                    }
                }
            }
        }

        return true;
    }

    private async Task OpenLedger()
    {
        // List<LoanLedgerDto>
        var parameters = new DialogParameters { ["Ledger"] = temporaryLedgerTable };

        DialogOptions noHeader = new DialogOptions() { MaxWidth = MaxWidth.Large, CloseButton = true };

        var dialog = Dialog.Show<LoanLedger>("Ledger", parameters, noHeader);

        var resultDialog = await dialog.Result;

        if (!resultDialog.Cancelled)
        {
        }
    }

}