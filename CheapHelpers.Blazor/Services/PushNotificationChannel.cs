using CheapHelpers.Blazor.Hybrid.Abstractions;
using CheapHelpers.Blazor.Hybrid.Models;
using CheapHelpers.Services.Notifications;
using CheapHelpers.Services.Notifications.Models;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Blazor.Services;

/// <summary>
/// Notification channel for push notification delivery
/// </summary>
public class PushNotificationChannel(
    IPushNotificationBackend pushBackend,
    ILogger<PushNotificationChannel> logger) : INotificationChannel
{
    /// <inheritdoc/>
    public string ChannelName => "push";

    /// <inheritdoc/>
    public bool SupportsNotificationType(string notificationType) => true;

    /// <inheritdoc/>
    public async Task<NotificationChannelResult> SendAsync(
        UnifiedNotification notification,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var allDevices = new List<DeviceInfo>();

            // Get devices for all recipient users
            foreach (var userId in notification.RecipientUserIds)
            {
                try
                {
                    var userDevices = await pushBackend.GetUserDevicesAsync(userId);
                    if (userDevices != null && userDevices.Count > 0)
                    {
                        allDevices.AddRange(userDevices);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to retrieve devices for user {UserId}", userId);
                }
            }

            if (allDevices.Count == 0)
            {
                return NotificationChannelResult.Failure(
                    ChannelName,
                    "No devices found for recipients");
            }

            // Create notification payload
            var payload = new NotificationPayload
            {
                Title = notification.Title,
                Body = notification.Body,
                DeviceIds = allDevices.Select(d => d.DeviceId).ToList(),
                Data = notification.Data,
                Silent = notification.Silent
            };

            var pushResult = await pushBackend.SendNotificationAsync(payload);

            if (pushResult.Success)
            {
                return NotificationChannelResult.Partial(
                    ChannelName,
                    pushResult.SuccessCount,
                    pushResult.FailureCount);
            }

            return NotificationChannelResult.Failure(
                ChannelName,
                pushResult.ErrorMessage ?? "Failed to send push notifications",
                pushResult.FailureCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending push notifications");
            return NotificationChannelResult.Failure(ChannelName, ex.Message);
        }
    }
}
