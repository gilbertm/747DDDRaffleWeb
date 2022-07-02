using EHULOG.BlazorWebAssembly.Client.Components.EntityContainer;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Common;
using EHULOG.BlazorWebAssembly.Client.Pages.Multitenancy;
using EHULOG.BlazorWebAssembly.Client.Shared;
using EHULOG.WebApi.Shared.Multitenancy;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog.Anons;

public partial class FrontAnonLoans
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthorizationService AuthService { get; set; } = default!;
    [Inject]
    protected IProductsClient ProductsClient { get; set; } = default!;
    [Inject]
    protected IAppUsersClient AppUsersClient { get; set; } = default!;
    [Inject]
    protected IAppUserProductsClient AppUserProductsClient { get; set; } = default!;
    [Inject]
    protected IInputOutputResourceClient InputOutputResourceClient { get; set; } = default!;
    [Inject]
    protected ILoansClient LoansClient { get; set; } = default!;
    [Inject]
    protected ILoanLedgersClient LoanLedgersClient { get; set; } = default!;
    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;
    [Inject]
    protected HttpClient HttpClient { get; set; } = default!;

    [Inject]
    protected AppDataService AppDataService { get; set; } = default!;

    private AppUserDto _appUserDto;

    protected EntityContainerContext<LoanDto> Context { get; set; } = default!;

    protected bool Loading { get; set; }

    protected override async Task OnInitializedAsync()
    {
        _appUserDto = await AppDataService.Start();

        if (_appUserDto is not null)
        {
            if (!string.IsNullOrEmpty(_appUserDto.RoleName) && _appUserDto.RoleName.Equals("Lender"))
            {
                NavigationManager.NavigateTo("/catalog/all/loans");
                return;
            }

            if (!string.IsNullOrEmpty(_appUserDto.RoleName) && _appUserDto.RoleName.Equals("Lessee"))
            {
                NavigationManager.NavigateTo("/");
                return;
            }

        }

        var anonLoans = await getAnonLoans(new());

        Context = new EntityContainerContext<LoanDto>(
                   searchFunc: async filter =>
                   {
                       var loanFilter = filter.Adapt<SearchLoansRequest>();

                       var anonLoans = await getAnonLoans(loanFilter);

                       if (anonLoans is { })
                       {
                           foreach (var item in anonLoans.Data)
                           {
                               var loanLender = item.LoanLenders?.Where(l => l.LoanId.Equals(item.Id)).FirstOrDefault();

                               if (loanLender is { })
                               {
                                   var image = await getImage(loanLender.ProductId);

                                   if (loanLender.Product is { } && image is { })
                                   {
                                       loanLender.Product.Image = image;
                                   }
                               }
                           }
                       }


                       return anonLoans.Adapt<EntityContainerPaginationResponse<LoanDto>>();
                   },
                   template: BodyTemplate);
    }

    private async Task<InputOutputResourceDto> getImage(Guid productId)
    {
        HttpRequestMessage request;
        HttpResponseMessage response;

        request = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{Config[ConfigNames.ApiBaseUrl]}api/tokens/"),
            Content = new StringContent(JsonSerializer.Serialize(new TokenRequest()
            {
                Email = MultitenancyConstants.Lessee.EmailAddress,
                Password = MultitenancyConstants.DefaultPassword
            })),
    };

        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Content.Headers.Add("tenant", "root");

        response = await HttpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (tokenResponse is not null)
            {
                using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri($"{Config[ConfigNames.ApiBaseUrl]}api/v1/inputoutputresource/anon/{productId}")))
                {
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
                                return images.First();
                            }
                        }
                    }
                }
            }
        }

        return default!;
    }

    private async Task<EntityContainerPaginationResponse<LoanDto>> getAnonLoans(SearchLoansRequest search)
    {
        HttpRequestMessage request;
        HttpResponseMessage response;

        request = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{Config[ConfigNames.ApiBaseUrl]}api/tokens/"),
            Content = new StringContent(JsonSerializer.Serialize(new TokenRequest()
            {
                Email = MultitenancyConstants.Lessee.EmailAddress,
                Password = MultitenancyConstants.DefaultPassword
            })),
        };

        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Content.Headers.Add("tenant", "root");

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
                // request.Content.Headers.Add("tenant", "root");

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