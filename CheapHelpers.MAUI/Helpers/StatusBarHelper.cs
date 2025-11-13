using System.Diagnostics;

#if ANDROID
using AndroidColor = Android.Graphics.Color;
using AndroidActivity = Android.App.Activity;
#elif IOS
using UIKit;
#endif

namespace CheapHelpers.MAUI.Helpers;

/// <summary>
/// Unified cross-platform helper for configuring status bars on iOS and Android.
/// Provides a consistent API for status bar configuration that works from any MAUI code
/// without requiring platform-specific knowledge.
/// </summary>
/// <remarks>
/// <para>
/// This helper abstracts platform differences and provides a single API for:
/// <list type="bullet">
/// <item>Status bar styling (light/dark content)</item>
/// <item>Status bar transparency</item>
/// <item>Background colors (Android only, iOS status bar is always transparent)</item>
/// <item>Navigation bar colors (Android only)</item>
/// <item>Status bar height retrieval</item>
/// </list>
/// </para>
/// <para>
/// <b>Platform-Specific Behavior:</b>
/// <list type="bullet">
/// <item><b>iOS:</b> Status bar is always transparent and overlays content. Only style (light/dark) can be configured.</item>
/// <item><b>Android:</b> Full control over status bar transparency, color, and navigation bar appearance.</item>
/// </list>
/// </para>
/// <para>
/// <b>Threading:</b> All methods automatically ensure they run on the UI thread using MainThread.InvokeOnMainThreadAsync.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Quick configuration for light background (dark icons)
/// StatusBarHelper.ConfigureForLightBackground();
///
/// // Custom configuration
/// var config = new StatusBarConfiguration
/// {
///     Style = StatusBarStyle.DarkContent,
///     IsTransparent = true,
///     NavigationBarColor = Colors.Black
/// };
/// StatusBarHelper.ConfigureStatusBar(config);
///
/// // Get status bar height for layout calculations
/// var height = StatusBarHelper.GetStatusBarHeight();
/// </code>
/// </example>
public static class StatusBarHelper
{
    /// <summary>
    /// Configures the status bar with the specified settings.
    /// This method automatically runs on the UI thread and works on both iOS and Android.
    /// </summary>
    /// <param name="config">The configuration settings to apply.</param>
    /// <remarks>
    /// <para>
    /// <b>Platform Behavior:</b>
    /// <list type="bullet">
    /// <item><b>iOS:</b> Only Style is applied. BackgroundColor, NavigationBarColor, and HideNavigationBar are ignored.</item>
    /// <item><b>Android:</b> All settings are applied. Requires API 21+ for transparency and colors.</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new StatusBarConfiguration
    /// {
    ///     Style = StatusBarStyle.LightContent,
    ///     IsTransparent = true,
    ///     NavigationBarColor = Colors.Black
    /// };
    /// StatusBarHelper.ConfigureStatusBar(config);
    /// </code>
    /// </example>
    public static void ConfigureStatusBar(StatusBarConfiguration config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
#if ANDROID
                ConfigureStatusBarAndroid(config);
#elif IOS
                ConfigureStatusBarIos(config);
#else
                Debug.WriteLine("StatusBarHelper: Platform not supported");
#endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"StatusBarHelper: Failed to configure status bar: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Configures the status bar for a light background (dark icons/text).
    /// This is the most common configuration for apps with light themes.
    /// </summary>
    /// <remarks>
    /// Equivalent to calling ConfigureStatusBar with Style = DarkContent and IsTransparent = true.
    /// </remarks>
    /// <example>
    /// <code>
    /// // In MauiProgram.cs, App.xaml.cs, or any page
    /// StatusBarHelper.ConfigureForLightBackground();
    /// </code>
    /// </example>
    public static void ConfigureForLightBackground()
    {
        ConfigureStatusBar(new StatusBarConfiguration
        {
            Style = StatusBarStyle.DarkContent,
            IsTransparent = true
        });
        Debug.WriteLine("StatusBarHelper: Configured for light background (dark icons)");
    }

    /// <summary>
    /// Configures the status bar for a dark background (light icons/text).
    /// Use this for apps with dark themes.
    /// </summary>
    /// <remarks>
    /// Equivalent to calling ConfigureStatusBar with Style = LightContent and IsTransparent = true.
    /// </remarks>
    /// <example>
    /// <code>
    /// // In MauiProgram.cs, App.xaml.cs, or any page
    /// StatusBarHelper.ConfigureForDarkBackground();
    /// </code>
    /// </example>
    public static void ConfigureForDarkBackground()
    {
        ConfigureStatusBar(new StatusBarConfiguration
        {
            Style = StatusBarStyle.LightContent,
            IsTransparent = true
        });
        Debug.WriteLine("StatusBarHelper: Configured for dark background (light icons)");
    }

    /// <summary>
    /// Sets the status bar to transparent with the specified style.
    /// </summary>
    /// <param name="style">The status bar style (light or dark content). Defaults to DarkContent.</param>
    /// <remarks>
    /// This is a convenience method for the most common use case: transparent status bar with custom styling.
    /// </remarks>
    /// <example>
    /// <code>
    /// StatusBarHelper.SetTransparent(StatusBarStyle.DarkContent);
    /// </code>
    /// </example>
    public static void SetTransparent(StatusBarStyle style = StatusBarStyle.DarkContent)
    {
        ConfigureStatusBar(new StatusBarConfiguration
        {
            Style = style,
            IsTransparent = true
        });
        Debug.WriteLine($"StatusBarHelper: Set transparent with {style} style");
    }

    /// <summary>
    /// Gets the current height of the status bar in device-independent units.
    /// </summary>
    /// <returns>The status bar height, or 0 if unavailable.</returns>
    /// <remarks>
    /// <para>
    /// Status bar height varies by platform and device:
    /// <list type="bullet">
    /// <item><b>iOS:</b> 20pt (standard), 44pt (notch devices), 0pt (hidden or landscape on some models)</item>
    /// <item><b>Android:</b> Typically 24dp, but varies by device and Android version</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Note:</b> This method returns immediately with the current value. It does not need to be awaited.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var statusBarHeight = StatusBarHelper.GetStatusBarHeight();
    /// var topMargin = statusBarHeight + 10; // Add 10 units of padding
    /// </code>
    /// </example>
    public static double GetStatusBarHeight()
    {
        try
        {
#if ANDROID
            return GetStatusBarHeightAndroid();
#elif IOS
            return GetStatusBarHeightIos();
#else
            Debug.WriteLine("StatusBarHelper: Platform not supported");
            return 0;
#endif
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"StatusBarHelper: Failed to get status bar height: {ex.Message}");
            return 0;
        }
    }

#if ANDROID
    private static void ConfigureStatusBarAndroid(StatusBarConfiguration config)
    {
        var activity = Platform.CurrentActivity;
        if (activity == null)
        {
            Debug.WriteLine("StatusBarHelper (Android): No current activity available");
            return;
        }

        var lightStatusBar = config.Style == StatusBarStyle.DarkContent;

        if (config.IsTransparent)
        {
            // Use the existing AndroidSystemBarsHelper for transparent status bar
            AndroidSystemBarsHelper.ConfigureTransparentStatusBar(
                activity,
                lightStatusBar,
                configureNavigationBar: false
            );
        }
        else if (config.BackgroundColor != null)
        {
            // Set opaque status bar with custom color
            var window = activity.Window;
            if (window != null && Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Lollipop)
            {
                var androidColor = ConvertToAndroidColor(config.BackgroundColor);
                window.SetStatusBarColor(androidColor);
                Debug.WriteLine($"StatusBarHelper (Android): Status bar color set to {config.BackgroundColor}");
            }
        }

        // Configure navigation bar if specified
        if (config.NavigationBarColor != null)
        {
            var navColor = ConvertToAndroidColor(config.NavigationBarColor);
            var lightNavigationBar = config.Style == StatusBarStyle.DarkContent;
            AndroidSystemBarsHelper.ConfigureNavigationBar(activity, navColor, lightNavigationBar);
        }

        Debug.WriteLine($"StatusBarHelper (Android): Configuration applied - Style: {config.Style}, Transparent: {config.IsTransparent}");
    }

    private static AndroidColor ConvertToAndroidColor(Microsoft.Maui.Graphics.Color color)
    {
        return AndroidColor.Argb(
            (int)(color.Alpha * 255),
            (int)(color.Red * 255),
            (int)(color.Green * 255),
            (int)(color.Blue * 255)
        );
    }

    private static double GetStatusBarHeightAndroid()
    {
        var activity = Platform.CurrentActivity;
        if (activity == null)
            return 0;

        var resources = activity.Resources;
        if (resources == null)
            return 0;

        var resourceId = resources.GetIdentifier("status_bar_height", "dimen", "android");
        if (resourceId > 0)
        {
            var heightInPixels = resources.GetDimensionPixelSize(resourceId);
            var density = resources.DisplayMetrics?.Density ?? 1;
            var heightInDp = heightInPixels / density;
            Debug.WriteLine($"StatusBarHelper (Android): Status bar height: {heightInDp}dp");
            return heightInDp;
        }

        return 0;
    }
#endif

#if IOS
    private static void ConfigureStatusBarIos(StatusBarConfiguration config)
    {
        var style = config.Style == StatusBarStyle.DarkContent
            ? UIStatusBarStyle.DarkContent
            : UIStatusBarStyle.LightContent;

        Platforms.iOS.IosStatusBarHelper.ConfigureStatusBarStyle(style);
        Debug.WriteLine($"StatusBarHelper (iOS): Configuration applied - Style: {config.Style}");
    }

    private static double GetStatusBarHeightIos()
    {
        var height = Platforms.iOS.IosStatusBarHelper.GetStatusBarHeight();
        Debug.WriteLine($"StatusBarHelper (iOS): Status bar height: {height}pt");
        return (double)height;
    }
#endif
}

/// <summary>
/// Configuration settings for status bar appearance across platforms.
/// </summary>
/// <remarks>
/// <para>
/// Platform support varies:
/// <list type="bullet">
/// <item><b>Style:</b> Supported on both iOS and Android</item>
/// <item><b>IsTransparent:</b> Supported on Android (API 21+), iOS status bar is always transparent</item>
/// <item><b>BackgroundColor:</b> Android only, ignored on iOS</item>
/// <item><b>NavigationBarColor:</b> Android only, ignored on iOS</item>
/// <item><b>HideNavigationBar:</b> Android only, ignored on iOS</item>
/// </list>
/// </para>
/// </remarks>
public class StatusBarConfiguration
{
    /// <summary>
    /// Gets or sets the status bar style (light or dark content).
    /// Default is DarkContent (dark icons for light backgrounds).
    /// </summary>
    public StatusBarStyle Style { get; set; } = StatusBarStyle.DarkContent;

    /// <summary>
    /// Gets or sets whether the status bar should be transparent.
    /// Default is true. iOS status bar is always transparent regardless of this setting.
    /// </summary>
    public bool IsTransparent { get; set; } = true;

    /// <summary>
    /// Gets or sets the status bar background color.
    /// Only applies to Android. Ignored if IsTransparent is true.
    /// </summary>
    public Microsoft.Maui.Graphics.Color? BackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the navigation bar color (Android only).
    /// Ignored on iOS which has no navigation bar.
    /// </summary>
    public Microsoft.Maui.Graphics.Color? NavigationBarColor { get; set; }

    /// <summary>
    /// Gets or sets whether to hide the navigation bar (Android only).
    /// Default is false. Ignored on iOS.
    /// </summary>
    public bool HideNavigationBar { get; set; } = false;
}

/// <summary>
/// Defines the style of status bar content (icons and text color).
/// </summary>
public enum StatusBarStyle
{
    /// <summary>
    /// Dark icons and text (for light backgrounds).
    /// This is the default and recommended for most apps.
    /// </summary>
    DarkContent,

    /// <summary>
    /// Light icons and text (for dark backgrounds).
    /// Use this when your app has a dark theme.
    /// </summary>
    LightContent
}
