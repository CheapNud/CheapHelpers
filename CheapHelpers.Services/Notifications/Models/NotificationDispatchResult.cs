namespace CheapHelpers.Services.Notifications.Models;

/// <summary>
/// Result of dispatching a notification across multiple channels
/// </summary>
public record NotificationDispatchResult
{
    /// <summary>
    /// Overall success status (true if at least one channel succeeded)
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// Notification type that was dispatched
    /// </summary>
    public required string NotificationType { get; init; }

    /// <summary>
    /// Results from each channel that processed the notification
    /// </summary>
    public required Dictionary<string, NotificationChannelResult> ChannelResults { get; init; } = [];

    /// <summary>
    /// Total number of recipients successfully notified across all channels
    /// </summary>
    public int TotalSentCount => ChannelResults.Values.Sum(r => r.SentCount);

    /// <summary>
    /// Total number of failed deliveries across all channels
    /// </summary>
    public int TotalFailedCount => ChannelResults.Values.Sum(r => r.FailedCount);

    /// <summary>
    /// Number of channels that successfully delivered the notification
    /// </summary>
    public int SuccessfulChannels => ChannelResults.Values.Count(r => r.IsSuccess);

    /// <summary>
    /// Number of channels that failed to deliver the notification
    /// </summary>
    public int FailedChannels => ChannelResults.Values.Count(r => !r.IsSuccess);

    /// <summary>
    /// Combined error messages from all failed channels
    /// </summary>
    public string? ErrorSummary => FailedChannels > 0
        ? string.Join("; ", ChannelResults.Values
            .Where(r => !r.IsSuccess && r.ErrorMessage != null)
            .Select(r => $"{r.ChannelName}: {r.ErrorMessage}"))
        : null;

    /// <summary>
    /// Creates a successful dispatch result
    /// </summary>
    public static NotificationDispatchResult Success(
        string notificationType,
        Dictionary<string, NotificationChannelResult> channelResults)
        => new()
        {
            IsSuccess = true,
            NotificationType = notificationType,
            ChannelResults = channelResults
        };

    /// <summary>
    /// Creates a failed dispatch result
    /// </summary>
    public static NotificationDispatchResult Failure(
        string notificationType,
        Dictionary<string, NotificationChannelResult> channelResults)
        => new()
        {
            IsSuccess = false,
            NotificationType = notificationType,
            ChannelResults = channelResults
        };

    /// <summary>
    /// Creates a dispatch result with automatic success determination
    /// </summary>
    public static NotificationDispatchResult Create(
        string notificationType,
        Dictionary<string, NotificationChannelResult> channelResults)
    {
        bool anySuccess = channelResults.Values.Any(r => r.IsSuccess);
        return new()
        {
            IsSuccess = anySuccess,
            NotificationType = notificationType,
            ChannelResults = channelResults
        };
    }
}
