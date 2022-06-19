using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using Microsoft.AspNetCore.Components;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog.Loans;

public partial class SpecificLoanLedger
{

    [Parameter]
    public List<LoanLedgerDto> Ledger { get; set; } = new();

    [Parameter]
    public bool CanUpdate { get; set; } = false;

    public List<LedgerModel> _ledgerModel { get; set; } = new();

    private float _runningTotal { get; set; }

    private float _runningBalance { get; set; }

    private bool _showNextLedgerAvailablePosition { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (Ledger is not null && Ledger.Count() > 0)
        {
            _runningTotal = Ledger.Sum(l => l.AmountDue);

            _runningBalance = _runningTotal;

            foreach (var item in Ledger)
            {
                _runningBalance -= item.AmountDue;

                _ledgerModel.Add(new()
                {
                    Id = item.Id,
                    Position = item.Position,
                    AmountDue = item.AmountDue,
                    Balance = _runningBalance,
                    DateDue = item.DateDue,
                    DatePaid = item.DatePaid,
                    Status = item.Status
                });
            }
        }
    }
}

public class LedgerModel : LoanLedgerDto
{
    public float Balance { get; set; }
}