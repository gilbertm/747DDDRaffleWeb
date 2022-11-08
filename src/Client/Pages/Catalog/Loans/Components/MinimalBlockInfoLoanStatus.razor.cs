using EHULOG.BlazorWebAssembly.Client.Components.Common.FileManagement;
using EHULOG.BlazorWebAssembly.Client.Components.Dialogs;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Common;
using EHULOG.BlazorWebAssembly.Client.Pages.Administration.User;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Geo.MapBox.Models.Responses;
using Microsoft.AspNetCore.Components;
using MudBlazor;
namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog.Loans.Components;

public partial class MinimalBlockInfoLoanStatus
{
    [CascadingParameter(Name = "AppDataService")]
    protected AppDataService AppDataService { get; set; } = default!;

    [Inject]
    protected ILoanApplicantsClient LoanApplicantsClient { get; set; } = default!;

    [Inject]
    protected ILoansClient ILoansClient { get; set; } = default!;

    [Inject]
    protected IInputOutputResourceClient InputOutputResourceClient { get; set; } = default!;

    [Inject]
    protected IUsersClient UsersClient { get; set; } = default!;

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
                            switch (Loan.Status)
                            {
                                case LoanStatus.Assigned:
                                case LoanStatus.Meetup:
                                    var iORMeetupId = Loan.Ledgers.OrderBy(ledger => ledger.Position).First(ledger => ledger.Position.Equals(0)).Id;
                                    _referenceIdOfActiveIOResource = iORMeetupId;

                                    if (await ApiHelper.ExecuteCallGuardedCustomSuppressAsync(() => InputOutputResourceClient.GetAsync(iORMeetupId), Snackbar, null, "Meetup receipt found.") is List<InputOutputResourceDto> iORMeetups)
                                    {
                                        if (iORMeetups != default && iORMeetups.Count() > 0)
                                        {
                                            _imageUrl = $"{Config[ConfigNames.ApiBaseUrl]}{iORMeetups[0].ImagePath}";
                                            _mainIdOfActiveIOResource = iORMeetups[0].Id;

                                            switch (iORMeetups[0].ResourceStatusType)
                                            {
                                                case InputOutputResourceStatusType.Disabled:

                                                    if (AppDataService.AppUser != default)
                                                    {
                                                        if (AppDataService.AppUser.RoleName != default)
                                                        {
                                                            if (AppDataService.AppUser.RoleName.Equals("Lender"))
                                                            {
                                                                _canUpdateRunningImage = true;
                                                                _canEnableRunningImage = true;
                                                            }

                                                            if (AppDataService.AppUser.RoleName.Equals("Lessee"))
                                                            {
                                                                _canUpdateRunningImage = true;
                                                            }
                                                        }
                                                    }

                                                    break;

                                                case InputOutputResourceStatusType.Enabled:
                                                    _canUpdateRunningImage = false;
                                                    _canEnableRunningImage = false;
                                                    break;
                                            }
                                        }
                                    }

                                    break;
                                case LoanStatus.Payment:
                                    // process or get the last position
                                    break;
                            }
                        }
                    }
                }

                /*if (AppDataService.AppUser.ApplicationUserId != default)
                {
                    *//* current uploaded but not verified, this happens when the user uploads files *//*
                    Guid guidUserId = default!;
                    Guid.TryParse(AppDataService.AppUser.ApplicationUserId, out guidUserId);
                    var referenceIdIOResources = await InputOutputResourceClient.GetAsync(guidUserId);

                    if (referenceIdIOResources is not null)
                    {
                        foreach (var ior in referenceIdIOResources)
                        {
                            if (!ior.ResourceType.Equals(InputOutputResourceType.Identification) || ior.ResourceDocumentType.Equals(InputOutputResourceDocumentType.None))
                                continue;

                            ForUploadFile = new ForUploadFile()
                            {
                                FileIdentifier = ior.ResourceDocumentType,
                                InputOutputResourceId = ior.Id.ToString(),
                                UserIdReferenceId = ior.ReferenceId.ToString(),
                                InputOutputResourceImgUrl = ior.ImagePath,
                                isVerified = ior.ResourceStatusType.Equals(InputOutputResourceStatusType.EnabledAndVerified) ? true : false,
                                isTemporarilyUploaded = true,
                                Opacity = new[] { InputOutputResourceDocumentType.Passport, InputOutputResourceDocumentType.NationalId, InputOutputResourceDocumentType.GovernmentId }.Contains(ior.ResourceDocumentType) ? "1" : "0.3",
                                Disabled = new[] { InputOutputResourceDocumentType.Passport, InputOutputResourceDocumentType.NationalId, InputOutputResourceDocumentType.GovernmentId }.Contains(ior.ResourceDocumentType) ? false : true
                            };
                        }
                    }

                    ForUploadFiles = ForUploadFiles.GroupBy(f => f.FileIdentifier).Select(f => f.First()).ToList();

                    foreach (int i in Enum.GetValues(typeof(InputOutputResourceDocumentType)))
                    {
                        if (((InputOutputResourceDocumentType)i).Equals(InputOutputResourceDocumentType.None))
                            continue;

                        if (ForUploadFiles.Where(forUpload => forUpload.FileIdentifier.Equals((InputOutputResourceDocumentType)i)).Count() > 0)
                            continue;

                        ForUploadFiles.Add(new ForUploadFile()
                        {
                            FileIdentifier = (InputOutputResourceDocumentType)i,
                            InputOutputResourceId = string.Empty,
                            UserIdReferenceId = AppDataService.AppUser.ApplicationUserId,
                            InputOutputResourceImgUrl = string.Empty,
                            isVerified = false,
                            isTemporarilyUploaded = false,
                            Opacity = new[] { InputOutputResourceDocumentType.Passport, InputOutputResourceDocumentType.NationalId, InputOutputResourceDocumentType.GovernmentId }.Contains((InputOutputResourceDocumentType)i) ? "1" : "0.3",
                            Disabled = new[] { InputOutputResourceDocumentType.Passport, InputOutputResourceDocumentType.NationalId, InputOutputResourceDocumentType.GovernmentId }.Contains((InputOutputResourceDocumentType)i) ? false : true
                        });

                    }
                }
*/
            }
        }
    }

    private async Task<LoanDto> GetLoanAsync(Guid loanId)
    {
        if (await ApiHelper.ExecuteCallGuardedAsync(() => ILoansClient.GetAsync(loanId), Snackbar, null, "Success") is LoanDto loan)
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

    protected async Task Apply()
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

    protected async Task ReApply()
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

    protected async Task CancelApply()
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

                    if (await ApiHelper.ExecuteCallGuardedAsync(() => ILoansClient.GetAsync(Loan.Id), Snackbar, null, "Success") is LoanDto loan)
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
                }

                StateHasChanged();
            }
        }
    }

    protected async Task EnableImageAsync()
    {
        var loanId = Loan.Id;
        Loan = default!;

        UpdateInputOutputResourceByIdRequest updateInputOutputResourceByIdRequest = new()
        {
            Id = _mainIdOfActiveIOResource,
            ImagePath = _imageUrl ?? default!,
            ResourceStatusType = InputOutputResourceStatusType.Enabled
        };

        var guid = await InputOutputResourceClient.UpdateAsync(_mainIdOfActiveIOResource, updateInputOutputResourceByIdRequest);
        if (guid != default)
        {

            _canUpdateRunningImage = false;
            _canEnableRunningImage = false;

            // this is paid

            // force reload
            Loan = await GetLoanAsync(loanId);

        }

        StateHasChanged();
    }
}
