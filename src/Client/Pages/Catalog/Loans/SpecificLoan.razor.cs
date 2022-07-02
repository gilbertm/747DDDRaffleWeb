﻿using EHULOG.BlazorWebAssembly.Client.Components.Common;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using static MudBlazor.CategoryTypes;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog.Loans;

public partial class SpecificLoan
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthorizationService AuthService { get; set; } = default!;
    [Inject]
    protected IUsersClient UsersClient { get; set; } = default!;
    [Inject]
    protected IInputOutputResourceClient InputOutputResourceClient { get; set; } = default!;
    [Inject]
    protected ILoansClient LoansClient { get; set; } = default!;
    [Inject]
    protected ILoanLedgersClient LoanLedgersClient { get; set; } = default!;
    [Inject]
    protected IAppUserProductsClient AppUserProductsClient { get; set; } = default!;
    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;
    [Inject]
    protected AppDataService AppDataService { get; set; } = default!;

    private LoanViewModel RequestModel { get; set; } = new();

    // lender
    private bool _canUpdate { get; set; } = false;

    // lessee
    private bool _canUpdateLedger { get; set; } = false;

    // lessee
    private bool _isPossibleToAppy { get; set; } = false;

    private AppUserDto _appUserDto { get; set; } = default!;

    private List<ForUploadFile> ForUploadFiles { get; set; } = new();

    private List<AppUserProductDto> appUserProducts { get; set; } = default!;

    private CustomValidation? _customValidation;

    public async Task OnClickChildComponent(Guid? loanId)
    {
        await Update(loanId);

        StateHasChanged();
    }

    protected override async Task OnInitializedAsync()
    {
        _appUserDto = await AppDataService.Start();

        if (_appUserDto is not null)
        {
            // show products, if lender
            if (!string.IsNullOrEmpty(_appUserDto.RoleName) && _appUserDto.RoleName.Equals("Lender"))
            {
                appUserProducts = (await AppUserProductsClient.GetByAppUserIdAsync(_appUserDto.Id)).ToList();

                if (appUserProducts.Count() > 0)
                {
                    foreach (var item in appUserProducts)
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

            await Update(loanId);

           }
    }

    private async Task Update(Guid? loanId)
    {
        if (loanId.HasValue)
        {
            if (await ApiHelper.ExecuteCallGuardedAsync(
                                            async () => await LoansClient.GetAsync(loanId.Value),
                                            Snackbar,
                                            _customValidation) is LoanDto loanDto)
            {

                if (loanDto is { })
                {
                    RequestModel.Id = loanDto.Id;

                    // lender
                    if (loanDto.LoanLenders is not null && loanDto.LoanLenders.Count() > 0)
                    {
                        var loanLender = loanDto.LoanLenders.Where(ll => ll.LoanId.Equals(loanDto.Id)).First();

                        RequestModel.Product = loanLender.Product is not null ? loanLender.Product : default!;
                        RequestModel.ProductId = !loanLender.ProductId.Equals(Guid.Empty) ? loanLender.ProductId : default;

                        if (_appUserDto.RoleName is not null && _appUserDto.RoleName.Equals("Lender"))
                        {
                            // owner
                            if (loanLender.Lender is not null && loanLender.LenderId.Equals(_appUserDto.Id))
                            {
                                _canUpdate = true;
                                _canUpdateLedger = true;
                            }

                        }

                        // get the product image
                        var image = await InputOutputResourceClient.GetAsync(RequestModel.ProductId);

                        if (image.Count() > 0)
                        {
                            RequestModel.Product.Image = image.First();
                        }
                    }

                    // lessee
                    if (loanDto.LoanLessees is not null && loanDto.LoanLessees.Count() > 0)
                    {
                        if (_appUserDto.RoleName is not null && _appUserDto.RoleName.Equals("Lessee"))
                        {
                            var loanLessee = loanDto.LoanLessees.Where(ll => ll.LoanId.Equals(loanDto.Id)).First();

                            if (loanLessee.Lessee is not null && loanLessee.LesseeId.Equals(_appUserDto.Id))
                            {
                                _canUpdateLedger = true;
                            }
                        }
                    }
                    else if (loanDto.LoanLessees is null || loanDto.LoanLessees.Count() <= 0)
                    {
                        if (_appUserDto.RoleName is not null && _appUserDto.RoleName.Equals("Lessee"))
                        {
                            _isPossibleToAppy = true;
                        }
                    }

                    if (loanDto.LoanApplicants is { })
                    {
                        if (loanDto.LoanApplicants.Count() > 0)
                        {
                            foreach (var loanApplicantDto in loanDto.LoanApplicants)
                            {
                                var userDetailsDto = await UsersClient.GetByIdAsync(loanApplicantDto.AppUser.ApplicationUserId);

                                loanApplicantDto.AppUser.FirstName = userDetailsDto.FirstName;
                                loanApplicantDto.AppUser.LastName = userDetailsDto.LastName;
                                loanApplicantDto.AppUser.Email = userDetailsDto.Email;
                                loanApplicantDto.AppUser.PhoneNumber = userDetailsDto.PhoneNumber;

                            }
                        }
                    }

                    RequestModel.InfoCollateral = loanDto.InfoCollateral;
                    RequestModel.IsCollateral = loanDto.IsCollateral;
                    RequestModel.Interest = loanDto.Interest;
                    RequestModel.InterestType = loanDto.InterestType;
                    RequestModel.Status = loanDto.Status;
                    RequestModel.Month = loanDto.Month;
                    RequestModel.StartOfPayment = loanDto.StartOfPayment;

                    RequestModel.LoanApplicants = loanDto.LoanApplicants;
                    RequestModel.Ledgers = loanDto.Ledgers;
                    RequestModel.LoanLessees = loanDto.LoanLessees;
                }

               
            }
        }
    }
}

public class LoanViewModel : UpdateLoanRequest
{
    public Guid ProductId { get; set; }

    public ProductDto Product { get; set; } = new();
}

public class TemporaryLedgerTableElement
{
    public int Position { get; set; }
    public DateTime Due { get; set; }
    public float Amount { get; set; }
    public float Balance { get; set; }
}