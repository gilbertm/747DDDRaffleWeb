using EHULOG.BlazorWebAssembly.Client.Components.EntityContainer;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Mapster;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Nager.Country;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog.Loans;

public partial class LesseeLoans
{
    [Inject]
    protected AppDataService AppDataService { get; set; } = default!;

    [Inject]
    protected ILoanLendersClient LoanLendersClient { get; set; } = default!;
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
    protected IUsersClient UsersClient { get; set; } = default!;
    [Inject]
    protected ILoanLedgersClient LoanLedgersClient { get; set; } = default!;
    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;
    protected EntityContainerContext<LoanDto> Context { get; set; } = default!;

    protected bool Loading { get; set; }

    private List<AppUserProductDto> appUserProducts { get; set; } = default!;

    private string _currency { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await AppDataService.InitializationAsync();

        if (AppDataService.AppUser != default)
        {
            if (!string.IsNullOrEmpty(AppDataService.AppUser.RoleName) && AppDataService.AppUser.RoleName.Equals("Lender"))
            {
                NavigationManager.NavigateTo("/catalog/all/loans", true);
            }
            else
            {
                if (!string.IsNullOrEmpty(AppDataService.AppUser.HomeCountry))
                {
                    var countryProvider = new CountryProvider();
                    var countryInfo = countryProvider.GetCountryByName(AppDataService.AppUser.HomeCountry);

                    if (countryInfo is { })
                    {
                        if (countryInfo.Currencies.Count() > 0)
                        {
                            _currency = countryInfo.Currencies.FirstOrDefault()?.IsoCode ?? string.Empty;
                        }
                    }
                }

                Context = new EntityContainerContext<LoanDto>(
                           searchFunc: async filter =>
                           {
                               var loanFilter = filter.Adapt<SearchLoansLesseeRequest>();

                               loanFilter.Status = new[] { LoanStatus.Published, LoanStatus.Assigned, LoanStatus.Payment };
                               loanFilter.AppUserId = AppDataService.AppUser.Id;

                               var result = await LoansClient.SearchLesseeAsync(loanFilter);

                               foreach (var item in result.Data)
                               {
                                   var loanLender = item.LoanLenders?.Where(l => l.LoanId.Equals(item.Id)).FirstOrDefault();

                                   if (loanLender is not null && loanLender.Product is not null)
                                   {

                                       var image = await InputOutputResourceClient.GetAsync(loanLender.ProductId);

                                       if (image.Count() > 0)
                                       {

                                           loanLender.Product.Image = image.First();
                                       }
                                   }
                               }

                               return result.Adapt<EntityContainerPaginationResponse<LoanDto>>();
                           },
                           template: BodyTemplate);
            }
        }
    }
}