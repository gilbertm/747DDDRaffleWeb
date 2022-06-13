using EHULOG.BlazorWebAssembly.Client.Infrastructure.ApiClient;

namespace EHULOG.BlazorWebAssembly.Client.Components.Common;

public class ForUploadFile
{
    public InputOutputResourceDocumentType? FileIdentifier { get; set; }
    public string? InputOutputResourceId { get; set; }
    public string? InputOutputResourceImgUrl { get; set; }
    public string? UserIdReferenceId { get; set; }
    public bool isVerified { get; set; } = false; // if the status type is enabled. disabled means uploaded temporarily.
    public bool isTemporarilyUploaded { get; set; } = false; // if on first load and already in the system
    public bool isHovered { get; set; } = false;
    public bool isRemove { get; set; } = false;
}
