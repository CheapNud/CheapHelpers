# Android System Bars Helper

Comprehensive helper utility for configuring Android system bars (status bar and navigation bar) in MAUI applications. Provides transparent status bars, proper navigation bar handling, and window insets management for edge-to-edge layouts.

## Overview

The `AndroidSystemBarsHelper` class simplifies common Android system bar configuration tasks that are typically complex and error-prone. It extracts battle-tested patterns from production apps to provide:

- **Transparent status bars** with light/dark icons
- **Opaque navigation bars** with customizable colors
- **Window insets handling** for proper content padding

## Installation

```bash
dotnet add package CheapHelpers.MAUI
```

## API Level Requirements

| Feature | Minimum API Level | Android Version |
|---------|------------------|-----------------|
| Status bar transparency | API 21+ | Android 5.0 (Lollipop) |
| Light status bar icons | API 23+ | Android 6.0 (Marshmallow) |
| Light navigation bar icons | API 27+ | Android 8.1 (Oreo) |
| SetDecorFitsSystemWindows | API 30+ | Android 11 |

## Quick Start

The simplest way to configure edge-to-edge layout with transparent status bar and black navigation bar:

```csharp
using CheapHelpers.MAUI.Helpers;

namespace YourApp.Platforms.Android;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // One-line edge-to-edge configuration
        AndroidSystemBarsHelper.ConfigureEdgeToEdge(this);
    }
}
```

## Public API

### ConfigureTransparentStatusBar

Configures a transparent status bar with light icons and proper system window handling.

```csharp
public static void ConfigureTransparentStatusBar(
    Activity activity,
    bool lightStatusBar = true,
    bool configureNavigationBar = true
)
```

**Parameters:**
- `activity` - The activity whose status bar should be configured
- `lightStatusBar` - Whether to use light status bar (dark icons). Default is true.
- `configureNavigationBar` - Whether to also configure navigation bar as black. Default is true.

**Example:**

```csharp
protected override void OnCreate(Bundle? savedInstanceState)
{
    base.OnCreate(savedInstanceState);

    // Transparent status bar with light icons
    AndroidSystemBarsHelper.ConfigureTransparentStatusBar(this);

    // Transparent status bar with dark icons (for dark backgrounds)
    AndroidSystemBarsHelper.ConfigureTransparentStatusBar(this, lightStatusBar: false);

    // Transparent status bar only, don't touch navigation bar
    AndroidSystemBarsHelper.ConfigureTransparentStatusBar(
        this,
        configureNavigationBar: false
    );
}
```

### ConfigureNavigationBar

Configures the navigation bar with a custom color and optional light navigation bar mode.

```csharp
public static void ConfigureNavigationBar(
    Activity activity,
    global::Android.Graphics.Color? color = null,
    bool lightNavigationBar = false
)
```

**Parameters:**
- `activity` - The activity whose navigation bar should be configured
- `color` - The color for the navigation bar. If null, defaults to black.
- `lightNavigationBar` - Whether to use light navigation bar (dark icons). Requires API 27+.

**Example:**

```csharp
// Black navigation bar (default)
AndroidSystemBarsHelper.ConfigureNavigationBar(this);

// White navigation bar with dark icons
AndroidSystemBarsHelper.ConfigureNavigationBar(
    this,
    global::Android.Graphics.Color.White,
    lightNavigationBar: true
);

// Custom color navigation bar
var customColor = global::Android.Graphics.Color.Rgb(33, 33, 33);
AndroidSystemBarsHelper.ConfigureNavigationBar(this, customColor);
```

### SetupWindowInsets

Sets up a window insets listener to properly handle system bars padding. Essential for edge-to-edge layouts to prevent content overlap with system UI.

```csharp
public static void SetupWindowInsets(
    Activity activity,
    bool applyPadding = true
)
```

**Parameters:**
- `activity` - The activity to configure
- `applyPadding` - Whether to automatically apply padding to the decor view. Default is true.

**Example:**

```csharp
protected override void OnCreate(Bundle? savedInstanceState)
{
    base.OnCreate(savedInstanceState);

    // Configure transparent status bar
    AndroidSystemBarsHelper.ConfigureTransparentStatusBar(this);

    // Setup window insets to prevent content overlap
    AndroidSystemBarsHelper.SetupWindowInsets(this);
}
```

### ConfigureEdgeToEdge

Configures a complete edge-to-edge layout with transparent status bar, black navigation bar, and proper window insets handling. This is the recommended all-in-one configuration.

```csharp
public static void ConfigureEdgeToEdge(
    Activity activity,
    bool lightStatusBar = true
)
```

**Parameters:**
- `activity` - The activity to configure
- `lightStatusBar` - Whether to use light status bar (dark icons). Default is true.

**Example:**

```csharp
protected override void OnCreate(Bundle? savedInstanceState)
{
    base.OnCreate(savedInstanceState);

    // One-line edge-to-edge configuration
    AndroidSystemBarsHelper.ConfigureEdgeToEdge(this);
}
```

## Complete MainActivity Example

Here's a complete example showing all features together:

```csharp
using Android.App;
using Android.Content.PM;
using Android.OS;
using CheapHelpers.MAUI.Helpers;

namespace YourApp.Platforms.Android;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode
)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Configure edge-to-edge layout (status bar + navigation bar + insets)
        AndroidSystemBarsHelper.ConfigureEdgeToEdge(this);
    }
}
```

## Advanced Scenarios

### Custom Color Scheme

Configure a custom color scheme for both status and navigation bars:

```csharp
protected override void OnCreate(Bundle? savedInstanceState)
{
    base.OnCreate(savedInstanceState);

    // Custom transparent status bar with dark icons
    AndroidSystemBarsHelper.ConfigureTransparentStatusBar(
        this,
        lightStatusBar: false,
        configureNavigationBar: false
    );

    // Custom navigation bar color
    var darkGray = global::Android.Graphics.Color.Rgb(33, 33, 33);
    AndroidSystemBarsHelper.ConfigureNavigationBar(this, darkGray);

    // Setup insets
    AndroidSystemBarsHelper.SetupWindowInsets(this);
}
```

### Conditional Configuration

Apply different configurations based on Android version or theme:

```csharp
protected override void OnCreate(Bundle? savedInstanceState)
{
    base.OnCreate(savedInstanceState);

    // Different configuration for newer Android versions
    if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
    {
        // Android 11+: Full edge-to-edge with white navigation bar
        AndroidSystemBarsHelper.ConfigureTransparentStatusBar(
            this,
            configureNavigationBar: false
        );
        AndroidSystemBarsHelper.ConfigureNavigationBar(
            this,
            global::Android.Graphics.Color.White,
            lightNavigationBar: true
        );
    }
    else
    {
        // Older versions: Standard edge-to-edge
        AndroidSystemBarsHelper.ConfigureEdgeToEdge(this);
    }

    AndroidSystemBarsHelper.SetupWindowInsets(this);
}
```

### Without Automatic Padding

If you want to handle padding manually in your MAUI app:

```csharp
protected override void OnCreate(Bundle? savedInstanceState)
{
    base.OnCreate(savedInstanceState);

    AndroidSystemBarsHelper.ConfigureTransparentStatusBar(this);

    // Setup insets listener but don't apply automatic padding
    // (you'll handle padding in your MAUI views)
    AndroidSystemBarsHelper.SetupWindowInsets(this, applyPadding: false);
}
```

## Troubleshooting

### Content Still Hidden Behind Status Bar

If your content is still being hidden behind the status bar:

1. Ensure you're calling `SetupWindowInsets(this)` after configuring the status bar
2. Check that your MAUI content is not applying additional padding
3. Verify you're not using `Window.SetDecorFitsSystemWindows(false)` elsewhere

### Navigation Bar Not Black

If the navigation bar is not appearing black:

1. Ensure you're on API 21+ (Lollipop)
2. Check that `configureNavigationBar: true` in `ConfigureTransparentStatusBar`
3. Verify no other code is overriding the navigation bar color


## Best Practices

1. **Call in OnCreate**: Always configure system bars in `OnCreate` before setting content view
2. **Use ConfigureEdgeToEdge**: For most apps, use the all-in-one method for consistency
3. **Handle Insets**: Always call `SetupWindowInsets` when using transparent status bars
4. **Check Logs**: Monitor Debug output for configuration status and errors
5. **Test on Multiple Devices**: Test on different Android versions (21, 23, 27, 30+)

## See Also

- [Status Bar Helper](StatusBarConfiguration.md) - Cross-platform status bar configuration
- [Firebase Token Helper](FirebaseTokenHelper.md) - Firebase token retrieval (separate concern)
- [Push Notifications](PushNotifications.md) - Complete push notification setup
- [Device Installation](DeviceInstallation.md) - Device registration details

## Source Location

`CheapHelpers.MAUI/Helpers/AndroidSystemBarsHelper.cs`
