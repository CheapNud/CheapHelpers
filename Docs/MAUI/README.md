# CheapHelpers.MAUI Documentation

Platform-specific implementations for MAUI Blazor Hybrid applications supporting iOS and Android push notifications, device registration, and local notifications.

## Package Information

**NuGet Package**: `CheapHelpers.MAUI`
**Version**: 1.1.3
**Target Frameworks**: net10.0-android, net10.0-ios
**Dependencies**: CheapHelpers.Blazor, Microsoft.Maui.Controls

## Features

- iOS APNS (Apple Push Notification Service) integration
- Android FCM (Firebase Cloud Messaging) integration
- Device registration and token management
- Local notification display for foreground notifications
- Cross-platform status bar configuration (zero platform-specific code!)
- Android system bars helper (transparent status bar, navigation bar, edge-to-edge)
- iOS status bar helper (style, visibility, height)
- Seamless integration with CheapHelpers.Blazor abstractions
- Automatic permission handling
- Token refresh management

## Documentation

### [Push Notifications Guide](PushNotifications.md)
Comprehensive setup guide covering:
- iOS APNS configuration (certificates, entitlements, AppDelegate)
- Android FCM configuration (Firebase setup, google-services.json, FcmService)
- MauiProgram.cs integration
- Backend implementation options (Azure NH, custom API)
- Permission handling (Android 13+, iOS)
- Token management and refresh
- Testing and troubleshooting

### [Device Installation & Registration](DeviceInstallation.md)
Device management documentation covering:
- IDeviceInstallationService interface
- Platform-specific implementations
- Device registration flow
- Token lifecycle management
- Tags and targeting
- Backend integration patterns
- Best practices

### [Local Notifications](LocalNotifications.md)
Foreground notification display guide covering:
- ILocalNotificationService interface
- iOS and Android implementations
- Automatic push-to-local conversion
- Custom data handling
- Android notification channels
- Permission management
- Usage examples and best practices

### [Status Bar Configuration](StatusBarConfiguration.md)
**NEW!** Unified cross-platform status bar solution covering:
- Zero platform-specific code required
- Configure from MauiProgram.cs with one line
- Works from any MAUI code (pages, view models, etc.)
- Consistent API across iOS and Android
- Light/dark status bar styles
- Transparent status bar support
- Status bar height retrieval
- iOS Info.plist configuration guide
- Android navigation bar control
- Complete usage examples and troubleshooting

### [Android System Bars Helper](AndroidSystemBars.md)
Android-specific system bar configuration utility covering:
- Transparent status bar with light/dark icons
- Navigation bar customization
- Edge-to-edge layout configuration
- Window insets handling
- Safe Firebase token retrieval
- Complete MainActivity examples
- API level requirements and best practices

## Quick Start

### 1. Install Package

```bash
dotnet add package CheapHelpers.MAUI
dotnet add package CheapHelpers.Blazor
```

### 2. Configure MauiProgram.cs

```csharp
using CheapHelpers.Blazor.Hybrid.Extensions;
using CheapHelpers.MAUI.Extensions;
using CheapHelpers.MAUI.Helpers;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseTransparentStatusBar(StatusBarStyle.DarkContent) // Configure status bar!
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Register Blazor Hybrid and MAUI services
        builder.Services
            .AddBlazorHybridPushNotifications(options =>
            {
                options.UseCustomBackend<MyPushNotificationBackend>();
            })
            .AddMauiPushNotifications();

        return builder.Build();
    }
}
```

### 3. Platform-Specific Setup

#### iOS

Create AppDelegate inheriting from `ApnsDelegate`:

```csharp
using CheapHelpers.MAUI.Platforms.iOS;

[Register("AppDelegate")]
public class AppDelegate : ApnsDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
```

Configure Entitlements.plist:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>aps-environment</key>
    <string>development</string>
</dict>
</plist>
```

#### Android

1. Add google-services.json with Build Action: GoogleServicesJson

2. Create MainApplication:

```csharp
using CheapHelpers.MAUI.Platforms.Android;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override void OnCreate()
    {
        base.OnCreate();
        FirebaseInitializer.Initialize(this);
    }
}
```

3. Create Firebase Messaging Service:

```csharp
using CheapHelpers.MAUI.Platforms.Android;

[Service(Exported = false)]
[IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
public class MyFirebaseMessagingService : FcmService
{
}
```

### 4. Request Permissions and Register

```csharp
@inject IDeviceInstallationService DeviceService

protected override async Task OnInitializedAsync()
{
    if (DeviceService.NotificationsSupported)
    {
        var granted = await DeviceService.RequestPermissionsAsync();

        if (granted)
        {
            await DeviceService.RegisterDeviceAsync(userId: "user123");
        }
    }
}
```

## Architecture

```
┌─────────────────────────────────────────────┐
│   Your MAUI Blazor App                      │
│   • Blazor Components                       │
│   • Dependency Injection                    │
├─────────────────────────────────────────────┤
│   CheapHelpers.Blazor.Hybrid                │
│   • IDeviceInstallationService              │
│   • ILocalNotificationService               │
│   • IPushNotificationBackend                │
│   • DeviceInstallation, DeviceInfo models   │
├─────────────────────────────────────────────┤
│   CheapHelpers.MAUI (Platform-Specific)     │
│   ├── iOS                                   │
│   │   ├── ApnsDelegate                      │
│   │   ├── DeviceInstallationService         │
│   │   └── LocalNotificationService          │
│   └── Android                               │
│       ├── FcmService                        │
│       ├── DeviceInstallationService         │
│       ├── LocalNotificationService          │
│       └── FirebaseInitializer               │
├─────────────────────────────────────────────┤
│   Native Platform APIs                      │
│   ├── iOS: UserNotifications, APNS          │
│   └── Android: Firebase Messaging, NotificationCompat│
└─────────────────────────────────────────────┘
```

## Key Interfaces

### IDeviceInstallationService

```csharp
public interface IDeviceInstallationService
{
    string Platform { get; }                // "apns" or "fcmv1"
    string? Token { get; }                  // APNS/FCM token
    bool NotificationsSupported { get; }    // Device capability
    bool IsRegistered { get; }              // Has valid token

    string GetDeviceId();
    string GetDeviceFingerprint();
    DeviceInstallation GetDeviceInstallation(params string[] tags);

    Task<bool> RegisterDeviceAsync(string userId);
    Task<bool> RequestPermissionsAsync();
    Task<bool> CheckPermissionsAsync();

    event Action<string>? TokenRefreshed;
}
```

### ILocalNotificationService

```csharp
public interface ILocalNotificationService
{
    Task ShowNotificationAsync(string title, string body, Dictionary<string, string>? data = null);
    Task<bool> IsPermittedAsync();
}
```

### IPushNotificationBackend

```csharp
public interface IPushNotificationBackend
{
    Task<bool> RegisterDeviceAsync(DeviceInstallation device);
    Task<DeviceInfo?> GetDeviceAsync(string deviceId);
    Task<List<DeviceInfo>> GetUserDevicesAsync(string userId);
    Task<bool> DeactivateDeviceAsync(string deviceId);
    Task<SendNotificationResult> SendNotificationAsync(NotificationPayload payload);
    Task<SendNotificationResult> SendTestNotificationAsync(string deviceId);
}
```

## Platform Requirements

### iOS
- iOS 14.2 or higher
- Valid Apple Developer account
- APNS certificate or .p8 key
- Push Notifications capability enabled
- Physical device (APNS doesn't work in simulator)

### Android
- Android API 21+ (Android 5.0)
- Google Play Services
- Firebase project with Cloud Messaging enabled
- google-services.json configuration file
- POST_NOTIFICATIONS permission (Android 13+)

## Common Scenarios

### Scenario 1: User Login Registration

```csharp
public async Task OnLoginAsync(string userId)
{
    if (DeviceService.NotificationsSupported)
    {
        var permitted = await DeviceService.CheckPermissionsAsync();

        if (permitted)
        {
            await DeviceService.RegisterDeviceAsync(userId);
        }
    }
}
```

### Scenario 2: Token Refresh Handling

```csharp
public class App : Application
{
    public App(IDeviceInstallationService deviceService)
    {
        deviceService.TokenRefreshed += async (newToken) =>
        {
            var userId = await GetCurrentUserIdAsync();
            await deviceService.RegisterDeviceAsync(userId);
        };
    }
}
```

### Scenario 3: Foreground Notification Display

Automatic! The system automatically converts push notifications to local notifications when the app is in the foreground.

## Troubleshooting

### iOS
- Verify Bundle ID matches Apple Developer Portal
- Check Entitlements.plist has aps-environment
- Ensure testing on physical device, not simulator
- Review AppDelegate inheritance and setup

### Android
- Verify google-services.json has correct Build Action
- Check FirebaseInitializer.Initialize() is called
- Ensure FcmService has correct attributes
- Request POST_NOTIFICATIONS permission on Android 13+

### Common
- Verify services are registered in MauiProgram.cs
- Check Debug output for token and error messages
- Ensure IPushNotificationBackend is implemented and registered
- Test permissions are granted before attempting operations

## Resources

- [GitHub Repository](https://github.com/CheapNud/CheapHelpers)
- [NuGet Package](https://www.nuget.org/packages/CheapHelpers.MAUI)
- [Apple APNS Documentation](https://developer.apple.com/documentation/usernotifications)
- [Firebase Cloud Messaging](https://firebase.google.com/docs/cloud-messaging)

## License

MIT License - See LICENSE file in repository

## Support

For issues, questions, or contributions, please visit the [GitHub repository](https://github.com/CheapNud/CheapHelpers).
