using CheapAvaloniaBlazor.Services;
using CheapHelpers.Services.Notifications;
using CheapHelpers.Services.Notifications.Models;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Avalonia.Bridge;

/// <summary>
/// Bridges CheapAvaloniaBlazor desktop OS notifications into the CheapHelpers unified notification system.
/// </summary>
/// <remarks>
/// <para>
/// This adapter allows desktop applications using CheapAvaloniaBlazor (Photino, MAUI WebView, etc.)
/// to participate in the CheapHelpers notification system as a delivery channel alongside
/// InApp, Email, SMS, and Push notifications.
/// </para>
/// <para>
/// Desktop notifications are delivered via the OS notification system (Windows Action Center,
/// macOS Notification Center, Linux notification daemon) using the browser Notification API
/// through the WebView bridge.
/// </para>
/// </remarks>
public class DesktopNotificationChannel(
    DesktopInteropService desktopInterop,
    ILogger<DesktopNotificationChannel> logger) : INotificationChannel
{
    /// <inheritdoc />
    public string ChannelName => "desktop";

    /// <inheritdoc />
    /// <remarks>
    /// Desktop notifications support all notification types as they are simple title+body displays.
    /// </remarks>
    public bool SupportsNotificationType(string notificationType) => true;

    /// <inheritdoc />
    public async Task<NotificationChannelResult> SendAsync(
        UnifiedNotification notification,
        CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Sending desktop notification to {RecipientCount} users: {Title}",
            notification.RecipientUserIds.Count,
            notification.Title);

        var successCount = 0;
        var failedUsers = new List<string>();

        // Desktop notifications are shown to the current user of the desktop application
        // Note: Unlike server-side channels (Email, SMS), desktop notifications are client-side
        // and show immediately to whoever is using the app, regardless of RecipientUserIds
        foreach (var userId in notification.RecipientUserIds)
        {
            try
            {
                await desktopInterop.ShowNotificationAsync(
                    notification.Title,
                    notification.Body ?? string.Empty);

                successCount++;

                logger.LogTrace(
                    "Desktop notification shown successfully for user {UserId}",
                    userId);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to show desktop notification for user {UserId}: {Error}",
                    userId,
                    ex.Message);

                failedUsers.Add(userId);
            }
        }

        logger.LogInformation(
            "Desktop notification delivery complete: {SuccessCount}/{TotalCount} succeeded",
            successCount,
            notification.RecipientUserIds.Count);

        if (failedUsers.Count == 0)
        {
            return NotificationChannelResult.Success(ChannelName, successCount);
        }
        else if (successCount == 0)
        {
            return NotificationChannelResult.Failure(
                ChannelName,
                $"All {failedUsers.Count} desktop notification(s) failed to display",
                failedUsers.Count);
        }
        else
        {
            return NotificationChannelResult.Partial(ChannelName, successCount, failedUsers.Count);
        }
    }
}
