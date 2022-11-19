using EHULOG.BlazorWebAssembly.Client.Components.Common;
using EHULOG.BlazorWebAssembly.Client.Components.Common.FileManagement;
using EHULOG.BlazorWebAssembly.Client.Components.Dialogs;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Common;
using EHULOG.BlazorWebAssembly.Client.Pages.Administration.User;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Geo.MapBox.Models.Responses;
using Mapster;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog.Loans.Components;

public partial class MinimalBlockInfoLoanStatus
{
    [CascadingParameter(Name = "AppDataService")]
    protected AppDataService AppDataService { get; set; } = default!;

    [Inject]
    protected ILoanApplicantsClient LoanApplicantsClient { get; set; } = default!;

    [Inject]
    protected ILoansClient LoansClient { get; set; } = default!;

    [Inject]
    protected ILoanLedgersClient LoanLedgersClient { get; set; } = default!;

    [Inject]
    protected IInputOutputResourceClient InputOutputResourceClient { get; set; } = default!;

    [Inject]
    protected IUsersClient UsersClient { get; set; } = default!;

    [Inject]
    protected IRatingsClient RatingsClient { get; set; } = default!;

    [Parameter]
    public LoanDto Loan { get; set; } = default!;

    [Parameter]
    public bool DisableDetailButton { get; set; } = false;

    [Parameter]
    public bool DisableViewButton { get; set; } = false;

    [Parameter]
    public bool IsLesseeCanApply { get; set; } = default!;

    [Parameter]
    public EventCallback<Guid> OnUpdatedLoan { get; set; } = default!;

    private FileUploadRequest FileUpload { get; set; } = default!;

    private string? _imageUrl;
    private Guid? _referenceIdOfActiveIOResource;
    private Guid _mainIdOfActiveIOResource;
    private bool _canUpdateRunningImage;
    private bool _canEnableRunningImage;

    private LoanLedgerDto? currentLedgerActivePayment;

    private RatingDto rating = new();

    protected override async Task OnInitializedAsync()
    {
        if (AppDataService != default)
        {
            if (AppDataService.AppUser != default)
            {
                if (Loan != default)
                {
                    IsLesseeCanApply = AppDataService.IsLesseeCanApply(Loan);

                    // image receipts
                    if (new[] { LoanStatus.Assigned, LoanStatus.Meetup, LoanStatus.Payment }.Contains(Loan.Status))
                    {
                        if (Loan.Ledgers != default)
                        {
                            await PrepareProcessReceiptAsync();
                        }
                    }

                    if (new[] { LoanStatus.Finish, LoanStatus.Rate, LoanStatus.RateFinal }.Contains(Loan.Status))
                    {
                        if (Loan.Ratings != default)
                        {
                            if (Loan.Ratings.Count > 0)
                            {
                                rating = Loan.Ratings.First();
                            }
                        }
                    }
                }
            }
        }
    }

    private async Task<LoanDto> GetLoanAsync(Guid loanId)
    {
        if (await ApiHelper.ExecuteCallGuardedAsync(() => LoansClient.GetAsync(loanId), Snackbar, null, "Success") is LoanDto loan)
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
                                loanApplicant.AppUser.ImageUrl = userDetailsDto.ImageUrl;
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
                                loanLessee.Lessee.ImageUrl = userDetailsDto.ImageUrl;
                            }
                        }
                    }
                }

                return loan;
            }

        }

        return default!;

    }

    protected async Task ApplyAsync()
    {
        if (AppDataService != default)
        {
            if (AppDataService.AppUser != default)
            {
                if (Loan != default)
                {
                    var createLoanApplicant = new CreateLoanApplicantRequest()
                    {
                        AppUserId = AppDataService.AppUser.Id,
                        LoanId = Loan.Id,
                        Flag = 0,
                        Reason = "Init"
                    };

                    if (await ApiHelper.ExecuteCallGuardedAsync(() => LoanApplicantsClient.CreateAsync(createLoanApplicant), Snackbar, null, "Success") is Guid loanApplicantId)
                    {
                        if (!string.IsNullOrEmpty(loanApplicantId.ToString()) && !loanApplicantId.Equals(Guid.Empty))
                        {
                            Snackbar.Add("Applied", Severity.Success);

                            Loan = await GetLoanAsync(Loan.Id);

                            // StateHasChanged();

                            await OnUpdatedLoan.InvokeAsync(Loan.Id);
                        }
                    }
                }
            }
        }
    }

    protected async Task ReApplyAsync()
    {
        if (Loan != default)
        {
            var updateLoanApplicant = new UpdateLoanApplicantRequest()
            {
                AppUserId = AppDataService.AppUser.Id,
                LoanId = Loan.Id,
                Flag = 0,
                Reason = "Reapply."
            };

            if (await ApiHelper.ExecuteCallGuardedAsync(() => LoanApplicantsClient.UpdateAsync(Loan.Id, AppDataService.AppUser.Id, updateLoanApplicant), Snackbar, null, "Success") is Guid loanApplicantId)
            {
                if (!string.IsNullOrEmpty(loanApplicantId.ToString()) && !loanApplicantId.Equals(Guid.Empty))
                {
                    Snackbar.Add("Reapplied", Severity.Success);

                    Loan = await GetLoanAsync(Loan.Id);

                    StateHasChanged();

                }
            }
        }
    }

    protected async Task CancelApplyAsync()
    {
        if (Loan != default)
        {
            var updateLoanApplicant = new UpdateLoanApplicantRequest()
            {
                AppUserId = AppDataService.AppUser.Id,
                LoanId = Loan.Id,
                Flag = 2,
                Reason = "Cancelled."
            };

            if (await ApiHelper.ExecuteCallGuardedAsync(() => LoanApplicantsClient.UpdateAsync(Loan.Id, AppDataService.AppUser.Id, updateLoanApplicant), Snackbar, null, "Success") is Guid loanApplicantId)
            {
                if (!string.IsNullOrEmpty(loanApplicantId.ToString()) && !loanApplicantId.Equals(Guid.Empty))
                {
                    Snackbar.Add("Cancelled. Remove application from this loan.", Severity.Success);

                    if (await ApiHelper.ExecuteCallGuardedAsync(() => LoansClient.GetAsync(Loan.Id), Snackbar, null, "Success") is LoanDto loan)
                    {
                        if (!string.IsNullOrEmpty(loanApplicantId.ToString()) && !loanApplicantId.Equals(Guid.Empty))
                        {
                            Snackbar.Add("Cancelled. Reapplication is available.", Severity.Success);

                            Loan = await GetLoanAsync(Loan.Id);

                            StateHasChanged();

                        }
                    }
                }
            }

        }
    }

    public async Task RemoveImageAsync()
    {
        string deleteContent = "You're sure you want to delete this meetup receipt image?";
        var parameters = new DialogParameters
        {
            { nameof(DeleteConfirmation.ContentText), deleteContent }
        };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true, DisableBackdropClick = true };
        var dialog = DialogService.Show<DeleteConfirmation>("Delete", parameters, options);
        var result = await dialog.Result;
        if (!result.Cancelled)
        {
            if (_referenceIdOfActiveIOResource != default)
            {
                var deletedGuid = await InputOutputResourceClient.DeleteAsync(_referenceIdOfActiveIOResource ?? default);

                if (deletedGuid != default)
                {
                    _imageUrl = default;
                }
            }
        }

        StateHasChanged();
    }

    private async Task UploadFileAsync(FileUploadRequest fileUpload)
    {
        if (fileUpload != default)
        {
            if (_referenceIdOfActiveIOResource != default)
            {
                CreateInputOutputResourceRequest createInputOutputResourceRequest = new CreateInputOutputResourceRequest
                {
                    ReferenceId = _referenceIdOfActiveIOResource ?? default,
                    Image = fileUpload,
                    InputOutputResourceDocumentType = InputOutputResourceDocumentType.None,
                    InputOutputResourceStatusType = InputOutputResourceStatusType.Disabled,
                    InputOutputResourceType = InputOutputResourceType.Ledger
                };

                var valueTupleOfGuidAndString = await InputOutputResourceClient.CreateAsync(createInputOutputResourceRequest);

                if (valueTupleOfGuidAndString != default)
                {
                    _imageUrl = string.IsNullOrEmpty(valueTupleOfGuidAndString.Value) ? string.Empty : (Config[ConfigNames.ApiBaseUrl] + valueTupleOfGuidAndString.Value);

                    if (AppDataService.AppUser != default)
                    {
                        if (currentLedgerActivePayment != default)
                        {
                            if (AppDataService.AppUser.RoleName != default)
                            {
                                if (AppDataService.AppUser.RoleName.Equals("Lender"))
                                {
                                    UpdateLoanLedgerRequest updateLoanLedgerRequest = new()
                                    {
                                        Id = currentLedgerActivePayment.Id,
                                        Position = currentLedgerActivePayment.Position,
                                        DatePaid = DateTime.UtcNow,
                                        LoanId = Loan.Id,
                                        LenderApprove = true,
                                        LesseeApprove = true,
                                        Remark = string.Empty,
                                        Status = LedgerStatus.LenderUpload
                                    };

                                    var loanLedgersClient = await LoanLedgersClient.UpdateAsync(currentLedgerActivePayment.Id, updateLoanLedgerRequest);
                                    if (loanLedgersClient != default)
                                    {
                                        // so that we can move on to the next
                                        Loan = await GetLoanAsync(Loan.Id);
                                        _canUpdateRunningImage = true;
                                        _canEnableRunningImage = true;
                                    }
                                }

                                if (AppDataService.AppUser.RoleName.Equals("Lessee"))
                                {
                                    UpdateLoanLedgerRequest updateLoanLedgerRequest = new()
                                    {
                                        Id = currentLedgerActivePayment.Id,
                                        Position = currentLedgerActivePayment.Position,
                                        DatePaid = DateTime.UtcNow,
                                        LoanId = Loan.Id,
                                        LenderApprove = false,
                                        LesseeApprove = true,
                                        Remark = string.Empty,
                                        Status = LedgerStatus.LesseeUpload
                                    };

                                    var loanLedgersClient = await LoanLedgersClient.UpdateAsync(currentLedgerActivePayment.Id, updateLoanLedgerRequest);
                                    if (loanLedgersClient != default)
                                    {
                                        _canUpdateRunningImage = true;

                                        // so that we can move on to the next
                                        Loan = await GetLoanAsync(Loan.Id);
                                    }
                                }
                            }
                        }

                    }

                }
            }
        }

        StateHasChanged();
    }

    // lender can enable the image
    protected async Task EnableImageAsync()
    {
        if (AppDataService != default)
        {
            if (AppDataService.AppUser != default)
            {
                if (AppDataService.AppUser.RoleName != default)
                {
                    if (AppDataService.AppUser.RoleName.Equals("Lender"))
                    {
                        UpdateInputOutputResourceByIdRequest updateInputOutputResourceByIdRequest = new()
                        {
                            Id = _mainIdOfActiveIOResource,
                            ImagePath = _imageUrl ?? string.Empty,
                            ResourceStatusType = InputOutputResourceStatusType.Enabled
                        };

                        if (await ApiHelper.ExecuteCallGuardedAsync(async () => await InputOutputResourceClient.UpdateAsync(_mainIdOfActiveIOResource, updateInputOutputResourceByIdRequest), Snackbar) is Guid guid)
                        {
                            if (guid != default)
                            {
                                // if io resource exists
                                _imageUrl = string.Empty;

                            }

                        }

                        // this is paid
                        if (currentLedgerActivePayment != default)
                        {
                            int totalLedgerRows = 0;

                            if (Loan.Ledgers != null)
                            {
                                totalLedgerRows = Loan.Ledgers.Count;
                            }

                            // first position
                            // change loan status
                            if (currentLedgerActivePayment.Position.Equals(0))
                            {
                                if (await ApiHelper.ExecuteCallGuardedAsync(
                                   async () => await LoansClient.UpdateStatusAsync(Loan.Id, new()
                                   {
                                       Id = Loan.Id,
                                       Status = LoanStatus.Payment
                                   }),
                                   Snackbar) is Guid loanId)
                                {
                                    if (loanId != default)
                                    {
                                        // loan updated
                                    }
                                }
                            }

                            // last position
                            else if (currentLedgerActivePayment.Position.Equals(totalLedgerRows - 1))
                            {
                                if (await ApiHelper.ExecuteCallGuardedAsync(
                                   async () => await LoansClient.UpdateStatusAsync(Loan.Id, new()
                                   {
                                       Id = Loan.Id,
                                       Status = LoanStatus.PaymentFinal
                                   }),
                                   Snackbar) is Guid loanId)
                                {
                                    if (loanId != default)
                                    {
                                        // loan updated
                                    }
                                }
                            }

                            UpdateLoanLedgerRequest updateLoanLedgerRequest = new()
                            {
                                Id = currentLedgerActivePayment.Id,
                                Position = currentLedgerActivePayment.Position,
                                DatePaid = DateTime.UtcNow,
                                LoanId = Loan.Id,
                                LenderApprove = true,
                                LesseeApprove = true,
                                Remark = string.Empty,
                                Status = LedgerStatus.Final
                            };

                            if (await ApiHelper.ExecuteCallGuardedAsync(
                                   async () => await LoanLedgersClient.UpdateAsync(currentLedgerActivePayment.Id, updateLoanLedgerRequest),
                                   Snackbar) is Guid loanLedger)
                            {
                                if (loanLedger != default)
                                {
                                    // loan updated
                                }
                            }

                            // move next
                            // activate next position
                            await PrepareProcessReceiptAsync();

                            Loan = await GetLoanAsync(Loan.Id);

                            await OnUpdatedLoan.InvokeAsync(Loan.Id);
                        }
                    }
                }
            }
        }

        StateHasChanged();
    }

    // prepare the access to actions for single file upload
    // check the images and role permissions
    protected async Task PrepareProcessReceiptAsync()
    {
        if (Loan != default)
        {
            if (Loan.Ledgers != default)
            {
                // default the reference to the current ledger
                // entry
                currentLedgerActivePayment = Loan.Ledgers.OrderBy(ledger => ledger.Position).FirstOrDefault(ledger => ledger.Status < LedgerStatus.Final);
                _referenceIdOfActiveIOResource = currentLedgerActivePayment != default ? currentLedgerActivePayment.Id : Guid.Empty;

                if (currentLedgerActivePayment != default)
                {
                    // check if file exists
                    if (await ApiHelper.ExecuteCallGuardedCustomSuppressAsync(() => InputOutputResourceClient.GetAsync(_referenceIdOfActiveIOResource ?? default), Snackbar, null) is List<InputOutputResourceDto> iOResources)
                    {
                        // might have multiple
                        if (iOResources != default && iOResources.Count() > 0)
                        {
                            _imageUrl = $"{Config[ConfigNames.ApiBaseUrl]}{iOResources.First().ImagePath}";
                            _mainIdOfActiveIOResource = iOResources.First().Id;

                            if (AppDataService.AppUser != default)
                            {
                                if (AppDataService.AppUser.RoleName != default)
                                {
                                    if (AppDataService.AppUser.RoleName.Equals("Lender") && currentLedgerActivePayment.Status <= LedgerStatus.LenderUpload)
                                    {
                                        _canUpdateRunningImage = true;
                                        _canEnableRunningImage = true;
                                    }

                                    if (AppDataService.AppUser.RoleName.Equals("Lessee") && currentLedgerActivePayment.Status <= LedgerStatus.LesseeUpload)
                                    {
                                        _canUpdateRunningImage = true;
                                    }
                                }
                            }

                            if (new LedgerStatus[] { LedgerStatus.Final, LedgerStatus.Finish }.ToList().Contains(currentLedgerActivePayment.Status ?? default))
                            {
                                _canUpdateRunningImage = false;
                                _canEnableRunningImage = false;
                            }
                        }

                        StateHasChanged();
                    }

                }
            }

        }
    }

    private async Task FinalizeAsync()
    {
        if (AppDataService != default)
        {
            if (AppDataService.IsLenderOfLoan(Loan))
            {
                if (await ApiHelper.ExecuteCallGuardedAsync(
                              async () => await LoansClient.UpdateStatusAsync(Loan.Id, new()
                              {
                                  Id = Loan.Id,
                                  Status = LoanStatus.Finish
                              }),
                              Snackbar) is Guid loanId)
                {
                    if (loanId != default)
                    {
                        if (await ApiHelper.ExecuteCallGuardedAsync(async () => await LoanLedgersClient.GetAsync(Loan.Id), Snackbar) is ICollection<LoanLedgerDto> loanLedger)
                        {
                            if (loanLedger != default)
                            {
                                foreach (var ledger in loanLedger)
                                {

                                    UpdateLoanLedgerRequest updateLoanLedgerRequest = ledger.Adapt<UpdateLoanLedgerRequest>();
                                    updateLoanLedgerRequest.Status = LedgerStatus.Finish;

                                    if (await ApiHelper.ExecuteCallGuardedAsync(async () => await LoanLedgersClient.UpdateAsync(ledger.Id, updateLoanLedgerRequest), Snackbar) is Guid ledgerId)
                                    {
                                        if (ledgerId != default)
                                        {

                                        }
                                    }
                                }
                            }
                        }

                        await OnUpdatedLoan.InvokeAsync(Loan.Id);
                    }
                }
            }
        }
    }

    private async void HandleValidRatingSubmit(EditContext context)
    {
        if (AppDataService != default)
        {
            if (AppDataService.AppUser != default)
            {
                if (Loan != default)
                {
                    if (context != default)
                    {
                        if (rating != default)
                        {
                            bool isNew = false;

                            if (rating.LesseeId.Equals(default) && rating.LenderId.Equals(default))
                            {
                                isNew = true;
                            }

                            rating.LoanId = Loan.Id;

                            if (AppDataService.IsLenderOfLoan(Loan))
                            {
                                rating.LenderId = AppDataService.AppUser.Id;
                            }

                            if (AppDataService.IsLesseeOfLoan(Loan))
                            {
                                rating.LesseeId = AppDataService.AppUser.Id;
                            }

                            if (isNew)
                            {
                                CreateRatingRequest createRatingRequest = rating.Adapt<CreateRatingRequest>();

                                if (await ApiHelper.ExecuteCallGuardedAsync(async () => await RatingsClient.CreateAsync(createRatingRequest), Snackbar) is Guid ratingGuid)
                                {
                                    if (ratingGuid != default)
                                    {
                                        // updated rating
                                        UpdateLoanStatusRequest updateLoanStatusRequest = new()
                                        {
                                            Id = Loan.Id,
                                            Status = LoanStatus.Rate
                                        };

                                        if (await ApiHelper.ExecuteCallGuardedAsync(async () => await LoansClient.UpdateStatusAsync(Loan.Id, updateLoanStatusRequest), Snackbar) is Guid loanId)
                                        {
                                            if (await ApiHelper.ExecuteCallGuardedAsync(async () => await RatingsClient.GetAsync(Loan.Id), Snackbar) is RatingDto rating)
                                            {
                                                this.rating = rating;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                UpdateRatingRequest updateRatingRequest = rating.Adapt<UpdateRatingRequest>();

                                if (await ApiHelper.ExecuteCallGuardedAsync(async () => await RatingsClient.UpdateAsync(Loan.Id, updateRatingRequest), Snackbar) is Guid ratingGuid)
                                {
                                    if (ratingGuid != default)
                                    {
                                        // updated rating
                                        UpdateLoanStatusRequest updateLoanStatusRequest = new()
                                        {
                                            Id = Loan.Id,
                                            Status = LoanStatus.Rate
                                        };

                                        if (await ApiHelper.ExecuteCallGuardedAsync(async () => await LoansClient.UpdateStatusAsync(Loan.Id, updateLoanStatusRequest), Snackbar) is Guid loanId)
                                        {
                                            if (await ApiHelper.ExecuteCallGuardedAsync(async () => await RatingsClient.GetAsync(Loan.Id), Snackbar) is RatingDto rating)
                                            {
                                                this.rating = rating;
                                            }
                                        }
                                    }
                                }

                            }

                        }
                    }

                    // check if both has lender and lessee rated
                    // if so, update status
                    if (AppDataService.HasRatedBothLenderLesseeHelper(Loan))
                    {
                        // updated rating
                        UpdateLoanStatusRequest updateLoanStatusRequest = new()
                        {
                            Id = Loan.Id,
                            Status = LoanStatus.RateFinal
                        };

                        if (await ApiHelper.ExecuteCallGuardedAsync(async () => await LoansClient.UpdateStatusAsync(Loan.Id, updateLoanStatusRequest), Snackbar) is Guid loanId)
                        {
                            // loan updated
                        }
                    }

                    await OnUpdatedLoan.InvokeAsync(Loan.Id);

                }

            }
        }


    }

}
