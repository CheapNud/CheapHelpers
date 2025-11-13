# CheapHelpers.MAUI

MAUI platform helpers for iOS and Android including status bar configuration, push notifications (iOS APNS + Android FCM), system UI helpers, and device services.

## Installation

```bash
dotnet add package CheapHelpers.MAUI
```

## Features

### Status Bar & System UI
- **[Status Bar Configuration](Docs/StatusBar.md)** - Cross-platform status bar styling with zero native code
- **[Android System Bars](Docs/AndroidSystemBars.md)** - Android-specific status bar and navigation bar helpers with edge-to-edge support
- **iOS Status Bar** - iOS status bar styling with proper safe area handling

### Push Notifications
- **[Push Notifications Setup](Docs/PushNotifications.md)** - Complete iOS APNS and Android FCM integration guide
- **[Device Installation Service](Docs/DeviceInstallation.md)** - Device registration and management with backend integration
- **[Firebase Token Helper](Docs/FirebaseTokenHelper.md)** - Safe FCM token retrieval with error handling
- **[Local Notifications](Docs/LocalNotifications.md)** - Foreground notification display

## Quick Start

### Status Bar Configuration (Cross-Platform)

The easiest way to configure your app's status bar is during startup in `MauiProgram.cs`:

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

Or configure from any page:

```csharp
using CheapHelpers.MAUI.Helpers;

protected override void OnAppearing()
{
    base.OnAppearing();

    // Dark icons for light background
    StatusBarHelper.ConfigureForLightBackground();

    // Or light icons for dark background
    StatusBarHelper.ConfigureForDarkBackground();
}
```

#### iOS Setup

Add to `Platforms/iOS/Info.plist`:

```xml
<key>UIViewControllerBasedStatusBarAppearance</key>
<false/>
<key>UIStatusBarStyle</key>
<string>UIStatusBarStyleDarkContent</string>
```

### Android Edge-to-Edge Layout

For Android apps with transparent status bar and proper content padding:

```csharp
using CheapHelpers.MAUI.Helpers;

// In MainActivity.cs
protected override void OnCreate(Bundle? savedInstanceState)
{
    base.OnCreate(savedInstanceState);

    // One-line edge-to-edge configuration
    AndroidSystemBarsHelper.ConfigureEdgeToEdge(this);
}
```

### Push Notifications

#### 1. Configure in MauiProgram.cs

```csharp
using CheapHelpers.MAUI.Extensions;

builder.Services.AddMauiPushNotifications();
```

#### 2. Platform-Specific Setup

**Android (MainActivity.cs):**

```csharp
using CheapHelpers.MAUI.Helpers;

public class MainActivity : MauiAppCompatActivity, Android.Gms.Tasks.IOnSuccessListener
{
    private IDeviceInstallationService? _deviceService;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Get service from DI
        _deviceService = IPlatformApplication.Current?.Services?
            .GetService<IDeviceInstallationService>();

        // Retrieve Firebase token
        if (_deviceService != null)
        {
            FirebaseTokenHelper.GetFirebaseTokenSafely(
                this,
                _deviceService,
                () => MainApplication.IsFirebaseAvailable
            );
        }
    }

    // Handle token response
    public void OnSuccess(Java.Lang.Object result)
    {
        var token = result?.ToString() ?? "unknown-token";
        if (_deviceService is DeviceInstallationService service)
        {
            service.SetToken(token);
        }
    }
}
```

**iOS (AppDelegate.cs):**

```csharp
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    [Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")]
    public void RegisteredForRemoteNotifications(
        UIApplication application,
        NSData deviceToken)
    {
        var token = deviceToken.ToHexString();
        var service = IPlatformApplication.Current.Services
            .GetService<IDeviceInstallationService>() as DeviceInstallationService;
        service?.SetToken(token);
    }
}
```

#### 3. Register Device from Blazor

```razor
@inject IDeviceInstallationService DeviceService

@code {
    private async Task RegisterForNotifications()
    {
        // Request permissions
        var granted = await DeviceService.RequestPermissionsAsync();

        if (granted)
        {
            // Register with backend
            await DeviceService.RegisterDeviceAsync(userId);
        }
    }
}
```

### Dynamic Theme Switching

```csharp
public void ApplyTheme(bool isDarkMode)
{
    if (isDarkMode)
    {
        StatusBarHelper.ConfigureForDarkBackground();  // Light icons
    }
    else
    {
        StatusBarHelper.ConfigureForLightBackground(); // Dark icons
    }
}
```

## Documentation

### Status Bar & UI
- [Status Bar Configuration](Docs/StatusBar.md) - Complete cross-platform guide with examples
- [Android System Bars](Docs/AndroidSystemBars.md) - Android-specific configuration and edge-to-edge layouts

### Push Notifications
- [Push Notifications](Docs/PushNotifications.md) - Complete setup guide for iOS and Android
- [Device Installation](Docs/DeviceInstallation.md) - Device registration and management
- [Firebase Token Helper](Docs/FirebaseTokenHelper.md) - Safe FCM token retrieval
- [Local Notifications](Docs/LocalNotifications.md) - Foreground notification handling

## Platform Requirements

### iOS
- iOS 14.2+ for push notifications
- iOS 13+ for dark status bar style
- Info.plist configuration for status bar control

### Android
- API 21+ (Android 5.0 Lollipop) for transparent status bar
- API 23+ (Android 6.0 Marshmallow) for light status bar icons
- API 27+ (Android 8.1 Oreo) for light navigation bar icons
- Google Play Services for FCM push notifications

## Related Packages

- [CheapHelpers.Blazor](../CheapHelpers.Blazor/README.md) - Blazor Hybrid integration and UI components
- [CheapHelpers](../CheapHelpers/README.md) - Core utilities and extensions
- [CheapHelpers.Services](../CheapHelpers.Services/README.md) - Business services

## Architecture

CheapHelpers.MAUI follows clean separation of concerns:

- **System UI** (Status Bar, Navigation Bar) - `AndroidSystemBarsHelper`, `IosStatusBarHelper`, `StatusBarHelper`
- **Application UI** (App Bar, Components) - See [CheapHelpers.Blazor](../CheapHelpers.Blazor/README.md)
- **Push Notifications** (FCM, APNS) - `FirebaseTokenHelper`, `DeviceInstallationService`
- **Device Management** - `IDeviceInstallationService` implementations

See [ARCHITECTURE.md](../Docs/ARCHITECTURE.md) for detailed architecture overview.

## License

MIT License - See [LICENSE.txt](../LICENSE.txt) for details
