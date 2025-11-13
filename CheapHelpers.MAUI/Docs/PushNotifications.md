# Push Notifications - CheapHelpers.MAUI

Comprehensive guide for implementing push notifications in MAUI apps using Apple Push Notification Service (APNS) for iOS and Firebase Cloud Messaging (FCM) for Android.

## Table of Contents

1. [Overview](#overview)
2. [Installation](#installation)
3. [iOS APNS Setup](#ios-apns-setup)
4. [Android FCM Setup](#android-fcm-setup)
5. [MAUI Integration](#maui-integration)
6. [Backend Implementation](#backend-implementation)
7. [Permission Handling](#permission-handling)
8. [Token Management](#token-management)
9. [Testing](#testing)
10. [Troubleshooting](#troubleshooting)

## Overview

The CheapHelpers.MAUI package provides platform-specific implementations for:

- **iOS APNS**: Native Apple Push Notification Service integration
- **Android FCM**: Firebase Cloud Messaging integration
- **Device Registration**: Automatic device token management
- **Foreground Notifications**: Local notification display when app is active
- **Blazor Hybrid Integration**: Seamless integration with CheapHelpers.Blazor abstractions

### Architecture

```
┌─────────────────────────────────────┐
│   Your MAUI Blazor App              │
├─────────────────────────────────────┤
│   CheapHelpers.Blazor.Hybrid        │ ← Abstractions & Models
│   (IDeviceInstallationService, etc) │
├─────────────────────────────────────┤
│   CheapHelpers.MAUI                 │ ← Platform Implementations
│   ├── iOS: ApnsDelegate             │
│   └── Android: FcmService            │
├─────────────────────────────────────┤
│   Platform Native APIs              │
│   ├── iOS: UserNotifications         │
│   └── Android: Firebase Messaging    │
└─────────────────────────────────────┘
```

## Installation

### NuGet Packages

```xml
<!-- Required packages -->
<PackageReference Include="CheapHelpers.Blazor" Version="1.x.x" />
<PackageReference Include="CheapHelpers.MAUI" Version="1.x.x" />
```

The MAUI package automatically includes platform-specific dependencies:
- **iOS**: UserNotifications framework (built-in)
- **Android**: Xamarin.Firebase.Messaging, Xamarin.GooglePlayServices.Base

### Project File Configuration

Your MAUI project file should target both platforms:

```xml
<TargetFrameworks>net10.0-android;net10.0-ios</TargetFrameworks>
```

## iOS APNS Setup

### 1. Apple Developer Account Configuration

#### Create an App ID with Push Notifications

1. Go to [Apple Developer Portal](https://developer.apple.com/account)
2. Navigate to **Certificates, Identifiers & Profiles**
3. Create/Edit your App ID:
   - Enable **Push Notifications** capability
   - Note your **Bundle Identifier** (e.g., `com.yourcompany.yourapp`)

#### Generate APNS Certificate or Key

**Option A: APNs Auth Key (Recommended)**

1. Go to **Keys** section
2. Click **+** to create a new key
3. Enable **Apple Push Notifications service (APNs)**
4. Download the `.p8` key file
5. Note your **Key ID** and **Team ID**

**Option B: Push Certificate**

1. Go to **Certificates** section
2. Create a new certificate
3. Select **Apple Push Notification service SSL**
4. Follow the CSR generation steps
5. Download the certificate and install in Keychain
6. Export as `.p12` file

### 2. Xcode Project Configuration

#### Info.plist

No special entries needed for basic push notifications. However, ensure your bundle identifier matches:

```xml
<key>CFBundleIdentifier</key>
<string>com.yourcompany.yourapp</string>
```

For background notifications (optional):

```xml
<key>UIBackgroundModes</key>
<array>
    <string>remote-notification</string>
</array>
```

#### Entitlements.plist

Add push notification entitlement:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>aps-environment</key>
    <string>development</string> <!-- Use 'production' for release builds -->
</dict>
</plist>
```

### 3. AppDelegate Implementation

Create your AppDelegate by inheriting from `ApnsDelegate`:

```csharp
using CheapHelpers.MAUI.Platforms.iOS;
using Foundation;

namespace YourApp;

[Register("AppDelegate")]
public class AppDelegate : ApnsDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    // Optional: Handle custom notification data
    protected override void OnNotificationReceived(string title, string body, Dictionary<string, string> data)
    {
        // Process notification data
        Debug.WriteLine($"Notification: {title} - {body}");

        // Check for custom data
        if (data.TryGetValue("action", out var action))
        {
            // Handle custom actions
        }

        base.OnNotificationReceived(title, body, data);
    }
}
```

### 4. Build Configuration

Ensure you're building with the correct provisioning profile:

**Debug Builds:**
- Use development provisioning profile
- `aps-environment` = `development`

**Release Builds:**
- Use distribution provisioning profile
- `aps-environment` = `production`

## Android FCM Setup

### 1. Firebase Console Setup

#### Create Firebase Project

1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Click **Add Project** or select existing project
3. Enter project name and follow setup wizard

#### Register Android App

1. In Firebase Console, click **Add App** → Android
2. Enter your **Android package name** (must match your MAUI app package)
   - Found in `AndroidManifest.xml` or project properties
   - Example: `com.yourcompany.yourapp`
3. Download `google-services.json`
4. Add SHA-1 fingerprint (optional, for additional security)

#### Enable Cloud Messaging

1. In Firebase Console, go to **Project Settings**
2. Navigate to **Cloud Messaging** tab
3. Note your **Sender ID** and **Server Key**
4. For FCM v1 API, go to **Service Accounts** and generate a new private key

### 2. Add google-services.json to Project

1. Place `google-services.json` in your Android project root:
   ```
   YourApp/Platforms/Android/google-services.json
   ```

2. Set Build Action in `.csproj`:
   ```xml
   <ItemGroup>
     <GoogleServicesJson Include="Platforms\Android\google-services.json" />
   </ItemGroup>
   ```

   Or manually in project file properties:
   - Right-click `google-services.json`
   - Properties → Build Action: **GoogleServicesJson**

### 3. AndroidManifest.xml Configuration

Add required permissions and service declaration:

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
    <application android:allowBackup="true" android:icon="@mipmap/appicon" android:roundIcon="@mipmap/appicon_round" android:supportsRtl="true">
    </application>

    <!-- Required Permissions -->
    <uses-permission android:name="android.permission.INTERNET" />
    <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />

    <!-- Android 13+ notification permission -->
    <uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
</manifest>
```

### 4. MainApplication Implementation

Create or modify your `MainApplication` class to initialize Firebase:

```csharp
using Android.App;
using Android.Runtime;
using CheapHelpers.MAUI.Platforms.Android;

namespace YourApp;

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

        // CRITICAL: Initialize Firebase
        FirebaseInitializer.Initialize(this);
    }
}
```

### 5. Firebase Messaging Service Implementation

Create a class that inherits from `FcmService`:

```csharp
using Android.App;
using CheapHelpers.MAUI.Platforms.Android;
using Firebase.Messaging;

namespace YourApp.Platforms.Android;

[Service(Exported = false)]
[IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
public class MyFirebaseMessagingService : FcmService
{
    // Optional: Override to handle custom notification processing
    protected override void OnNotificationReceived(string title, string body, Dictionary<string, string> data)
    {
        // Custom processing
        Debug.WriteLine($"FCM Notification: {title}");

        // Check for custom data
        if (data.TryGetValue("customField", out var value))
        {
            // Handle custom data
        }

        // Always call base to display notification
        base.OnNotificationReceived(title, body, data);
    }
}
```

**Important Attributes:**
- `[Service(Exported = false)]`: Required for Android security
- `[IntentFilter]`: Registers service to receive FCM messages

### 6. Verify Firebase Initialization

Add diagnostic logging to check Firebase status:

```csharp
// In your app startup or debug page
var (isAvailable, status, app) = FirebaseInitializer.GetStatus();
Debug.WriteLine($"Firebase Status: {status}");

if (!isAvailable)
{
    Debug.WriteLine("Firebase not initialized - check google-services.json");
}
```

## MAUI Integration

### 1. MauiProgram.cs Configuration

Register services in your `MauiProgram.cs`:

```csharp
using CheapHelpers.Blazor.Hybrid.Extensions;
using CheapHelpers.MAUI.Extensions;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Register Blazor Hybrid services with backend
        builder.Services.AddBlazorHybridPushNotifications(options =>
        {
            // Option 1: Use custom backend implementation
            options.UseCustomBackend<MyPushNotificationBackend>();

            // Option 2: Use Azure Notification Hubs
            // options.UseAzureNotificationHub("connection-string", "hub-name");
        });

        // Register MAUI platform-specific services
        builder.Services.AddMauiPushNotifications(options =>
        {
            // Optional: Configure Android notification channel
            options.ConfigureAndroidChannel(
                channelId: "my_app_notifications",
                channelName: "App Notifications",
                channelDescription: "Notifications from My App"
            );
        });

        return builder.Build();
    }
}
```

### 2. Alternative: Combined Registration

You can also register both in one call:

```csharp
builder.Services.AddMauiPushNotifications(
    configureCore: options =>
    {
        options.UseCustomBackend<MyPushNotificationBackend>();
    },
    configurePlatform: options =>
    {
        options.ConfigureAndroidChannel("notifications", "App Notifications", "All app notifications");
    }
);
```

### 3. Dependency Injection in Blazor Components

Inject services into your Blazor components:

```razor
@inject IDeviceInstallationService DeviceService
@inject IPushNotificationBackend BackendService

@code {
    protected override async Task OnInitializedAsync()
    {
        // Check if notifications are supported
        if (DeviceService.NotificationsSupported)
        {
            // Request permissions
            var granted = await DeviceService.RequestPermissionsAsync();

            if (granted)
            {
                // Register device
                await DeviceService.RegisterDeviceAsync(userId: "user123");
            }
        }
    }
}
```

## Backend Implementation

### Option 1: Custom Backend API

Implement `IPushNotificationBackend` for your own API:

```csharp
using CheapHelpers.Blazor.Hybrid.Abstractions;
using CheapHelpers.Blazor.Hybrid.Models;

public class MyPushNotificationBackend : IPushNotificationBackend
{
    private readonly HttpClient _httpClient;

    public MyPushNotificationBackend(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("PushAPI");
        _httpClient.BaseAddress = new Uri("https://your-api.com");
    }

    public async Task<bool> RegisterDeviceAsync(DeviceInstallation device)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/devices/register", device);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Device registration failed: {ex.Message}");
            return false;
        }
    }

    public async Task<DeviceInfo?> GetDeviceAsync(string deviceId)
    {
        var response = await _httpClient.GetFromJsonAsync<DeviceInfo>($"/api/devices/{deviceId}");
        return response;
    }

    public async Task<List<DeviceInfo>> GetUserDevicesAsync(string userId)
    {
        var response = await _httpClient.GetFromJsonAsync<List<DeviceInfo>>($"/api/users/{userId}/devices");
        return response ?? new List<DeviceInfo>();
    }

    public async Task<bool> DeactivateDeviceAsync(string deviceId)
    {
        var response = await _httpClient.DeleteAsync($"/api/devices/{deviceId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<SendNotificationResult> SendNotificationAsync(NotificationPayload payload)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/notifications/send", payload);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<SendNotificationResult>()
                ?? new SendNotificationResult { Success = true };
        }

        return new SendNotificationResult
        {
            Success = false,
            ErrorMessage = $"HTTP {response.StatusCode}"
        };
    }

    public async Task<SendNotificationResult> SendTestNotificationAsync(string deviceId)
    {
        var testPayload = new NotificationPayload
        {
            Title = "Test Notification",
            Body = "This is a test notification from your app",
            Data = new Dictionary<string, string>
            {
                { "test", "true" },
                { "timestamp", DateTime.UtcNow.ToString("o") }
            }
        };

        return await SendNotificationAsync(testPayload);
    }
}
```

### Option 2: Azure Notification Hubs

Use the built-in Azure Notification Hubs integration:

```csharp
builder.Services.AddBlazorHybridPushNotifications(options =>
{
    options.UseAzureNotificationHub(
        connectionString: "Endpoint=sb://...;SharedAccessKey=...",
        hubName: "your-notification-hub"
    );
});
```

Backend API for sending notifications with Azure:

```csharp
// Server-side code (ASP.NET Core API)
using Microsoft.Azure.NotificationHubs;

public class NotificationService
{
    private readonly NotificationHubClient _hubClient;

    public NotificationService(IConfiguration config)
    {
        var connectionString = config["Azure:NotificationHub:ConnectionString"];
        var hubName = config["Azure:NotificationHub:HubName"];
        _hubClient = NotificationHubClient.CreateClientFromConnectionString(connectionString, hubName);
    }

    public async Task SendToUserAsync(string userId, string title, string body, Dictionary<string, string>? data = null)
    {
        // iOS notification
        var apnsPayload = new
        {
            aps = new
            {
                alert = new { title, body },
                sound = "default"
            },
            customData = data
        };

        // Android notification
        var fcmPayload = new
        {
            notification = new { title, body },
            data = data
        };

        try
        {
            // Send to user tag
            await _hubClient.SendAppleNativeNotificationAsync(
                JsonSerializer.Serialize(apnsPayload),
                $"user_{userId}"
            );

            await _hubClient.SendFcmNativeNotificationAsync(
                JsonSerializer.Serialize(fcmPayload),
                $"user_{userId}"
            );
        }
        catch (Exception ex)
        {
            // Handle error
        }
    }
}
```

## Permission Handling

### Android 13+ Runtime Permissions

Android 13 (API 33+) requires runtime permission for notifications:

```csharp
public async Task RequestNotificationPermissionAsync()
{
    if (DeviceInfo.Platform == DevicePlatform.Android)
    {
        // Check current permission status
        var status = await DeviceService.CheckPermissionsAsync();

        if (!status)
        {
            // Request permission
            var granted = await DeviceService.RequestPermissionsAsync();

            if (!granted)
            {
                // Show explanation and guide to settings
                await Shell.Current.DisplayAlert(
                    "Notifications Disabled",
                    "Please enable notifications in app settings to receive updates.",
                    "OK"
                );
            }
        }
    }
}
```

### iOS Permission Request

iOS always requires explicit permission:

```csharp
public async Task<bool> SetupPushNotificationsAsync()
{
    if (!DeviceService.NotificationsSupported)
    {
        Debug.WriteLine("Push notifications not supported on this device");
        return false;
    }

    // Request permission (shows iOS popup)
    var granted = await DeviceService.RequestPermissionsAsync();

    if (!granted)
    {
        Debug.WriteLine("User denied notification permission");
        return false;
    }

    // Wait for APNS token
    await Task.Delay(1000); // Give iOS time to fetch token

    if (string.IsNullOrEmpty(DeviceService.Token))
    {
        Debug.WriteLine("Failed to obtain APNS token");
        return false;
    }

    return true;
}
```

### Best Practices for Permission Requests

1. **Explain Before Asking**: Show UI explaining why you need notifications
2. **Timing**: Request permissions when user expects them (e.g., after login)
3. **Graceful Degradation**: App should work without notifications
4. **Settings Link**: Provide a way to open app settings if permission denied

```csharp
// Example permission flow
public async Task<bool> RequestPermissionsWithExplanationAsync()
{
    // First check current status
    var alreadyGranted = await DeviceService.CheckPermissionsAsync();
    if (alreadyGranted) return true;

    // Show explanation
    var userWants = await Shell.Current.DisplayAlert(
        "Stay Updated",
        "Enable notifications to receive important updates and messages.",
        "Enable",
        "Not Now"
    );

    if (!userWants) return false;

    // Request permission
    var granted = await DeviceService.RequestPermissionsAsync();

    if (!granted)
    {
        // Offer to open settings
        var openSettings = await Shell.Current.DisplayAlert(
            "Permission Required",
            "Notifications are currently disabled. Would you like to enable them in settings?",
            "Open Settings",
            "Cancel"
        );

        if (openSettings)
        {
            AppInfo.ShowSettingsUI();
        }
    }

    return granted;
}
```

## Token Management

### Token Lifecycle

Both platforms automatically handle token refresh:

**iOS APNS:**
- Token generated on first permission grant
- Token can change when app is reinstalled or restored
- `ApnsDelegate` automatically captures new tokens
- `TokenRefreshed` event fires on update

**Android FCM:**
- Token generated when Firebase initializes
- Token refreshes when app is updated or reinstalled
- `FcmService.OnNewToken()` handles refreshes
- `TokenRefreshed` event fires on update

### Handling Token Refresh

Subscribe to token refresh events:

```csharp
public class NotificationManager
{
    private readonly IDeviceInstallationService _deviceService;
    private readonly IPushNotificationBackend _backend;

    public NotificationManager(
        IDeviceInstallationService deviceService,
        IPushNotificationBackend backend)
    {
        _deviceService = deviceService;
        _backend = backend;

        // Subscribe to token refresh
        _deviceService.TokenRefreshed += OnTokenRefreshed;
    }

    private async void OnTokenRefreshed(string newToken)
    {
        Debug.WriteLine($"Token refreshed: {newToken[..8]}...");

        // Re-register device with new token
        var userId = await GetCurrentUserIdAsync();
        if (!string.IsNullOrEmpty(userId))
        {
            await _deviceService.RegisterDeviceAsync(userId);
        }
    }

    private async Task<string> GetCurrentUserIdAsync()
    {
        // Get from your auth service
        return "user123";
    }
}
```

### Token Validation

Check token status before operations:

```csharp
public async Task<bool> EnsureTokenValidAsync()
{
    // Check if token exists
    if (string.IsNullOrEmpty(DeviceService.Token))
    {
        Debug.WriteLine("No token available");

        // Request permissions to trigger token generation
        var granted = await DeviceService.RequestPermissionsAsync();
        if (!granted) return false;

        // Wait for token (with timeout)
        for (int i = 0; i < 10; i++)
        {
            await Task.Delay(500);
            if (!string.IsNullOrEmpty(DeviceService.Token))
                return true;
        }

        Debug.WriteLine("Timeout waiting for token");
        return false;
    }

    return true;
}
```

## Testing

### iOS Testing

**Development Testing:**

1. Build app with development provisioning profile
2. Install on physical device (APNS doesn't work in simulator)
3. Grant notification permission when prompted
4. Check Debug output for APNS token

**Send Test Notification (Terminal):**

```bash
# Using APNs HTTP/2 with .p8 key
curl -v \
  --header "apns-topic: com.yourcompany.yourapp" \
  --header "apns-push-type: alert" \
  --header "authorization: bearer $JWT_TOKEN" \
  --data '{"aps":{"alert":{"title":"Test","body":"Hello from APNS"}}}' \
  https://api.sandbox.push.apple.com/3/device/YOUR_DEVICE_TOKEN
```

**Production Testing:**
- Use production certificate/key
- Use `https://api.push.apple.com` (without 'sandbox')
- Deploy via TestFlight or App Store

### Android Testing

**Development Testing:**

1. Ensure device has Google Play Services
2. Build and install app
3. Grant notification permission (Android 13+)
4. Check logcat for FCM token

**Logcat Filtering:**

```bash
adb logcat | grep -i "firebase\|fcm\|notification"
```

**Send Test Notification (Firebase Console):**

1. Go to Firebase Console → Cloud Messaging
2. Click **Send Test Message**
3. Enter FCM token from your device
4. Send notification

**Send Test Notification (API):**

```bash
# Using FCM v1 API
curl -X POST \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "message": {
      "token": "YOUR_FCM_TOKEN",
      "notification": {
        "title": "Test Notification",
        "body": "Hello from FCM"
      }
    }
  }' \
  https://fcm.googleapis.com/v1/projects/YOUR_PROJECT_ID/messages:send
```

### Debug Checklist

**iOS:**
- [ ] Bundle ID matches App ID in Apple Developer Portal
- [ ] Push Notifications capability enabled in App ID
- [ ] Correct provisioning profile selected
- [ ] Entitlements.plist has `aps-environment`
- [ ] Testing on physical device (not simulator)
- [ ] Token appears in Debug output
- [ ] Certificate/key uploaded to backend

**Android:**
- [ ] google-services.json in project with correct Build Action
- [ ] Package name matches Firebase app registration
- [ ] FirebaseInitializer.Initialize() called in OnCreate
- [ ] FcmService registered with correct IntentFilter
- [ ] Google Play Services available on device
- [ ] POST_NOTIFICATIONS permission granted (Android 13+)
- [ ] Token appears in logcat

## Troubleshooting

### iOS Issues

**Problem: "No APNS token received"**

Solutions:
- Ensure device has internet connection
- Check provisioning profile includes Push Notifications
- Verify `aps-environment` in Entitlements.plist
- Restart device and try again
- Check Apple Developer Portal for App ID configuration

**Problem: "didFailToRegisterForRemoteNotificationsWithError"**

Solutions:
- Check error message in Debug output
- Verify Bundle ID matches App ID exactly
- Ensure using physical device, not simulator
- Check network connectivity
- Regenerate provisioning profile

**Problem: "Notifications not appearing in foreground"**

Solutions:
- Verify `LocalNotificationService` is registered
- Check `UNUserNotificationCenter.Current.Delegate` is set
- Ensure permissions granted
- Check `WillPresentNotification` implementation

**Problem: "Token changes frequently"**

This is normal when:
- Reinstalling app
- Restoring to new device
- iOS updates
- Solution: Handle `TokenRefreshed` event to update backend

### Android Issues

**Problem: "Firebase not initialized"**

Solutions:
- Verify google-services.json exists and has correct Build Action
- Check FirebaseInitializer.Initialize() called
- Review logcat for Firebase errors
- Ensure package name matches Firebase registration
- Rebuild project (Clean + Rebuild)

**Problem: "No FCM token received"**

Solutions:
- Check Google Play Services installed and up-to-date
- Verify internet connection
- Wait longer (token can take 5-10 seconds)
- Check logcat for Firebase errors
- Ensure google-services.json is valid

**Problem: "FcmService.OnMessageReceived not called"**

Solutions:
- Verify `[Service(Exported = false)]` attribute
- Check `[IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]`
- Ensure service class is public
- Check AndroidManifest.xml includes service registration
- Rebuild project

**Problem: "Notifications not showing on Android 13+"**

Solutions:
- Request POST_NOTIFICATIONS permission
- Check permission status with `CheckPermissionsAsync()`
- Verify notification channel created
- Check NotificationManager settings

**Problem: "Google Play Services not available"**

Solutions:
- Update Google Play Services on device
- Test on different device
- Check device API level (minimum 21)
- Review `DeviceInstallationService.CheckNotificationSupport()` logs

### Common Issues (Both Platforms)

**Problem: "IDeviceInstallationService not registered"**

Solutions:
- Ensure `AddMauiPushNotifications()` called in MauiProgram.cs
- Verify `AddBlazorHybridPushNotifications()` also called
- Check service registration order

**Problem: "Backend registration fails"**

Solutions:
- Verify `IPushNotificationBackend` implementation registered
- Check backend API connectivity
- Review backend error logs
- Ensure device token is valid

**Problem: "Notifications work in debug but not release"**

iOS:
- Use production APNS certificate/key
- Update `aps-environment` to `production`
- Use correct provisioning profile

Android:
- Ensure google-services.json includes release SHA-1
- Check ProGuard/R8 rules aren't removing Firebase classes

**Problem: "Foreground notifications not appearing"**

Solutions:
- Verify `ILocalNotificationService` registered
- Check permissions granted
- Review platform-specific delegate/service implementations
- Test with simplified notification payload

### Getting Help

If you encounter issues not covered here:

1. Enable verbose logging:
   ```csharp
   Debug.WriteLine($"Platform: {DeviceService.Platform}");
   Debug.WriteLine($"Token: {DeviceService.Token?[..8]}...");
   Debug.WriteLine($"Supported: {DeviceService.NotificationsSupported}");
   Debug.WriteLine($"Registered: {DeviceService.IsRegistered}");
   ```

2. Check platform-specific logs:
   - iOS: Xcode Console or Device Log
   - Android: adb logcat

3. Verify configuration:
   - iOS: Provisioning profile, entitlements, capabilities
   - Android: google-services.json, manifest permissions

4. Test with minimal setup:
   - Remove custom implementations
   - Use default configurations
   - Test with Firebase/APNS test tools

5. Review sample implementations in the [GitHub repository](https://github.com/CheapNud/CheapHelpers)

## Next Steps

- [Device Installation Documentation](DeviceInstallation.md)
- [Local Notifications Documentation](LocalNotifications.md)
- [CheapHelpers.Blazor Documentation](../Blazor/README.md)
