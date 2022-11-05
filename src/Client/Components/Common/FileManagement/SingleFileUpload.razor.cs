using System.Security.Claims;
using EHULOG.BlazorWebAssembly.Client.Components.Common;
using EHULOG.BlazorWebAssembly.Client.Components.Dialogs;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Auth;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Common;
using EHULOG.BlazorWebAssembly.Client.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace EHULOG.BlazorWebAssembly.Client.Components.Common.FileManagement;

public partial class SingleFileUpload
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthenticationService AuthService { get; set; } = default!;
    [Inject]
    protected IPersonalClient PersonalClient { get; set; } = default!;
    [Parameter]
    public string ImageUrl { get; set; } = default!;

    [CascadingParameter(Name = "FileUpload")]
    public FileUploadRequest FileUpload { get; set; } = default!;

    [Parameter]
    public string ForName { get; set; } = default!;

    [Parameter]
    public EventCallback OnRemoveImage { get; set; } = default!;

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