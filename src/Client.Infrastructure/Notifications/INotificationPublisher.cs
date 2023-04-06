using RAFFLE.WebApi.Shared.Notifications;

namespace RAFFLE.BlazorWebAssembly.Client.Infrastructure.Notifications;

public interface INotificationPublisher
{
    Task PublishAsync(INotificationMessage notification);
}