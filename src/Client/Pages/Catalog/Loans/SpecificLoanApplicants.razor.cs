using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Pages.Identity.Users;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog.Loans;

public partial class SpecificLoanApplicants
{
    [Parameter]
    public ICollection<LoanApplicantDto> Applicants { get; set; } = default!;

    [Parameter]
    public bool IsPossibleToAppy { get; set; } = false;

    [Parameter]
    public Guid LoanId { get; set; }

    [Inject]
    protected AppDataService AppDataService { get; set; } = default!;

    [Inject]
    protected ILoanApplicantsClient LoanApplicantsClient { get; set; } = default!;


    private AppUserDto _appUserDto { get; set; } = default!;

    private LoanApplicantDto? _loanApplicantDto { get; set; }

    [Parameter]
    public EventCallback OnClick { get; set; }

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
            // load their profile images for display.
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
}