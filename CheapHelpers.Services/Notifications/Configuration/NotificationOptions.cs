using CheapHelpers.Models.Enums;

namespace CheapHelpers.Services.Notifications.Configuration;

/// <summary>
/// Configuration options for the notification system
/// </summary>
public class NotificationOptions
{
    /// <summary>
    /// Configuration section name for binding from appsettings.json
    /// </summary>
    public const string SectionName = "Notifications";

    /// <summary>
    /// Number of days to retain notifications before automatic cleanup (default: 30 days)
    /// </summary>
    public int RetentionDays { get; set; } = 30;

    /// <summary>
    /// Maximum number of notifications to store per user before cleanup (default: 500)
    /// </summary>
    public int MaxStoredPerUser { get; set; } = 500;

    /// <summary>
    /// Enable automatic cleanup of old notifications via background service (default: true)
    /// </summary>
    public bool EnableAutoCleanup { get; set; } = true;

    /// <summary>
    /// Real-time notification provider to use: "SignalR" or "RabbitMQ" (default: "SignalR")
    /// </summary>
    public string RealTimeProvider { get; set; } = "SignalR";

    /// <summary>
    /// Enable Redis caching for notification data (default: false)
    /// </summary>
    public bool EnableRedisCache { get; set; }

    /// <summary>
    /// Redis connection string for caching (required if EnableRedisCache is true)
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// RabbitMQ connection string for real-time delivery (required if RealTimeProvider is "RabbitMQ")
    /// </summary>
    public string? RabbitMQConnectionString { get; set; }

    /// <summary>
    /// Notification channels that are enabled globally (default: all channels)
    /// </summary>
    public NotificationChannelFlags EnabledChannels { get; set; } = NotificationChannelFlags.All;
}
