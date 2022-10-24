using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using MudBlazor;
using Mapster;

namespace EHULOG.BlazorWebAssembly.Client.Shared.Dialogs;

public partial class AdminsUserInspectionView
{
    [Parameter]
    public LoanLenderDto LoanLenderDto { get; set; } = default!;

}