using EHULOG.BlazorWebAssembly.Client.Components.Common;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Common;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Nager.Country;
using static MudBlazor.CategoryTypes;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog.Loans;

public partial class TheLoan
{
    [Inject]
    protected AppDataService AppDataService { get; set; } = default!;
    [Inject]
    protected IUsersClient UsersClient { get; set; } = default!;
    [Inject]
    protected IAppUserProductsClient AppUserProductsClient { get; set; } = default!;
    [Inject]
    protected ILoansClient LoansClient { get; set; } = default!;

    [Inject]
    protected IInputOutputResourceClient InputOutputResourceClient { get; set; } = default!;

    private LoanViewModel RequestModel { get; set; } = new();

    private LoanDto Loan { get; set; } = default!;

    private string Currency { get; set; } = default!;

    private bool CanUpdate { get; set; } = default!;

    [Parameter]
    public Guid? loanId { get; set; }

    public async Task OnClickChildComponent(Guid? loanId)
    {
        if (loanId.HasValue && loanId != default)
        {
            Loan = await LoadLoan(loanId.Value);
        }

        StateHasChanged();
    }

    protected override async Task OnInitializedAsync()
    {
        await AppDataService.InitializationAsync();

        if (AppDataService != default)
        {
            if (AppDataService.AppUser != default)
            {
                if (loanId.HasValue && loanId != default)
                {
                    Loan = await LoadLoan(loanId.Value);

                    if (Loan != default)
                    {
                        if (Loan.LoanLenders != default)
                        {
                            LoanLenderDto? loanLender = Loan.LoanLenders.FirstOrDefault(l => l.LoanId.Equals(Loan.Id)) ?? default;

                            if (loanLender != default && loanLender.Product != default)
                            {
                                var image = await InputOutputResourceClient.GetAsync(loanLender.ProductId);

                                if (image.Count > 0)
                                {
                                    loanLender.Product.Image = image.First();
                                }
                            }
                        }

                        if (Loan.LoanApplicants != default)
                        {

                            if (Loan.LoanApplicants.Count > 0)
                            {
                                foreach (var loanApplicant in Loan.LoanApplicants)
                                {
                                    if (loanApplicant.AppUser != default)
                                    {
                                        var userDetailsDto = await UsersClient.GetByIdAsync(loanApplicant.AppUser.ApplicationUserId);

                                        loanApplicant.AppUser.FirstName = userDetailsDto.FirstName;
                                        loanApplicant.AppUser.LastName = userDetailsDto.LastName;
                                        loanApplicant.AppUser.Email = userDetailsDto.Email;
                                        loanApplicant.AppUser.PhoneNumber = userDetailsDto.PhoneNumber;
                                        loanApplicant.AppUser.ImageUrl = $"{Config[ConfigNames.ApiBaseUrl]}{userDetailsDto.ImageUrl}";
                                    }
                                }
                            }
                        }

                        // show products, if lender
                        if (!string.IsNullOrEmpty(AppDataService.AppUser.RoleName) && AppDataService.AppUser.RoleName.Equals("Lender"))
                        {
                            CanUpdate = AppDataService.IsLenderCanUpdateLoan(Loan);
                        }
                        else if (!string.IsNullOrEmpty(AppDataService.AppUser.RoleName) && AppDataService.AppUser.RoleName.Equals("Lessee"))
                        {
                            // can participate
                            // in updating the loan
                            // more on
                            // ledger updates
                            // rating
                            CanUpdate = AppDataService.IsLesseeOfThisLoan(Loan);
                        }
                    }
                }
            }
        }
    }

    private async Task<LoanDto> LoadLoan(Guid loanId)
    {
        if (await ApiHelper.ExecuteCallGuardedAsync(
                                                async () => await LoansClient.GetAsync(loanId),
                                                Snackbar,
                                                null) is LoanDto loan)
        {
            if (loan != default)
            {
                if (loan.LoanApplicants != default)
                {
                    if (loan.LoanApplicants.Count > 0)
                    {
                        foreach (var loanApplicant in loan.LoanApplicants)
                        {
                            if (loanApplicant.AppUser != default)
                            {
                                var userDetailsDto = await UsersClient.GetByIdAsync(loanApplicant.AppUser.ApplicationUserId);

                                loanApplicant.AppUser.FirstName = userDetailsDto.FirstName;
                                loanApplicant.AppUser.LastName = userDetailsDto.LastName;
                                loanApplicant.AppUser.Email = userDetailsDto.Email;
                                loanApplicant.AppUser.PhoneNumber = userDetailsDto.PhoneNumber;
                            }
                        }
                    }
                }

                if (loan.LoanLessees != default)
                {
                    if (loan.LoanLessees.Count > 0)
                    {
                        foreach (var loanLessee in loan.LoanLessees)
                        {
                            if (loanLessee.Lessee != default)
                            {
                                var userDetailsDto = await UsersClient.GetByIdAsync(loanLessee.Lessee.ApplicationUserId);

                                loanLessee.Lessee.FirstName = userDetailsDto.FirstName;
                                loanLessee.Lessee.LastName = userDetailsDto.LastName;
                                loanLessee.Lessee.Email = userDetailsDto.Email;
                                loanLessee.Lessee.PhoneNumber = userDetailsDto.PhoneNumber;
                            }
                        }
                    }
                }

                return loan;
            }
        }

        return default!;
    }
}