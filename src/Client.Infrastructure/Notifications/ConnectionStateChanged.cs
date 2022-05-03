using EHULOG.WebApi.Shared.Notifications;

namespace EHULOG.BlazorWebAssembly.Client.Infrastructure.Notifications;

public record ConnectionStateChanged(ConnectionState State, string? Message) : INotificationMessage;