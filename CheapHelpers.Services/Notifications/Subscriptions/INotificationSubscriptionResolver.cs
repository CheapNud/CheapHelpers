using CheapHelpers.Services.Notifications.Models;

namespace CheapHelpers.Services.Notifications.Subscriptions;

/// <summary>
/// Resolves notification subscriptions by determining which channels each user should receive notifications on.
/// Uses registered <see cref="INotificationSubscriptionProvider"/> instances to determine enabled channels.
/// </summary>
public interface INotificationSubscriptionResolver
{
    /// <summary>
    /// Resolves subscriptions for a notification by determining which channels each target user should receive it on.
    /// </summary>
    /// <param name="notification">The notification to resolve subscriptions for.</param>
    /// <param name="context">Optional context containing additional information about the subscription request.</param>
    /// <param name="ct">Cancellation token for async operations.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a list of
    /// <see cref="UnifiedNotification"/> instances, one per user, with their enabled channels resolved.
    /// Users who have no enabled channels will be excluded from the result list.
    /// </returns>
    /// <remarks>
    /// This method:
    /// 1. Iterates through all target users in the notification
    /// 2. For each user, queries registered providers (in priority order) to determine enabled channels
    /// 3. Applies Do Not Disturb settings if applicable
    /// 4. Returns a list of notifications with resolved channel flags per user
    /// </remarks>
    Task<List<UnifiedNotification>> ResolveSubscriptionsAsync(
        UnifiedNotification notification,
        ISubscriptionContext? context,
        CancellationToken ct = default);
}
