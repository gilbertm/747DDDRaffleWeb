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

namespace EHULOG.BlazorWebAssembly.Client.Components.Common;

public partial class SingleFileUpload
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthenticationService AuthService { get; set; } = default!;
    [Inject]
    protected IPersonalClient PersonalClient { get; set; } = default!;
    [Inject]
    protected IInputOutputResourceClient InputOutputResourceClient { get; set; } = default!;

    [Parameter]
    public List<ForUploadFile>? ForUploadFiles { get; set; }

    [Parameter]
    public InputOutputResourceDocumentType FileIdentifier { get; set; }

    private ForUploadFile? _forUploadFile { get; set; } = new();

    private async Task UploadFiles(InputFileChangeEventArgs e)
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

            string? fileName = $"{Guid.NewGuid():N}";
            fileName = fileName[..Math.Min(fileName.Length, 90)];
            var imageFile = await file.RequestImageFileAsync(ApplicationConstants.StandardImageFormat, ApplicationConstants.MaxImageWidth, ApplicationConstants.MaxImageHeight);
            byte[]? buffer = new byte[imageFile.Size];
            await imageFile.OpenReadStream(ApplicationConstants.MaxAllowedSize).ReadAsync(buffer);
            string? base64String = $"data:{ApplicationConstants.StandardImageFormat};base64,{Convert.ToBase64String(buffer)}";

            if (Guid.TryParse(fileName, out var referenceId))
            {
                CreateInputOutputResourceRequest createInputOutputResourceRequest = new CreateInputOutputResourceRequest()
                {
                    ReferenceId = referenceId == Guid.Empty ? default! : referenceId,
                    Image = new FileUploadRequest() { Name = fileName, Data = base64String, Extension = extension },
                    InputOutputResourceStatusType = InputOutputResourceStatusType.Disabled,
                    InputOutputResourceType = InputOutputResourceType.Loan
                };

                var valueTupleOfGuidAndString = await InputOutputResourceClient.CreateAsync(createInputOutputResourceRequest);

                _forUploadFile.InputOutputResourceImgUrl = valueTupleOfGuidAndString.Value;
                _forUploadFile.InputOutputResourceId = valueTupleOfGuidAndString.Key.ToString();
                _forUploadFile.Opacity = "1";
                _forUploadFile.Disabled = false;
                _forUploadFile.isTemporarilyUploaded = true;
            }
        }
    }

    public async Task RemoveImageAsync()
    {
        string deleteContent = "You're sure you want to delete your Profile Image?";
        var parameters = new DialogParameters
        {
            { nameof(DeleteConfirmation.ContentText), deleteContent }
        };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true, DisableBackdropClick = true };
        var dialog = DialogService.Show<DeleteConfirmation>("Delete", parameters, options);
        var result = await dialog.Result;
        if (!result.Cancelled)
        {
            // _profileModel.DeleteCurrentImage = true;
            // await UpdateUploadedFilesAsync();
        }
    }

    public void Hover(ForUploadFile forUploadFile)
    {
        forUploadFile.isHovered = true;
        StateHasChanged();
    }

    public void ClearHover(ForUploadFile forUploadFile)
    {
        forUploadFile.isHovered = false;
        StateHasChanged();
    }

    public async Task Remove(ForUploadFile? forUploadFile)
    {
        if (forUploadFile is not null)
        {
            string id = forUploadFile.InputOutputResourceId ?? string.Empty;

            if (!string.IsNullOrEmpty(id))
            {
                Guid guid = await InputOutputResourceClient.DeleteByIdAsync(Guid.Parse(id));

                if (forUploadFile is not null)
                {
                    forUploadFile.InputOutputResourceId = string.Empty;
                    forUploadFile.InputOutputResourceImgUrl = string.Empty;
                    forUploadFile.isVerified = false;
                    forUploadFile.isTemporarilyUploaded = false;
                }

                StateHasChanged();
            }
        }
    }
}