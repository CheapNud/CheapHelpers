using CheapHelpers.Models.Enums;

namespace CheapHelpers.Services.Notifications.Subscriptions;

/// <summary>
/// Defines a provider that determines which notification channels are enabled for a user and notification type.
/// Providers are evaluated in priority order (highest first) until one handles the request.
/// </summary>
public interface INotificationSubscriptionProvider
{
    /// <summary>
    /// Gets the priority of this provider. Higher values are evaluated first.
    /// </summary>
    /// <remarks>
    /// Typical priority values:
    /// - 1000+: Override providers (user preferences, admin overrides)
    /// - 500-999: Application-specific providers
    /// - 100-499: Default providers
    /// - 1-99: Fallback providers
    /// </remarks>
    int Priority { get; }

    /// <summary>
    /// Gets the name of this provider for logging and diagnostics.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Determines whether this provider can handle the given subscription context.
    /// </summary>
    /// <param name="subscriptionContext">The context containing information about the subscription request.</param>
    /// <returns>True if this provider can handle the context; otherwise, false.</returns>
    bool CanHandle(ISubscriptionContext? subscriptionContext);

    /// <summary>
    /// Gets the enabled notification channels for a specific user and notification type.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="notificationType">The type of notification being sent.</param>
    /// <param name="context">Optional context containing additional information about the subscription request.</param>
    /// <param name="ct">Cancellation token for async operations.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains:
    /// - The enabled channels as a <see cref="NotificationChannelFlags"/> if this provider handles the request
    /// - Null if this provider cannot determine the enabled channels (next provider will be tried)
    /// </returns>
    Task<NotificationChannelFlags?> GetEnabledChannelsAsync(
        string userId,
        string notificationType,
        ISubscriptionContext? context,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the channels that are currently in "Do Not Disturb" mode for a user.
    /// Notifications sent to these channels may be queued, delayed, or suppressed based on implementation.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="ct">Cancellation token for async operations.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains:
    /// - The channels in DND mode as a <see cref="NotificationChannelFlags"/>
    /// - Null if this provider does not implement DND functionality (default implementation)
    /// </returns>
    Task<NotificationChannelFlags?> GetDoNotDisturbChannelsAsync(
        string userId,
        CancellationToken ct = default)
    {
        return Task.FromResult<NotificationChannelFlags?>(null);
    }
}
