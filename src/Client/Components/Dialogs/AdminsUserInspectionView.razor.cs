using EHULOG.BlazorWebAssembly.Client.Infrastructure.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using MudBlazor;
using EHULOG.BlazorWebAssembly.Client.Components.Common;
using Mapster;

namespace EHULOG.BlazorWebAssembly.Client.Shared.Dialogs;

public partial class AdminsUserInspectionView
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;

    [CascadingParameter]
    protected MudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public LoanLenderDto LoanLenderDto { get; set; } = default!;
        
    [CascadingParameter(Name = "__appUserDto")]
    private AppUserDto? _appUserDto { get; set; }

    private CustomValidation? _customValidation;
}