using EHULOG.BlazorWebAssembly.Client.Components.EntityContainer;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog;

public partial class FrontLoans
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

    protected EntityContainerContext<LoanDto> Context { get; set; } = default!;

    protected bool Loading { get; set; }

    private AppUserDto _appUserDto;

    private List<AppUserProductDto> appUserProducts;

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