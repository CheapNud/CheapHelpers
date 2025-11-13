# Firebase Token Helper

Helper utility for safely retrieving Firebase Cloud Messaging (FCM) tokens in MAUI Android applications.

## Overview

The `FirebaseTokenHelper` is a dedicated utility for Firebase token management, properly separated from system UI concerns. It provides safe token retrieval with comprehensive error handling.

## Why Separate from System Bars?

**Architecture Principle:** Firebase token retrieval is a **background service concern**, not a UI concern.

```
┌─────────────────────────────────┐
│   System UI (Status Bar)       │  ← AndroidSystemBarsHelper
├─────────────────────────────────┤
│   Application UI (App Bar)     │  ← AppBar Component
├─────────────────────────────────┤
│   Business Logic                │
├─────────────────────────────────┤
│   Background Services           │  ← FirebaseTokenHelper
│   (Push Notifications)          │
└─────────────────────────────────┘
```

## Installation

```bash
dotnet add package CheapHelpers.MAUI
```

## Public API

### GetFirebaseTokenSafely

Safely retrieves the Firebase Cloud Messaging (FCM) token with proper null checks and error handling.

```csharp
public static void GetFirebaseTokenSafely(
    Activity activity,
    IDeviceInstallationService deviceService,
    Func<bool>? firebaseAvailabilityCheck = null
)
```

**Parameters:**
- `activity` - The activity context (must implement `IOnSuccessListener`)
- `deviceService` - The device installation service that will receive the token
- `firebaseAvailabilityCheck` - Optional function to check if Firebase is available

**Performs the following checks:**
1. Validates Firebase availability (if check function provided)
2. Checks device notification support
3. Validates activity implements `IOnSuccessListener`
4. Requests FCM token asynchronously
5. Handles all exceptions gracefully

### RefreshFirebaseToken

Forces a Firebase token refresh, useful after user logout/login.

```csharp
public static void RefreshFirebaseToken(
    Activity activity,
    Func<bool>? firebaseAvailabilityCheck = null
)
```

**Parameters:**
- `activity` - The activity context (must implement `IOnSuccessListener`)
- `firebaseAvailabilityCheck` - Optional function to check if Firebase is available

## Usage Example

### Complete MainActivity Implementation

```csharp
using Android.App;
using Android.Content.PM;
using Android.OS;
using CheapHelpers.MAUI.Helpers;
using CheapHelpers.Blazor.Hybrid.Abstractions;
using System.Diagnostics;

namespace YourApp.Platforms.Android;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode
)]
public class MainActivity : MauiAppCompatActivity, global::Android.Gms.Tasks.IOnSuccessListener
{
    private IDeviceInstallationService? _deviceService;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Configure system UI (separate concern)
        AndroidSystemBarsHelper.ConfigureEdgeToEdge(this);

        // Get device service from DI
        _deviceService = IPlatformApplication.Current?.Services?
            .GetService<IDeviceInstallationService>();

        // Safely retrieve Firebase token (background service concern)
        if (_deviceService != null)
        {
            FirebaseTokenHelper.GetFirebaseTokenSafely(
                this,
                _deviceService,
                () => MainApplication.IsFirebaseAvailable
            );
        }
    }

    // Handle Firebase token response
    public void OnSuccess(Java.Lang.Object result)
    {
        try
        {
            var token = result?.ToString() ?? "unknown-token";

            if (_deviceService is CheapHelpers.MAUI.Platforms.Android.DeviceInstallationService service)
            {
                service.SetToken(token);
                Debug.WriteLine($"Firebase token set successfully: {token[..Math.Min(8, token.Length)]}...");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to process Firebase token: {ex.Message}");
        }
    }
}
```

### Refresh Token After Login

```csharp
private void OnUserLoggedIn()
{
    // Refresh Firebase token for new user
    FirebaseTokenHelper.RefreshFirebaseToken(
        this,
        () => MainApplication.IsFirebaseAvailable
    );
}
```

### With Custom Firebase Check

```csharp
// In MainApplication.cs
public class MainApplication : MauiApplication
{
    public static bool IsFirebaseAvailable { get; private set; }

    protected override void OnCreate()
    {
        base.OnCreate();

        try
        {
            Firebase.FirebaseApp.InitializeApp(this);
            IsFirebaseAvailable = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Firebase initialization failed: {ex.Message}");
            IsFirebaseAvailable = false;
        }
    }
}

// In MainActivity.cs
FirebaseTokenHelper.GetFirebaseTokenSafely(
    this,
    deviceService,
    () => MainApplication.IsFirebaseAvailable // Custom check
);
```

## Error Handling

The helper automatically handles the following scenarios:

### Firebase Not Available

```
Output: "FirebaseTokenHelper: Firebase is not available, skipping token retrieval"
```

**Cause:** Firebase SDK not initialized or missing google-services.json

**Solution:** Ensure Firebase is properly configured in your project

### Notifications Not Supported

```
Output: "FirebaseTokenHelper: Notifications not supported on this device, skipping token retrieval"
```

**Cause:** Google Play Services unavailable or device doesn't support FCM

**Solution:** This is expected on emulators without Google Play. Check `IDeviceInstallationService.NotificationsSupported`

### Activity Doesn't Implement IOnSuccessListener

```
Output: "FirebaseTokenHelper: Activity does not implement IOnSuccessListener, cannot retrieve token"
```

**Cause:** MainActivity doesn't implement the required interface

**Solution:** Add `Android.Gms.Tasks.IOnSuccessListener` to MainActivity:

```csharp
public class MainActivity : MauiAppCompatActivity, Android.Gms.Tasks.IOnSuccessListener
{
    public void OnSuccess(Java.Lang.Object result)
    {
        // Handle token
    }
}
```

### General Exceptions

```
Output: "FirebaseTokenHelper: Failed to get Firebase token: [error message]"
```

**Cause:** Unexpected error during token retrieval

**Solution:** Check logcat for full exception details and verify Firebase configuration

## Best Practices

1. **Call Early in Lifecycle**
   - Retrieve token in `OnCreate` of MainActivity
   - Don't wait for user interaction

2. **Separate Concerns**
   - Use `FirebaseTokenHelper` for FCM tokens
   - Use `AndroidSystemBarsHelper` for UI configuration
   - Don't mix push notification logic with UI code

3. **Error Handling**
   - Always provide a Firebase availability check
   - Log token retrieval success/failure
   - Handle `OnSuccess` callback exceptions

4. **Token Management**
   - Store tokens via `IDeviceInstallationService.SetToken()`
   - Refresh tokens after user login/logout
   - Don't store tokens in plain text preferences

5. **Testing**
   - Test on real devices with Google Play Services
   - Test on devices without Google Play Services
   - Verify token persistence across app restarts

## Integration with DeviceInstallationService

The Firebase token is typically stored in the `DeviceInstallationService`:

```csharp
public void OnSuccess(Java.Lang.Object result)
{
    var token = result?.ToString() ?? "unknown-token";

    if (_deviceService is DeviceInstallationService service)
    {
        // This triggers OnTokenReceived event
        service.SetToken(token);

        // Now you can register with your backend
        await service.RegisterDeviceAsync(userId);
    }
}
```

The service will automatically:
- Trigger `OnTokenReceived` event for first-time tokens
- Trigger `OnTokenUpdated` event for token refreshes
- Provide the token via `IDeviceInstallationService.Token` property

## Troubleshooting

### Token Not Received

**Check List:**
1. Is Firebase properly initialized in `MainApplication`?
2. Is `google-services.json` included in the project?
3. Does the device have Google Play Services?
4. Does MainActivity implement `IOnSuccessListener`?
5. Are you testing on a real device (not an emulator without Google Play)?

### Token Received but Not Stored

**Check List:**
1. Is `OnSuccess` callback implemented correctly?
2. Is `SetToken()` being called on the device service?
3. Are there any exceptions in the `OnSuccess` callback?
4. Is the token being overwritten elsewhere?

### Token Changes on Every Launch

**This is normal behavior** when:
- Firebase token is refreshed by FCM service
- App is reinstalled
- App data is cleared

**Solution:** Always be prepared to handle token updates via the `OnTokenUpdated` event.

## See Also

- [Android System Bars](AndroidSystemBars.md) - System UI configuration (separate concern)
- [Push Notifications](PushNotifications.md) - Complete push notification setup
- [Device Installation](DeviceInstallation.md) - Device registration with backend
- [Local Notifications](LocalNotifications.md) - Foreground notification handling

## Source Location

`CheapHelpers.MAUI/Helpers/FirebaseTokenHelper.cs`
