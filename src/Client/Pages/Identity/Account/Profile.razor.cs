using System;
using System.Security.Claims;
using System.Threading;
using EHULOG.BlazorWebAssembly.Client.Components.Common;
using EHULOG.BlazorWebAssembly.Client.Components.Dialogs;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Auth;
using EHULOG.BlazorWebAssembly.Client.Infrastructure.Common;
using EHULOG.BlazorWebAssembly.Client.Shared;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualBasic.FileIO;
using MudBlazor;
using static MudBlazor.CategoryTypes;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Identity.Account;

public partial class Profile
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthenticationService AuthService { get; set; } = default!;
    [Inject]
    protected IPersonalClient PersonalClient { get; set; } = default!;
    [Inject]
    protected IDialogService Dialog { get; set; } = default!;
    [Inject]
    protected IInputOutputResourceClient InputOutputResourceClient { get; set; } = default!;

    private readonly UpdateUserRequest _profileModel = new();

    private string? _imageUrl;
    private string? _userId;
    private char _firstLetterOfName;

    private FileUploadRequest FileUpload { get; set; } = default!;

    private CustomValidation? _customValidation;

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
            _profileModel.DeleteCurrentImage = true;

            _imageUrl = default;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        if ((await AuthState).User is { } user)
        {
            _userId = user.GetUserId();
            _profileModel.Email = user.GetEmail() ?? string.Empty;
            _profileModel.FirstName = user.GetFirstName() ?? string.Empty;
            _profileModel.LastName = user.GetSurname() ?? string.Empty;
            _profileModel.PhoneNumber = user.GetPhoneNumber() ?? string.Empty;
            _imageUrl = string.IsNullOrEmpty(user?.GetImageUrl()) ? string.Empty : (Config[ConfigNames.ApiBaseUrl] + user?.GetImageUrl());
            if (_userId is not null) _profileModel.Id = _userId;
        }

        if (_profileModel.FirstName?.Length > 0)
        {
            _firstLetterOfName = _profileModel.FirstName.ToUpper().FirstOrDefault();
        }
    }

    private async Task UpdateProfileAsync()
    {
        if (await ApiHelper.ExecuteCallGuardedAsync(
            () => PersonalClient.UpdateProfileAsync(_profileModel), Snackbar, _customValidation))
        {
            Snackbar.Add(L["Your Profile has been updated. Please Login again to Continue."], Severity.Success);

            DialogOptions noHeader = new DialogOptions() { NoHeader = true, MaxWidth = MaxWidth.Medium, CloseButton = true, CloseOnEscapeKey = true, DisableBackdropClick = true };
            Dialog.Show<TimerReloginDialog>("Relogin", noHeader);

            return;

            // await AuthService.ReLoginAsync(Navigation.Uri);
        }
    }

    private async Task UploadFileAsync(FileUploadRequest fileUpload)
    {
        if (_userId != default)
        {
            _profileModel.Image = fileUpload;

            CreateInputOutputResourceRequest createInputOutputResourceRequest = new CreateInputOutputResourceRequest
            {
                ReferenceId = Guid.NewGuid(),
                Image = fileUpload,
                InputOutputResourceDocumentType = InputOutputResourceDocumentType.None,
                InputOutputResourceStatusType = InputOutputResourceStatusType.Disabled,
                InputOutputResourceType = InputOutputResourceType.AppUser
            };

            var valueTupleOfGuidAndString = await InputOutputResourceClient.CreateAsync(createInputOutputResourceRequest);

            if (valueTupleOfGuidAndString != default)
            {
                _imageUrl = string.IsNullOrEmpty(valueTupleOfGuidAndString.Value) ? string.Empty : (Config[ConfigNames.ApiBaseUrl] + valueTupleOfGuidAndString.Value);
            }

            StateHasChanged();
        }
    }
}