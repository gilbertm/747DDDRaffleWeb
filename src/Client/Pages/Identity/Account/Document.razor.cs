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
using System.Linq;
using static EHULOG.BlazorWebAssembly.Client.Infrastructure.Common.StorageConstants;
using static MudBlazor.CategoryTypes;

namespace EHULOG.BlazorWebAssembly.Client.Pages.Identity.Account;

public partial class Document
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;

    [Inject]
    protected IAuthenticationService AuthService { get; set; } = default!;

    [Inject]
    protected IInputOutputResourceClient InputOutputResourceClient { get; set; } = default!;

    [Inject]
    protected AppDataService AppDataService { get; set; } = default!;
    [Inject]
    private IAppUsersClient AppUsersClient { get; set; } = default!;

    [Inject]
    protected IDialogService Dialog { get; set; } = default!;

    private List<ForUploadFile> ForUploadFiles { get; set; } = new();

    private UpdateAppUserRequest UpdateAppUserRequest { get; set; } = default!;

    private bool PassPortCompleted { get; set; } = false;
    private bool NationalIdCompleted { get; set; } = false;
    private bool GovernmentIdCompleted { get; set; } = false;
    private bool SelfieWithAtLeastOneCard { get; set; } = false;

    private bool SubmitForVerficationDisabled { get; set; } = true;

    private bool Verified { get; set; } = false;

    protected override async Task OnInitializedAsync()
    {
        await AppDataService.InitializationAsync();

        if (AppDataService != default && AppDataService.AppUser != default)
        {
            if (AppDataService.AppUser.ApplicationUserId != default)
            {
                /* current uploaded but not verified, this happens when the user uploads files */
                Guid guidUserId = default!;
                Guid.TryParse(AppDataService.AppUser.ApplicationUserId, out guidUserId);
                var referenceIdIOResources = await InputOutputResourceClient.GetAsync(guidUserId);

                if (referenceIdIOResources is not null)
                {
                    foreach (var ior in referenceIdIOResources)
                    {
                        if (!ior.ResourceType.Equals(InputOutputResourceType.Identification) || ior.ResourceDocumentType.Equals(InputOutputResourceDocumentType.None))
                            continue;

                        ForUploadFiles.Add(new ForUploadFile()
                        {
                            FileIdentifier = ior.ResourceDocumentType,
                            InputOutputResourceId = ior.Id.ToString(),
                            UserIdReferenceId = ior.ReferenceId.ToString(),
                            InputOutputResourceImgUrl = ior.ImagePath,
                            isVerified = ior.ResourceStatusType.Equals(InputOutputResourceStatusType.EnabledAndVerified) ? true : false,
                            isTemporarilyUploaded = true,
                            Opacity = new[] { InputOutputResourceDocumentType.Passport, InputOutputResourceDocumentType.NationalId, InputOutputResourceDocumentType.GovernmentId }.Contains(ior.ResourceDocumentType) ? "1" : "0.3",
                            Disabled = new[] { InputOutputResourceDocumentType.Passport, InputOutputResourceDocumentType.NationalId, InputOutputResourceDocumentType.GovernmentId }.Contains(ior.ResourceDocumentType) ? false : true
                        });
                    }
                }

                ForUploadFiles = ForUploadFiles.GroupBy(f => f.FileIdentifier).Select(f => f.First()).ToList();

                foreach (int i in Enum.GetValues(typeof(InputOutputResourceDocumentType)))
                {
                    if (((InputOutputResourceDocumentType)i).Equals(InputOutputResourceDocumentType.None))
                        continue;

                    if (ForUploadFiles.Where(forUpload => forUpload.FileIdentifier.Equals((InputOutputResourceDocumentType)i)).Count() > 0)
                        continue;

                    ForUploadFiles.Add(new ForUploadFile()
                    {
                        FileIdentifier = (InputOutputResourceDocumentType)i,
                        InputOutputResourceId = string.Empty,
                        UserIdReferenceId = AppDataService.AppUser.ApplicationUserId,
                        InputOutputResourceImgUrl = string.Empty,
                        isVerified = false,
                        isTemporarilyUploaded = false,
                        Opacity = new[] { InputOutputResourceDocumentType.Passport, InputOutputResourceDocumentType.NationalId, InputOutputResourceDocumentType.GovernmentId }.Contains((InputOutputResourceDocumentType)i) ? "1" : "0.3",
                        Disabled = new[] { InputOutputResourceDocumentType.Passport, InputOutputResourceDocumentType.NationalId, InputOutputResourceDocumentType.GovernmentId }.Contains((InputOutputResourceDocumentType)i) ? false : true
                    });

                }

                OnChildChanges(ForUploadFiles);
            }
        }
    }

    private void OnChildChanges(List<ForUploadFile> forUploadFiles)
    {
        foreach (var fuf in forUploadFiles.Where(fuf => fuf.FileIdentifier.HasValue && new[] { InputOutputResourceDocumentType.Passport, InputOutputResourceDocumentType.NationalId, InputOutputResourceDocumentType.GovernmentId }.Contains((InputOutputResourceDocumentType)fuf.FileIdentifier)))
        {
            fuf.Disabled = false;
            fuf.Opacity = "1";

            if (fuf is not null && fuf.FileIdentifier.HasValue)
            {
                switch (fuf.FileIdentifier)
                {
                    case InputOutputResourceDocumentType.Passport:
                        forUploadFiles.First(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.PassportBack)).Disabled = !fuf.isTemporarilyUploaded; // toggle disabled upload box, if there's temporary uploaded document
                        forUploadFiles.First(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.PassportBack)).Opacity = fuf.isTemporarilyUploaded ? "1" : "0.3";
                        break;
                    case InputOutputResourceDocumentType.NationalId:
                        forUploadFiles.First(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.NationalIdBack)).Disabled = !fuf.isTemporarilyUploaded; // toggle disabled upload box, if there's temporary uploaded document
                        forUploadFiles.First(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.NationalIdBack)).Opacity = fuf.isTemporarilyUploaded ? "1" : "0.3";
                        break;
                    case InputOutputResourceDocumentType.GovernmentId:
                        forUploadFiles.First(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.GovernmentIdBack)).Disabled = !fuf.isTemporarilyUploaded; // toggle disabled upload box, if there's temporary uploaded document
                        forUploadFiles.First(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.GovernmentIdBack)).Opacity = fuf.isTemporarilyUploaded ? "1" : "0.3";
                        break;

                }
            }

        }

        int validDocuments = 0;
        int verifiedDocuments = 0;

        bool isSubmitForVerification = false;

        if (forUploadFiles.First(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.Passport)).isTemporarilyUploaded &&
            forUploadFiles.First(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.PassportBack)).isTemporarilyUploaded)
        {
            validDocuments++;
        }

        if (forUploadFiles.First(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.Passport)).isVerified &&
            forUploadFiles.First(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.PassportBack)).isVerified)
        {
            verifiedDocuments++;
        }

        if (forUploadFiles.First(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.NationalId)).isTemporarilyUploaded &&
            forUploadFiles.First(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.NationalIdBack)).isTemporarilyUploaded)
        {
            validDocuments++;
        }

        if (forUploadFiles.First(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.NationalId)).isVerified &&
            forUploadFiles.First(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.NationalIdBack)).isVerified)
        {
            verifiedDocuments++;
        }

        if (forUploadFiles.First(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.GovernmentId)).isTemporarilyUploaded &&
            forUploadFiles.First(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.GovernmentIdBack)).isTemporarilyUploaded)
        {
            validDocuments++;
        }

        if (forUploadFiles.First(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.GovernmentId)).isVerified &&
            forUploadFiles.First(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.GovernmentIdBack)).isVerified)
        {
            verifiedDocuments++;
        }

        if (forUploadFiles.First(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.SelfieWithAtLeastOneCard)).isTemporarilyUploaded)
        {
            isSubmitForVerification = true;
        }

        if (forUploadFiles.First(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.SelfieWithAtLeastOneCard)).isVerified)
        {
            verifiedDocuments++;
        }

        if (validDocuments > 1)
        {
            forUploadFiles.First(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.SelfieWithAtLeastOneCard)).Disabled = false;
            forUploadFiles.First(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.SelfieWithAtLeastOneCard)).Opacity = "1";
        }

        if (validDocuments > 1 && isSubmitForVerification)
        {
            SubmitForVerficationDisabled = false;
        }
        else
        {
            SubmitForVerficationDisabled = true;
        }

        if (verifiedDocuments > 2)
        {
            Verified = true;
        }

        StateHasChanged();
    }

    private async Task UpdateProfileAsync()
    {
        DialogOptions noHeader = new DialogOptions() { NoHeader = true };
        var result = await DialogService.Show<DocumentsForVerification>("For Verification", noHeader).Result;

        if (!result.Cancelled)
        {
            if ((bool)(result.Data ?? false))
            {
                UpdateAppUsersDocumentsForAdminVerification();
            }
        }
    }

    private async Task UpdateAppUsersDocumentsForAdminVerification()
    {
        if (AppDataService != default)
        {
            if (AppDataService.AppUser != default)
            {

                UpdateAppUserRequest = new()
                {
                    Id = AppDataService.AppUser.Id,
                    ApplicationUserId = AppDataService.AppUser.ApplicationUserId,

                    //// submitted but yet to be verified
                    DocumentsStatus = VerificationStatus.Submitted,
                };

                if (await ApiHelper.ExecuteCallGuardedAsync(() => AppUsersClient.UpdateAsync(AppDataService.AppUser.Id, UpdateAppUserRequest), Snackbar, null, L["AppUser updated. "]) is Guid guid)
                {
                    Snackbar.Add(L["For verification images submitted."], Severity.Success);

                    var timer = new Timer(
                        new TimerCallback(_ =>
                        {
                            Navigation.NavigateTo(Navigation.Uri, forceLoad: true);
                        }),
                        null,
                        2000,
                        2000);
                }
            }
        }
    }
}