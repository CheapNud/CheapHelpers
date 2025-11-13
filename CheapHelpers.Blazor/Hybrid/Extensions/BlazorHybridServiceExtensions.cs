using CheapHelpers.Blazor.Hybrid.Abstractions;
using CheapHelpers.Blazor.Hybrid.Notifications.Core;
using CheapHelpers.Blazor.Hybrid.Services;
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

    /// <summary>
    /// Add app bar component services for Blazor Hybrid applications.
    /// This registers the IAppBarService for programmatic app bar control.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Optional configuration action</param>
    /// <returns>The service collection for chaining</returns>
    /// <remarks>
    /// <para>
    /// The app bar service is completely separate from system status bar configuration.
    /// It provides application-level navigation bar functionality with automatic status bar adjustment.
    /// </para>
    /// <para>
    /// <b>Usage:</b>
    /// <list type="number">
    /// <item>Register the service in your Program.cs or MauiProgram.cs</item>
    /// <item>Add the AppBar component to your layout</item>
    /// <item>Inject IAppBarService to control it programmatically</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In Program.cs / MauiProgram.cs
    /// builder.Services.AddAppBar(options =>
    /// {
    ///     options.DefaultBackgroundColor = "#6200EE";
    ///     options.DefaultTextColor = "#FFFFFF";
    ///     options.DefaultElevation = true;
    /// });
    ///
    /// // In a component
    /// @inject IAppBarService AppBarService
    ///
    /// protected override void OnInitialized()
    /// {
    ///     AppBarService.SetTitle("My Page");
    ///     AppBarService.SetVisible(true);
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddAppBar(
        this IServiceCollection services,
        Action<AppBarOptions>? configure = null)
    {
        var options = new AppBarOptions();
        configure?.Invoke(options);

        // Register the app bar service as a singleton
        services.AddSingleton<IAppBarService, AppBarService>();

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

/// <summary>
/// Options for app bar configuration
/// </summary>
public class AppBarOptions
{
    /// <summary>
    /// Default background color for the app bar
    /// </summary>
    public string DefaultBackgroundColor { get; set; } = "#6200EE";

    /// <summary>
    /// Default text color for the app bar
    /// </summary>
    public string DefaultTextColor { get; set; } = "#FFFFFF";

    /// <summary>
    /// Whether to apply elevation/shadow to the app bar by default
    /// </summary>
    public bool DefaultElevation { get; set; } = true;

    /// <summary>
    /// Default height for the app bar in pixels (excluding status bar)
    /// </summary>
    public double DefaultHeight { get; set; } = 56;

    /// <summary>
    /// Whether to automatically adjust for status bar by default
    /// </summary>
    public bool DefaultAdjustForStatusBar { get; set; } = true;
}
