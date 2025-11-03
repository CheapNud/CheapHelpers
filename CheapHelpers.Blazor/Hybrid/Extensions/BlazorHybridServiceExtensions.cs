using CheapHelpers.Blazor.Hybrid.Abstractions;
using CheapHelpers.Blazor.Hybrid.Notifications.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CheapHelpers.Blazor.Hybrid.Extensions;

/// <summary>
/// Extension methods for registering Blazor Hybrid services
/// </summary>
public static class BlazorHybridServiceExtensions
{
    /// <summary>
    /// Add Blazor Hybrid push notification services
    /// </summary>
    public static IServiceCollection AddBlazorHybridPushNotifications(
        this IServiceCollection services,
        Action<PushNotificationOptions>? configure = null)
    {
        var options = new PushNotificationOptions();
        configure?.Invoke(options);

        // Register core services
        services.AddSingleton<DeviceRegistrationManager>();

        // Register backend if provided
        if (options.BackendFactory != null)
        {
            services.AddSingleton(sp => options.BackendFactory(sp));
        }

        // Register preferences service if provided
        if (options.PreferencesServiceFactory != null)
        {
            services.AddSingleton(sp => options.PreferencesServiceFactory(sp));
        }

        return services;
    }

    /// <summary>
    /// Add WebView bridge for data extraction
    /// </summary>
    public static IServiceCollection AddWebViewBridge<TData>(
        this IServiceCollection services,
        Action<WebViewBridgeOptions>? configure = null) where TData : class
    {
        var options = new WebViewBridgeOptions();
        configure?.Invoke(options);

        // TODO: Implement WebViewStorageBridge<TData> registration when ready

        return services;
    }
}

/// <summary>
/// Options for push notification configuration
/// </summary>
public class PushNotificationOptions
{
    /// <summary>
    /// Factory for creating the push notification backend
    /// </summary>
    public Func<IServiceProvider, IPushNotificationBackend>? BackendFactory { get; set; }

    /// <summary>
    /// Factory for creating the preferences service
    /// </summary>
    public Func<IServiceProvider, IPreferencesService>? PreferencesServiceFactory { get; set; }

    /// <summary>
    /// Enable smart permission flow (check backend before requesting)
    /// </summary>
    public bool SmartPermissionFlow { get; set; } = true;

    /// <summary>
    /// Show foreground notifications when app is active
    /// </summary>
    public bool ForegroundNotifications { get; set; } = true;

    /// <summary>
    /// Configure Azure Notification Hubs backend
    /// </summary>
    public void UseAzureNotificationHubs(string connectionString, string hubName)
    {
        // TODO: Implement Azure NH backend when ready
        throw new NotImplementedException("Azure Notification Hubs backend not yet implemented");
    }

    /// <summary>
    /// Use a custom backend implementation
    /// </summary>
    public void UseCustomBackend<TBackend>() where TBackend : class, IPushNotificationBackend
    {
        BackendFactory = sp => sp.GetRequiredService<TBackend>();
    }
}

/// <summary>
/// Options for WebView bridge configuration
/// </summary>
public class WebViewBridgeOptions
{
    /// <summary>
    /// Storage keys to monitor
    /// </summary>
    public string[] StorageKeys { get; set; } = [];

    /// <summary>
    /// Polling interval for monitoring changes
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Enable cookie extraction
    /// </summary>
    public bool EnableCookies { get; set; }

    /// <summary>
    /// Enable DOM scraping for data extraction
    /// </summary>
    public bool EnableDomScraping { get; set; }
}
