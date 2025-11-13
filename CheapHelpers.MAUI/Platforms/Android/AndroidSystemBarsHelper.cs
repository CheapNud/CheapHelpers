#if ANDROID
using Android.OS;
using AndroidX.Core.View;
using AndroidActivity = Android.App.Activity;
using AndroidView = Android.Views.View;
using WindowManagerFlags = Android.Views.WindowManagerFlags;
using SystemUiFlags = Android.Views.SystemUiFlags;
using StatusBarVisibility = Android.Views.StatusBarVisibility;

namespace CheapHelpers.MAUI.Platforms.Android;

/// <summary>
/// Comprehensive helper utility for configuring Android system bars (status bar and navigation bar).
/// Extracted from production patterns to provide transparent status bars, proper navigation bar handling,
/// and window insets management for edge-to-edge layouts.
/// </summary>
/// <remarks>
/// This helper simplifies common Android system bar configuration tasks:
/// <list type="bullet">
/// <item>Transparent status bars with light/dark icons</item>
/// <item>Opaque navigation bars with customizable colors</item>
/// <item>Window insets handling for proper content padding</item>
/// </list>
/// <para>
/// <b>API Level Requirements:</b>
/// <list type="bullet">
/// <item>Status bar transparency: API 21+ (Lollipop)</item>
/// <item>Light status bar icons: API 23+ (Marshmallow)</item>
/// <item>SetDecorFitsSystemWindows: API 30+ (Android 11)</item>
/// </list>
/// </para>
/// </remarks>
public static class AndroidSystemBarsHelper
{
    /// <summary>
    /// Configures a transparent status bar with light icons and proper system window handling.
    /// This is the most common configuration for modern Android apps with edge-to-edge content.
    /// </summary>
    /// <param name="activity">The activity whose status bar should be configured</param>
    /// <param name="lightStatusBar">Whether to use light status bar (dark icons). Default is true.</param>
    /// <param name="configureNavigationBar">Whether to also configure navigation bar as black. Default is true.</param>
    /// <exception cref="ArgumentNullException">Thrown when activity is null</exception>
    /// <remarks>
    /// <para>
    /// This method performs the following operations:
    /// <list type="number">
    /// <item>Sets status bar to transparent</item>
    /// <item>Optionally sets navigation bar to opaque black</item>
    /// <item>Clears translucency flags to prevent content bleeding</item>
    /// <item>Configures proper window decoration fitting (API 30+) or root view fitting (API 21-29)</item>
    /// <item>Sets light status bar flag if supported (API 23+)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Requires:</b> API 21+ (Android 5.0 Lollipop) for transparency.
    /// Light icons require API 23+ (Android 6.0 Marshmallow).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override void OnCreate(Bundle? savedInstanceState)
    /// {
    ///     base.OnCreate(savedInstanceState);
    ///
    ///     // Configure transparent status bar with light icons
    ///     AndroidSystemBarsHelper.ConfigureTransparentStatusBar(this);
    /// }
    /// </code>
    /// </example>
    public static void ConfigureTransparentStatusBar(AndroidActivity activity, bool lightStatusBar = true, bool configureNavigationBar = true)
    {
        if (activity == null)
            throw new ArgumentNullException(nameof(activity));

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
        {
            var window = activity.Window;
            if (window == null)
            {
                System.Diagnostics.Debug.WriteLine("AndroidSystemBarsHelper: Window is null, cannot configure status bar");
                return;
            }

            // Make status bar transparent but keep icons visible
            window.SetStatusBarColor(global::Android.Graphics.Color.Transparent);

            // Configure navigation bar if requested
            if (configureNavigationBar)
            {
                // Force navigation bar to be FULLY OPAQUE black
                window.SetNavigationBarColor(global::Android.Graphics.Color.Black);
            }

            // Clear ALL flags that might make bars translucent or allow content behind
            window.ClearFlags(WindowManagerFlags.TranslucentNavigation);
            window.ClearFlags(WindowManagerFlags.TranslucentStatus);
            window.ClearFlags(WindowManagerFlags.Fullscreen);
            window.ClearFlags(WindowManagerFlags.LayoutNoLimits);

            // Add flag to draw system bar backgrounds
            window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);

            // Force the window to NOT extend behind navigation bar
            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                // Android 11+ (API 30) - Force decor to FIT system windows
#pragma warning disable CA1416 // Validate platform compatibility - already checked with BuildVersionCodes.R
                window.SetDecorFitsSystemWindows(true);
#pragma warning restore CA1416
            }
            else if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                // For older versions, use the root view approach
                var rootView = window.DecorView?.RootView;
                if (rootView != null)
                {
                    rootView.SetFitsSystemWindows(true);
                }
            }

            // Set proper flags for system UI (light status bar)
            if (lightStatusBar && Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                var decorView = window.DecorView;
                if (decorView != null)
                {
                    // Use ONLY the light status bar flag, avoid all layout flags
#pragma warning disable CS0618 // SystemUiVisibility is obsolete but required for API < 30
                    decorView.SystemUiVisibility = (StatusBarVisibility)SystemUiFlags.LightStatusBar;
#pragma warning restore CS0618
                }
            }

            System.Diagnostics.Debug.WriteLine($"AndroidSystemBarsHelper: Status bar configured (light: {lightStatusBar}, navigation: {configureNavigationBar})");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("AndroidSystemBarsHelper: API level < 21, status bar configuration not supported");
        }
    }

    /// <summary>
    /// Configures the navigation bar with a custom color and optional light navigation bar mode.
    /// </summary>
    /// <param name="activity">The activity whose navigation bar should be configured</param>
    /// <param name="color">The color for the navigation bar. If null, defaults to black.</param>
    /// <param name="lightNavigationBar">Whether to use light navigation bar (dark icons). Requires API 27+.</param>
    /// <exception cref="ArgumentNullException">Thrown when activity is null</exception>
    /// <remarks>
    /// <para>
    /// <b>Requires:</b> API 21+ (Android 5.0 Lollipop) for navigation bar color.
    /// Light navigation bar requires API 27+ (Android 8.1 Oreo).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Set navigation bar to white with dark icons
    /// AndroidSystemBarsHelper.ConfigureNavigationBar(
    ///     this,
    ///     global::Android.Graphics.Color.White,
    ///     lightNavigationBar: true
    /// );
    /// </code>
    /// </example>
    public static void ConfigureNavigationBar(AndroidActivity activity, global::Android.Graphics.Color? color = null, bool lightNavigationBar = false)
    {
        if (activity == null)
            throw new ArgumentNullException(nameof(activity));

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
        {
            var window = activity.Window;
            if (window == null)
            {
                System.Diagnostics.Debug.WriteLine("AndroidSystemBarsHelper: Window is null, cannot configure navigation bar");
                return;
            }

            // Set navigation bar color (default to black if not specified)
            var navColor = color ?? global::Android.Graphics.Color.Black;
            window.SetNavigationBarColor(navColor);

            // Add flag to draw system bar backgrounds
            window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);

            // Configure light navigation bar (API 27+)
            if (lightNavigationBar && Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var decorView = window.DecorView;
                if (decorView != null)
                {
#pragma warning disable CS0618 // SystemUiVisibility is obsolete but required for API < 30
                    var flags = (SystemUiFlags)decorView.SystemUiVisibility;
                    flags |= SystemUiFlags.LightNavigationBar;
                    decorView.SystemUiVisibility = (StatusBarVisibility)flags;
#pragma warning restore CS0618
                }
            }

            System.Diagnostics.Debug.WriteLine($"AndroidSystemBarsHelper: Navigation bar configured (color: {navColor}, light: {lightNavigationBar})");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("AndroidSystemBarsHelper: API level < 21, navigation bar configuration not supported");
        }
    }

    /// <summary>
    /// Sets up a window insets listener to properly handle system bars padding.
    /// This ensures your content doesn't get hidden behind system bars in edge-to-edge layouts.
    /// </summary>
    /// <param name="activity">The activity to configure</param>
    /// <param name="applyPadding">Whether to automatically apply padding to the decor view. Default is true.</param>
    /// <exception cref="ArgumentNullException">Thrown when activity is null</exception>
    /// <remarks>
    /// <para>
    /// The listener applies padding to the window's decor view based on system bar insets.
    /// This is essential for edge-to-edge layouts to prevent content overlap with system UI.
    /// </para>
    /// <para>
    /// <b>Requires:</b> API 21+ (Android 5.0 Lollipop)
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override void OnCreate(Bundle? savedInstanceState)
    /// {
    ///     base.OnCreate(savedInstanceState);
    ///
    ///     // Configure transparent status bar
    ///     AndroidSystemBarsHelper.ConfigureTransparentStatusBar(this);
    ///
    ///     // Setup window insets to prevent content overlap
    ///     AndroidSystemBarsHelper.SetupWindowInsets(this);
    /// }
    /// </code>
    /// </example>
    public static void SetupWindowInsets(AndroidActivity activity, bool applyPadding = true)
    {
        if (activity == null)
            throw new ArgumentNullException(nameof(activity));

        try
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                var decorView = activity.Window?.DecorView;
                if (decorView != null)
                {
                    ViewCompat.SetOnApplyWindowInsetsListener(decorView, new WindowInsetsListener(applyPadding));

                    // Request to apply insets
                    ViewCompat.RequestApplyInsets(decorView);

                    System.Diagnostics.Debug.WriteLine($"AndroidSystemBarsHelper: Window insets listener configured (applyPadding: {applyPadding})");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("AndroidSystemBarsHelper: DecorView is null, cannot setup window insets");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("AndroidSystemBarsHelper: API level < 21, window insets not supported");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AndroidSystemBarsHelper: Failed to setup window insets listener: {ex.Message}");
        }
    }

    /// <summary>
    /// Configures a complete edge-to-edge layout with transparent status bar, black navigation bar,
    /// and proper window insets handling. This is the recommended all-in-one configuration.
    /// </summary>
    /// <param name="activity">The activity to configure</param>
    /// <param name="lightStatusBar">Whether to use light status bar (dark icons). Default is true.</param>
    /// <exception cref="ArgumentNullException">Thrown when activity is null</exception>
    /// <remarks>
    /// This convenience method calls:
    /// <list type="bullet">
    /// <item>ConfigureTransparentStatusBar with navigation bar configuration</item>
    /// <item>SetupWindowInsets to handle system bar padding</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override void OnCreate(Bundle? savedInstanceState)
    /// {
    ///     base.OnCreate(savedInstanceState);
    ///
    ///     // One-line edge-to-edge configuration
    ///     AndroidSystemBarsHelper.ConfigureEdgeToEdge(this);
    /// }
    /// </code>
    /// </example>
    public static void ConfigureEdgeToEdge(AndroidActivity activity, bool lightStatusBar = true)
    {
        if (activity == null)
            throw new ArgumentNullException(nameof(activity));

        ConfigureTransparentStatusBar(activity, lightStatusBar, configureNavigationBar: true);
        SetupWindowInsets(activity, applyPadding: true);

        System.Diagnostics.Debug.WriteLine("AndroidSystemBarsHelper: Edge-to-edge layout configured");
    }

    /// <summary>
    /// Private nested class that implements window insets listener to apply padding for system bars.
    /// </summary>
    private class WindowInsetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
    {
        private readonly bool _applyPadding;

        public WindowInsetsListener(bool applyPadding = true)
        {
            _applyPadding = applyPadding;
        }

        public WindowInsetsCompat OnApplyWindowInsets(AndroidView v, WindowInsetsCompat insets)
        {
            try
            {
                if (_applyPadding)
                {
                    // Get system window insets (status bar, navigation bar, etc.)
                    var systemBars = insets.GetInsets(WindowInsetsCompat.Type.SystemBars());

                    // Apply padding to the view to account for system bars
                    v.SetPadding(systemBars.Left, systemBars.Top, systemBars.Right, systemBars.Bottom);

                    System.Diagnostics.Debug.WriteLine($"AndroidSystemBarsHelper: Window insets applied - L:{systemBars.Left} T:{systemBars.Top} R:{systemBars.Right} B:{systemBars.Bottom}");
                }

                // Return consumed insets to indicate we handled them
                return WindowInsetsCompat.Consumed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AndroidSystemBarsHelper: Failed to apply window insets: {ex.Message}");
                return insets;
            }
        }
    }
}
#endif
