using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Microsoft.AspNetCore.Components;
using Nager.Country;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog.Loans.Components;

public partial class BlockLoan
{
    [CascadingParameter(Name = "AppDataService")]
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
    [Parameter]
    public LoanDto Loan { get; set; } = default!;
    [Parameter]
    public bool DisableStatus { get; set; } = default!;

    private async Task UpdateLoan(Guid loanId)
    {
        if (await ApiHelper.ExecuteCallGuardedAsync(() => LoansClient.GetAsync(loanId), Snackbar, null, "Success") is LoanDto loan)
        {
            if (loan != default)
            {
                if (loan.LoanApplicants != default)
                {

                    if (loan.LoanApplicants.Count > 0)
                    {
                        foreach (var loanApplicant in loan.LoanApplicants)
                        {
                            if (loanApplicant.AppUser != default)
                            {
                                var userDetailsDto = await UsersClient.GetByIdAsync(loanApplicant.AppUser.ApplicationUserId);

                                loanApplicant.AppUser.FirstName = userDetailsDto.FirstName;
                                loanApplicant.AppUser.LastName = userDetailsDto.LastName;
                                loanApplicant.AppUser.Email = userDetailsDto.Email;
                                loanApplicant.AppUser.PhoneNumber = userDetailsDto.PhoneNumber;
                            }
                        }
                    }
                }

                if (loan.LoanLessees != default)
                {
                    if (loan.LoanLessees.Count > 0)
                    {
                        foreach (var loanLessee in loan.LoanLessees)
                        {
                            if (loanLessee.Lessee != default)
                            {
                                var userDetailsDto = await UsersClient.GetByIdAsync(loanLessee.Lessee.ApplicationUserId);

                                loanLessee.Lessee.FirstName = userDetailsDto.FirstName;
                                loanLessee.Lessee.LastName = userDetailsDto.LastName;
                                loanLessee.Lessee.Email = userDetailsDto.Email;
                                loanLessee.Lessee.PhoneNumber = userDetailsDto.PhoneNumber;
                            }
                        }
                    }
                }

                Loan = loan;

                StateHasChanged();
            }

        }

    }

}
