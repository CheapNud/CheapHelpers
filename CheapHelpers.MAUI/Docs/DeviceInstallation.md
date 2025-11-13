# Device Installation & Registration - CheapHelpers.MAUI

Comprehensive guide for managing device registration, token handling, and backend integration for push notifications.

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [IDeviceInstallationService Interface](#ideviceinstallationservice-interface)
4. [Platform Implementations](#platform-implementations)
5. [Device Registration Flow](#device-registration-flow)
6. [Token Management](#token-management)
7. [Tags & Targeting](#tags--targeting)
8. [Backend Integration](#backend-integration)
9. [Best Practices](#best-practices)
10. [Troubleshooting](#troubleshooting)

## Overview

The Device Installation system manages the lifecycle of push notification registration across iOS and Android platforms. It handles:

- **Token Acquisition**: Obtaining APNS/FCM device tokens
- **Device Identification**: Unique device IDs and fingerprints
- **Backend Registration**: Registering devices with your notification backend
- **Token Refresh**: Automatic handling of token updates
- **Permission Management**: Requesting and checking notification permissions
- **Tag Management**: User-based and device-based targeting

## Architecture

```
┌────────────────────────────────────────────────┐
│  Your App / Blazor Components                  │
│  ↓ Inject IDeviceInstallationService           │
├────────────────────────────────────────────────┤
│  CheapHelpers.Blazor.Hybrid.Abstractions       │
│  • IDeviceInstallationService (interface)      │
│  • DeviceInstallation (model)                  │
├────────────────────────────────────────────────┤
│  CheapHelpers.MAUI Platform Implementations    │
│  ├── iOS: DeviceInstallationService            │
│  │   • APNS token management                   │
│  │   • UIDevice.IdentifierForVendor            │
│  │   • UserNotifications permissions           │
│  └── Android: DeviceInstallationService        │
│      • FCM token management                    │
│      • Secure.AndroidId                        │
│      • POST_NOTIFICATIONS permissions          │
├────────────────────────────────────────────────┤
│  Backend Integration                           │
│  • IPushNotificationBackend                    │
│  • Azure Notification Hubs / Custom API        │
└────────────────────────────────────────────────┘
```

## IDeviceInstallationService Interface

The core abstraction defined in CheapHelpers.Blazor.Hybrid:

```csharp
public interface IDeviceInstallationService
{
    // Platform and token info
    string Platform { get; }                    // "apns" or "fcmv1"
    string? Token { get; }                      // APNS/FCM token
    bool NotificationsSupported { get; }        // Device capability check
    bool IsRegistered { get; }                  // Has valid token

    // Device identification
    string GetDeviceId();                       // Platform-specific device ID
    string GetDeviceFingerprint();              // Persistent unique identifier

    // Registration and permissions
    DeviceInstallation GetDeviceInstallation(params string[] tags);
    Task<bool> RegisterDeviceAsync(string userId);
    Task<bool> RequestPermissionsAsync();
    Task<bool> CheckPermissionsAsync();

    // Events
    event Action<string>? TokenRefreshed;       // Fired when token updates
}
```

### Properties

#### Platform
- **iOS**: Returns `"apns"`
- **Android**: Returns `"fcmv1"` (Firebase Cloud Messaging v1)
- Used by backend to determine which notification service to use

#### Token
- **iOS**: APNS device token in hex format (64 characters)
- **Android**: FCM registration token (152+ characters)
- `null` if not yet obtained or permissions not granted
- Automatically updated when token refreshes

#### NotificationsSupported
- Checks if device/platform supports push notifications
- **iOS**: Requires iOS 14.2+
- **Android**: Requires Google Play Services, API 19+, internet permission

#### IsRegistered
- `true` if device has a valid token
- Shorthand for `!string.IsNullOrEmpty(Token)`

### Methods

#### GetDeviceId()
Returns platform-specific unique device identifier:

- **iOS**: `UIDevice.IdentifierForVendor` (changes on app reinstall)
- **Android**: `Secure.AndroidId` (persistent per device)

#### GetDeviceFingerprint()
Returns persistent device fingerprint that survives reinstalls:

- Stored in MAUI Preferences
- Format: `{platform}_{deviceId}_{bundleId}_{guid}`
- Example: `ios_12345-ABCDE_com.app.name_a1b2c3d4...`

#### GetDeviceInstallation(params string[] tags)
Creates a `DeviceInstallation` object for backend registration:

```csharp
public class DeviceInstallation
{
    public string InstallationId { get; set; }  // Device ID
    public string Platform { get; set; }         // "apns" or "fcmv1"
    public string PushChannel { get; set; }      // Token
    public List<string> Tags { get; set; }       // Targeting tags
}
```

Automatic tags added:
- `platform_ios` or `platform_android`
- `device_{deviceId}`
- `notifications_supported` (if true)
- `has_valid_token` (if token exists)

#### RegisterDeviceAsync(string userId)
Registers device with backend:

1. Validates token exists
2. Retrieves `IPushNotificationBackend` from DI
3. Creates `DeviceInstallation` with `user_{userId}` tag
4. Calls `backend.RegisterDeviceAsync()`
5. Returns success status

#### RequestPermissionsAsync()
Requests notification permissions from user:

- **iOS**: Shows system permission dialog, registers for remote notifications
- **Android**: Requests `POST_NOTIFICATIONS` permission on Android 13+
- Returns `true` if granted, `false` if denied

#### CheckPermissionsAsync()
Checks current permission status without requesting:

- **iOS**: Queries `UNUserNotificationCenter` authorization status
- **Android**: Checks `POST_NOTIFICATIONS` permission status
- Returns current permission state

### Events

#### TokenRefreshed
Fired when platform token is updated:

```csharp
deviceService.TokenRefreshed += async (newToken) =>
{
    Debug.WriteLine($"Token updated: {newToken[..8]}...");

    // Re-register with backend
    await deviceService.RegisterDeviceAsync(currentUserId);
};
```

Triggered by:
- **iOS**: APNS token received or refreshed
- **Android**: FCM token received or refreshed

## Platform Implementations

### iOS Implementation

File: `CheapHelpers.MAUI/Platforms/iOS/DeviceInstallationService.cs`

#### Token Acquisition

APNS tokens are obtained via `ApnsDelegate`:

```csharp
// ApnsDelegate.cs
[Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")]
public void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
{
    var token = deviceToken.ToHexString();

    if (DeviceInstallationService is DeviceInstallationService service)
    {
        service.SetToken(token);
    }
}
```

The service stores and exposes the token:

```csharp
public class DeviceInstallationService : IDeviceInstallationService
{
    private string _token = string.Empty;

    public void SetToken(string token)
    {
        var oldToken = _token;
        _token = token ?? string.Empty;

        if (!string.IsNullOrEmpty(_token) && _token != oldToken)
        {
            TokenRefreshed?.Invoke(_token);
        }
    }

    public string? Token => string.IsNullOrEmpty(_token) ? null : _token;
}
```

#### Permission Flow

```csharp
public async Task<bool> RequestPermissionsAsync()
{
    var tcs = new TaskCompletionSource<bool>();

    UNUserNotificationCenter.Current.RequestAuthorization(
        UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound,
        (approvalGranted, error) =>
        {
            if (approvalGranted && error == null)
            {
                // Register for remote notifications to get token
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    UIApplication.SharedApplication.RegisterForRemoteNotifications();
                });
                tcs.SetResult(true);
            }
            else
            {
                tcs.SetResult(false);
            }
        });

    return await tcs.Task;
}
```

### Android Implementation

File: `CheapHelpers.MAUI/Platforms/Android/DeviceInstallationService.cs`

#### Token Acquisition

FCM tokens are obtained via `FcmService`:

```csharp
// FcmService.cs
public override void OnNewToken(string token)
{
    var serviceProvider = IPlatformApplication.Current?.Services;
    var deviceService = serviceProvider?.GetService<IDeviceInstallationService>();

    if (deviceService is DeviceInstallationService service)
    {
        service.SetToken(token);
    }
}
```

#### Notification Support Check

```csharp
public bool NotificationsSupported
{
    get
    {
        // Check Google Play Services
        var availability = GoogleApiAvailability.Instance;
        var resultCode = availability.IsGooglePlayServicesAvailable(context);
        bool isPlayServicesAvailable = resultCode == ConnectionResult.Success;

        // Check API level (FCM requires API 19+)
        var apiLevel = Build.VERSION.SdkInt;
        if ((int)apiLevel < 19) return false;

        // Check internet permission
        var hasInternetPermission = context.CheckSelfPermission(Permission.Internet)
            == Permission.Granted;

        return isPlayServicesAvailable && hasInternetPermission;
    }
}
```

#### Token Wait Helper

Android-specific method to wait for FCM token with timeout:

```csharp
public async Task<bool> WaitForTokenAsync(int timeoutSeconds = 10)
{
    if (!string.IsNullOrEmpty(Token) && !Token.StartsWith("development-"))
        return true;

    var tokenReceived = false;
    void handler(string token) => tokenReceived = true;

    TokenRefreshed += handler;

    var waitCount = 0;
    while (!tokenReceived && waitCount < timeoutSeconds * 2)
    {
        await Task.Delay(500);
        waitCount++;

        if (!string.IsNullOrEmpty(Token) && !Token.StartsWith("development-"))
        {
            tokenReceived = true;
            break;
        }
    }

    TokenRefreshed -= handler;
    return tokenReceived;
}
```

## Device Registration Flow

### Complete Registration Example

```csharp
@inject IDeviceInstallationService DeviceService
@inject IAuthenticationService AuthService

public class NotificationSetup
{
    public async Task<bool> RegisterForNotificationsAsync()
    {
        // Step 1: Check if notifications are supported
        if (!DeviceService.NotificationsSupported)
        {
            Debug.WriteLine("Notifications not supported on this device");
            return false;
        }

        // Step 2: Check existing permissions
        var alreadyGranted = await DeviceService.CheckPermissionsAsync();

        if (!alreadyGranted)
        {
            // Step 3: Request permissions
            var granted = await DeviceService.RequestPermissionsAsync();

            if (!granted)
            {
                Debug.WriteLine("User denied notification permissions");
                return false;
            }
        }

        // Step 4: Wait for token (especially important on Android)
        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            var androidService = DeviceService as Platforms.Android.DeviceInstallationService;
            var hasToken = await androidService?.WaitForTokenAsync(10);

            if (hasToken != true)
            {
                Debug.WriteLine("Timeout waiting for FCM token");
                return false;
            }
        }

        // Step 5: Verify token
        if (string.IsNullOrEmpty(DeviceService.Token))
        {
            Debug.WriteLine("No device token available");
            return false;
        }

        // Step 6: Register with backend
        var currentUser = await AuthService.GetCurrentUserAsync();
        var success = await DeviceService.RegisterDeviceAsync(currentUser.Id);

        if (success)
        {
            Debug.WriteLine("Device registered successfully");
            return true;
        }
        else
        {
            Debug.WriteLine("Device registration with backend failed");
            return false;
        }
    }
}
```

### Simplified Registration

For simpler scenarios:

```csharp
public async Task QuickRegisterAsync(string userId)
{
    if (!DeviceService.NotificationsSupported)
        return;

    // Request permissions and register
    var granted = await DeviceService.RequestPermissionsAsync();

    if (granted)
    {
        // Small delay for token acquisition
        await Task.Delay(1000);

        await DeviceService.RegisterDeviceAsync(userId);
    }
}
```

### Auto-Registration on Login

```csharp
// In your authentication service
public class AuthenticationService
{
    private readonly IDeviceInstallationService _deviceService;

    public async Task<bool> LoginAsync(string username, string password)
    {
        var loginResult = await PerformLoginAsync(username, password);

        if (loginResult.Success)
        {
            // Automatically register device for notifications
            await RegisterDeviceForNotificationsAsync(loginResult.UserId);
        }

        return loginResult.Success;
    }

    private async Task RegisterDeviceForNotificationsAsync(string userId)
    {
        try
        {
            if (!_deviceService.NotificationsSupported)
                return;

            // Check if already permitted
            var permitted = await _deviceService.CheckPermissionsAsync();

            if (permitted && !string.IsNullOrEmpty(_deviceService.Token))
            {
                // Already set up, just re-register
                await _deviceService.RegisterDeviceAsync(userId);
            }
            // Don't auto-request permissions on login - let user initiate
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Auto-registration failed: {ex.Message}");
        }
    }
}
```

## Token Management

### Handling Token Refresh

Set up token refresh handling during app initialization:

```csharp
public class App : Application
{
    private readonly IDeviceInstallationService _deviceService;
    private readonly IAuthenticationService _authService;

    public App(IDeviceInstallationService deviceService, IAuthenticationService authService)
    {
        _deviceService = deviceService;
        _authService = authService;

        InitializeComponent();

        // Subscribe to token refresh
        _deviceService.TokenRefreshed += OnTokenRefreshed;
    }

    private async void OnTokenRefreshed(string newToken)
    {
        Debug.WriteLine($"Token refreshed: {newToken[..Math.Min(8, newToken.Length)]}...");

        // Get current user
        var user = await _authService.GetCurrentUserAsync();

        if (user != null)
        {
            // Re-register with new token
            var success = await _deviceService.RegisterDeviceAsync(user.Id);

            if (success)
            {
                Debug.WriteLine("Re-registered with new token");
            }
            else
            {
                Debug.WriteLine("Failed to re-register with new token");
            }
        }
    }
}
```

### Token Validation

Validate tokens before sending notifications:

```csharp
public class TokenValidator
{
    public static bool IsValidApnsToken(string token)
    {
        // APNS tokens are 64 hex characters
        return !string.IsNullOrEmpty(token) &&
               token.Length == 64 &&
               token.All(c => char.IsDigit(c) || (c >= 'A' && c <= 'F'));
    }

    public static bool IsValidFcmToken(string token)
    {
        // FCM tokens are typically 152+ characters
        return !string.IsNullOrEmpty(token) &&
               token.Length >= 140 &&
               !token.StartsWith("development-") &&
               !token.StartsWith("no-notifications-");
    }

    public static bool IsValidToken(string platform, string token)
    {
        return platform switch
        {
            "apns" => IsValidApnsToken(token),
            "fcmv1" => IsValidFcmToken(token),
            _ => false
        };
    }
}
```

## Tags & Targeting

### Tag System

Tags allow targeted notification delivery to specific user groups or devices.

#### Automatic Tags

The system automatically adds:

```csharp
installation.Tags.Add("platform_ios");              // or "platform_android"
installation.Tags.Add($"device_{deviceId}");        // Unique device tag
installation.Tags.Add("notifications_supported");   // If supported
installation.Tags.Add("has_valid_token");          // If token exists
```

#### User Tags

Add user-specific tags:

```csharp
var installation = DeviceService.GetDeviceInstallation(
    $"user_{userId}",           // User identifier
    $"role_{userRole}",         // User role (admin, user, etc.)
    $"subscription_premium"     // Subscription tier
);
```

#### Custom Tags

Add custom targeting tags:

```csharp
var tags = new List<string>
{
    $"user_{userId}",
    $"language_{userLanguage}",
    $"region_{userRegion}",
    "beta_tester",
    "push_notifications_enabled"
};

var installation = DeviceService.GetDeviceInstallation(tags.ToArray());
```

### Tag-Based Notification Targeting

**Backend Example (Azure Notification Hubs):**

```csharp
// Send to all premium users
await hubClient.SendNotificationAsync(notification, "subscription_premium");

// Send to specific user across all their devices
await hubClient.SendNotificationAsync(notification, $"user_{userId}");

// Send to iOS devices only
await hubClient.SendNotificationAsync(notification, "platform_ios");

// Complex tag expressions
await hubClient.SendNotificationAsync(
    notification,
    "platform_ios && subscription_premium && language_en"
);
```

**Custom Backend Example:**

```csharp
public async Task SendToUserAsync(string userId, NotificationPayload payload)
{
    // Get all devices for user
    var devices = await _database.Devices
        .Where(d => d.Tags.Contains($"user_{userId}"))
        .ToListAsync();

    foreach (var device in devices)
    {
        if (device.Platform == "apns")
        {
            await SendApnsNotificationAsync(device.PushChannel, payload);
        }
        else if (device.Platform == "fcmv1")
        {
            await SendFcmNotificationAsync(device.PushChannel, payload);
        }
    }
}
```

## Backend Integration

### IPushNotificationBackend Implementation

Complete backend implementation example:

```csharp
public class CustomPushBackend : IPushNotificationBackend
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CustomPushBackend> _logger;

    public CustomPushBackend(
        IHttpClientFactory httpClientFactory,
        ILogger<CustomPushBackend> logger)
    {
        _httpClient = httpClientFactory.CreateClient("PushAPI");
        _logger = logger;
    }

    public async Task<bool> RegisterDeviceAsync(DeviceInstallation device)
    {
        try
        {
            _logger.LogInformation(
                "Registering device {DeviceId} on {Platform}",
                device.InstallationId,
                device.Platform
            );

            var response = await _httpClient.PostAsJsonAsync(
                "/api/devices/register",
                device
            );

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Device registered successfully");
                return true;
            }

            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Device registration failed: {Error}", error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Device registration exception");
            return false;
        }
    }

    public async Task<DeviceInfo?> GetDeviceAsync(string deviceId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<DeviceInfo>(
                $"/api/devices/{deviceId}"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get device info");
            return null;
        }
    }

    public async Task<List<DeviceInfo>> GetUserDevicesAsync(string userId)
    {
        try
        {
            var devices = await _httpClient.GetFromJsonAsync<List<DeviceInfo>>(
                $"/api/users/{userId}/devices"
            );
            return devices ?? new List<DeviceInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user devices");
            return new List<DeviceInfo>();
        }
    }

    public async Task<bool> DeactivateDeviceAsync(string deviceId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(
                $"/api/devices/{deviceId}"
            );
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate device");
            return false;
        }
    }

    public async Task<SendNotificationResult> SendNotificationAsync(NotificationPayload payload)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/api/notifications/send",
                payload
            );

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SendNotificationResult>();
                return result ?? new SendNotificationResult { Success = true };
            }

            return new SendNotificationResult
            {
                Success = false,
                ErrorMessage = $"HTTP {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification");
            return new SendNotificationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<SendNotificationResult> SendTestNotificationAsync(string deviceId)
    {
        var testPayload = new NotificationPayload
        {
            Title = "Test Notification",
            Body = $"Test sent at {DateTime.Now:HH:mm:ss}",
            Data = new Dictionary<string, string>
            {
                { "test", "true" },
                { "timestamp", DateTime.UtcNow.ToString("o") }
            },
            TargetDeviceIds = new List<string> { deviceId }
        };

        return await SendNotificationAsync(testPayload);
    }
}
```

### Backend API Endpoints

Example ASP.NET Core API:

```csharp
[ApiController]
[Route("api/devices")]
public class DevicesController : ControllerBase
{
    private readonly IDeviceRepository _deviceRepo;
    private readonly INotificationHub _notificationHub;

    [HttpPost("register")]
    public async Task<IActionResult> RegisterDevice([FromBody] DeviceInstallation device)
    {
        // Validate device
        if (string.IsNullOrEmpty(device.PushChannel))
            return BadRequest("Push channel required");

        // Save or update device
        var existingDevice = await _deviceRepo.GetByIdAsync(device.InstallationId);

        if (existingDevice != null)
        {
            // Update existing
            existingDevice.PushChannel = device.PushChannel;
            existingDevice.Platform = device.Platform;
            existingDevice.Tags = device.Tags;
            existingDevice.UpdatedAt = DateTime.UtcNow;

            await _deviceRepo.UpdateAsync(existingDevice);
        }
        else
        {
            // Create new
            var newDevice = new DeviceRecord
            {
                InstallationId = device.InstallationId,
                Platform = device.Platform,
                PushChannel = device.PushChannel,
                Tags = device.Tags,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _deviceRepo.CreateAsync(newDevice);
        }

        // Register with notification hub (Azure NH, FCM Admin, etc.)
        await _notificationHub.CreateOrUpdateInstallationAsync(device);

        return Ok();
    }

    [HttpGet("{deviceId}")]
    public async Task<ActionResult<DeviceInfo>> GetDevice(string deviceId)
    {
        var device = await _deviceRepo.GetByIdAsync(deviceId);

        if (device == null)
            return NotFound();

        return Ok(device.ToDeviceInfo());
    }

    [HttpDelete("{deviceId}")]
    public async Task<IActionResult> DeactivateDevice(string deviceId)
    {
        await _deviceRepo.DeactivateAsync(deviceId);
        await _notificationHub.DeleteInstallationAsync(deviceId);

        return NoContent();
    }
}
```

## Best Practices

### 1. Always Check Support Before Registering

```csharp
if (!DeviceService.NotificationsSupported)
{
    // Don't attempt registration
    return;
}
```

### 2. Handle Token Refresh Throughout App Lifecycle

```csharp
// Subscribe during initialization
DeviceService.TokenRefreshed += async (token) =>
{
    await ReregisterDeviceAsync();
};
```

### 3. Graceful Permission Handling

```csharp
// Check before requesting
var alreadyGranted = await DeviceService.CheckPermissionsAsync();

if (!alreadyGranted)
{
    // Show explanation first
    await ShowPermissionExplanationAsync();

    // Then request
    await DeviceService.RequestPermissionsAsync();
}
```

### 4. Implement Retry Logic

```csharp
public async Task<bool> RegisterWithRetryAsync(string userId, int maxRetries = 3)
{
    for (int attempt = 0; attempt < maxRetries; attempt++)
    {
        var success = await DeviceService.RegisterDeviceAsync(userId);

        if (success)
            return true;

        // Exponential backoff
        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
    }

    return false;
}
```

### 5. Log Registration Status

```csharp
_logger.LogInformation(
    "Device registration - Platform: {Platform}, Token: {Token}, Supported: {Supported}",
    DeviceService.Platform,
    DeviceService.Token?[..8] + "...",
    DeviceService.NotificationsSupported
);
```

### 6. Clean Up on Logout

```csharp
public async Task LogoutAsync()
{
    var deviceId = DeviceService.GetDeviceId();

    // Deactivate device on backend
    await BackendService.DeactivateDeviceAsync(deviceId);

    // Clear local auth state
    await ClearAuthStateAsync();
}
```

### 7. Use Device Fingerprints for Analytics

```csharp
// Track unique devices across reinstalls
var fingerprint = DeviceService.GetDeviceFingerprint();
await _analytics.TrackDeviceAsync(fingerprint);
```

## Troubleshooting

### iOS Issues

**Token not received:**
```csharp
// Add logging to AppDelegate
public void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
{
    var token = deviceToken.ToHexString();
    Debug.WriteLine($"APNS Token: {token}");

    // Check if service is available
    if (DeviceInstallationService == null)
    {
        Debug.WriteLine("ERROR: DeviceInstallationService is null!");
    }
}
```

**Permission denied:**
```csharp
// Check authorization status
var settings = await UNUserNotificationCenter.Current.GetNotificationSettingsAsync();
Debug.WriteLine($"Auth Status: {settings.AuthorizationStatus}");
```

### Android Issues

**FCM token not received:**
```csharp
// Check Firebase initialization
var (isAvailable, status, app) = FirebaseInitializer.GetStatus();
Debug.WriteLine($"Firebase: {status}");

if (!isAvailable)
{
    Debug.WriteLine("Firebase not properly configured");
}
```

**Google Play Services unavailable:**
```csharp
var androidService = DeviceService as Platforms.Android.DeviceInstallationService;
androidService?.RefreshNotificationSupportStatus();

Debug.WriteLine($"Notifications supported: {DeviceService.NotificationsSupported}");
```

### Common Issues

**Backend registration fails:**
```csharp
// Verify backend is registered
var backend = serviceProvider.GetService<IPushNotificationBackend>();
if (backend == null)
{
    Debug.WriteLine("ERROR: IPushNotificationBackend not registered!");
}
```

**Token changes unexpectedly:**
- This is normal behavior on app reinstall
- Handle via `TokenRefreshed` event
- Ensure backend updates registration

## Next Steps

- [Push Notifications Setup Guide](PushNotifications.md)
- [Local Notifications Documentation](LocalNotifications.md)
- [CheapHelpers.Blazor Documentation](../../CheapHelpers.Blazor/README.md)
