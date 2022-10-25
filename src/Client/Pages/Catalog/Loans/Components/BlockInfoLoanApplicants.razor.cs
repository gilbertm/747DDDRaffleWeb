using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Shared;
using EHULOG.BlazorWebAssembly.Client.Shared.Dialogs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog.Loans.Components;

public partial class BlockInfoLoanApplicants
{
    [Parameter]
    public LoanDto Loan { get; set; } = default!;

    [Inject]
    protected IDialogService Dialog { get; set; } = default!;

    [Inject]
    protected ILoanApplicantsClient LoanApplicantsClient { get; set; } = default!;

    [Inject]
    protected ILoansClient ILoansClient { get; set; } = default!;

    [CascadingParameter(Name = "AppUser")]
    private AppUserDto AppUser { get; set; } = default!;

    private LoanApplicantDto MyselfApplicant { get; set; } = default!;

    private LoanLenderDto MyselfLender { get; set; } = default!;

    private LoanLesseeDto MyselfLessee { get; set; } = default!;

    private bool IsOwner { get; set; } = false;

    private void OpenLendersUserInspectionView(LoanApplicantDto loanApplicant)
    {
        var parameters = new DialogParameters { ["LoanApplicantDto"] = loanApplicant, ["IsOwner"] = IsOwner, ["LoanStatus"] = Loan.Status };

        DialogOptions noHeader = new DialogOptions() { CloseButton = true };

        if (MyselfLender != default)
        {
            Dialog.Show<LendersUserInspectionView>("User's Details", parameters, noHeader);
        }
    }
}
