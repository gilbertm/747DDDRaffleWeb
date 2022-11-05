using System.Globalization;
using System.Security.Claims;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Common;
using EHULOG.BlazorWebAssembly.Client.Shared.Dialogs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using static MudBlazor.CategoryTypes;

namespace EHULOG.BlazorWebAssembly.Client.Components.Common.Popup;

public partial class ManageUserFilesDocuments
{
    [CascadingParameter]
    public MudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public InputOutputResourceDto IOResource { get; set; } = default!;

    public void Approved() => MudDialog.Close(DialogResult.Ok("Approved"));
    public void Denied() => MudDialog.Close(DialogResult.Ok("Denied"));

    private bool _isImageHovered;

    private bool IsImageHovered
    {
        get
        {
            return _isImageHovered;
        }
        set
        {
            if (value == true)
            {
                _isImageHovered = true;
            }
            else
            {
                _isImageHovered = false;
            }
        }
    }
}