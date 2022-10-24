using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using Microsoft.AspNetCore.Components;
using Nager.Country;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog.Loans;

public partial class SpecificLoanLedger
{

    [Parameter]
    public List<LoanLedgerDto> Ledger { get; set; } = new();

    [Parameter]
    public bool CanUpdate { get; set; } = false;

    public List<LedgerModel> LedgerModel { get; set; } = new();

    private float _runningTotal { get; set; }

    [Parameter]
    public string Currency { get; set; } = string.Empty;

    private float _runningBalance { get; set; }

    private bool _showNextLedgerAvailablePosition { get; set; }

    protected override void OnInitialized()
    {
        if (Ledger is not null && Ledger.Count > 0)
        {
            _runningTotal = Ledger.Sum(l => l.AmountDue);

            _runningBalance = _runningTotal;

            foreach (var item in Ledger)
            {
                _runningBalance -= item.AmountDue;

                LedgerModel.Add(new()
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