﻿using System.Security.Claims;
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

namespace EHULOG.BlazorWebAssembly.Client.Pages.Identity.Account;

public partial class DocumentFileUploader
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
    public List<UploadedFile>? UploadedFiles { get; set; }

    [Parameter]
    public InputOutputResourceDocumentType FileIdentifier { get; set; }

    private CustomValidation? _customValidation;

    private async Task UpdateUploadedFilesAsync()
    {
    }

    private async Task UploadFiles(InputFileChangeEventArgs e, UploadedFile? uploadedFile)
    {
        var file = e.File;

        if (file is not null && uploadedFile is not null)
        {
            string? extension = Path.GetExtension(file.Name);
            if (!ApplicationConstants.SupportedImageFormats.Contains(extension.ToLower()))
            {
                Snackbar.Add("Image Format Not Supported.", Severity.Error);
                return;
            }

            string? fileName = $"{Enum.GetName(typeof(InputOutputResourceDocumentType), uploadedFile.FileIdentifier ?? default)}--{uploadedFile?.UserId?.ToString()}--{Guid.NewGuid():N}";
            fileName = fileName[..Math.Min(fileName.Length, 90)];
            var imageFile = await file.RequestImageFileAsync(ApplicationConstants.StandardImageFormat, ApplicationConstants.MaxImageWidth, ApplicationConstants.MaxImageHeight);
            byte[]? buffer = new byte[imageFile.Size];
            await imageFile.OpenReadStream(ApplicationConstants.MaxAllowedSize).ReadAsync(buffer);
            string? base64String = $"data:{ApplicationConstants.StandardImageFormat};base64,{Convert.ToBase64String(buffer)}";

            if (Guid.TryParse(uploadedFile?.UserId?.ToString(), out var referenceId))
            {
                CreateInputOutputResourceRequest createInputOutputResourceRequest = new CreateInputOutputResourceRequest()
                {
                    ReferenceId = referenceId == Guid.Empty ? default! : referenceId,
                    Image = new FileUploadRequest() { Name = fileName, Data = base64String, Extension = extension },
                    InputOutputResourceDocumentType = uploadedFile.FileIdentifier ?? default,
                    InputOutputResourceStatusType = InputOutputResourceStatusType.Disabled,
                    InputOutputResourceType = InputOutputResourceType.Identification
                };

                var valueTupleOfGuidAndString = await InputOutputResourceClient.CreateAsync(createInputOutputResourceRequest);

                if (UploadedFiles is not null)
                {
                    uploadedFile.InputOutputResourceImgUrl = valueTupleOfGuidAndString.Value;
                    uploadedFile.InputOutputResourceId = valueTupleOfGuidAndString.Key.ToString();
                }
            }
        }
    }

    public async Task RemoveImageAsync()
    {
        string deleteContent = L["You're sure you want to delete your Profile Image?"];
        var parameters = new DialogParameters
        {
            { nameof(DeleteConfirmation.ContentText), deleteContent }
        };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true, DisableBackdropClick = true };
        var dialog = DialogService.Show<DeleteConfirmation>(L["Delete"], parameters, options);
        var result = await dialog.Result;
        if (!result.Cancelled)
        {
            //_profileModel.DeleteCurrentImage = true;
            await UpdateUploadedFilesAsync();
        }
    }
}