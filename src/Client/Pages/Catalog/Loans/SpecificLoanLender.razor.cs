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
namespace EHULOG.BlazorWebAssembly.Client.Pages.Catalog.Loans;

public partial class SpecificLoanLender
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthenticationService AuthService { get; set; } = default!;

    [Parameter]
    public LoanStatus LoanStatus { get; set; }

    [Parameter]
    public bool IsOwner { get; set; } = false;

    [Parameter]
    public bool IsMinimal { get; set; } = false;

    [CascadingParameter(Name = "__appUserDto")]
    private AppUserDto? _appUserDto { get; set; }

    [Parameter]
    public EventCallback OnClick { get; set; }

    [Inject]
    protected IDialogService Dialog { get; set; } = default!;

    private CustomValidation? _customValidation;

}