using EHULOG.BlazorWebAssembly.Client.Components.Common;
using EHULOG.BlazorWebAssembly.Client.Components.Dialogs;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Auth;
using EHULOG.BlazorWebAssembly.Client.Pages.Identity.Users;
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
    public ICollection<LoanApplicantDto> Applicants { get; set; } = default!;

    public List<LoanApplicantDtoVM>? _applicants { get; set; }

    [Parameter]
    public bool IsPossibleToAppy { get; set; } = false;

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

        if (_appUserDto is not null)
        {
            // applied
            if ((Applicants.Count() > 0) && (Applicants.Where(a => a.AppUser.Id.Equals(_appUserDto.Id)).SingleOrDefault() != null))
            {
                _loanApplicantDto = Applicants?.Where(a => a.AppUser.Id.Equals(_appUserDto.Id)).Single();
            }
        }

        if (Applicants is not null && Applicants.Count() > 0)
        {
            _applicants = new List<LoanApplicantDtoVM>();

            foreach (var item in Applicants)
            {
                // load their profile images for display.
                if (await ApiHelper.ExecuteCallGuardedAsync(() => UsersClient.GetByIdAsync(item.AppUser.ApplicationUserId), Snackbar, _customValidation) is UserDetailsDto userDetails)
                {
                    var loanApplicationDtoVM = userDetails.Adapt<LoanApplicantDtoVM>();
                    loanApplicationDtoVM.AppUser = item.AppUser;
                    loanApplicationDtoVM.Email = userDetails.Email ?? string.Empty;
                    loanApplicationDtoVM.FirstName = userDetails.FirstName ?? string.Empty;
                    loanApplicationDtoVM.LastName = userDetails.LastName ?? string.Empty;
                    loanApplicationDtoVM.PhoneNumber = userDetails.PhoneNumber ?? string.Empty;
                    loanApplicationDtoVM.ImageURL = userDetails.ImageUrl ?? string.Empty;

                    _applicants.Add(loanApplicationDtoVM);
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

public class LoanApplicantDtoVM : LoanApplicantDto
{
    public string Email { get; set; } = default!;

    public string FirstName { get; set; } = default!;

    public string LastName { get; set; } = default!;

    public string? PhoneNumber { get; set; }

    public string? ImageURL { get; set; }
}