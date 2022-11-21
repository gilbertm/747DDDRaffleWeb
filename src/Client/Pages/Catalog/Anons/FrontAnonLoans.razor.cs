using EHULOG.BlazorWebAssembly.Client.Components.EntityContainer;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Common;
using EHULOG.BlazorWebAssembly.Client.Pages.Multitenancy;
using EHULOG.BlazorWebAssembly.Client.Shared;
using EHULOG.WebApi.Shared.Multitenancy;
using Mapster;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Headers;
using System.Text.Json;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog.Anons;

public partial class FrontAnonLoans
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private HttpClient HttpClient { get; set; } = default!;

    [Inject]
    private AppDataService AppDataService { get; set; } = default!;

    // [Inject]
    //private ILocalStorageService LocalStorage { get; set; } = default!;

    private EntityContainerContext<LoanDto> Context { get; set; } = default!;

    private string Country { get; set; } = default!;
    private string CountryCurrency { get; set; } = default!;

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
            NavigationManager.NavigateTo("/");

            return;
        }

        var anonLoans = await GetAnonLoans(new());

        Context = new EntityContainerContext<LoanDto>(
                   searchFunc: async filter =>
                   {
                       var loanFilter = filter.Adapt<SearchLoansRequest>();

                       var anonLoans = await GetAnonLoans(loanFilter);

                       if (anonLoans is { })
                       {
                           if (anonLoans.Data is { } && anonLoans.Data.Count > 0)
                           {
                               foreach (var item in anonLoans.Data)
                               {
                                   dictOverlayVisibility.Add(item.Id, false);

                                   var loanLender = item.LoanLenders?.Where(l => l.LoanId.Equals(item.Id)).FirstOrDefault();

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

                       if (anonLoans is { })
                           return anonLoans.Adapt<EntityContainerPaginationResponse<LoanDto>>();

                       return default!;
                   },
                   template: BodyTemplate);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            Country = await AppDataService.GetCountryOnAnonymousStateAsync();

            if (!string.IsNullOrEmpty(Country))
            {
                CountryCurrency = AppDataService.GetCurrencyAnonymous(Country);
            }

            StateHasChanged();
        }
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

    private async Task<EntityContainerPaginationResponse<LoanDto>> GetAnonLoans(SearchLoansRequest search)
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
                    var dataLoans = JsonSerializer.Deserialize<EntityContainerPaginationResponse<LoanDto>>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (dataLoans is not null)
                        return dataLoans;
                }
            }
        }

        return default!;
    }
}