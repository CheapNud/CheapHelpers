using CheapHelpers.Models.Enums;
using CheapHelpers.Services.Notifications.Models;
using CheapHelpers.Services.Notifications.Subscriptions;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Services.Notifications;

/// <summary>
/// Orchestrates multi-channel notification sending by resolving subscriptions and dispatching to appropriate channels
/// </summary>
public class NotificationDispatcher(
    IEnumerable<INotificationChannel> channels,
    INotificationSubscriptionResolver subscriptionResolver,
    ILogger<NotificationDispatcher> logger)
{
    private readonly Dictionary<string, INotificationChannel> _channelMap =
        channels.ToDictionary(c => c.ChannelName, StringComparer.OrdinalIgnoreCase);

    // ntfy publishes to a topic and desktop shows on this machine, so neither is per user.
    // They go out once per dispatch with the original notification, not once per recipient.
    // This is deliberate even when no recipients resolve: an ops alert to a topic has no user recipients.
    private static readonly NotificationChannelFlags BroadcastChannels =
        NotificationChannelFlags.Ntfy | NotificationChannelFlags.Desktop;

    // Single channel flags in value order; their lower-case names match the registered ChannelName values
    private static readonly NotificationChannelFlags[] SingleChannelFlags =
        [.. Enum.GetValues<NotificationChannelFlags>()
            .Where(flag => flag is not NotificationChannelFlags.None and not NotificationChannelFlags.All)];

    /// <summary>
    /// Sends a notification across multiple channels based on resolved user subscriptions
    /// </summary>
    /// <param name="notification">The notification to send</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Dispatch result containing channel-specific results and overall status</returns>
    public async Task<NotificationDispatchResult> SendAsync(
        UnifiedNotification notification,
        CancellationToken ct = default)
    {
        // Resolve subscriptions to get per-recipient notifications with enabled channels
        List<UnifiedNotification> resolvedNotifications =
            await subscriptionResolver.ResolveSubscriptionsAsync(
                notification,
                notification.SubscriptionContext,
                ct);

        // Create overall dispatch result
        Dictionary<string, NotificationChannelResult> allChannelResults = [];
        int totalSent = 0;
        int totalFailed = 0;

        // Send broadcast channels once with the original notification
        MergeResult(await SendToChannelsAsync(notification, notification.Channels & BroadcastChannels, ct));

        // Send to each resolved recipient
        foreach (UnifiedNotification resolvedNotification in resolvedNotifications)
        {
            MergeResult(await SendToChannelsAsync(resolvedNotification, resolvedNotification.Channels & ~BroadcastChannels, ct));
        }

        void MergeResult(NotificationDispatchResult dispatchResult)
        {
            // Merge channel results
            foreach (KeyValuePair<string, NotificationChannelResult> channelResult in dispatchResult.ChannelResults)
            {
                string key = channelResult.Key;
                if (allChannelResults.TryGetValue(key, out NotificationChannelResult? existingResult))
                {
                    // Combine results for the same channel
                    allChannelResults[key] = NotificationChannelResult.Partial(
                        key,
                        existingResult.SentCount + channelResult.Value.SentCount,
                        existingResult.FailedCount + channelResult.Value.FailedCount);
                }
                else
                {
                    allChannelResults[key] = channelResult.Value;
                }
            }

            totalSent += dispatchResult.TotalSentCount;
            totalFailed += dispatchResult.TotalFailedCount;
        }

        // Determine overall success
        bool isSuccess = allChannelResults.Values.Any(r => r.IsSuccess);

        logger.LogInformation(
            "Notification dispatch completed: Type={NotificationType}, Channels={ChannelCount}, Sent={SentCount}, Failed={FailedCount}",
            notification.NotificationType,
            allChannelResults.Count,
            totalSent,
            totalFailed);

        return NotificationDispatchResult.Create(notification.NotificationType, allChannelResults);
    }

    /// <summary>
    /// Sends a notification across the given channel flags
    /// </summary>
    private async Task<NotificationDispatchResult> SendToChannelsAsync(
        UnifiedNotification resolvedNotification,
        NotificationChannelFlags channelFlags,
        CancellationToken ct)
    {
        Dictionary<string, NotificationChannelResult> channelResults = [];
        List<Task<(string ChannelName, NotificationChannelResult Result)>> tasks = [];

        // Add tasks for each enabled channel flag
        foreach (NotificationChannelFlags flag in SingleChannelFlags.Where(flag => channelFlags.HasFlag(flag)))
        {
            if (_channelMap.TryGetValue(flag.ToString().ToLowerInvariant(), out INotificationChannel? channel))
            {
                tasks.Add(SendToChannelAsync(channel, resolvedNotification, ct));
            }
        }

        // Execute all channel sends in parallel
        (string ChannelName, NotificationChannelResult Result)[] results = await Task.WhenAll(tasks);

        // Populate channel results dictionary
        foreach ((string channelName, NotificationChannelResult result) in results)
        {
            channelResults[channelName] = result;
        }

        return NotificationDispatchResult.Create(resolvedNotification.NotificationType, channelResults);
    }

    /// <summary>
    /// Sends a notification to a specific channel with exception handling
    /// </summary>
    private async Task<(string ChannelName, NotificationChannelResult Result)> SendToChannelAsync(
        INotificationChannel channel,
        UnifiedNotification notification,
        CancellationToken ct)
    {
        try
        {
            NotificationChannelResult result = await channel.SendAsync(notification, ct);

            if (result.IsSuccess)
            {
                logger.LogInformation(
                    "Notification sent via {ChannelName}: Type={NotificationType}, Recipients={RecipientCount}",
                    channel.ChannelName,
                    notification.NotificationType,
                    result.SentCount);
            }
            else
            {
                logger.LogWarning(
                    "Notification failed via {ChannelName}: Type={NotificationType}, Error={ErrorMessage}",
                    channel.ChannelName,
                    notification.NotificationType,
                    result.ErrorMessage);
            }

            return (channel.ChannelName, result);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Exception while sending notification via {ChannelName}: Type={NotificationType}",
                channel.ChannelName,
                notification.NotificationType);

            return (channel.ChannelName, NotificationChannelResult.Failure(
                channel.ChannelName,
                $"Exception: {ex.Message}"));
        }
    }
}
