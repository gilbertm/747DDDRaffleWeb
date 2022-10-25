using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog.Loans.Components;

public partial class MinimalBlockInfoLoanStatus
{
    [Inject]
    protected ILoanApplicantsClient LoanApplicantsClient { get; set; } = default!;

    [Inject]
    protected ILoansClient ILoansClient { get; set; } = default!;

    [Parameter]
    public LoanDto Loan { get; set; } = default!;

    [Parameter]
    public string Role { get; set; } = default!;

    [Parameter]
    public Guid AppUserId { get; set; } = default!;

    [Parameter]
    public string ApplicationUserId { get; set; } = default!;

    [Parameter]
    public string? Href { get; set; }

    // can apply if
    // 1. not yet an applicant
    // 2. meets conditions, as per packages
    [Parameter]
    public bool CanApply { get; set; } = default!;

    protected async Task Apply()
    {
        if (Loan != default)
        {
            var createLoanApplicant = new CreateLoanApplicantRequest()
            {
                AppUserId = AppUserId,
                LoanId = Loan.Id,
                Flag = 0,
                Reason = "Init"
            };

            if (await ApiHelper.ExecuteCallGuardedAsync(() => LoanApplicantsClient.CreateAsync(createLoanApplicant), Snackbar, null, "Success") is Guid loanApplicantId)
            {
                if (!string.IsNullOrEmpty(loanApplicantId.ToString()) && !loanApplicantId.Equals(Guid.Empty))
                {
                    Snackbar.Add("Applied", Severity.Success);

                    if (await ApiHelper.ExecuteCallGuardedAsync(() => ILoansClient.GetAsync(Loan.Id), Snackbar, null, "Success") is LoanDto loan)
                    {
                        if (!string.IsNullOrEmpty(loanApplicantId.ToString()) && !loanApplicantId.Equals(Guid.Empty))
                        {
                            Snackbar.Add("Applied", Severity.Success);

                            // reload the load
                            // reloads the loan
                            // without the need to refresh
                            Loan = loan;
                        }
                    }
                }
            }

            StateHasChanged();
        }
    }

    protected async Task ReApply()
    {
        if (Loan != default)
        {
            var updateLoanApplicant = new UpdateLoanApplicantRequest()
            {
                AppUserId = AppUserId,
                LoanId = Loan.Id,
                Flag = 0,
                Reason = "Reapply."
            };

            if (await ApiHelper.ExecuteCallGuardedAsync(() => LoanApplicantsClient.UpdateAsync(Loan.Id, AppUserId, updateLoanApplicant), Snackbar, null, "Success") is Guid loanApplicantId)
            {
                if (!string.IsNullOrEmpty(loanApplicantId.ToString()) && !loanApplicantId.Equals(Guid.Empty))
                {
                    Snackbar.Add("Cancelled. Remove application from this loan.", Severity.Success);

                    if (await ApiHelper.ExecuteCallGuardedAsync(() => ILoansClient.GetAsync(Loan.Id), Snackbar, null, "Success") is LoanDto loan)
                    {
                        if (!string.IsNullOrEmpty(loanApplicantId.ToString()) && !loanApplicantId.Equals(Guid.Empty))
                        {
                            Snackbar.Add("Reapplied", Severity.Success);

                            // reload the load
                            // reloads the loan
                            // without the need to refresh
                            Loan = loan;
                        }
                    }
                }
            }

            StateHasChanged();
        }
    }

    protected async Task CancelApply()
    {
        if (Loan != default)
        {
            var updateLoanApplicant = new UpdateLoanApplicantRequest()
            {
                AppUserId = AppUserId,
                LoanId = Loan.Id,
                Flag = 2,
                Reason = "Cancelled."
            };

            if (await ApiHelper.ExecuteCallGuardedAsync(() => LoanApplicantsClient.UpdateAsync(Loan.Id, AppUserId, updateLoanApplicant), Snackbar, null, "Success") is Guid loanApplicantId)
            {
                if (!string.IsNullOrEmpty(loanApplicantId.ToString()) && !loanApplicantId.Equals(Guid.Empty))
                {
                    Snackbar.Add("Cancelled. Remove application from this loan.", Severity.Success);

                    if (await ApiHelper.ExecuteCallGuardedAsync(() => ILoansClient.GetAsync(Loan.Id), Snackbar, null, "Success") is LoanDto loan)
                    {
                        if (!string.IsNullOrEmpty(loanApplicantId.ToString()) && !loanApplicantId.Equals(Guid.Empty))
                        {
                            Snackbar.Add("Applied", Severity.Success);

                            // reload the load
                            // reloads the loan
                            // without the need to refresh
                            Loan = loan;
                        }
                    }
                }
            }

            StateHasChanged();
        }
    }
}
