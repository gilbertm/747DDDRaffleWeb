using EHULOG.BlazorWebAssembly.Client.Components.Common;
using EHULOG.BlazorWebAssembly.Client.Components.Dialogs;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Auth;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Mapster;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using static MudBlazor.CategoryTypes;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog.Loans;

public partial class SpecificLoanApplicants
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthenticationService AuthService { get; set; } = default!;

    [Parameter]
    public List<LoanApplicantDtoVM> Applicants { get; set; } = default!;

    [Parameter]
    public bool IsPossibleToAppy { get; set; } = false;

    [Parameter]
    public bool IsOwner { get; set; } = false;

    [Parameter]
    public bool IsMinimal { get; set; } = false;

    [Parameter]
    public Guid LoanId { get; set; }

    [Inject]
    protected AppDataService AppDataService { get; set; } = default!;
    [Inject]
    protected IPersonalClient PersonalClient { get; set; } = default!;

    [Inject]
    protected IUsersClient UsersClient { get; set; } = default!;

    [Inject]
    protected ILoanApplicantsClient LoanApplicantsClient { get; set; } = default!;


    private AppUserDto _appUserDto { get; set; } = default!;

    private LoanApplicantDto? _loanApplicantDto { get; set; }

    [Parameter]
    public EventCallback OnClick { get; set; }

    [Inject]
    protected IDialogService Dialog { get; set; } = default!;

    private CustomValidation? _customValidation;

    protected override async Task OnInitializedAsync()
    {
        _appUserDto = await AppDataService.Start();

        if (Applicants is not null && Applicants.Count() > 0)
        {
            foreach (var item in Applicants)
            {
                // initially loaded from fragments
                // just reload, as per record info.
                // else loaded fully. see specific loan for fully loaded.
                if (string.IsNullOrEmpty(item.Email))
                {
                    if (await ApiHelper.ExecuteCallGuardedAsync(async () => await UsersClient.GetByIdAsync(item.AppUser.ApplicationUserId), Snackbar) is UserDetailsDto userDetails)
                    {
                        item.Email = userDetails.Email ?? string.Empty;
                        item.FirstName = userDetails.FirstName ?? string.Empty;
                        item.LastName = userDetails.LastName ?? string.Empty;
                        item.PhoneNumber = userDetails.PhoneNumber ?? string.Empty;
                        item.ImageURL = userDetails.ImageUrl ?? string.Empty;
                    }
                }
            }
        }
    }

    private async Task LesseeLoanApply()
    {
        if (_appUserDto is { } && (!string.IsNullOrEmpty(LoanId.ToString()) && !LoanId.Equals(Guid.Empty)))
        {
            var createLoanApplicant = new CreateLoanApplicantRequest()
            {
                AppUserId = _appUserDto.Id,
                LoanId = LoanId,
                Flag = 0,
                Reason = "Init"
            };

            if (await ApiHelper.ExecuteCallGuardedAsync(
            () => LoanApplicantsClient.CreateAsync(createLoanApplicant), Snackbar) is Guid loanApplicantId)
            {
                if (!string.IsNullOrEmpty(loanApplicantId.ToString()) && !loanApplicantId.Equals(Guid.Empty))
                {
                    Snackbar.Add("Applied", Severity.Success);
                }
            }
        }

    }

    private void OpenLendersUserInspectionView(LoanApplicantDtoVM loanApplicantDtoVM)
    {
        var parameters = new DialogParameters { ["loanApplicantDtoVM"] = loanApplicantDtoVM };

        DialogOptions noHeader = new DialogOptions() { CloseButton = true };
        Dialog.Show<LendersUserInspectionView>("User's Details", parameters, noHeader);
    }
}