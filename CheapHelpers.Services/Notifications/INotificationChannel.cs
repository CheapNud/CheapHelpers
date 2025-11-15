using CheapHelpers.Models.Enums;
using CheapHelpers.Services.Notifications.Models;

namespace CheapHelpers.Services.Notifications;

/// <summary>
/// Interface for notification channel implementations (Email, SMS, Push, InApp)
/// </summary>
public interface INotificationChannel
{
    /// <summary>
    /// Name identifying this notification channel
    /// </summary>
    string ChannelName { get; }

    /// <summary>
    /// Determines if this channel can handle the specified notification type
    /// </summary>
    /// <param name="notificationType">Type of notification to check</param>
    /// <returns>True if this channel supports the notification type</returns>
    bool SupportsNotificationType(string notificationType);

    /// <summary>
    /// Sends a notification through this channel
    /// </summary>
    /// <param name="notification">Notification to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the send operation</returns>
    Task<NotificationChannelResult> SendAsync(
        UnifiedNotification notification,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a notification channel send operation
/// </summary>
/// <param name="IsSuccess">Indicates if the send operation succeeded</param>
/// <param name="ChannelName">Name of the channel that processed the notification</param>
/// <param name="ErrorMessage">Error message if the operation failed</param>
/// <param name="SentCount">Number of recipients successfully notified</param>
/// <param name="FailedCount">Number of recipients that failed to receive notification</param>
public record NotificationChannelResult(
    bool IsSuccess,
    string ChannelName,
    string? ErrorMessage = null,
    int SentCount = 0,
    int FailedCount = 0)
{
    /// <summary>
    /// Creates a successful channel result
    /// </summary>
    public static NotificationChannelResult Success(string channelName, int sentCount = 1)
        => new(true, channelName, null, sentCount, 0);

    /// <summary>
    /// Creates a failed channel result
    /// </summary>
    public static NotificationChannelResult Failure(string channelName, string errorMessage, int failedCount = 1)
        => new(false, channelName, errorMessage, 0, failedCount);

    /// <summary>
    /// Creates a partial success result (some sent, some failed)
    /// </summary>
    public static NotificationChannelResult Partial(string channelName, int sentCount, int failedCount)
        => new(sentCount > 0, channelName, sentCount > 0 ? null : "All recipients failed", sentCount, failedCount);
}
