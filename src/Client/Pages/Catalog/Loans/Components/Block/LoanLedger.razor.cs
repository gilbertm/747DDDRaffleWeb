using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Common;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Microsoft.AspNetCore.Components;
using Nager.Country;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog.Loans.Components.Block;

public partial class LoanLedger
{

    [Parameter]
    public List<LoanLedgerDto> Ledger { get; set; } = new();

    [Parameter]
    public bool CanUpdate { get; set; } = false;
    [Parameter]
    public bool DisableStatusPaymentColumns { get; set; } = false;
    [Parameter]
    public bool DisableHeader { get; set; } = false;
    [Parameter]
    public bool Dense { get; set; } = false;

    [Inject]
    public IInputOutputResourceClient InputOutputResourceClient { get; set; } = default!;

    public List<LedgerViewModel> LedgerModel { get; set; } = new();

    private float _runningTotal { get; set; }

    [Parameter]
    public string Currency { get; set; } = string.Empty;

    private float _runningBalance { get; set; }

    private bool _showNextLedgerAvailablePosition { get; set; }

    protected override async Task OnInitializedAsync()
    {
        CanUpdate = true;

        if (Ledger is not null && Ledger.Count > 0)
        {
            _runningTotal = Ledger.Sum(l => l.AmountDue);

            _runningBalance = _runningTotal;

            foreach (var item in Ledger)
            {
                _runningBalance -= item.AmountDue;

                LedgerViewModel ledgerViewModel = new()
                {
                    Id = item.Id,
                    Position = item.Position,
                    AmountDue = item.AmountDue,
                    Balance = _runningBalance,
                    DateDue = item.DateDue,
                    DatePaid = item.DatePaid,
                    Status = item.Status
                };

                if (await ApiHelper.ExecuteCallGuardedAsync(async () => await InputOutputResourceClient.GetAsync(item.Id), Snackbar) is ICollection<InputOutputResourceDto> iOResources)
                {
                    if (iOResources != default)
                    {
                        if (iOResources.Count > 0)
                        {
                            var firstIOResource = iOResources.FirstOrDefault();

                            if (firstIOResource != default)
                            {
                                ledgerViewModel.ImageUrl = Config[ConfigNames.ApiBaseUrl] + firstIOResource.ImagePath;
                            }
                        }
                    }
                }

                LedgerModel.Add(ledgerViewModel);
            }
        }
    }
}

public class LedgerViewModel : LoanLedgerDto
{
    public float Balance { get; set; }

    public string? ImageUrl { get; set; }
}