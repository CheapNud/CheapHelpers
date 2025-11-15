using CheapHelpers.Models.Entities;

namespace CheapHelpers.Services.Notifications;

/// <summary>
/// Service for delivering real-time notifications to connected users via SignalR
/// </summary>
public interface INotificationRealTimeService
{
    /// <summary>
    /// Sends a notification to a specific user in real-time
    /// </summary>
    /// <param name="userId">ID of the user to notify</param>
    /// <param name="notification">The notification to send</param>
    /// <param name="ct">Cancellation token</param>
    Task NotifyUserAsync(string userId, InAppNotification notification, CancellationToken ct);

    /// <summary>
    /// Sends a notification to multiple users in real-time
    /// </summary>
    /// <param name="userIds">IDs of the users to notify</param>
    /// <param name="notification">The notification to send</param>
    /// <param name="ct">Cancellation token</param>
    Task NotifyUsersAsync(IEnumerable<string> userIds, InAppNotification notification, CancellationToken ct);

    /// <summary>
    /// Broadcasts a notification to all connected users
    /// </summary>
    /// <param name="notification">The notification to broadcast</param>
    /// <param name="ct">Cancellation token</param>
    Task BroadcastAsync(InAppNotification notification, CancellationToken ct);
}
