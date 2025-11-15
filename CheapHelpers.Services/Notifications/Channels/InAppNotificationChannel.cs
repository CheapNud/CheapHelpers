using CheapHelpers.Models.Entities;
using CheapHelpers.Services.Notifications.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CheapHelpers.Services.Notifications.Channels;

/// <summary>
/// Notification channel for in-app notifications with optional real-time delivery
/// </summary>
public class InAppNotificationChannel(
    IInAppNotificationService inAppService,
    INotificationRealTimeService? realTimeService,
    ILogger<InAppNotificationChannel> logger) : INotificationChannel
{
    /// <inheritdoc/>
    public string ChannelName => "inapp";

    /// <inheritdoc/>
    public bool SupportsNotificationType(string notificationType) => true;

    /// <inheritdoc/>
    public async Task<NotificationChannelResult> SendAsync(
        UnifiedNotification notification,
        CancellationToken cancellationToken = default)
    {
        var sentCount = 0;
        var failedCount = 0;

        try
        {
            foreach (var userId in notification.RecipientUserIds)
            {
                try
                {
                    var inAppNotification = new InAppNotification
                    {
                        NotificationType = notification.NotificationType,
                        Title = notification.Title,
                        Body = notification.Body,
                        HtmlBody = notification.HtmlBody,
                        UserId = userId,
                        Priority = notification.Priority,
                        ActionUrl = notification.ActionUrl,
                        IconUrl = notification.IconUrl,
                        DataJson = notification.Data != null
                            ? JsonSerializer.Serialize(notification.Data)
                            : null,
                        ExpiresAt = notification.ExpiresAt,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    var created = await inAppService.CreateAsync(inAppNotification, cancellationToken);

                    if (created != null)
                    {
                        sentCount++;

                        // Send real-time notification if service is available
                        if (realTimeService != null)
                        {
                            try
                            {
                                await realTimeService.NotifyUserAsync(userId, created, cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                logger.LogWarning(ex, "Failed to send real-time notification to user {UserId}", userId);
                            }
                        }
                    }
                    else
                    {
                        failedCount++;
                        logger.LogWarning("Failed to create in-app notification for user {UserId}", userId);
                    }
                }
                catch (Exception ex)
                {
                    failedCount++;
                    logger.LogError(ex, "Error creating in-app notification for user {UserId}", userId);
                }
            }

            if (sentCount > 0)
            {
                return NotificationChannelResult.Partial(ChannelName, sentCount, failedCount);
            }

            return NotificationChannelResult.Failure(
                ChannelName,
                "Failed to create in-app notifications for all recipients",
                failedCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending in-app notifications");
            return NotificationChannelResult.Failure(ChannelName, ex.Message);
        }
    }
}

/// <summary>
/// Service interface for managing in-app notifications
/// </summary>
public interface IInAppNotificationService
{
    /// <summary>
    /// Creates a new in-app notification
    /// </summary>
    Task<InAppNotification?> CreateAsync(InAppNotification notification, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service interface for real-time notification delivery
/// </summary>
public interface INotificationRealTimeService
{
    /// <summary>
    /// Sends a real-time notification to a specific user
    /// </summary>
    Task NotifyUserAsync(string userId, InAppNotification notification, CancellationToken cancellationToken = default);
}
