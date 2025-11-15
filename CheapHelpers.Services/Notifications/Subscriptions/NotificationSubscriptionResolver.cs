using CheapHelpers.Models.Enums;
using CheapHelpers.Services.Notifications.Models;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Services.Notifications.Subscriptions;

/// <summary>
/// Resolves notification subscriptions by determining which channels each user should receive notifications on.
/// Evaluates registered providers in priority order to determine enabled channels and applies DND rules.
/// </summary>
public class NotificationSubscriptionResolver(
    IEnumerable<INotificationSubscriptionProvider> providers,
    ILogger<NotificationSubscriptionResolver> logger) : INotificationSubscriptionResolver
{
    private readonly List<INotificationSubscriptionProvider> _orderedProviders =
        providers.OrderByDescending(p => p.Priority).ToList();

    /// <summary>
    /// Resolves subscriptions for a notification by determining which channels each target user should receive it on.
    /// </summary>
    /// <param name="notification">The notification to resolve subscriptions for.</param>
    /// <param name="context">Optional context containing additional information about the subscription request.</param>
    /// <param name="ct">Cancellation token for async operations.</param>
    /// <returns>
    /// A list of UnifiedNotification instances, one per user, with their enabled channels resolved.
    /// Users who have no enabled channels (NotificationChannelFlags.None) are excluded from the result.
    /// </returns>
    public async Task<List<UnifiedNotification>> ResolveSubscriptionsAsync(
        UnifiedNotification notification,
        ISubscriptionContext? context,
        CancellationToken ct = default)
    {
        logger.LogDebug(
            "Resolving subscriptions for notification type {NotificationType} with {RecipientCount} recipients",
            notification.NotificationType,
            notification.RecipientUserIds.Count);

        var resolvedNotifications = new List<UnifiedNotification>();

        foreach (var userId in notification.RecipientUserIds)
        {
            var enabledChannels = await ResolveUserChannelsAsync(
                userId,
                notification.NotificationType,
                notification.Channels,
                context,
                ct);

            if (enabledChannels == NotificationChannelFlags.None)
            {
                logger.LogDebug(
                    "Skipping user {UserId} - no enabled channels for notification type {NotificationType}",
                    userId,
                    notification.NotificationType);
                continue;
            }

            // Create a per-recipient notification with resolved channels
            var recipientNotification = notification with
            {
                RecipientUserIds = [userId],
                Channels = enabledChannels
            };

            resolvedNotifications.Add(recipientNotification);

            logger.LogDebug(
                "Resolved channels for user {UserId}: {Channels}",
                userId,
                enabledChannels);
        }

        logger.LogInformation(
            "Resolved {ResolvedCount} notifications from {OriginalCount} recipients for type {NotificationType}",
            resolvedNotifications.Count,
            notification.RecipientUserIds.Count,
            notification.NotificationType);

        return resolvedNotifications;
    }

    /// <summary>
    /// Resolves the enabled channels for a specific user by querying providers in priority order.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="notificationType">The type of notification being sent.</param>
    /// <param name="defaultChannels">The default channels to use if no provider returns a value.</param>
    /// <param name="context">Optional context containing additional information about the subscription request.</param>
    /// <param name="ct">Cancellation token for async operations.</param>
    /// <returns>The resolved notification channels after applying provider rules and DND settings.</returns>
    private async Task<NotificationChannelFlags> ResolveUserChannelsAsync(
        string userId,
        string notificationType,
        NotificationChannelFlags defaultChannels,
        ISubscriptionContext? context,
        CancellationToken ct)
    {
        NotificationChannelFlags? enabledChannels = null;

        // Try providers in priority order (highest first)
        foreach (var provider in _orderedProviders)
        {
            if (!provider.CanHandle(context))
            {
                logger.LogDebug(
                    "Provider {ProviderName} cannot handle context, skipping",
                    provider.Name);
                continue;
            }

            var channels = await provider.GetEnabledChannelsAsync(
                userId,
                notificationType,
                context,
                ct);

            if (channels.HasValue)
            {
                enabledChannels = channels.Value;
                logger.LogInformation(
                    "Provider {ProviderName} (Priority={Priority}) returned channels for UserId={UserId}, NotificationType={NotificationType}: {Channels}",
                    provider.Name,
                    provider.Priority,
                    userId,
                    notificationType,
                    enabledChannels);
                break;
            }

            logger.LogDebug(
                "Provider {ProviderName} returned null, trying next provider",
                provider.Name);
        }

        // If no provider returned a value, use the notification's default channels
        if (!enabledChannels.HasValue)
        {
            enabledChannels = defaultChannels;
            logger.LogDebug(
                "No provider returned channels for UserId={UserId}, using default: {Channels}",
                userId,
                enabledChannels);
        }

        // Apply Do Not Disturb settings from all providers
        var finalChannels = await ApplyDoNotDisturbAsync(userId, enabledChannels.Value, ct);

        return finalChannels;
    }

    /// <summary>
    /// Applies Do Not Disturb settings from all providers to the enabled channels.
    /// Checks all providers for DND settings and removes DND channels from enabled channels.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="enabledChannels">The currently enabled channels.</param>
    /// <param name="ct">Cancellation token for async operations.</param>
    /// <returns>The modified channels after applying DND rules.</returns>
    private async Task<NotificationChannelFlags> ApplyDoNotDisturbAsync(
        string userId,
        NotificationChannelFlags enabledChannels,
        CancellationToken ct)
    {
        var modifiedChannels = enabledChannels;

        // Check ALL providers for DND settings (not just the one that handled enabled channels)
        foreach (var provider in _orderedProviders)
        {
            var dndChannels = await provider.GetDoNotDisturbChannelsAsync(userId, ct);

            if (dndChannels.HasValue && dndChannels.Value != NotificationChannelFlags.None)
            {
                // Remove DND channels from enabled channels using bitwise AND with NOT
                modifiedChannels &= ~dndChannels.Value;

                logger.LogDebug(
                    "Provider {ProviderName} applied DND for UserId={UserId}, removing channels {DndChannels}. Before={Before}, After={After}",
                    provider.Name,
                    userId,
                    dndChannels.Value,
                    enabledChannels,
                    modifiedChannels);
            }
        }

        return modifiedChannels;
    }
}
