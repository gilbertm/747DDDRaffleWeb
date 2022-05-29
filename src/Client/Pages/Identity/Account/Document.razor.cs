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

namespace EHULOG.BlazorWebAssembly.Client.Pages.Identity.Account;

public class ForUploadFile
{
    public InputOutputResourceDocumentType? FileIdentifier { get; set; }
    public string? InputOutputResourceId { get; set; }
    public string? InputOutputResourceImgUrl { get; set; }
    public string? UserIdReferenceId { get; set; }
    public bool isVerified { get; set; } = false; // if the status type is enabled. disabled means uploaded temporarily.
    public bool isTemporarilyUploaded { get; set; } = false; // if on first load and already in the system

}

public partial class Document
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthenticationService AuthService { get; set; } = default!;
    [Inject]
    protected IInputOutputResourceClient InputOutputResourceClient { get; set; } = default!;

    private string? UserId { get; set; }

    private List<ForUploadFile> ForUploadFiles { get; set; } = new();

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
                            isVerified = ior.ResourceStatusType.Equals(InputOutputResourceStatusType.Enabled) ? true : false,
                            isTemporarilyUploaded = true
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
                        isTemporarilyUploaded = false
                    });
                }
            }
        }
    }

    private async Task UpdateProfileAsync()
    {
        /*
         * 
         * Submitting will trigger verification. Informing the admins.
         * 
         */
        Console.WriteLine("Test");
    }
}