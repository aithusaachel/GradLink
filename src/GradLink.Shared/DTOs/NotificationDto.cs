using GradLink.Shared.Enums;

namespace GradLink.Shared.DTOs;

public class NotificationDto
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public NotificationType Type { get; set; }
}
