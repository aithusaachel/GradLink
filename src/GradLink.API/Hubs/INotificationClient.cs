using GradLink.Shared.DTOs;

namespace GradLink.API.Hubs;

public interface INotificationClient
{
    Task ReceiveNotification(NotificationDto notification);
}
