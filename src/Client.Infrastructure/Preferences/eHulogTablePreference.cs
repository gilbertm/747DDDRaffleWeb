using EHULOG.WebApi.Shared.Notifications;

namespace EHULOG.BlazorWebAssembly.Client.Infrastructure.Preferences;

public class EhulogTablePreference : INotificationMessage
{
    public bool IsDense { get; set; }
    public bool IsStriped { get; set; }
    public bool HasBorder { get; set; }
    public bool IsHoverable { get; set; }
}