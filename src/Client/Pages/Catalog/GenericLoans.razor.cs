using EHULOG.BlazorWebAssembly.Client.Components.Common;
using EHULOG.BlazorWebAssembly.Client.Components.EntityTable;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Common;
using EHULOG.BlazorWebAssembly.Client.Shared;
using EHULOG.WebApi.Shared.Authorization;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog;

public partial class GenericLoans
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthorizationService AuthService { get; set; } = default!;
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
    [Inject]
    protected AppDataService AppDataService { get; set; } = default!;

    protected EntityServerTableContext<LoanDto, Guid, LoanViewModel> Context { get; set; } = default!;

    private EntityTable<LoanDto, Guid, LoanViewModel> _table = default!;

    private AppUserDto _appUserDto;

    private List<AppUserProductDto> appUserProducts;

    private CustomValidation? _customValidation;

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
                   entityName: L["Loan"],
                   entityNamePlural: L["Loans"],
                   entityResource: EHULOGResource.Loans,
                   enableAdvancedSearch: false,
                   fields: new()
                   {
                        new(loan => loan.Id, L["Id"], Template: LoanTemplate),
                        new(loan => loan.StartOfPayment, L["Loan"], "StartOfPayment", Template: LoanDetailsTemplate),
                        new(loan => loan.Id, L["Status"], Template: LoanStatusTemplate),
                        new(loan => loan.LoanLenders?.Where(l => l.LoanId.Equals(loan.Id) && l.LenderId.Equals(_appUserDto.Id)).FirstOrDefault()?.ProductId, L["ProductId"], "LoanLenders.ProductId"),
                   },
                   idFunc: loanLender => loanLender.Id,
                   searchFunc: async filter =>
                   {
                       var loanFilter = filter.Adapt<SearchLoansRequest>();

                       if (_appUserDto.RoleName.Equals("Lender"))
                       {
                           loanFilter.LenderId = _appUserDto.Id;
                           loanFilter.IsLender = true;
                       }

                       var result = await LoansClient.SearchAsync(loanFilter);

                       foreach (var item in result.Data)
                       {
                           var loanLender = item.LoanLenders?.Where(l => l.LoanId.Equals(item.Id) && l.LenderId.Equals(_appUserDto.Id)).FirstOrDefault();

                           if (loanLender is not null)
                           {
                               loanLender.Product.Image = appUserProducts.Where(ap => ap.ProductId.Equals(loanLender.ProductId)).First()?.Product?.Image;
                           }
                       }

                       return result.Adapt<PaginationResponse<LoanDto>>();
                   },
                   createFunc: async loanLender =>
                   {
                       var createLoanRequest = loanLender.Adapt<CreateLoanRequest>();

                       if (await ApiHelper.ExecuteCallGuardedAsync(
                           async () => await LoansClient.CreateAsync(createLoanRequest),
                           Snackbar,
                           _customValidation) is Guid loanId)
                       {
                           if (loanId != Guid.Empty && loanId != default!)
                           {
                               var createLoanLenderRequest = new CreateLoanLenderRequest()
                               {
                                   LenderId = _appUserDto.Id,
                                   LoanId = loanId,
                                   ProductId = loanLender.ProductId
                               };

                               if (await ApiHelper.ExecuteCallGuardedAsync(
                                   async () => await LoanLendersClient.CreateAsync(createLoanLenderRequest),
                                   Snackbar,
                                   _customValidation) is Guid loanLenderId)
                               {
                                   /*if (loanLenderId != Guid.Empty && loanLenderId != default!)
                                   {
                                       // create now ledger
                                       var createLoanLedgerRequest = loanLender.Adapt<CreateLoanLedgerRequest>();
                                       if (await ApiHelper.ExecuteCallGuardedAsync(
                                           async () => await LoanLedgersClient.CreateAsync(createLoanLedgerRequest),
                                           Snackbar,
                                           _customValidation) is Guid loanLedgerId)
                                       {
                                           if (loanLedgerId != Guid.Empty && loanLedgerId != default!)
                                           {
                                               Snackbar.Add(L["Loan successfully created."], Severity.Success);

                                               NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
                                           }
                                       }
                                   } */
                               }
                           }
                       }
                   }
                   );
        }
    }
}

public class LoanViewModel : UpdateLoanRequest
{
    public Guid ProductId { get; set; }

    public ProductDto Product { get; set; } = new ProductDto()
    {
        Image = new InputOutputResourceDto(),
        Category = new CategoryDto(),
        Brand = new BrandDto()
    };
}