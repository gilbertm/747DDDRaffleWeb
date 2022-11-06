using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Microsoft.AspNetCore.Components;
using Nager.Country;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog.Loans.Components;

public partial class CreateUpdateLoan
{
    [Parameter]
    public LoanDto Loan { get; set; } = default!;
    [Parameter]
    public bool CanUpdate { get; set; } = default!;
    [CascadingParameter(Name = "AppDataService")]
    public AppDataService AppDataService { get; set; } = default!;
    [Inject]
    protected IUsersClient UsersClient { get; set; } = default!;
    [Inject]
    protected IAppUserProductsClient AppUserProductsClient { get; set; } = default!;
    [Inject]
    protected IInputOutputResourceClient InputOutputResourceClient { get; set; } = default!;
    [Inject]
    protected ILoanLedgersClient LoanLedgersClient { get; set; } = default!;
    private LoanViewModel Model { get; set; } = default!;
    private List<AppUserProductDto> AppUserProducts { get; set; } = default!;
    private List<TemporaryLedgerTableElement> TemporaryLedgerTable { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await AppDataService.InitializationAsync();

        AppUserProducts = (await AppUserProductsClient.GetByAppUserIdAsync(AppDataService.AppUser.Id)).ToList();

        if (AppUserProducts.Count > 0)
        {
            foreach (var item in AppUserProducts)
            {
                if (item.Product is not null)
                {
                    var image = await InputOutputResourceClient.GetAsync(item.Product.Id);

                    if (image.Count() > 0)
                    {

                        item.Product.Image = image.First();
                    }
                }
            }
        }
    }

    protected class LoanViewModel : UpdateLoanRequest
    {
        public Guid ProductId { get; set; }

        public ProductDto Product { get; set; } = new();
    }

    protected class TemporaryLedgerTableElement
    {
        public int Position { get; set; }
        public DateTime Due { get; set; }
        public float Amount { get; set; }
        public float Balance { get; set; }
    }

    private bool _disabledInterestSelection { get; set; } = false;

    // private string _infoCollateralGeneric = "Anything of value (goods, items, memoirs, collector's items, etc) that would encourage the Lender to consider you recipient of the hard earned money.";
    // private string _infoCollateralGenericHelpText = "Collateral adds more security from the lender. This info can contain required information that MUST be fulfilled by the lessee or applicants.";

    private bool IsCollateralChange
    {
        get
        {
            return Model.IsCollateral ?? false;
        }
        set
        {
            Model.IsCollateral = value;

            StateHasChanged();
        }
    }

    private int MonthsToPay
    {
        get
        {
            return Model.Month;
        }
        set
        {
            Model.Month = value;

            Task.Run(async () => await HandleInterest());
        }
    }

    private float ChangeInterest
    {
        get
        {
            return Model.Interest;
        }
        set
        {
            Model.Interest = value;

            Task.Run(async () => await HandleInterest());
        }
    }

    private InterestType ChangeInterestType
    {
        get
        {
            return Model.InterestType;
        }
        set
        {
            Model.InterestType = value;

            _disabledInterestSelection = false;

            if (value.Equals(InterestType.Zero))
            {
                _disabledInterestSelection = true;
            }

            Task.Run(async () => await HandleInterest());
        }
    }

    private async Task HandleInterest()
    {
        // update can only be allowed, if the following
        // 1. not a running loan
        // 2. owner has still business logic package - slots
        // 3. published with applicants
        // 4. published with lender
        if (!CanUpdate)
            return;

        // changing values of
        // the selected variable used on selects
        // triggers dynamic changes on the dropdown selected value
        if (Model.InterestType == InterestType.Zero)
        {
            Model.Interest = 0.0f;
        }
        else
        {
            // default to 5%, if number is weird and below 0
            if (Model.Interest <= 0.0f)
            {
                Model.Interest = 5.0f;
            }

        }

        GetLoanLedgerMemRequest getLoanLedgerMemRequest = new()
        {
            Amount = Model.Product.Amount,
            Interest = Model.Interest,
            InterestType = Model.InterestType,
            Month = Model.Month,
            StartOfPayment = Model.StartOfPayment
        };

        if (await ApiHelper.ExecuteCallGuardedAsync(
        () => LoanLedgersClient.GetTemporaryLedgerAsync(getLoanLedgerMemRequest), Snackbar)
        is string resultDict)
        {
            TemporaryLedgerTable.Clear();

            if (!string.IsNullOrEmpty(resultDict))
            {
                var dictionary = System.Text.Json.JsonSerializer.Deserialize<Dictionary<int, Dictionary<string, object>>>(resultDict);

                if (dictionary != default && dictionary.Count > 0)
                {
                    foreach (var item in dictionary)
                    {
                        float amountDue = 0.00f;
                        float balance = 0.00f;
                        DateTime dateDue = DateTime.UtcNow;

                        foreach (var kv in item.Value)
                        {
                            switch (kv.Key)
                            {
                                case "AmountDue":
                                    amountDue = Convert.ToSingle(kv.Value.ToString());
                                    break;
                                case "Balance":
                                    balance = Convert.ToSingle(kv.Value.ToString());
                                    break;
                                case "DateDue":
                                    dateDue = Convert.ToDateTime(kv.Value.ToString());
                                    break;
                            }
                        }

                        TemporaryLedgerTable.Add(new TemporaryLedgerTableElement()
                        {
                            Position = item.Key,
                            Due = dateDue,
                            Amount = amountDue,
                            Balance = balance
                        });
                    }
                }
            }
        }

        StateHasChanged();
    }

    public string DateIsNewlyCreated(DateTime? date = null)
    {
        if (date is null)
        {
            date = DateTime.Now.Date;
        }

        return string.Format("{0:dddd, MMMM d, yyyy}", date);
    }

    public bool DisableOldDates(DateTime val)
    {
        if (val < DateTime.Now.AddDays(-1))
        {
            return true;
        }

        return false;
    }

    public void StartDatePayment(DateTime? dateTime)
    {
        if (dateTime != default && dateTime != null)
        {
            Model.StartOfPayment = (DateTime)dateTime;
        }
    }

    public void AutocompleteProductChange(Guid id)
    {
        if (!CanUpdate)
            return;

        var userProduct = AppUserProducts.Where(ap => (ap.Product != default) && ap.Product.Id.Equals(id)).FirstOrDefault();

        if (userProduct != default)
        {
            Model.ProductId = default!;
            Model.Product = new();
            Model.Product.Image = new();
            Model.Product.Brand = new();
            Model.Product.Category = new();

            if (userProduct.Product != default)
            {
                Model.ProductId = userProduct.Product.Id;
                Model.Product.Id = userProduct.Product.Id;
                Model.Product.Name = userProduct.Product.Name;
                Model.Product.Amount = userProduct.Product.Amount;

                if (userProduct.Product.Category != default)
                {
                    Model.Product.Category = userProduct.Product.Category;
                }

                if (userProduct.Product.Brand != default)
                {
                    Model.Product.Brand = userProduct.Product.Brand;
                }

                if (userProduct.Product.Image != default)
                {
                    Model.Product.Image = userProduct.Product.Image;
                }

                Task.Run(async () => await HandleInterest());

            }
        }
    }
}
