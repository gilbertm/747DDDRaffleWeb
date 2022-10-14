using EHULOG.BlazorWebAssembly.Client.Components.EntityContainer;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Mapster;
using Microsoft.AspNetCore.Components;
using Nager.Country;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog;

public partial class FrontLoans
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
    protected ILoanLedgersClient LoanLedgersClient { get; set; } = default!;
    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;
    protected EntityContainerContext<LoanDto> Context { get; set; } = default!;

    protected bool Loading { get; set; }

    private AppUserDto? _appUserDto { get; set; }

    private List<AppUserProductDto> appUserProducts { get; set; } = default!;

    private string _currency { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        _appUserDto = await AppDataService.InitializationAsync();

        if (_appUserDto is not null)
        {
            if (!string.IsNullOrEmpty(_appUserDto.RoleName) && _appUserDto.RoleName.Equals("Lender"))
            {
                NavigationManager.NavigateTo("/catalog/all/loans", true);
                return;
            }

            if (!string.IsNullOrEmpty(_appUserDto.HomeCountry))
            {
                var countryProvider = new CountryProvider();
                var countryInfo = countryProvider.GetCountryByName(_appUserDto.HomeCountry);

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