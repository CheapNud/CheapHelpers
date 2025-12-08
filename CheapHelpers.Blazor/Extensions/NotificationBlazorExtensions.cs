using CheapHelpers.Blazor.Hubs;
using CheapHelpers.Blazor.Services;
using CheapHelpers.Services.Notifications;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Blazor.Extensions;

/// <summary>
/// Extension methods for registering Blazor-specific notification services
/// </summary>
public static class NotificationBlazorExtensions
{
    /// <summary>
    /// Adds Blazor notification services including SignalR support for real-time notifications
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <returns>The service collection for chaining</returns>
    /// <remarks>
    /// This method registers:
    /// <list type="bullet">
    /// <item><description>SignalR services for real-time notification delivery</description></item>
    /// <item><description>INotificationRealTimeService implementation using SignalR</description></item>
    /// </list>
    /// <para>
    /// After calling this method, you must call MapCheapNotificationsHub() on the endpoint
    /// route builder to map the SignalR hub endpoint.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddCheapNotificationsBlazor(this IServiceCollection services)
    {
        // Register SignalR
        services.AddSignalR();

        // Register SignalR-based real-time notification service
        services.AddScoped<INotificationRealTimeService, SignalRNotificationRealTimeService>();

        return services;
    }

    /// <summary>
    /// Maps the notification SignalR hub endpoint
    /// </summary>
    /// <param name="endpoints">The endpoint route builder</param>
    /// <returns>The endpoint route builder for chaining</returns>
    /// <remarks>
    /// This method maps the NotificationHub to the "/hubs/notifications" endpoint.
    /// Clients can connect to this endpoint to receive real-time notifications.
    /// </remarks>
    public static IEndpointRouteBuilder MapCheapNotificationsHub(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<NotificationHub>("/hubs/notifications");
        return endpoints;
    }
}
