# Status Bar Configuration for MAUI

Complete cross-platform solution for configuring status bars in MAUI applications. Configure status bar appearance from **any MAUI code** without writing platform-specific code.

## Table of Contents

- [Overview](#overview)
- [Quick Start](#quick-start)
- [Cross-Platform API](#cross-platform-api)
- [Platform-Specific Setup](#platform-specific-setup)
- [Usage Examples](#usage-examples)
- [Platform Considerations](#platform-considerations)
- [Troubleshooting](#troubleshooting)
- [API Reference](#api-reference)

## Overview

The CheapHelpers.MAUI status bar solution provides three layers:

1. **Cross-Platform Helper** (`StatusBarHelper`) - Works from any MAUI code
2. **MauiApp Extensions** - Configure during app startup in MauiProgram.cs
3. **Platform-Specific Helpers** - Direct platform control when needed
   - `IosStatusBarHelper` (iOS)
   - `AndroidSystemBarsHelper` (Android)

### Features

- Zero platform-specific code required for developers
- Consistent API across iOS and Android
- Configure from MauiProgram.cs, App.xaml.cs, or any page
- Automatic UI thread marshalling
- Safe area handling built-in
- Dark mode support
- Comprehensive error handling

## Quick Start

### Option 1: Configure During App Startup (Recommended)

The easiest way is to configure the status bar in your `MauiProgram.cs`:

```csharp
using CheapHelpers.MAUI.Extensions;
using CheapHelpers.MAUI.Helpers;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseTransparentStatusBar(StatusBarStyle.DarkContent); // ONE LINE!

        return builder.Build();
    }
}
```

### Option 2: Configure from Any Page

You can also configure the status bar from anywhere in your app:

```csharp
using CheapHelpers.MAUI.Helpers;

// In your page code-behind or view model
protected override void OnAppearing()
{
    base.OnAppearing();
    StatusBarHelper.ConfigureForLightBackground(); // Dark icons for light background
}
```

### Platform-Specific Requirements

#### iOS: Info.plist Configuration

Add these keys to your `Info.plist` for global status bar control:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <!-- Allow app-wide status bar control (required!) -->
    <key>UIViewControllerBasedStatusBarAppearance</key>
    <false/>

    <!-- Default status bar style -->
    <key>UIStatusBarStyle</key>
    <string>UIStatusBarStyleDarkContent</string>
</dict>
</plist>
```

#### Android: No Additional Setup Required

Android configuration works out of the box. For optimal results, ensure your app theme in `Resources/values/styles.xml` doesn't override system bar settings:

```xml
<style name="MainTheme" parent="@android:style/Theme.Material.Light.NoActionBar">
    <!-- Your theme settings -->
</style>
```

## Cross-Platform API

### StatusBarHelper

The main cross-platform helper with methods that work on both iOS and Android:

```csharp
using CheapHelpers.MAUI.Helpers;

// Quick configuration methods
StatusBarHelper.ConfigureForLightBackground(); // Dark icons (for light UI)
StatusBarHelper.ConfigureForDarkBackground();  // Light icons (for dark UI)
StatusBarHelper.SetTransparent(StatusBarStyle.DarkContent);

// Custom configuration
var config = new StatusBarConfiguration
{
    Style = StatusBarStyle.DarkContent,
    IsTransparent = true,
    BackgroundColor = Colors.White,        // Android only
    NavigationBarColor = Colors.Black      // Android only
};
StatusBarHelper.ConfigureStatusBar(config);

// Get status bar height for layout calculations
double height = StatusBarHelper.GetStatusBarHeight();
```

### MauiAppBuilder Extensions

Configure status bar during app startup with fluent API:

```csharp
using CheapHelpers.MAUI.Extensions;
using CheapHelpers.MAUI.Helpers;

// Simple transparent status bar
builder.UseTransparentStatusBar(StatusBarStyle.DarkContent);

// Light theme (dark icons)
builder.UseLightStatusBar();

// Dark theme (light icons)
builder.UseDarkStatusBar();

// With navigation bar control (Android)
builder.UseStatusBarWithNavigation(
    StatusBarStyle.DarkContent,
    Colors.White
);

// Full custom configuration
builder.ConfigureStatusBar(new StatusBarConfiguration
{
    Style = StatusBarStyle.LightContent,
    IsTransparent = true,
    NavigationBarColor = Colors.Black
});
```

## Platform-Specific Setup

### iOS Setup

#### 1. Configure Info.plist

Add to `Platforms/iOS/Info.plist`:

```xml
<key>UIViewControllerBasedStatusBarAppearance</key>
<false/>
<key>UIStatusBarStyle</key>
<string>UIStatusBarStyleDarkContent</string>
```

**Note:** Setting `UIViewControllerBasedStatusBarAppearance` to `false` enables application-wide status bar control, which is what StatusBarHelper uses. If you need view controller-based control, set this to `true` and override `PreferredStatusBarStyle` in your view controllers.

#### 2. iOS Behavior Notes

- Status bar is **always transparent** on iOS (overlays content)
- Only style (light/dark icons) can be controlled
- BackgroundColor and NavigationBarColor settings are ignored
- Status bar respects iOS dark mode unless overridden
- Safe area insets automatically account for status bar

### Android Setup

#### 1. Ensure Material Theme

Your theme should inherit from a Material or AppCompat theme:

```xml
<!-- Platforms/Android/Resources/values/styles.xml -->
<style name="MainTheme" parent="@android:style/Theme.Material.Light.NoActionBar">
    <!-- Don't set android:statusBarColor or android:navigationBarColor here -->
    <!-- Let StatusBarHelper control them -->
</style>
```

#### 2. Android Behavior Notes

- Transparent status bar requires API 21+ (Android 5.0)
- Light status bar icons require API 23+ (Android 6.0 Marshmallow)
- Light navigation bar icons require API 27+ (Android 8.1 Oreo)
- Navigation bar color can be customized (Android-specific)
- Edge-to-edge layouts supported via `AndroidSystemBarsHelper`

## Usage Examples

### Example 1: Simple Light Theme App

```csharp
// MauiProgram.cs
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();

    builder
        .UseMauiApp<App>()
        .UseLightStatusBar(); // Dark icons for light background

    return builder.Build();
}
```

**Result:** Transparent status bar with dark icons on both iOS and Android.

### Example 2: Dark Theme App

```csharp
// MauiProgram.cs
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();

    builder
        .UseMauiApp<App>()
        .UseDarkStatusBar(); // Light icons for dark background

    return builder.Build();
}
```

**Result:** Transparent status bar with light icons on both iOS and Android.

### Example 3: Dynamic Theme Switching

```csharp
// In your page or App.xaml.cs
public void ApplyTheme(bool isDarkMode)
{
    if (isDarkMode)
    {
        StatusBarHelper.ConfigureForDarkBackground();
    }
    else
    {
        StatusBarHelper.ConfigureForLightBackground();
    }
}
```

### Example 4: Per-Page Status Bar Style

```csharp
public partial class LoginPage : ContentPage
{
    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Dark theme for login page
        StatusBarHelper.ConfigureForDarkBackground();
    }
}

public partial class HomePage : ContentPage
{
    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Light theme for home page
        StatusBarHelper.ConfigureForLightBackground();
    }
}
```

### Example 5: Android-Specific Navigation Bar

```csharp
// MauiProgram.cs - Android app with white navigation bar
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();

    builder
        .UseMauiApp<App>()
        .ConfigureStatusBar(new StatusBarConfiguration
        {
            Style = StatusBarStyle.DarkContent,
            IsTransparent = true,
            NavigationBarColor = Colors.White // Android only
        });

    return builder.Build();
}
```

### Example 6: Layout with Status Bar Height

```csharp
public partial class CustomHeaderPage : ContentPage
{
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Get status bar height for manual layout
        var statusBarHeight = StatusBarHelper.GetStatusBarHeight();

        // Add padding to avoid content overlap
        MyHeader.Margin = new Thickness(0, statusBarHeight, 0, 0);
    }
}
```

**Note:** For most cases, use MAUI's safe area insets instead of manual calculations.

### Example 7: Full Edge-to-Edge with Android Navigation Bar Control

```csharp
// MauiProgram.cs
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();

    builder
        .UseMauiApp<App>()
        .UseStatusBarWithNavigation(
            StatusBarStyle.DarkContent,
            navigationBarColor: Colors.Black
        );

    return builder.Build();
}

// In MainActivity.cs (Android-specific)
#if ANDROID
protected override void OnCreate(Bundle? savedInstanceState)
{
    base.OnCreate(savedInstanceState);

    // Optional: Advanced edge-to-edge configuration
    AndroidSystemBarsHelper.ConfigureEdgeToEdge(this, lightStatusBar: true);
}
#endif
```

## Platform Considerations

### iOS

#### Status Bar Styles

| Style | Description | iOS Version |
|-------|-------------|-------------|
| `DarkContent` | Dark icons/text (light background) | iOS 13+ |
| `LightContent` | Light icons/text (dark background) | iOS 7+ |
| `Default` | System default (usually dark) | iOS 7+ |

#### Important Notes

- Status bar is **always transparent** on iOS
- BackgroundColor setting is ignored
- NavigationBarColor is ignored (no Android-style navigation bar)
- Status bar height varies by device:
  - Standard iPhones: 20pt
  - iPhones with notch/Dynamic Island: 44pt
  - Landscape on some models: 0pt
- Use `UIView.SafeAreaLayoutGuide` for proper content layout

#### Dark Mode

iOS automatically switches status bar style based on dark mode unless you override it:

```csharp
// Force light icons even in light mode
StatusBarHelper.ConfigureForDarkBackground();
```

### Android

#### Status Bar Styles

| Style | Description | API Level |
|-------|-------------|-----------|
| `DarkContent` | Dark icons/text (light background) | API 23+ |
| `LightContent` | Light icons/text (dark background) | API 21+ |

#### Important Notes

- Transparent status bar requires API 21+ (Lollipop)
- Light status bar icons require API 23+ (Marshmallow)
- Light navigation bar icons require API 27+ (Oreo)
- On older devices, may fall back to default styles
- Status bar height typically 24dp (varies by device)

#### Navigation Bar Control

Android has a software navigation bar that can be styled:

```csharp
var config = new StatusBarConfiguration
{
    Style = StatusBarStyle.DarkContent,
    IsTransparent = true,
    NavigationBarColor = Colors.White // Custom navigation bar color
};
StatusBarHelper.ConfigureStatusBar(config);
```

#### Edge-to-Edge Layouts

For advanced edge-to-edge layouts on Android, use `AndroidSystemBarsHelper`:

```csharp
#if ANDROID
// In MainActivity.cs
protected override void OnCreate(Bundle? savedInstanceState)
{
    base.OnCreate(savedInstanceState);

    // Configure edge-to-edge with window insets
    AndroidSystemBarsHelper.ConfigureEdgeToEdge(this);
}
#endif
```

See [Android System Bars documentation](AndroidSystemBars.md) for details.

## Troubleshooting

### iOS Issues

#### Status Bar Not Changing

**Problem:** Status bar style doesn't change when calling StatusBarHelper methods.

**Solution:** Ensure `UIViewControllerBasedStatusBarAppearance` is set to `false` in Info.plist:

```xml
<key>UIViewControllerBasedStatusBarAppearance</key>
<false/>
```

#### Dark Icons Not Working

**Problem:** Dark icons (DarkContent style) not showing.

**Solution:** DarkContent requires iOS 13+. On older devices, it falls back to Default style. Check iOS version:

```csharp
if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
{
    // DarkContent supported
}
```

#### Status Bar Hidden in Landscape

**Problem:** Status bar disappears in landscape mode on some iPhones.

**Solution:** This is expected iOS behavior on certain models (e.g., iPhone X series in landscape). Use safe area insets for layout instead of status bar height.

### Android Issues

#### Status Bar Not Transparent

**Problem:** Status bar shows solid color instead of transparent.

**Solution:**
1. Ensure device is running Android 5.0+ (API 21+)
2. Check that your theme doesn't override status bar color
3. Verify `IsTransparent = true` in configuration

```csharp
StatusBarHelper.SetTransparent(StatusBarStyle.DarkContent);
```

#### Dark Icons Not Working

**Problem:** Status bar icons are light when they should be dark.

**Solution:** Light status bar (dark icons) requires Android 6.0+ (API 23+). On older devices, icons are always light. Check Android version:

```csharp
#if ANDROID
if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
{
    // Light status bar supported
}
#endif
```

#### Content Behind Status Bar

**Problem:** Content is hidden behind the transparent status bar.

**Solution:** Use MAUI safe area insets or Android window insets:

```xml
<!-- In XAML -->
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui">
    <Grid Padding="{OnPlatform iOS='0,44,0,0', Android='0,24,0,0'}">
        <!-- Your content -->
    </Grid>
</ContentPage>
```

Or use `AndroidSystemBarsHelper.SetupWindowInsets()` for automatic padding.

#### Navigation Bar Issues

**Problem:** Navigation bar color not changing.

**Solution:**
1. Ensure you're setting NavigationBarColor in configuration
2. Light navigation bar requires Android 8.1+ (API 27+)
3. Some devices have gesture navigation with no visible bar

```csharp
var config = new StatusBarConfiguration
{
    NavigationBarColor = Colors.Black
};
StatusBarHelper.ConfigureStatusBar(config);
```

### General Issues

#### Configuration Not Applied

**Problem:** Status bar configuration doesn't take effect.

**Solution:**
1. Check Debug output for error messages
2. Ensure you're calling on UI thread (StatusBarHelper does this automatically)
3. Verify platform-specific setup is complete (Info.plist for iOS)

#### Status Bar Height Returns 0

**Problem:** `GetStatusBarHeight()` returns 0.

**Solution:**
- iOS: Status bar may be hidden or unavailable. Check `UIApplication.SharedApplication.StatusBarHidden`
- Android: Call after the activity window is available
- Both: May return 0 in landscape on certain devices

```csharp
var height = StatusBarHelper.GetStatusBarHeight();
if (height == 0)
{
    // Use safe area insets instead
    var safeAreaTop = On<iOS>().SafeAreaInsets().Top;
}
```

## API Reference

### StatusBarHelper

Main cross-platform helper class.

#### Methods

```csharp
// Configure with custom settings
void ConfigureStatusBar(StatusBarConfiguration config);

// Quick configuration methods
void ConfigureForLightBackground(); // Dark icons
void ConfigureForDarkBackground();  // Light icons
void SetTransparent(StatusBarStyle style = StatusBarStyle.DarkContent);

// Get status bar height
double GetStatusBarHeight();
```

### StatusBarConfiguration

Configuration model for status bar appearance.

```csharp
public class StatusBarConfiguration
{
    // Status bar style (both platforms)
    public StatusBarStyle Style { get; set; } = StatusBarStyle.DarkContent;

    // Transparency (Android only, iOS always transparent)
    public bool IsTransparent { get; set; } = true;

    // Background color (Android only, ignored if IsTransparent = true)
    public Color? BackgroundColor { get; set; }

    // Navigation bar color (Android only)
    public Color? NavigationBarColor { get; set; }

    // Hide navigation bar (Android only, not commonly used)
    public bool HideNavigationBar { get; set; } = false;
}
```

### StatusBarStyle Enum

```csharp
public enum StatusBarStyle
{
    DarkContent,   // Dark icons/text (for light backgrounds)
    LightContent   // Light icons/text (for dark backgrounds)
}
```

### MauiAppBuilder Extensions

Extension methods for configuring status bar during app startup.

```csharp
// Full configuration
MauiAppBuilder ConfigureStatusBar(StatusBarConfiguration config);

// Simple transparent status bar
MauiAppBuilder UseTransparentStatusBar(StatusBarStyle style = StatusBarStyle.DarkContent);

// Convenience methods
MauiAppBuilder UseLightStatusBar();  // Dark icons
MauiAppBuilder UseDarkStatusBar();   // Light icons

// With navigation bar (Android)
MauiAppBuilder UseStatusBarWithNavigation(StatusBarStyle statusBarStyle, Color navigationBarColor);
```

### Platform-Specific Helpers

#### IosStatusBarHelper (iOS Only)

Direct iOS status bar control.

```csharp
void ConfigureStatusBarStyle(UIStatusBarStyle style = UIStatusBarStyle.DarkContent);
void SetStatusBarHidden(bool hidden, UIStatusBarAnimation animation = UIStatusBarAnimation.Fade);
void ConfigureForLightBackground();
void ConfigureForDarkBackground();
nfloat GetStatusBarHeight();
```

#### AndroidSystemBarsHelper (Android Only)

Comprehensive Android system bars configuration. See [Android System Bars documentation](AndroidSystemBars.md).

```csharp
void ConfigureTransparentStatusBar(Activity activity, bool lightStatusBar = true, bool configureNavigationBar = true);
void ConfigureNavigationBar(Activity activity, Color? color = null, bool lightNavigationBar = false);
void ConfigureEdgeToEdge(Activity activity, bool lightStatusBar = true);
void SetupWindowInsets(Activity activity, bool applyPadding = true);
```

## Best Practices

### 1. Configure Once at Startup

For consistent status bar appearance, configure in MauiProgram.cs:

```csharp
builder.UseTransparentStatusBar(StatusBarStyle.DarkContent);
```

### 2. Use Safe Area Insets

Don't rely solely on status bar height. Use MAUI safe area insets:

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui">
    <Grid Padding="{OnPlatform iOS='0,44,0,0'}">
```

### 3. Test on Physical Devices

Status bar behavior varies significantly:
- iOS notch/Dynamic Island devices
- Different Android manufacturers (Samsung, OnePlus, etc.)
- Landscape orientation
- Dark mode

### 4. Handle Platform Differences

Some features are platform-specific:

```csharp
var config = new StatusBarConfiguration
{
    Style = StatusBarStyle.DarkContent,
    IsTransparent = true,
#if ANDROID
    NavigationBarColor = Colors.Black // Android only
#endif
};
```

### 5. Respond to Theme Changes

Update status bar when app theme changes:

```csharp
Application.Current.RequestedThemeChanged += (s, e) =>
{
    if (e.RequestedTheme == AppTheme.Dark)
        StatusBarHelper.ConfigureForDarkBackground();
    else
        StatusBarHelper.ConfigureForLightBackground();
};
```

## Resources

- [GitHub Repository](https://github.com/CheapNud/CheapHelpers)
- [Android System Bars Documentation](AndroidSystemBars.md)
- [Apple Human Interface Guidelines - Status Bars](https://developer.apple.com/design/human-interface-guidelines/status-bars)
- [Android Edge-to-Edge Guidelines](https://developer.android.com/develop/ui/views/layout/edge-to-edge)

## License

MIT License - See LICENSE file in repository
