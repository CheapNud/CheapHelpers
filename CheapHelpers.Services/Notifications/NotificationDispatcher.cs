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

        // Send to each resolved recipient
        foreach (UnifiedNotification resolvedNotification in resolvedNotifications)
        {
            NotificationDispatchResult recipientResult =
                await SendToResolvedRecipientAsync(resolvedNotification, ct);

            // Merge channel results
            foreach (KeyValuePair<string, NotificationChannelResult> channelResult in recipientResult.ChannelResults)
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

            totalSent += recipientResult.TotalSentCount;
            totalFailed += recipientResult.TotalFailedCount;
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
    /// Sends a resolved notification to a single recipient across their enabled channels
    /// </summary>
    private async Task<NotificationDispatchResult> SendToResolvedRecipientAsync(
        UnifiedNotification resolvedNotification,
        CancellationToken ct)
    {
        Dictionary<string, NotificationChannelResult> channelResults = [];
        List<Task<(string ChannelName, NotificationChannelResult Result)>> tasks = [];

        // Add tasks for each enabled channel flag
        if (resolvedNotification.Channels.HasFlag(NotificationChannelFlags.InApp))
        {
            if (_channelMap.TryGetValue("inapp", out INotificationChannel? channel))
            {
                tasks.Add(SendToChannelAsync(channel, resolvedNotification, ct));
            }
        }

        if (resolvedNotification.Channels.HasFlag(NotificationChannelFlags.Email))
        {
            if (_channelMap.TryGetValue("email", out INotificationChannel? channel))
            {
                tasks.Add(SendToChannelAsync(channel, resolvedNotification, ct));
            }
        }

        if (resolvedNotification.Channels.HasFlag(NotificationChannelFlags.Sms))
        {
            if (_channelMap.TryGetValue("sms", out INotificationChannel? channel))
            {
                tasks.Add(SendToChannelAsync(channel, resolvedNotification, ct));
            }
        }

        if (resolvedNotification.Channels.HasFlag(NotificationChannelFlags.Push))
        {
            if (_channelMap.TryGetValue("push", out INotificationChannel? channel))
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
