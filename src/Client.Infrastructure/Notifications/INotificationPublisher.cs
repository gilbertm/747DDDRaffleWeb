using EHULOG.WebApi.Shared.Notifications;

namespace EHULOG.BlazorWebAssembly.Client.Infrastructure.Notifications;

public interface INotificationPublisher
{
    Task PublishAsync(INotificationMessage notification);
}