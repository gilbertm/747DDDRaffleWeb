using EHULOG.BlazorWebAssembly.Client.Components.Common;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Nager.Country;
using static MudBlazor.CategoryTypes;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog.Loans;

public partial class SpecificLoan
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthorizationService AuthService { get; set; } = default!;
    [Inject]
    protected IUsersClient UsersClient { get; set; } = default!;
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

    // lender
    private bool _canUpdate { get; set; } = false;

    // lessee
    private bool _canUpdateLedger { get; set; } = false;

    // lessee
    private bool _isPossibleToAppy { get; set; } = false;

    private string _currency { get; set; } = string.Empty;

    private List<ForUploadFile> ForUploadFiles { get; set; } = new();

    private List<AppUserProductDto> appUserProducts { get; set; } = default!;


    public async Task OnClickChildComponent(Guid? loanId)
    {
        await Update(loanId);

        StateHasChanged();
    }

    protected override async Task OnInitializedAsync()
    {
        await AppDataService.InitializationAsync();

        if (AppDataService != default)
        {
            if (AppDataService.AppUser != default)
            {
                // show products, if lender
                if (!string.IsNullOrEmpty(AppDataService.AppUser.RoleName) && AppDataService.AppUser.RoleName.Equals("Lender"))
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

                    appUserProducts = (await AppUserProductsClient.GetByAppUserIdAsync(AppDataService.AppUser.Id)).ToList();

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

                await Update(loanId);



            }
        }
    }

    private async Task Update(Guid? loanId)
    {
        if (loanId.HasValue)
        {
            if (await ApiHelper.ExecuteCallGuardedAsync(
                                            async () => await LoansClient.GetAsync(loanId.Value),
                                            Snackbar,
                                            null) is LoanDto loanDto)
            {

                if (loanDto is { })
                {
                    RequestModel.Id = loanDto.Id;

                    // the loan is with a lender
                    // just casual redo checks
                    if (loanDto.LoanLenders is not null && loanDto.LoanLenders.Count() > 0)
                    {
                        var loanLender = loanDto.LoanLenders.Where(ll => ll.LoanId.Equals(loanDto.Id)).First();

                        if (loanLender is { } && loanLender.Lender is { })
                        {
                            // get the currency from the lender
                            var countryProvider = new CountryProvider();
                            var countryInfo = countryProvider.GetCountryByName(loanLender.Lender.HomeCountry);

                            if (countryInfo is { })
                            {
                                if (countryInfo.Currencies.Count() > 0)
                                {
                                    _currency = countryInfo.Currencies.FirstOrDefault()?.IsoCode ?? string.Empty;
                                }

                            }

                            RequestModel.Product = loanLender.Product is not null ? loanLender.Product : default!;
                            RequestModel.ProductId = !loanLender.ProductId.Equals(Guid.Empty) ? loanLender.ProductId : default;

                            // lender checks
                            if (AppDataService.AppUser.RoleName is not null && AppDataService.AppUser.RoleName.Equals("Lender"))
                            {
                                // owner
                                if (loanLender.Lender is not null && loanLender.LenderId.Equals(AppDataService.AppUser.Id))
                                {
                                    _canUpdate = true;
                                    _canUpdateLedger = true;
                                }

                            }

                        }

                        // get the product image
                        var image = await InputOutputResourceClient.GetAsync(RequestModel.ProductId);

                        if (image.Count() > 0)
                        {
                            RequestModel.Product.Image = image.First();
                        }
                    }

                    // lessee
                    if (loanDto.LoanLessees is not null && loanDto.LoanLessees.Count() > 0)
                    {
                        if (AppDataService.AppUser.RoleName is not null && AppDataService.AppUser.RoleName.Equals("Lessee"))
                        {
                            var loanLessee = loanDto.LoanLessees.Where(ll => ll.LoanId.Equals(loanDto.Id)).First();

                            if (loanLessee.Lessee is not null && loanLessee.LesseeId.Equals(AppDataService.AppUser.Id))
                            {
                                _canUpdateLedger = true;
                            }
                        }
                    }
                    else if (loanDto.LoanLessees is null || loanDto.LoanLessees.Count() <= 0)
                    {
                        if (AppDataService.AppUser.RoleName is not null && AppDataService.AppUser.RoleName.Equals("Lessee"))
                        {
                            _isPossibleToAppy = true;
                        }
                    }

                    // applicants
                    if (loanDto.LoanApplicants is { })
                    {
                        if (loanDto.LoanApplicants.Count() > 0)
                        {
                            foreach (var loanApplicantDto in loanDto.LoanApplicants)
                            {
                                var userDetailsDto = await UsersClient.GetByIdAsync(loanApplicantDto.AppUser.ApplicationUserId);

                                loanApplicantDto.AppUser.FirstName = userDetailsDto.FirstName;
                                loanApplicantDto.AppUser.LastName = userDetailsDto.LastName;
                                loanApplicantDto.AppUser.Email = userDetailsDto.Email;
                                loanApplicantDto.AppUser.PhoneNumber = userDetailsDto.PhoneNumber;

                            }
                        }
                    }

                    RequestModel.InfoCollateral = loanDto.InfoCollateral ?? string.Empty;
                    RequestModel.IsCollateral = loanDto.IsCollateral;
                    RequestModel.Interest = loanDto.Interest;
                    RequestModel.InterestType = loanDto.InterestType;
                    RequestModel.Status = loanDto.Status;
                    RequestModel.Month = loanDto.Month;
                    RequestModel.StartOfPayment = loanDto.StartOfPayment;

                    RequestModel.LoanApplicants = loanDto.LoanApplicants;
                    RequestModel.Ledgers = loanDto.Ledgers;
                    RequestModel.LoanLessees = loanDto.LoanLessees;
                }


            }
        }
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