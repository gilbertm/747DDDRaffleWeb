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
    public Guid AppUserId { get; set; } = default!;

    [Parameter]
    public bool IsOwner { get; set; } = default!;

    [Parameter]
    public bool IsApplicant { get; set; } = default!;

    [Parameter]
    public bool IsLessee { get; set; } = default!;

    [Parameter]
    public LoanDto Loan { get; set; } = default!;

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

    [Inject]
    protected IUsersClient UsersClient { get; set; } = default!;

    [Inject]
    protected IAppUsersClient AppUsersClient { get; set; } = default!;

    private UserDetailsDto UserDetails { get; set; } = default!;

    private AppUserDto AppUser { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        if (await ApiHelper.ExecuteCallGuardedAsync(async () => await AppUsersClient.GetAsync(AppUserId), Snackbar) is AppUserDto appUser)
        {
            if (appUser != default)
            {
                AppUser = appUser;

                if (await ApiHelper.ExecuteCallGuardedAsync(async () => await UsersClient.GetByIdAsync(appUser.ApplicationUserId), Snackbar) is UserDetailsDto userDetails)
                {
                    UserDetails = userDetails;
                }
            }

        }
    }

    private async Task Submit()
    {
        await AppDataService.InitializationAsync();

        if (AppDataService != default!)
        {
            if (AppDataService.AppUser != default)
            {
                if (IsApplicant)
                {
                    if (Loan != default)
                    {
                        var updateLoanStatusRequest = Loan.Adapt<UpdateLoanStatusRequest>();

                        updateLoanStatusRequest.Status = LoanStatus.Assigned;

                        if (await ApiHelper.ExecuteCallGuardedAsync(() => LoansClient.UpdateStatusAsync(Loan.Id, updateLoanStatusRequest), Snackbar, null) is Guid loanId)
                        {
                            if (!string.IsNullOrEmpty(loanId.ToString()) && !loanId.Equals(Guid.Empty))
                            {
                                Snackbar.Add("Granted", Severity.Success);

                                var createLoanLesseeRequest = new CreateLoanLesseeRequest()
                                {
                                    LesseeId = AppUserId,
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

                if (IsLessee)
                {
                    // if lessee do handling
                }

                MudDialog.Close(DialogResult.Cancel());
            }
        }
    }
}