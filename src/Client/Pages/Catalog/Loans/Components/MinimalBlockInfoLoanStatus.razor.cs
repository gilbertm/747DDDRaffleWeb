using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog.Loans.Components;

public partial class MinimalBlockInfoLoanStatus
{
    [CascadingParameter(Name = "AppDataService")]
    protected AppDataService AppDataService { get; set; } = default!;

    [Inject]
    protected ILoanApplicantsClient LoanApplicantsClient { get; set; } = default!;

    [Inject]
    protected ILoansClient ILoansClient { get; set; } = default!;

    [Parameter]
    public LoanDto Loan { get; set; } = default!;

    private bool _isLesseeCanApply;

    protected override async Task OnInitializedAsync()
    {
        if (AppDataService != default)
        {
            if (AppDataService.AppUser != default)
            {
                _isLesseeCanApply = await AppDataService.IsLesseeCanApplyAsync(Loan.Id);
            }
        }
    }

    protected async Task Apply()
    {
        if (Loan != default)
        {
            var createLoanApplicant = new CreateLoanApplicantRequest()
            {
                AppUserId = AppDataService.AppUser.Id,
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

                            // reloads the loan
                            // without the need to refresh
                            Loan = loan;

                            if (AppDataService != default)
                            {
                                if (AppDataService.AppUser != default)
                                {
                                    _isLesseeCanApply = await AppDataService.IsLesseeCanApplyAsync(Loan.Id);
                                }
                            }
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
                AppUserId = AppDataService.AppUser.Id,
                LoanId = Loan.Id,
                Flag = 0,
                Reason = "Reapply."
            };

            if (await ApiHelper.ExecuteCallGuardedAsync(() => LoanApplicantsClient.UpdateAsync(Loan.Id, AppDataService.AppUser.Id, updateLoanApplicant), Snackbar, null, "Success") is Guid loanApplicantId)
            {
                if (!string.IsNullOrEmpty(loanApplicantId.ToString()) && !loanApplicantId.Equals(Guid.Empty))
                {
                    Snackbar.Add("Cancelled. Remove application from this loan.", Severity.Success);

                    if (await ApiHelper.ExecuteCallGuardedAsync(() => ILoansClient.GetAsync(Loan.Id), Snackbar, null, "Success") is LoanDto loan)
                    {
                        if (!string.IsNullOrEmpty(loanApplicantId.ToString()) && !loanApplicantId.Equals(Guid.Empty))
                        {
                            Snackbar.Add("Reapplied", Severity.Success);

                            // reloads the loan
                            // without the need to refresh
                            Loan = loan;

                            if (AppDataService != default)
                            {
                                if (AppDataService.AppUser != default)
                                {
                                    _isLesseeCanApply = await AppDataService.IsLesseeCanApplyAsync(Loan.Id);
                                }
                            }
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
                AppUserId = AppDataService.AppUser.Id,
                LoanId = Loan.Id,
                Flag = 2,
                Reason = "Cancelled."
            };

            if (await ApiHelper.ExecuteCallGuardedAsync(() => LoanApplicantsClient.UpdateAsync(Loan.Id, AppDataService.AppUser.Id, updateLoanApplicant), Snackbar, null, "Success") is Guid loanApplicantId)
            {
                if (!string.IsNullOrEmpty(loanApplicantId.ToString()) && !loanApplicantId.Equals(Guid.Empty))
                {
                    Snackbar.Add("Cancelled. Remove application from this loan.", Severity.Success);

                    if (await ApiHelper.ExecuteCallGuardedAsync(() => ILoansClient.GetAsync(Loan.Id), Snackbar, null, "Success") is LoanDto loan)
                    {
                        if (!string.IsNullOrEmpty(loanApplicantId.ToString()) && !loanApplicantId.Equals(Guid.Empty))
                        {
                            Snackbar.Add("Applied", Severity.Success);

                            // reloads the loan
                            // without the need to refresh
                            Loan = loan;

                            if (AppDataService != default)
                            {
                                if (AppDataService.AppUser != default)
                                {
                                    _isLesseeCanApply = await AppDataService.IsLesseeCanApplyAsync(Loan.Id);
                                }
                            }
                        }
                    }
                }
            }

            StateHasChanged();
        }
    }
}
