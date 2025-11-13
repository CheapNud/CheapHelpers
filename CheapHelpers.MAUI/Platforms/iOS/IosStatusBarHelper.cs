#if IOS
using UIKit;
using Foundation;
using System.Diagnostics;

namespace CheapHelpers.MAUI.Platforms.iOS;

/// <summary>
/// Comprehensive helper utility for configuring iOS status bar appearance and behavior.
/// Provides methods to control status bar styling, visibility, and height for modern iOS applications.
/// </summary>
/// <remarks>
/// This helper simplifies common iOS status bar configuration tasks:
/// <list type="bullet">
/// <item>Light/dark status bar content (icon colors)</item>
/// <item>Status bar visibility with animations</item>
/// <item>Quick configuration for light/dark backgrounds</item>
/// <item>Status bar height retrieval for layout purposes</item>
/// </list>
/// <para>
/// <b>Important iOS Considerations:</b>
/// <list type="bullet">
/// <item>For application-wide status bar control, set UIViewControllerBasedStatusBarAppearance = NO in Info.plist</item>
/// <item>For view controller-based control, override PreferredStatusBarStyle in your view controllers</item>
/// <item>iOS 13+ automatically adapts to dark mode unless overridden</item>
/// <item>Status bar style changes may require calling SetNeedsStatusBarAppearanceUpdate on the active view controller</item>
/// </list>
/// </para>
/// <para>
/// <b>Required Info.plist Configuration (for global control):</b>
/// <code>
/// &lt;key&gt;UIViewControllerBasedStatusBarAppearance&lt;/key&gt;
/// &lt;false/&gt;
/// &lt;key&gt;UIStatusBarStyle&lt;/key&gt;
/// &lt;string&gt;UIStatusBarStyleDarkContent&lt;/string&gt;
/// </code>
/// </para>
/// </remarks>
public static class IosStatusBarHelper
{
    /// <summary>
    /// Configures the status bar style (light or dark content).
    /// </summary>
    /// <param name="style">The desired status bar style. Defaults to DarkContent (dark icons for light backgrounds).</param>
    /// <remarks>
    /// <para>
    /// Available styles:
    /// <list type="bullet">
    /// <item><b>DarkContent:</b> Dark icons and text (for light backgrounds) - iOS 13+</item>
    /// <item><b>LightContent:</b> Light icons and text (for dark backgrounds)</item>
    /// <item><b>Default:</b> System default (usually dark content)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Requirements:</b>
    /// <list type="bullet">
    /// <item>Info.plist must have UIViewControllerBasedStatusBarAppearance = NO for global control</item>
    /// <item>DarkContent style requires iOS 13+, falls back to Default on older versions</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In MauiProgram.cs or AppDelegate
    /// IosStatusBarHelper.ConfigureStatusBarStyle(UIStatusBarStyle.DarkContent);
    /// </code>
    /// </example>
    public static void ConfigureStatusBarStyle(UIStatusBarStyle style = UIStatusBarStyle.DarkContent)
    {
        try
        {
            if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
            {
                // iOS 13+ supports DarkContent style
                UIApplication.SharedApplication.SetStatusBarStyle(style, false);
                Debug.WriteLine($"IosStatusBarHelper: Status bar style set to {style}");
            }
            else
            {
                // iOS 12 and below - only supports Default and LightContent
                var fallbackStyle = style == UIStatusBarStyle.LightContent
                    ? UIStatusBarStyle.LightContent
                    : UIStatusBarStyle.Default;

                UIApplication.SharedApplication.SetStatusBarStyle(fallbackStyle, false);
                Debug.WriteLine($"IosStatusBarHelper: Status bar style set to {fallbackStyle} (iOS < 13 fallback)");
            }

            // Request status bar appearance update from the active view controller
            var viewController = GetActiveViewController();
            if (viewController != null)
            {
                viewController.SetNeedsStatusBarAppearanceUpdate();
                Debug.WriteLine("IosStatusBarHelper: Requested status bar appearance update from view controller");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"IosStatusBarHelper: Failed to configure status bar style: {ex.Message}");
        }
    }

    /// <summary>
    /// Hides or shows the status bar with optional animation.
    /// </summary>
    /// <param name="hidden">True to hide the status bar, false to show it.</param>
    /// <param name="animation">The animation style to use. Defaults to Fade.</param>
    /// <remarks>
    /// <para>
    /// Available animations:
    /// <list type="bullet">
    /// <item><b>None:</b> Instant hide/show with no animation</item>
    /// <item><b>Fade:</b> Smooth fade in/out transition</item>
    /// <item><b>Slide:</b> Slide up/down animation</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Note:</b> Hiding the status bar affects the safe area insets and may require layout adjustments.
    /// Consider using view.SafeAreaLayoutGuide for proper content positioning.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Hide status bar with fade animation
    /// IosStatusBarHelper.SetStatusBarHidden(true, UIStatusBarAnimation.Fade);
    ///
    /// // Show status bar instantly
    /// IosStatusBarHelper.SetStatusBarHidden(false, UIStatusBarAnimation.None);
    /// </code>
    /// </example>
    public static void SetStatusBarHidden(bool hidden, UIStatusBarAnimation animation = UIStatusBarAnimation.Fade)
    {
        try
        {
            UIApplication.SharedApplication.SetStatusBarHidden(hidden, animation);
            Debug.WriteLine($"IosStatusBarHelper: Status bar {(hidden ? "hidden" : "shown")} with {animation} animation");

            // Request status bar appearance update from the active view controller
            var viewController = GetActiveViewController();
            if (viewController != null)
            {
                viewController.SetNeedsStatusBarAppearanceUpdate();
                Debug.WriteLine("IosStatusBarHelper: Requested status bar appearance update from view controller");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"IosStatusBarHelper: Failed to set status bar hidden state: {ex.Message}");
        }
    }

    /// <summary>
    /// Configures the status bar for use with a light background (e.g., white, light gray).
    /// This sets dark icons and text for optimal visibility.
    /// </summary>
    /// <remarks>
    /// This is a convenience method that calls ConfigureStatusBarStyle with DarkContent.
    /// Use this when your app has a light-colored UI theme.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Configure for light theme
    /// IosStatusBarHelper.ConfigureForLightBackground();
    /// </code>
    /// </example>
    public static void ConfigureForLightBackground()
    {
        ConfigureStatusBarStyle(UIStatusBarStyle.DarkContent);
        Debug.WriteLine("IosStatusBarHelper: Configured status bar for light background (dark icons)");
    }

    /// <summary>
    /// Configures the status bar for use with a dark background (e.g., black, dark gray).
    /// This sets light icons and text for optimal visibility.
    /// </summary>
    /// <remarks>
    /// This is a convenience method that calls ConfigureStatusBarStyle with LightContent.
    /// Use this when your app has a dark-colored UI theme.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Configure for dark theme
    /// IosStatusBarHelper.ConfigureForDarkBackground();
    /// </code>
    /// </example>
    public static void ConfigureForDarkBackground()
    {
        ConfigureStatusBarStyle(UIStatusBarStyle.LightContent);
        Debug.WriteLine("IosStatusBarHelper: Configured status bar for dark background (light icons)");
    }

    /// <summary>
    /// Gets the current height of the status bar in points.
    /// </summary>
    /// <returns>The status bar height, or 0 if the status bar is hidden or unavailable.</returns>
    /// <remarks>
    /// <para>
    /// The status bar height varies by device and orientation:
    /// <list type="bullet">
    /// <item>Standard iPhones: 20pt (portrait), 0pt (landscape on some models)</item>
    /// <item>iPhones with notch (X and later): 44pt (portrait), 0pt or 30pt (landscape)</item>
    /// <item>iPads: 20pt (both orientations)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>iOS 13+ Deprecation:</b> StatusBarFrame is deprecated in iOS 13+. This method uses
    /// StatusBarManager.StatusBarFrame when available, falling back to the legacy API for older iOS versions.
    /// </para>
    /// <para>
    /// <b>Safe Area Alternative:</b> Consider using UIView.SafeAreaInsets.Top instead for modern apps,
    /// as it provides more accurate layout information including the status bar and any notch/island areas.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var statusBarHeight = IosStatusBarHelper.GetStatusBarHeight();
    /// Console.WriteLine($"Status bar height: {statusBarHeight}pt");
    ///
    /// // Adjust view layout to account for status bar
    /// myView.Frame = new CGRect(0, statusBarHeight, width, height);
    /// </code>
    /// </example>
    public static nfloat GetStatusBarHeight()
    {
        try
        {
            // iOS 13+ uses StatusBarManager
            if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
            {
                var windowScene = GetActiveWindowScene();
                if (windowScene?.StatusBarManager != null)
                {
                    var height = windowScene.StatusBarManager.StatusBarFrame.Height;
                    Debug.WriteLine($"IosStatusBarHelper: Status bar height: {height}pt (iOS 13+ API)");
                    return height;
                }
            }

            // Fallback for iOS 12 and below
#pragma warning disable CA1422 // StatusBarFrame is obsolete in iOS 13+
            var legacyHeight = UIApplication.SharedApplication.StatusBarFrame.Height;
#pragma warning restore CA1422
            Debug.WriteLine($"IosStatusBarHelper: Status bar height: {legacyHeight}pt (legacy API)");
            return legacyHeight;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"IosStatusBarHelper: Failed to get status bar height: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Gets the active UIWindowScene for the application (iOS 13+).
    /// </summary>
    /// <returns>The active window scene, or null if not available.</returns>
    private static UIWindowScene? GetActiveWindowScene()
    {
        try
        {
            if (!UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
                return null;

            var connectedScenes = UIApplication.SharedApplication.ConnectedScenes;
            if (connectedScenes == null)
                return null;

            foreach (var scene in connectedScenes)
            {
                if (scene is UIWindowScene windowScene && scene.ActivationState == UISceneActivationState.ForegroundActive)
                {
                    return windowScene;
                }
            }

            // Fallback to first window scene
            foreach (var scene in connectedScenes)
            {
                if (scene is UIWindowScene windowScene)
                {
                    return windowScene;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"IosStatusBarHelper: Failed to get active window scene: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets the currently active view controller.
    /// </summary>
    /// <returns>The active view controller, or null if not available.</returns>
    private static UIViewController? GetActiveViewController()
    {
        try
        {
            // Get the key window
            UIWindow? keyWindow = null;

            if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
            {
                // iOS 13+ - get from window scene
                var windowScene = GetActiveWindowScene();
                if (windowScene != null)
                {
                    foreach (var window in windowScene.Windows)
                    {
                        if (window.IsKeyWindow)
                        {
                            keyWindow = window;
                            break;
                        }
                    }

                    // Fallback to first window
                    if (keyWindow == null && windowScene.Windows.Length > 0)
                    {
                        keyWindow = windowScene.Windows[0];
                    }
                }
            }
            else
            {
                // iOS 12 and below
#pragma warning disable CA1422 // KeyWindow is obsolete in iOS 13+
                keyWindow = UIApplication.SharedApplication.KeyWindow;
#pragma warning restore CA1422
            }

            if (keyWindow?.RootViewController == null)
                return null;

            // Get the presented view controller if available
            var viewController = keyWindow.RootViewController;
            while (viewController.PresentedViewController != null)
            {
                viewController = viewController.PresentedViewController;
            }

            return viewController;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"IosStatusBarHelper: Failed to get active view controller: {ex.Message}");
            return null;
        }
    }
}
#endif
