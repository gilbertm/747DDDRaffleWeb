using RAFFLE.WebApi.Shared.Notifications;

namespace RAFFLE.BlazorWebAssembly.Client.Infrastructure.Preferences;

public class RaffleTablePreference : INotificationMessage
{
    public bool IsDense { get; set; }
    public bool IsStriped { get; set; }
    public bool HasBorder { get; set; }
    public bool IsHoverable { get; set; }
}