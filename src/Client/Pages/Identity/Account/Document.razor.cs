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

public class UploadedFile
{
    public InputOutputResourceDocumentType? FileIdentifier { get; set; }
    public string? InputOutputResourceId { get; set; }
    public string? InputOutputResourceImgUrl { get; set; }
    public string? UserId { get; set; }
    public bool isVisible { get; set; } = false;
    public bool isVerified { get; set; } = false;

}

public partial class Document
{
    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject]
    protected IAuthenticationService AuthService { get; set; } = default!;

    private string? UserId { get; set; }

    private List<UploadedFile> UploadedFiles { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        if ((await AuthState).User is { } user)
        {
            UserId = user.GetUserId();

            if (UserId is not null)
            {
                UploadedFiles = new()
                {
                     new UploadedFile()
                     {
                         FileIdentifier = InputOutputResourceDocumentType.Passport,
                         InputOutputResourceId = string.Empty,
                         InputOutputResourceImgUrl = string.Empty,
                         UserId = UserId,
                         isVisible = true,
                         isVerified = false
                     },
                     new UploadedFile()
                     {
                         FileIdentifier = InputOutputResourceDocumentType.PassportBack,
                         InputOutputResourceId = string.Empty,
                         InputOutputResourceImgUrl = string.Empty,
                         UserId = UserId,
                         isVisible = true,
                         isVerified = false
                     }
                };
            }
        }
    }

    private async Task UpdateProfileAsync()
    {
        Console.WriteLine("Test");
    }
}