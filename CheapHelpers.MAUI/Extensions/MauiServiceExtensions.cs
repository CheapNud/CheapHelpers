using CheapHelpers.Blazor.Hybrid.Abstractions;
using CheapHelpers.Blazor.Hybrid.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.MAUI.Extensions;

/// <summary>
/// Extension methods for registering MAUI platform-specific services
/// </summary>
public static class MauiServiceExtensions
{
    /// <summary>
    /// Register MAUI push notification services for the current platform (iOS or Android)
    /// IMPORTANT: Also call builder.Services.AddBlazorHybridPushNotifications() to register core services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Optional configuration for push notifications</param>
    /// <returns>The service collection for chaining</returns>
    /// <remarks>
    /// This method automatically registers the correct platform-specific implementations:
    /// - iOS: DeviceInstallationService (APNS), LocalNotificationService
    /// - Android: DeviceInstallationService (FCM), LocalNotificationService
    ///
    /// For Android, you must also:
    /// 1. Add google-services.json to your Android project with Build Action: GoogleServicesJson
    /// 2. Call FirebaseInitializer.Initialize(this) in your MainApplication.OnCreate()
    /// 3. Create a FirebaseMessagingService implementation inheriting from FcmService
    ///
    /// For iOS, you must also:
    /// 1. Make your AppDelegate inherit from ApnsDelegate
    /// 2. Configure APNS in your Apple Developer account
    ///
    /// Example usage:
    /// <code>
    /// builder.Services
    ///     .AddBlazorHybridPushNotifications(options =>
    ///     {
    ///         options.UseCustomBackend&lt;MyPushNotificationBackend&gt;();
    ///     })
    ///     .AddMauiPushNotifications();
    /// </code>
    /// </remarks>
    public static IServiceCollection AddMauiPushNotifications(
        this IServiceCollection services,
        Action<MauiPushNotificationOptions>? configure = null)
    {
        var options = new MauiPushNotificationOptions();
        configure?.Invoke(options);

#if IOS
        // Register iOS-specific services
        services.AddSingleton<IDeviceInstallationService, Platforms.iOS.DeviceInstallationService>();
        services.AddSingleton<ILocalNotificationService, Platforms.iOS.LocalNotificationService>();
#elif ANDROID
        // Register Android-specific services
        services.AddSingleton<IDeviceInstallationService, Platforms.Android.DeviceInstallationService>();

        // Create LocalNotificationService with custom or default channel settings
        if (!string.IsNullOrEmpty(options.AndroidChannelId))
        {
            services.AddSingleton<ILocalNotificationService>(sp =>
                new Platforms.Android.LocalNotificationService(
                    options.AndroidChannelId,
                    options.AndroidChannelName ?? "App Notifications",
                    options.AndroidChannelDescription ?? "Notifications from this app"
                ));
        }
        else
        {
            services.AddSingleton<ILocalNotificationService, Platforms.Android.LocalNotificationService>();
        }
#endif

        return services;
    }

    /// <summary>
    /// Register MAUI push notification services with iOS and Android specific configurations
    /// </summary>
    public static IServiceCollection AddMauiPushNotifications(
        this IServiceCollection services,
        Action<PushNotificationOptions> configureCore,
        Action<MauiPushNotificationOptions>? configurePlatform = null)
    {
        // Register core Blazor Hybrid services
        services.AddBlazorHybridPushNotifications(configureCore);

        // Register platform-specific services
        services.AddMauiPushNotifications(configurePlatform);

        return services;
    }
}

/// <summary>
/// Options for configuring MAUI platform-specific push notification features
/// </summary>
public class MauiPushNotificationOptions
{
    /// <summary>
    /// Android notification channel ID (Android only)
    /// </summary>
    public string? AndroidChannelId { get; set; }

    /// <summary>
    /// Android notification channel name (Android only)
    /// </summary>
    public string? AndroidChannelName { get; set; }

    /// <summary>
    /// Android notification channel description (Android only)
    /// </summary>
    public string? AndroidChannelDescription { get; set; }

    /// <summary>
    /// Configure Android notification channel
    /// </summary>
    public void ConfigureAndroidChannel(string channelId, string channelName, string channelDescription)
    {
        AndroidChannelId = channelId;
        AndroidChannelName = channelName;
        AndroidChannelDescription = channelDescription;
    }
}
