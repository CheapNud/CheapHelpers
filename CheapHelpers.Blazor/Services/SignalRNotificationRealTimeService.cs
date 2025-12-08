using CheapHelpers.Blazor.Hubs;
using CheapHelpers.Models.Entities;
using CheapHelpers.Services.Notifications;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Blazor.Services;

/// <summary>
/// SignalR-based implementation of real-time notification delivery service
/// </summary>
/// <param name="hubContext">Hub context for sending messages to connected clients</param>
/// <param name="logger">Logger for tracking notification delivery</param>
public class SignalRNotificationRealTimeService(
    IHubContext<NotificationHub> hubContext,
    ILogger<SignalRNotificationRealTimeService> logger) : INotificationRealTimeService
{
    /// <summary>
    /// Sends a notification to a specific user in real-time
    /// </summary>
    /// <param name="userId">ID of the user to notify</param>
    /// <param name="notification">The notification to send</param>
    /// <param name="ct">Cancellation token</param>
    public async Task NotifyUserAsync(string userId, InAppNotification notification, CancellationToken ct)
    {
        logger.LogDebug("Sending real-time notification {NotificationId} to user {UserId}", notification.Id, userId);
        await hubContext.Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", notification, ct);
    }

    /// <summary>
    /// Sends a notification to multiple users in real-time
    /// </summary>
    /// <param name="userIds">IDs of the users to notify</param>
    /// <param name="notification">The notification to send</param>
    /// <param name="ct">Cancellation token</param>
    public async Task NotifyUsersAsync(IEnumerable<string> userIds, InAppNotification notification, CancellationToken ct)
    {
        var groups = userIds.Select(uid => $"user_{uid}").ToList();
        logger.LogDebug("Sending real-time notification {NotificationId} to {UserCount} users", notification.Id, groups.Count);
        await hubContext.Clients.Groups(groups).SendAsync("ReceiveNotification", notification, ct);
    }

    /// <summary>
    /// Broadcasts a notification to all connected users
    /// </summary>
    /// <param name="notification">The notification to broadcast</param>
    /// <param name="ct">Cancellation token</param>
    public async Task BroadcastAsync(InAppNotification notification, CancellationToken ct)
    {
        logger.LogDebug("Broadcasting real-time notification {NotificationId} to all users", notification.Id);
        await hubContext.Clients.All.SendAsync("ReceiveNotification", notification, ct);
    }
}
