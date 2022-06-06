using EHULOG.BlazorWebAssembly.Client.Components.EntityTable;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Common;
using EHULOG.BlazorWebAssembly.Client.Shared;
using EHULOG.WebApi.Shared.Authorization;
using Mapster;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog;

public partial class Loans
{
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
    protected AppDataService AppDataService { get; set; } = default!;

    protected EntityServerTableContext<LoanLenderDto, Guid, LoanLenderViewModel> Context { get; set; } = default!;

    private EntityTable<LoanLenderDto, Guid, LoanLenderViewModel> _table = default!;

    private AppUserDto _appUserDto;

    private List<AppUserProductDto> appUserProducts;

    protected override async Task OnInitializedAsync()
    {
        _appUserDto = await AppDataService.Start();

        if (_appUserDto is not null)
        {
            appUserProducts = (await AppUserProductsClient.GetByAppUserIdAsync(_appUserDto.Id)).ToList();

            if (appUserProducts.Count() > 0)
            {
                foreach (var item in appUserProducts)
                {
                    if (item.Product is not null)
                    {
                        var image = await InputOutputResourceClient.GetAsync(item.Product.Id);

                        if (image.Count() > 0)
                        {

                            item.Product.Image = image.First();
                        }
                    }
                }
            }

            Context = new(
                   entityName: L["LoanLenders"],
                   entityNamePlural: L["LoanLenders"],
                   entityResource: EHULOGResource.LoanLenders,
                   fields: new()
                   {
                        new(loanLender => loanLender.Id, L["Id"], "Id"),
                        new(loanLender => loanLender.LenderId, L["LenderId"], "LenderId"),
                        new(loanLender => loanLender.LoanId, L["LoanId"], "LoanId"),
                        new(loanLender => loanLender.ProductId, L["ProductId"], "ProductId"),
                   },
                   enableAdvancedSearch: true,
                   idFunc: loanLender => loanLender.Id,
                   searchFunc: async filter =>
                   {
                       var loanFilter = filter.Adapt<SearchLoanLendersRequest>();

                       var result = await LoanLendersClient.SearchAsync(loanFilter);

                       return result.Adapt<PaginationResponse<LoanLenderDto>>();
                   },
                   createFunc: async loanLender =>
                   {
                       await LoanLendersClient.CreateAsync(loanLender.Adapt<CreateLoanLenderRequest>());
                   }
                   );
        }
    }
}

public class LoanLenderViewModel : UpdateLoanLenderRequest
{
    public LoanDto Loan { get; set; } = new LoanDto();

    public ProductDto Product { get; set; } = new ProductDto()
    {
        Image = new InputOutputResourceDto(),
        Category = new CategoryDto(),
        Brand = new BrandDto()
    };

    public AppUserDto User { get; set; } = new AppUserDto();
}