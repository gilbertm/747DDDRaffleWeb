using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using Microsoft.AspNetCore.Components;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog.Loans;

public partial class SpecificLoanLessee
{

    [Parameter]
    public LoanLesseeDto Lessee { get; set; } = default!;

    [Parameter]
    public bool CanUpdate { get; set; } = false;

    protected override async Task OnInitializedAsync()
    {

    }
}