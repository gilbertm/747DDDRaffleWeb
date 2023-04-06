using System.Security.Claims;
using RAFFLE.BlazorWebAssembly.Client.Components.Common;
using RAFFLE.BlazorWebAssembly.Client.Components.Dialogs;
using RAFFLE.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using RAFFLE.BlazorWebAssembly.Client.Infrastructure.Auth;
using RAFFLE.BlazorWebAssembly.Client.Infrastructure.Common;
using RAFFLE.BlazorWebAssembly.Client.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace RAFFLE.BlazorWebAssembly.Client.Components.Common.FileManagement;

public partial class SingleFileUpload
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;

    [Inject]
    protected IAuthenticationService AuthService { get; set; } = default!;

    [Parameter]
    public string ImageUrl { get; set; } = default!;

    [CascadingParameter(Name = "FileUpload")]
    public FileUploadRequest FileUpload { get; set; } = default!;

    [Parameter]
    public string ForName { get; set; } = default!;

    // can update is primarily used for owners of the image
    [Parameter]
    public bool CanUpdate { get; set; } = default!;

    [Parameter]
    public EventCallback OnRemoveImage { get; set; } = default!;

    // enabled is primarily used for lenders or role that
    // can elevate the status
    // verification can only be done by admins
    [Parameter]
    public bool CanEnable { get; set; } = default!;

    [Parameter]
    public EventCallback OnEnableImage { get; set; } = default!;

    [Parameter]
    public EventCallback<FileUploadRequest> OnUploadImage { get; set; } = default!;

    [Parameter]
    public string UserId { get; set; } = default!;

    private bool _isHovered = false;

    private async Task UploadFile(InputFileChangeEventArgs e)
    {
        var file = e.File;

        if (file is not null)
        {
            string? extension = Path.GetExtension(file.Name);
            if (!ApplicationConstants.SupportedImageFormats.Contains(extension.ToLower()))
            {
                Snackbar.Add("Image Format Not Supported.", Severity.Error);
                return;
            }

            string? fileName = $"{UserId}-{Guid.NewGuid():N}";
            fileName = fileName[..Math.Min(fileName.Length, 90)];
            var imageFile = await file.RequestImageFileAsync(ApplicationConstants.StandardImageFormat, ApplicationConstants.MaxImageWidth, ApplicationConstants.MaxImageHeight);
            byte[]? buffer = new byte[imageFile.Size];
            await imageFile.OpenReadStream(ApplicationConstants.MaxAllowedSize).ReadAsync(buffer);
            string? base64String = $"data:{ApplicationConstants.StandardImageFormat};base64,{Convert.ToBase64String(buffer)}";
            FileUpload = new FileUploadRequest() { Name = fileName, Data = base64String, Extension = extension };

            await OnUploadImage.InvokeAsync(FileUpload);

        }
    }

    public void Hover()
    {
        _isHovered = true;
        StateHasChanged();
    }

    public void ClearHover()
    {
        _isHovered = false;
        StateHasChanged();
    }

    public void Remove()
    {
        ImageUrl = string.Empty;

        FileUpload = default!;

        StateHasChanged();
    }
}