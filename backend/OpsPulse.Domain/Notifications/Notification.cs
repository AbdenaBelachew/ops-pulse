using OpsPulse.Domain.Common;
using OpsPulse.Domain.Organizations;

namespace OpsPulse.Domain.Notifications;

public enum NotificationType
{
    Alert = 0,
    Incident = 1,
    System = 2
}

public enum NotificationChannel
{
    InApp = 0,
    Email = 1
}

public enum DeliveryStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2,
    Skipped = 3
}

public class Notification : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Organization Organizations { get; set; } = null!;

    public Guid? UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public NotificationType Type { get; set; } = NotificationType.Alert;

    public NotificationChannel Channel { get; set; } = NotificationChannel.InApp;

    public string? Target { get; set; }

    public bool IsRead { get; set; }

    public DeliveryStatus DeliveryStatus { get; set; } = DeliveryStatus.Pending;

    public DateTime? DeliveredAtUtc { get; set; }

    public DateTime? ReadAtUtc { get; set; }
}
