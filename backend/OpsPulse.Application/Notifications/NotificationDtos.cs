using OpsPulse.Domain.Notifications;

namespace OpsPulse.Application.Notifications;

public sealed record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    NotificationType Type,
    NotificationChannel Channel,
    bool IsRead,
    DateTime CreatedAtUtc,
    DateTime? ReadAtUtc);

public sealed record UnreadCountDto(int Count);
