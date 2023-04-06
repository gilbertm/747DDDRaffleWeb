using RAFFLE.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using RAFFLE.BlazorWebAssembly.Client.Components.Common.Popup;
using System;

namespace RAFFLE.BlazorWebAssembly.Client.Components.Common.FileManagement;

public partial class SingleFileSimpleManage
{
    [Parameter]
    public InputOutputResourceDto InputOutputResource { get; set; } = default!;

    [Parameter]
    public EventCallback OnApprove { get; set; } = default!;

    [Parameter]
    public EventCallback OnDeny { get; set; } = default!;

    [Inject]
    public IDialogService Dialog { get; set; } = default!;

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

    private async Task FileAndManage(string action)
    {
        switch (action)
        {
            case "Approved":
                await OnApprove.InvokeAsync();
                break;
            case "Denied":
                await OnDeny.InvokeAsync();
                break;
        }
    }

    private async Task PopupFileAndManage(InputOutputResourceDto inputOutput)
    {
        var parameters = new DialogParameters { ["IOResource"] = inputOutput };

        DialogOptions noHeader = new DialogOptions() { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = true };

        var dialog = Dialog.Show<ManageUserFilesDocuments>("File or Document details", parameters, noHeader);

        var resultDialog = await dialog.Result;

        if (!resultDialog.Cancelled)
        {
            if (resultDialog.Data is string action)
            {
                switch (action)
                {
                    case "Approved":
                        await OnApprove.InvokeAsync();
                        break;
                    case "Denied":
                        await OnDeny.InvokeAsync();
                        break;
                }
            }
        }
    }
}