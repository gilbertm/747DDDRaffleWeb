using RAFFLE.WebApi.Shared.Notifications;

namespace RAFFLE.BlazorWebAssembly.Client.Infrastructure.Notifications;

public record ConnectionStateChanged(ConnectionState State, string? Message) : INotificationMessage;