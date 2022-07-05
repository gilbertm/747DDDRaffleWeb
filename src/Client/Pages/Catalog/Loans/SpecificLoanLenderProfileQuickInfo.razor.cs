using EHULOG.BlazorWebAssembly.Client.Components.Common;
using EHULOG.BlazorWebAssembly.Client.Components.Dialogs;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Auth;
using EHULOG.BlazorWebAssembly.Client.Shared;
using EHULOG.BlazorWebAssembly.Client.Shared.Dialogs;
using Mapster;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using System.Globalization;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog.Loans;

public partial class SpecificLoanLenderProfileQuickInfo
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthenticationService AuthService { get; set; } = default!;

    [Parameter]
    public LoanLenderDto? LoanLender { get; set; }

    [CascadingParameter(Name = "__appUserDto")]
    private AppUserDto? _appUserDto { get; set; }

    [Parameter]
    public EventCallback OnClick { get; set; }

    [Inject]
    protected IDialogService Dialog { get; set; } = default!;

    private CustomValidation? _customValidation;

    private void OpenLendersUserInspectionView(LoanLenderDto loanLender)
    {
        var parameters = new DialogParameters { ["LoanLenderDto"] = loanLender };

        DialogOptions noHeader = new DialogOptions() { CloseButton = true };

        Dialog.Show<AdminsUserInspectionView>("Lender User's Details", parameters, noHeader);
    }

}