using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using GradLink.API.Data;
using GradLink.API.Hubs;
using GradLink.Shared.DTOs;
using GradLink.Shared.Enums;

namespace GradLink.API.Services;

public class NotificationService
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 100;

    private readonly GradLinkDbContext _context;
    private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        GradLinkDbContext context,
        IHubContext<NotificationHub, INotificationClient> hubContext,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }

    // Staged on the shared DbContext: the caller's SaveChangesAsync persists it atomically with the change that caused it.
    public Notification Add(string userId, string message, NotificationType type)
    {
        var notification = new Notification { UserId = userId, Message = message, Type = type };
        _context.Notifications.Add(notification);
        return notification;
    }

    // Best-effort: a failed push is logged, and the notification remains in the user's history.
    public async Task PublishAsync(Notification notification)
    {
        try
        {
            await _hubContext.Clients.Group(notification.UserId).ReceiveNotification(ToDto(notification));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Real-time delivery of notification {NotificationId} to user {UserId} failed.",
                notification.Id, notification.UserId);
        }
    }

    public async Task<List<NotificationDto>> GetUserNotificationsAsync(string userId, int skip = 0, int take = DefaultPageSize)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Skip(Math.Max(skip, 0))
            .Take(Math.Clamp(take, 1, MaxPageSize))
            .Select(n => ToDto(n))
            .ToListAsync();
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, string userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null) return false;

        notification.IsRead = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true));
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    private static NotificationDto ToDto(Notification notification) => new()
    {
        Id = notification.Id,
        Message = notification.Message,
        IsRead = notification.IsRead,
        CreatedAt = notification.CreatedAt,
        Type = notification.Type
    };
}
