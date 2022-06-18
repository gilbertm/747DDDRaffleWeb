using EHULOG.BlazorWebAssembly.Client.Components.EntityContainer;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
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

    protected EntityContainerContext<LoanDto> Context { get; set; } = default!;

    protected bool Loading { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var anonLoans = await getAnonLoans(new());

        Context = new EntityContainerContext<LoanDto>(
                   searchFunc: async filter =>
                   {
                       var loanFilter = filter.Adapt<SearchLoansRequest>();

                       var anonLoans = await getAnonLoans(loanFilter);

                       return anonLoans.Adapt<EntityContainerPaginationResponse<LoanDto>>();
                   },
                   template: BodyTemplate);
    }

    private string _baseUrl = @"https://localhost:5001";

    private async Task<EntityContainerPaginationResponse<LoanDto>> getAnonLoans(SearchLoansRequest search)
    {
        HttpRequestMessage request;
        HttpResponseMessage response;

        request = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{_baseUrl}/api/tokens/"),
            Content = new StringContent(JsonSerializer.Serialize(new TokenRequest()
            {
                Email = "admin@root.com",
                Password = "123Pa$$word!"
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
                    RequestUri = new Uri($"{_baseUrl}/api/v1/loans/search/anon"),
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