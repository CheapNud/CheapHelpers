using CheapHelpers.MAUI.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace CheapHelpers.MAUI.Extensions;

/// <summary>
/// Extension methods for configuring status bar appearance during MAUI app startup.
/// Provides fluent API for simple, declarative status bar configuration in MauiProgram.cs.
/// </summary>
/// <remarks>
/// <para>
/// These extensions make it easy to configure status bars with a single line in your MauiProgram.cs:
/// <code>
/// builder.UseTransparentStatusBar(StatusBarStyle.DarkContent);
/// </code>
/// </para>
/// <para>
/// The configuration is applied immediately after the MauiApp is created,
/// ensuring the status bar is properly styled before your first page loads.
/// </para>
/// </remarks>
public static class MauiAppStatusBarExtensions
{
    /// <summary>
    /// Configures the status bar with the specified settings during app startup.
    /// The configuration is applied after the MauiApp is built.
    /// </summary>
    /// <param name="builder">The MauiAppBuilder instance.</param>
    /// <param name="config">The status bar configuration to apply.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method stores the configuration and applies it when the app starts.
    /// The actual configuration happens on the UI thread automatically.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public static MauiApp CreateMauiApp()
    /// {
    ///     var builder = MauiApp.CreateBuilder();
    ///
    ///     builder
    ///         .UseMauiApp&lt;App&gt;()
    ///         .ConfigureStatusBar(new StatusBarConfiguration
    ///         {
    ///             Style = StatusBarStyle.DarkContent,
    ///             IsTransparent = true,
    ///             NavigationBarColor = Colors.Black
    ///         });
    ///
    ///     return builder.Build();
    /// }
    /// </code>
    /// </example>
    public static MauiAppBuilder ConfigureStatusBar(
        this MauiAppBuilder builder,
        StatusBarConfiguration config)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        if (config == null)
            throw new ArgumentNullException(nameof(config));

        // Register a startup action to configure the status bar
        builder.Services.AddSingleton<IStatusBarConfigurationStartup>(
            new StatusBarConfigurationStartup(config));

        Debug.WriteLine("MauiAppStatusBarExtensions: Status bar configuration registered");

        return builder;
    }

    /// <summary>
    /// Configures a transparent status bar with the specified style during app startup.
    /// This is the most common configuration for modern MAUI apps.
    /// </summary>
    /// <param name="builder">The MauiAppBuilder instance.</param>
    /// <param name="style">The status bar style. Defaults to DarkContent (dark icons for light backgrounds).</param>
    /// <returns>The builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This is a convenience method equivalent to:
    /// <code>
    /// builder.ConfigureStatusBar(new StatusBarConfiguration
    /// {
    ///     Style = style,
    ///     IsTransparent = true
    /// });
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public static MauiApp CreateMauiApp()
    /// {
    ///     var builder = MauiApp.CreateBuilder();
    ///
    ///     builder
    ///         .UseMauiApp&lt;App&gt;()
    ///         .UseTransparentStatusBar(StatusBarStyle.DarkContent);
    ///
    ///     return builder.Build();
    /// }
    /// </code>
    /// </example>
    public static MauiAppBuilder UseTransparentStatusBar(
        this MauiAppBuilder builder,
        StatusBarStyle style = StatusBarStyle.DarkContent)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        return builder.ConfigureStatusBar(new StatusBarConfiguration
        {
            Style = style,
            IsTransparent = true
        });
    }

    /// <summary>
    /// Configures the status bar for a light background (dark icons/text) during app startup.
    /// </summary>
    /// <param name="builder">The MauiAppBuilder instance.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <remarks>
    /// This is a convenience method for apps with light themes.
    /// Equivalent to UseTransparentStatusBar(StatusBarStyle.DarkContent).
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.UseMauiApp&lt;App&gt;()
    ///        .UseLightStatusBar();
    /// </code>
    /// </example>
    public static MauiAppBuilder UseLightStatusBar(this MauiAppBuilder builder)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        return builder.UseTransparentStatusBar(StatusBarStyle.DarkContent);
    }

    /// <summary>
    /// Configures the status bar for a dark background (light icons/text) during app startup.
    /// </summary>
    /// <param name="builder">The MauiAppBuilder instance.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <remarks>
    /// This is a convenience method for apps with dark themes.
    /// Equivalent to UseTransparentStatusBar(StatusBarStyle.LightContent).
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.UseMauiApp&lt;App&gt;()
    ///        .UseDarkStatusBar();
    /// </code>
    /// </example>
    public static MauiAppBuilder UseDarkStatusBar(this MauiAppBuilder builder)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        return builder.UseTransparentStatusBar(StatusBarStyle.LightContent);
    }

    /// <summary>
    /// Configures the status bar with Android-specific navigation bar settings.
    /// </summary>
    /// <param name="builder">The MauiAppBuilder instance.</param>
    /// <param name="statusBarStyle">The status bar style.</param>
    /// <param name="navigationBarColor">The navigation bar color (Android only).</param>
    /// <returns>The builder for method chaining.</returns>
    /// <remarks>
    /// This method is useful when you need to control both status bar and navigation bar appearance.
    /// The navigationBarColor parameter is ignored on iOS.
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.UseMauiApp&lt;App&gt;()
    ///        .UseStatusBarWithNavigation(
    ///            StatusBarStyle.DarkContent,
    ///            Colors.White
    ///        );
    /// </code>
    /// </example>
    public static MauiAppBuilder UseStatusBarWithNavigation(
        this MauiAppBuilder builder,
        StatusBarStyle statusBarStyle,
        Microsoft.Maui.Graphics.Color navigationBarColor)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        return builder.ConfigureStatusBar(new StatusBarConfiguration
        {
            Style = statusBarStyle,
            IsTransparent = true,
            NavigationBarColor = navigationBarColor
        });
    }
}

/// <summary>
/// Internal interface for status bar configuration startup.
/// </summary>
internal interface IStatusBarConfigurationStartup
{
    void ApplyConfiguration();
}

/// <summary>
/// Internal implementation of status bar configuration startup.
/// This class is registered as a singleton and applies the configuration when the app starts.
/// </summary>
internal class StatusBarConfigurationStartup : IStatusBarConfigurationStartup
{
    private readonly StatusBarConfiguration _config;
    private bool _applied;

    public StatusBarConfigurationStartup(StatusBarConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public void ApplyConfiguration()
    {
        if (_applied)
        {
            Debug.WriteLine("StatusBarConfigurationStartup: Configuration already applied, skipping");
            return;
        }

        try
        {
            StatusBarHelper.ConfigureStatusBar(_config);
            _applied = true;
            Debug.WriteLine("StatusBarConfigurationStartup: Configuration applied successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"StatusBarConfigurationStartup: Failed to apply configuration: {ex.Message}");
        }
    }
}

/// <summary>
/// Extension methods for MauiApp to apply status bar configuration after build.
/// </summary>
public static class MauiAppStartupExtensions
{
    /// <summary>
    /// Applies any registered status bar configuration. This is called automatically
    /// but can be called manually if needed.
    /// </summary>
    /// <param name="app">The MauiApp instance.</param>
    /// <returns>The app for method chaining.</returns>
    public static MauiApp ApplyStatusBarConfiguration(this MauiApp app)
    {
        if (app == null)
            throw new ArgumentNullException(nameof(app));

        try
        {
            var startup = app.Services.GetService<IStatusBarConfigurationStartup>();
            startup?.ApplyConfiguration();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MauiAppStartupExtensions: Failed to apply status bar configuration: {ex.Message}");
        }

        return app;
    }
}
