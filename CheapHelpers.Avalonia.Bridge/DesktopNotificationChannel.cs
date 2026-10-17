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
        logger.LogDebug("Sending desktop notification: {Title}", notification.Title);

        // Desktop notifications are shown to the current user of the desktop application
        // Note: Unlike server-side channels (Email, SMS), desktop notifications are client-side
        // and show immediately to whoever is using the app, regardless of RecipientUserIds.
        // The dispatcher sends this channel once per dispatch, so it shows exactly once.
        try
        {
            await desktopInterop.ShowNotificationAsync(
                notification.Title,
                notification.Body ?? string.Empty);

            logger.LogTrace("Desktop notification shown successfully");

            return NotificationChannelResult.Success(ChannelName);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to show desktop notification: {Error}",
                ex.Message);

            return NotificationChannelResult.Failure(
                ChannelName,
                "Desktop notification failed to display");
        }
    }
}
