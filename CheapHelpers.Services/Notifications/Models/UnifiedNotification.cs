using CheapHelpers.Models.Enums;

namespace CheapHelpers.Services.Notifications.Models;

/// <summary>
/// Unified notification model supporting multiple delivery channels (InApp, Email, SMS, Push)
/// </summary>
public record UnifiedNotification
{
    /// <summary>
    /// Type of notification (e.g., "OrderShipped", "CommentAdded", "TaskAssigned")
    /// </summary>
    public required string NotificationType { get; init; }

    /// <summary>
    /// Notification title/subject
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Plain text notification body
    /// </summary>
    public required string Body { get; init; }

    /// <summary>
    /// HTML version of notification body (optional, for email)
    /// </summary>
    public string? HtmlBody { get; init; }

    /// <summary>
    /// User IDs to receive this notification
    /// </summary>
    public required List<string> RecipientUserIds { get; init; } = [];

    /// <summary>
    /// Channels to use for delivery
    /// </summary>
    public NotificationChannelFlags Channels { get; init; } = NotificationChannelFlags.InApp;

    /// <summary>
    /// Priority level affecting delivery and display
    /// </summary>
    public NotificationPriority Priority { get; init; } = NotificationPriority.Normal;

    /// <summary>
    /// URL to navigate to when notification is clicked
    /// </summary>
    public string? ActionUrl { get; init; }

    /// <summary>
    /// URL to icon/image for the notification
    /// </summary>
    public string? IconUrl { get; init; }

    /// <summary>
    /// Additional structured data for the notification
    /// </summary>
    public Dictionary<string, string>? Data { get; init; }

    /// <summary>
    /// Email addresses for email channel (alternative to RecipientUserIds)
    /// </summary>
    public List<string>? EmailRecipients { get; init; }

    /// <summary>
    /// Phone numbers for SMS channel (alternative to RecipientUserIds)
    /// </summary>
    public List<string>? SmsRecipients { get; init; }

    /// <summary>
    /// Email template name for email channel
    /// </summary>
    public string? EmailTemplateName { get; init; }

    /// <summary>
    /// Data for email template rendering
    /// </summary>
    public Dictionary<string, object>? EmailTemplateData { get; init; }

    /// <summary>
    /// Expiration time for the notification
    /// </summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>
    /// Indicates if notification should be delivered silently (no sound/vibration)
    /// </summary>
    public bool Silent { get; init; }

    /// <summary>
    /// Subscription context defining what entity this notification relates to
    /// </summary>
    public ISubscriptionContext? SubscriptionContext { get; init; }
}
