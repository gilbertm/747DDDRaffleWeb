using EHULOG.BlazorWebAssembly.Client.Infrastructure.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using MudBlazor;
using EHULOG.BlazorWebAssembly.Client.Components.Common;
using Mapster;

namespace EHULOG.BlazorWebAssembly.Client.Shared.Dialogs;

public partial class LendersUserInspectionView
{
    [Parameter]
    public LoanApplicantDto LoanApplicantDto { get; set; } = default!;

    [Parameter]
    public bool IsOwner { get; set; } = default!;

    [Parameter]
    public LoanStatus LoanStatus { get; set; } = default!;

    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;

    [CascadingParameter]
    protected MudDialogInstance MudDialog { get; set; } = default!;

    [Inject]
    protected IAuthenticationService AuthService { get; set; } = default!;
    [Inject]
    protected ILoanApplicantsClient LoanApplicantsClient { get; set; } = default!;
    [Inject]
    protected ILoanLesseesClient LoanLesseesClient { get; set; } = default!;
    [Inject]
    protected ILoansClient LoansClient { get; set; } = default!;
    [Inject]
    protected AppDataService AppDataService { get; set; } = default!;

    private async Task Submit()
    {
        await AppDataService.InitializationAsync();

        if (AppDataService != default!)
        {
            if (AppDataService.AppUser != default)
            {
                if (!string.IsNullOrEmpty(LoanApplicantDto.LoanId.ToString()) && !LoanApplicantDto.LoanId.Equals(Guid.Empty))
                {
                    if (await ApiHelper.ExecuteCallGuardedAsync(() => LoansClient.GetAsync(LoanApplicantDto.LoanId), Snackbar, null) is LoanDto loan)
                    {
                        if (loan is { })
                        {
                            var updateLoanStatusRequest = loan.Adapt<UpdateLoanStatusRequest>();

                            updateLoanStatusRequest.Status = LoanStatus.Assigned;

                            if (await ApiHelper.ExecuteCallGuardedAsync(() => LoansClient.UpdateStatusAsync(LoanApplicantDto.LoanId, updateLoanStatusRequest), Snackbar, null) is Guid loanId)
                            {
                                if (!string.IsNullOrEmpty(loanId.ToString()) && !loanId.Equals(Guid.Empty))
                                {
                                    Snackbar.Add("Granted", Severity.Success);

                                    var createLoanLesseeRequest = new CreateLoanLesseeRequest()
                                    {
                                        LesseeId = LoanApplicantDto.AppUserId,
                                        LoanId = loanId
                                    };

                                    if (await ApiHelper.ExecuteCallGuardedAsync(() => LoanLesseesClient.CreateAsync(createLoanLesseeRequest), Snackbar, null) is Guid loanLesseeId)
                                    {
                                        if (!string.IsNullOrEmpty(loanLesseeId.ToString()) && !loanLesseeId.Equals(Guid.Empty))
                                        {
                                            Snackbar.Add("Loan / Lessee record updated.", Severity.Success);

                                            MudDialog.Close(DialogResult.Ok(loanLesseeId.ToString()));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                MudDialog.Close(DialogResult.Cancel());
            }
        }
    }
}