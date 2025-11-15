using CheapHelpers.Models.Enums;
using CheapHelpers.Services.Notifications.Channels;
using CheapHelpers.Services.Notifications.Configuration;
using CheapHelpers.Services.Notifications.Subscriptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Services.Notifications.Extensions;

/// <summary>
/// Extension methods for registering notification services
/// </summary>
public static class NotificationServiceExtensions
{
    /// <summary>
    /// Registers the CheapHelpers notification system with dependency injection
    /// </summary>
    /// <typeparam name="TUser">The user type that inherits from IdentityUser</typeparam>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="configureOptions">Optional action to configure notification options</param>
    /// <returns>The service collection for chaining</returns>
    /// <remarks>
    /// <para>
    /// IMPORTANT: This method registers notification channels, but some channels have dependencies
    /// that must be registered separately:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Email channel requires IEmailService to be registered</description></item>
    /// <item><description>SMS channel requires ISmsService to be registered</description></item>
    /// <item><description>Push channel requires IPushNotificationBackend to be registered</description></item>
    /// </list>
    /// <para>
    /// If these dependencies are not registered, the respective channels will fail at runtime
    /// when attempting to send notifications.
    /// </para>
    /// <para>
    /// For Blazor applications with real-time notifications, use AddCheapNotificationsBlazor()
    /// to register SignalR support after calling this method.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddCheapNotifications<TUser>(
        this IServiceCollection services,
        Action<NotificationOptions>? configureOptions = null)
        where TUser : IdentityUser
    {
        // Create and configure options
        NotificationOptions options = new();
        configureOptions?.Invoke(options);

        // Register options with DI
        services.Configure<NotificationOptions>(opts =>
        {
            opts.RetentionDays = options.RetentionDays;
            opts.MaxStoredPerUser = options.MaxStoredPerUser;
            opts.EnableAutoCleanup = options.EnableAutoCleanup;
            opts.RealTimeProvider = options.RealTimeProvider;
            opts.EnableRedisCache = options.EnableRedisCache;
            opts.RedisConnectionString = options.RedisConnectionString;
            opts.RabbitMQConnectionString = options.RabbitMQConnectionString;
            opts.EnabledChannels = options.EnabledChannels;
        });

        // Register core services
        services.AddScoped<IInAppNotificationService, InAppNotificationService<TUser>>();

        // Register subscription system
        services.AddScoped<INotificationSubscriptionProvider, GlobalUserPreferencesProvider<TUser>>();
        services.AddScoped<INotificationSubscriptionResolver, NotificationSubscriptionResolver>();

        // Register notification channels based on enabled channels configuration
        if (options.EnabledChannels.HasFlag(NotificationChannelFlags.InApp))
        {
            services.AddScoped<INotificationChannel, InAppNotificationChannel>();
        }

        if (options.EnabledChannels.HasFlag(NotificationChannelFlags.Email))
        {
            services.AddScoped<INotificationChannel, EmailNotificationChannel>();
        }

        if (options.EnabledChannels.HasFlag(NotificationChannelFlags.Sms))
        {
            services.AddScoped<INotificationChannel, SmsNotificationChannel>();
        }

        // Note: Push channel is in Blazor project and should be registered separately
        // if (options.EnabledChannels.HasFlag(NotificationChannelFlags.Push))
        // {
        //     services.AddScoped<INotificationChannel, PushNotificationChannel>();
        // }

        // Register notification dispatcher
        services.AddScoped<NotificationDispatcher>();

        // Configure Redis caching if enabled
        if (options.EnableRedisCache)
        {
            // TODO: Implement Redis caching support
            // services.AddStackExchangeRedisCache(redisOptions =>
            // {
            //     redisOptions.Configuration = options.RedisConnectionString;
            // });
        }

        // Configure real-time provider
        if (string.Equals(options.RealTimeProvider, "SignalR", StringComparison.OrdinalIgnoreCase))
        {
            // SignalR registration is handled in Blazor project via AddCheapNotificationsBlazor()
            // because INotificationRealTimeService implementation is in Blazor assembly
        }
        else if (string.Equals(options.RealTimeProvider, "RabbitMQ", StringComparison.OrdinalIgnoreCase))
        {
            // TODO: Implement RabbitMQ real-time notification support
            // services.AddScoped<INotificationRealTimeService, RabbitMQNotificationRealTimeService>();
        }

        return services;
    }
}
