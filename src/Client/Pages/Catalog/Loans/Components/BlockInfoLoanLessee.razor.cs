using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Shared;
using EHULOG.BlazorWebAssembly.Client.Shared.Dialogs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog.Loans.Components;

public partial class BlockInfoLoanLessee
{
    [CascadingParameter(Name = "AppDataService")]
    private AppDataService AppDataService { get; set; } = default!;

    [Parameter]
    public LoanDto Loan { get; set; } = default!;

    [Inject]
    protected IDialogService Dialog { get; set; } = default!;

    [Inject]
    protected ILoanApplicantsClient LoanApplicantsClient { get; set; } = default!;

    [Inject]
    protected ILoansClient ILoansClient { get; set; } = default!;

    private LoanLenderDto MyselfLender { get; set; } = default!;

    private LoanLesseeDto MyselfLessee { get; set; } = default!;

    private bool IsOwner { get; set; } = false;

    [Parameter]
    public EventCallback OnClickApprove { get; set; } = default!;

    private async Task OpenLendersUserInspectionView(Guid appUserId)
    {
        var parameters = new DialogParameters { ["AppUserId"] = appUserId, ["IsLessee"] = true, ["IsOwner"] = IsOwner, ["Loan"] = Loan };

        DialogOptions noHeader = new DialogOptions() { MaxWidth = MaxWidth.Large, CloseButton = true };

        if (MyselfLender != default)
        {
            var dialog = Dialog.Show<LendersUserInspectionView>("User's Details", parameters, noHeader);

            var resultDialog = await dialog.Result;

            if (!resultDialog.Cancelled)
            {
                if (resultDialog.Data is string resultLoanLesseeId)
                {
                    await OnClickApprove.InvokeAsync();
                }
            }

        }
    }

}
