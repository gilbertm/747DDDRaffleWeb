using EHULOG.BlazorWebAssembly.Client.Components.Common;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog.Loans;

public partial class SpecificLoan
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthorizationService AuthService { get; set; } = default!;
    [Inject]
    protected IInputOutputResourceClient InputOutputResourceClient { get; set; } = default!;
    [Inject]
    protected ILoansClient LoansClient { get; set; } = default!;
    [Inject]
    protected ILoanLedgersClient LoanLedgersClient { get; set; } = default!;
    [Inject]
    protected IAppUserProductsClient AppUserProductsClient { get; set; } = default!;
    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;
    [Inject]
    protected AppDataService AppDataService { get; set; } = default!;

    private LoanViewModel RequestModel { get; set; } = new();

    private bool _canUpdate { get; set; } = false;
    private bool _canApply { get; set; } = false;

    private AppUserDto _appUserDto { get; set; } = default!;

    private List<ForUploadFile> ForUploadFiles { get; set; } = new();

    private List<AppUserProductDto> appUserProducts { get; set; } = default!;

    private CustomValidation? _customValidation;

    protected override async Task OnInitializedAsync()
    {
        _appUserDto = await AppDataService.Start();

        if (_appUserDto is not null)
        {
            // show products, if lender
            if (!string.IsNullOrEmpty(_appUserDto.RoleName) && _appUserDto.RoleName.Equals("Lender"))
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
            }

            if (loanId.HasValue)
            {
                if (await ApiHelper.ExecuteCallGuardedAsync(
                                                async () => await LoansClient.GetAsync(loanId.Value),
                                                Snackbar,
                                                _customValidation) is LoanDto loanDto && loanDto is not null)
                {
                    RequestModel.Id = loanDto.Id;

                    if (loanDto.LoanLenders is not null && loanDto.LoanLenders.Count() > 0)
                    {
                        var loanLender = loanDto.LoanLenders.Where(ll => ll.LoanId.Equals(loanDto.Id)).First();

                        RequestModel.Product = loanLender.Product is not null ? loanLender.Product : default!;
                        RequestModel.ProductId = !loanLender.ProductId.Equals(Guid.Empty) ? loanLender.ProductId : default;

                        if (_appUserDto.RoleName is not null && _appUserDto.RoleName.Equals("Lender"))
                        {
                            // lender and owner
                            if (loanLender.Lender is not null && loanLender.Lender.Id.Equals(_appUserDto.Id))
                            {
                                _canUpdate = true;
                            }

                        }

                        // get the product image
                        var image = await InputOutputResourceClient.GetAsync(RequestModel.ProductId);

                        if (image.Count() > 0)
                        {

                            RequestModel.Product.Image = image.First();
                        }
                    }

                    RequestModel.InfoCollateral = loanDto.InfoCollateral;
                    RequestModel.IsCollateral = loanDto.IsCollateral;
                    RequestModel.Interest = loanDto.Interest;
                    RequestModel.InterestType = loanDto.InterestType;
                    RequestModel.Status = loanDto.Status;
                    RequestModel.Month = loanDto.Month;
                    RequestModel.StartOfPayment = loanDto.StartOfPayment;

                    RequestModel.LoanApplicants = loanDto.LoanApplicants;
                    RequestModel.Ledgers = loanDto.Ledgers;
                    RequestModel.LoanLessees = loanDto.LoanLessees;

                    // await Task.Run(() => HandleInterest());
                }
            }
        }
    }

    private void updateupdate(Guid id, Loan loan)
    {
        /*  var updateLoanRequest = loan.Adapt<UpdateLoanRequest>();

         if (await ApiHelper.ExecuteCallGuardedAsync(
            async () => await LoansClient.UpdateAsync(id, updateLoanRequest),
            Snackbar,
            _customValidation) is Guid loanId)
         {
             if (id.Equals(loanId))
             {
                 var createLoanLedgerRequest = new CreateLoanLedgerRequest()
                 {
                     LoanId = loanId
                 };

                 if (await ApiHelper.ExecuteCallGuardedAsync(
                     async () => await LoanLedgersClient.CreateAsync(createLoanLedgerRequest),
                     Snackbar,
                     _customValidation) is Guid loanLedgerId)
                 {
                     if (loanLedgerId != Guid.Empty && loanLedgerId != default!)
                     {
                         Snackbar.Add(L["Loan successfully updated."], Severity.Success);

                         NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
                     }
                 }
             }

         }

         NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true); */
    }
}

public class LoanViewModel : UpdateLoanRequest
{
    public Guid ProductId { get; set; }

    public ProductDto Product { get; set; } = new();
}

public class TemporaryLedgerTableElement
{
    public int Position { get; set; }
    public DateTime Due { get; set; }
    public float Amount { get; set; }
    public float Balance { get; set; }
}