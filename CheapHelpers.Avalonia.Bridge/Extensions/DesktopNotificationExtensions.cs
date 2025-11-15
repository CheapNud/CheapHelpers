using CheapHelpers.Services.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Avalonia.Bridge.Extensions;

/// <summary>
/// Extension methods for integrating desktop OS notifications with CheapHelpers notification system
/// </summary>
public static class DesktopNotificationExtensions
{
    /// <summary>
    /// Registers desktop OS notifications as a delivery channel in the CheapHelpers notification system
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <returns>The service collection for chaining</returns>
    /// <remarks>
    /// <para>
    /// <strong>PREREQUISITES:</strong>
    /// </para>
    /// <list type="number">
    /// <item><description>CheapAvaloniaBlazor services must be registered (including DesktopInteropService)</description></item>
    /// <item><description>CheapHelpers notification system must be registered via AddCheapNotifications()</description></item>
    /// </list>
    /// <para>
    /// <strong>Registration Order:</strong>
    /// </para>
    /// <code>
    /// builder.Services
    ///     .AddCheapAvaloniaBlazorServices()     // 1. Register CheapAvaloniaBlazor
    ///     .AddCheapNotifications&lt;MyUser&gt;()        // 2. Register CheapHelpers notifications
    ///     .AddDesktopNotificationBridge();      // 3. Register this bridge
    /// </code>
    /// <para>
    /// After registration, desktop notifications can be enabled for users by including
    /// NotificationChannelFlags.Desktop in notification channel preferences.
    /// </para>
    /// <para>
    /// <strong>Example Usage:</strong>
    /// </para>
    /// <code>
    /// // Send notification to desktop + email
    /// await notificationDispatcher.SendAsync(new UnifiedNotification
    /// {
    ///     NotificationType = "OrderShipped",
    ///     Title = "Your order has shipped!",
    ///     Body = "Track your package...",
    ///     RecipientUserIds = [userId],
    ///     Channels = NotificationChannelFlags.Desktop | NotificationChannelFlags.Email
    /// });
    /// </code>
    /// </remarks>
    public static IServiceCollection AddDesktopNotificationBridge(this IServiceCollection services)
    {
        // Register DesktopNotificationChannel as an INotificationChannel
        // The NotificationDispatcher will automatically discover it via DI
        services.AddScoped<INotificationChannel, DesktopNotificationChannel>();

        return services;
    }
}
