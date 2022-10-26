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

}
