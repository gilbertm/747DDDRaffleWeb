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
    protected IDialogService Dialog { get; set; } = default!;

    private string? UserId { get; set; }

    private List<ForUploadFile> ForUploadFiles { get; set; } = new();

    private bool PassPortCompleted { get; set; } = false;
    private bool NationalIdCompleted { get; set; } = false;
    private bool GovernmentIdCompleted { get; set; } = false;
    private bool SelfieWithAtLeastOneCard { get; set; } = false;

    private bool SubmitForVerficationDisabled { get; set; } = true;

    private bool Verified { get; set; } = false;

    protected override async Task OnInitializedAsync()
    {
        if ((await AuthState).User is { } user)
        {
            UserId = user.GetUserId();

            if (UserId is not null)
            {
                /* current uploaded but not verified, this happens when the user uploads files */
                Guid guidUserId = default!;
                Guid.TryParse(UserId, out guidUserId);
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
                        UserIdReferenceId = UserId,
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
                        forUploadFiles.Where(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.PassportBack)).First().Disabled = fuf.isTemporarilyUploaded ? false : true;
                        forUploadFiles.Where(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.PassportBack)).First().Opacity = fuf.isTemporarilyUploaded ? "1" : "0.3";
                    break;
                    case InputOutputResourceDocumentType.NationalId:
                        forUploadFiles.Where(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.NationalIdBack)).First().Disabled = fuf.isTemporarilyUploaded ? false : true; ;
                        forUploadFiles.Where(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.NationalIdBack)).First().Opacity = fuf.isTemporarilyUploaded ? "1" : "0.3";
                    break;
                    case InputOutputResourceDocumentType.GovernmentId:
                        forUploadFiles.Where(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.GovernmentIdBack)).First().Disabled = fuf.isTemporarilyUploaded ? false : true;
                        forUploadFiles.Where(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.GovernmentIdBack)).First().Opacity = fuf.isTemporarilyUploaded ? "1" : "0.3";
                    break;

                }
            }

        }

        int validDocuments = 0;
        int verifiedDocuments = 0;

        bool isSubmitForVerification = false;

        if (forUploadFiles.Where(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.Passport)).First().isTemporarilyUploaded &&
            forUploadFiles.Where(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.PassportBack)).First().isTemporarilyUploaded)
        {
            validDocuments++;
        }

        if (forUploadFiles.Where(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.Passport)).First().isVerified &&
            forUploadFiles.Where(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.PassportBack)).First().isVerified)
        {
            verifiedDocuments++;
        }

        if (forUploadFiles.Where(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.NationalId)).First().isTemporarilyUploaded &&
            forUploadFiles.Where(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.NationalIdBack)).First().isTemporarilyUploaded)
        {
            validDocuments++;
        }

        if (forUploadFiles.Where(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.NationalId)).First().isVerified &&
            forUploadFiles.Where(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.NationalIdBack)).First().isVerified)
        {
            verifiedDocuments++;
        }

        if (forUploadFiles.Where(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.GovernmentId)).First().isTemporarilyUploaded &&
            forUploadFiles.Where(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.GovernmentIdBack)).First().isTemporarilyUploaded)
        {
            validDocuments++;
        }

        if (forUploadFiles.Where(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.GovernmentId)).First().isVerified &&
            forUploadFiles.Where(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.GovernmentIdBack)).First().isVerified)
        {
            verifiedDocuments++;
        }

        if (forUploadFiles.Where(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.SelfieWithAtLeastOneCard)).First().isTemporarilyUploaded)
        {
            isSubmitForVerification = true;
        }

        if (forUploadFiles.Where(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.SelfieWithAtLeastOneCard)).First().isVerified)
        {
            verifiedDocuments++;
        }

        if (validDocuments > 1)
        {
            forUploadFiles.Where(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.SelfieWithAtLeastOneCard)).First().Disabled = false;
            forUploadFiles.Where(fuf => fuf.FileIdentifier.HasValue && fuf.FileIdentifier.Equals(InputOutputResourceDocumentType.SelfieWithAtLeastOneCard)).First().Opacity = "1";
        }

        if (validDocuments > 1 && isSubmitForVerification)
        {
            SubmitForVerficationDisabled = false;
        } else
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
        

        await Task.Run(() =>
        {
            /*
        * TODO:// 
        * a. notification to all admins that can check and verify the documents
        * b. flag down that the documents is for checking
        * 
        * Submitting will trigger verification. Informing the admins.
        * 
        */
            Snackbar.Add(L["Your Profile has been updated. Please Login again to Continue."], Severity.Success);

            DialogOptions noHeader = new DialogOptions() { NoHeader = true };
            Dialog.Show<TimerReloginDialog>("Relogin", noHeader);

            return;
        });
    }
}