# CheapHelpers.Blazor - Hybrid Features

Comprehensive guide to Blazor Hybrid features including WebView bridge, push notification abstractions, device registration management, and smart permission flow for MAUI, Photino, and Avalonia applications.

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Push Notifications](#push-notifications)
  - [Abstractions](#abstractions)
  - [Device Registration Manager](#device-registration-manager)
  - [Models](#models)
  - [Setup Examples](#setup-examples)
- [WebView Bridge](#webview-bridge)
  - [Data Extraction](#data-extraction)
  - [Storage Monitoring](#storage-monitoring)
  - [Implementation Examples](#implementation-examples)
- [Platform Implementation Guide](#platform-implementation-guide)
- [Complete Integration Examples](#complete-integration-examples)

---

## Overview

CheapHelpers.Blazor provides a comprehensive hybrid abstraction layer that enables Blazor applications to run seamlessly in native containers (MAUI, Photino, Avalonia) with full access to native platform features.

**Key Features:**
- Platform-agnostic push notification abstractions
- WebView data extraction and monitoring
- Smart permission flow (checks backend before requesting)
- Device registration management with automatic cleanup
- Support for multiple notification backends (Azure NH, Firebase, custom)
- JSON parsing utilities for WebView quirks

**Philosophy:**
- Abstractions defined in CheapHelpers.Blazor
- Platform-specific implementations in CheapHelpers.MAUI (or your host project)
- Backend-agnostic design (works with any push notification service)

---

## Architecture

### Layered Design

```
┌─────────────────────────────────────────────────┐
│         Blazor Application (Shared Code)       │
│  - UI Components                                │
│  - Business Logic                               │
│  - Uses Hybrid Abstractions                    │
└─────────────────────────────────────────────────┘
                      ▲
                      │ Uses Interfaces
                      │
┌─────────────────────────────────────────────────┐
│     CheapHelpers.Blazor (Abstractions)         │
│  - IDeviceInstallationService                   │
│  - ILocalNotificationService                    │
│  - IPushNotificationBackend                     │
│  - IWebViewBridge<T>                            │
│  - DeviceRegistrationManager (Core Logic)      │
└─────────────────────────────────────────────────┘
                      ▲
                      │ Implements Interfaces
                      │
┌─────────────────────────────────────────────────┐
│   Platform Projects (Implementations)          │
│  - MAUI: FCM/APNS implementations               │
│  - Photino: WebPush implementation              │
│  - Avalonia: WebPush implementation             │
└─────────────────────────────────────────────────┘
```

**Why This Design?**
- Blazor code stays platform-agnostic
- Easy to add new platforms (just implement interfaces)
- Test without platform dependencies
- Share business logic across all platforms

---

## Push Notifications

### Abstractions

#### IDeviceInstallationService

Platform-specific service for managing device push notification setup and registration.

```csharp
public interface IDeviceInstallationService
{
    // Platform identification
    string Platform { get; }              // "apns", "fcm", "webpush"
    string? Token { get; }                // Current push token
    bool NotificationsSupported { get; }  // Platform supports notifications
    bool IsRegistered { get; }            // Device registered with backend

    // Device identification
    string GetDeviceId();                 // Unique device ID
    string GetDeviceFingerprint();        // Persistent device fingerprint

    // Registration
    Task<bool> RegisterDeviceAsync(string userId);
    DeviceInstallation GetDeviceInstallation(params string[] tags);

    // Permissions
    Task<bool> RequestPermissionsAsync(); // Request from user
    Task<bool> CheckPermissionsAsync();   // Check without requesting

    // Events
    event Action<string>? TokenRefreshed; // Token changed/refreshed
}
```

**Platform Values:**
- iOS: `"apns"` (Apple Push Notification Service)
- Android: `"fcm"` (Firebase Cloud Messaging)
- Desktop/Web: `"webpush"` (Web Push Protocol)

---

#### ILocalNotificationService

Service for displaying local notifications when app is in foreground.

```csharp
public interface ILocalNotificationService
{
    Task ShowNotificationAsync(
        string title,
        string body,
        Dictionary<string, string>? data = null);

    Task<bool> IsPermittedAsync();
}
```

**Why Local Notifications?**

Push notifications typically don't display when the app is in the foreground. Local notifications solve this by converting incoming push notifications to local notifications that the user can see.

---

#### IPushNotificationBackend

Abstraction for push notification backend communication (Azure NH, Firebase, custom API).

```csharp
public interface IPushNotificationBackend
{
    // Device management
    Task<bool> RegisterDeviceAsync(DeviceInstallation device);
    Task<DeviceInfo?> GetDeviceAsync(string deviceId);
    Task<List<DeviceInfo>> GetUserDevicesAsync(string userId);
    Task<bool> DeactivateDeviceAsync(string deviceId);

    // Sending notifications
    Task<SendNotificationResult> SendNotificationAsync(NotificationPayload payload);
    Task<SendNotificationResult> SendTestNotificationAsync(string deviceId);
}
```

**Backend Options:**
- Azure Notification Hubs
- Firebase Admin SDK
- OneSignal
- Custom REST API
- Any notification service with device registration

---

### Device Registration Manager

Smart device registration manager that checks backend before requesting permissions.

```csharp
public class DeviceRegistrationManager
{
    // Check current registration status
    Task<DeviceRegistrationState> CheckDeviceStatusAsync(string userId);

    // Determine if permission request is needed
    Task<bool> ShouldRequestPermissionsAsync(string userId);

    // Smart registration (checks backend first)
    Task<bool> RegisterDeviceIfNeededAsync(string userId);

    // Cleanup old/inactive devices
    Task CleanupOldDevicesAsync(string userId);

    // Device identification
    string GetDeviceIdentifier();

    // Permission state tracking
    void StorePermissionState(bool granted);
}
```

**Smart Permission Flow:**

Traditional approach (BAD):
```
App Launch → Request Permission → User Annoyed → Denies
```

Smart approach (GOOD):
```
App Launch → Check Backend → Already Registered? → Skip Permission Request
App Launch → Check Backend → Not Registered → Request Permission
App Launch → Check Backend → Permission Denied Before → Don't Ask Again
```

**States:**

```csharp
public enum DeviceRegistrationState
{
    NotRegistered,      // Device not in backend
    PermissionPending,  // Waiting for user response
    PermissionDenied,   // User denied permissions
    Registered,         // Active and registered
    Failed              // Registration error
}
```

**Usage Example:**

```csharp
@inject DeviceRegistrationManager RegistrationManager
@inject AuthenticationStateProvider AuthState

protected override async Task OnInitializedAsync()
{
    var authState = await AuthState.GetAuthenticationStateAsync();
    var userId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrEmpty(userId))
        return;

    // Check if we should request permissions
    if (await RegistrationManager.ShouldRequestPermissionsAsync(userId))
    {
        // Only ask if not already registered or denied
        var registered = await RegistrationManager.RegisterDeviceIfNeededAsync(userId);

        if (registered)
        {
            // Clean up old devices for this user
            await RegistrationManager.CleanupOldDevicesAsync(userId);
        }
    }
}
```

**Configuration:**

```csharp
public interface IPreferencesService
{
    string Get(string key, string defaultValue);
    void Set(string key, string value);
    void Remove(string key);
}
```

Implement this for your platform:
- MAUI: Use `Microsoft.Maui.Storage.Preferences`
- Photino/Avalonia: Use local storage, registry, or file-based storage

---

### Models

#### DeviceInstallation

Platform-agnostic device installation for backend registration.

```csharp
public class DeviceInstallation
{
    public string InstallationId { get; set; }  // Unique device ID
    public string Platform { get; set; }        // "apns", "fcm", "webpush"
    public string PushChannel { get; set; }     // Platform token/endpoint
    public List<string> Tags { get; set; }      // For targeted delivery
}
```

**Tags Examples:**
```csharp
// User-based targeting
Tags = new List<string> { "user_123", "premium" }

// Group targeting
Tags = new List<string> { "department_sales", "region_west" }

// Topic targeting
Tags = new List<string> { "topic_news", "topic_updates" }
```

---

#### DeviceInfo

Device information from backend.

```csharp
public class DeviceInfo
{
    public string DeviceId { get; set; }
    public string Platform { get; set; }
    public string PushToken { get; set; }
    public string? UserId { get; set; }
    public bool IsActive { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime LastUpdated { get; set; }
    public List<string> Tags { get; set; }
}
```

---

#### NotificationPayload

Payload for sending notifications.

```csharp
public class NotificationPayload
{
    public string Title { get; set; }
    public string Body { get; set; }
    public List<string>? DeviceIds { get; set; }    // Specific devices
    public List<string>? Tags { get; set; }         // Tag-based targeting
    public Dictionary<string, string>? Data { get; } // Custom data
    public bool Silent { get; set; }                 // Silent/data-only
}
```

**Targeting Examples:**

```csharp
// Send to specific devices
var payload = new NotificationPayload
{
    Title = "Order Update",
    Body = "Your order has shipped!",
    DeviceIds = new List<string> { "device_123", "device_456" }
};

// Send to all users with tag
var payload = new NotificationPayload
{
    Title = "New Feature",
    Body = "Check out our new dashboard!",
    Tags = new List<string> { "premium" }
};

// Silent notification with data
var payload = new NotificationPayload
{
    Title = "",
    Body = "",
    Silent = true,
    Data = new Dictionary<string, string>
    {
        { "action", "refresh_data" },
        { "entity_id", "123" }
    }
};
```

---

### Setup Examples

#### MAUI Setup

**MauiProgram.cs:**

```csharp
using CheapHelpers.Blazor.Hybrid.Extensions;
using CheapHelpers.MAUI.Services; // Your platform implementations

var builder = MauiApp.CreateBuilder();

builder
    .UseMauiApp<App>()
    .ConfigureFonts(fonts =>
    {
        fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
    });

// Add Blazor Hybrid services
builder.Services.AddMauiBlazorWebView();

// Register platform-specific implementations
builder.Services.AddSingleton<IDeviceInstallationService, MauiDeviceInstallationService>();
builder.Services.AddSingleton<ILocalNotificationService, MauiLocalNotificationService>();
builder.Services.AddSingleton<IPreferencesService, MauiPreferencesService>();

// Register backend (example: custom API)
builder.Services.AddSingleton<IPushNotificationBackend, CustomPushNotificationBackend>();

// Add push notification services with configuration
builder.Services.AddBlazorHybridPushNotifications(options =>
{
    options.SmartPermissionFlow = true;
    options.ForegroundNotifications = true;
    options.UseCustomBackend<CustomPushNotificationBackend>();
});

return builder.Build();
```

**Platform Implementation (Android):**

```csharp
// Platforms/Android/Services/MauiDeviceInstallationService.cs
public class MauiDeviceInstallationService : IDeviceInstallationService
{
    private readonly FirebaseMessaging _firebaseMessaging;
    private string? _token;

    public string Platform => "fcm";
    public string? Token => _token;
    public bool NotificationsSupported => true;
    public bool IsRegistered { get; private set; }

    public MauiDeviceInstallationService()
    {
        _firebaseMessaging = FirebaseMessaging.Instance;

        // Listen for token refresh
        FirebaseMessaging.Instance.Token.AddOnSuccessListener(
            new OnSuccessListener(token =>
            {
                _token = token;
                TokenRefreshed?.Invoke(token);
            }));
    }

    public string GetDeviceId()
    {
        return Android.Provider.Settings.Secure.GetString(
            Android.App.Application.Context.ContentResolver,
            Android.Provider.Settings.Secure.AndroidId);
    }

    public string GetDeviceFingerprint()
    {
        // Combine device identifiers for persistent fingerprint
        var androidId = GetDeviceId();
        var manufacturer = Android.OS.Build.Manufacturer;
        var model = Android.OS.Build.Model;
        return $"{manufacturer}_{model}_{androidId}";
    }

    public async Task<bool> RequestPermissionsAsync()
    {
        // Android 13+ requires runtime permission
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
        {
            var status = await Permissions.RequestAsync<NotificationPermission>();
            return status == PermissionStatus.Granted;
        }

        return true; // No permission needed on older Android
    }

    public async Task<bool> CheckPermissionsAsync()
    {
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
        {
            var status = await Permissions.CheckStatusAsync<NotificationPermission>();
            return status == PermissionStatus.Granted;
        }

        return true;
    }

    public async Task<bool> RegisterDeviceAsync(string userId)
    {
        try
        {
            // Request permission first
            var hasPermission = await RequestPermissionsAsync();
            if (!hasPermission)
                return false;

            // Get FCM token
            _token = await _firebaseMessaging.GetToken();

            if (string.IsNullOrEmpty(_token))
                return false;

            IsRegistered = true;
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Registration failed: {ex.Message}");
            return false;
        }
    }

    public DeviceInstallation GetDeviceInstallation(params string[] tags)
    {
        return new DeviceInstallation
        {
            InstallationId = GetDeviceId(),
            Platform = Platform,
            PushChannel = Token ?? string.Empty,
            Tags = tags.ToList()
        };
    }

    public event Action<string>? TokenRefreshed;
}

// Helper for Android 13+ notification permission
public class NotificationPermission : Permissions.BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
        new[] { (Android.Manifest.Permission.PostNotifications, true) };
}
```

**Platform Implementation (iOS):**

```csharp
// Platforms/iOS/Services/MauiDeviceInstallationService.cs
public class MauiDeviceInstallationService : IDeviceInstallationService
{
    private string? _token;

    public string Platform => "apns";
    public string? Token => _token;
    public bool NotificationsSupported => true;
    public bool IsRegistered { get; private set; }

    public string GetDeviceId()
    {
        return UIDevice.CurrentDevice.IdentifierForVendor.AsString();
    }

    public string GetDeviceFingerprint()
    {
        return GetDeviceId(); // iOS vendor ID is stable
    }

    public async Task<bool> RequestPermissionsAsync()
    {
        var center = UNUserNotificationCenter.Current;

        var (granted, error) = await center.RequestAuthorizationAsync(
            UNAuthorizationOptions.Alert |
            UNAuthorizationOptions.Badge |
            UNAuthorizationOptions.Sound);

        if (granted)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                UIApplication.SharedApplication.RegisterForRemoteNotifications();
            });
        }

        return granted;
    }

    public async Task<bool> CheckPermissionsAsync()
    {
        var center = UNUserNotificationCenter.Current;
        var settings = await center.GetNotificationSettingsAsync();

        return settings.AuthorizationStatus == UNAuthorizationStatus.Authorized;
    }

    public async Task<bool> RegisterDeviceAsync(string userId)
    {
        var hasPermission = await RequestPermissionsAsync();
        if (!hasPermission)
            return false;

        // Token is set via AppDelegate when Apple calls back
        // See AppDelegate.cs implementation below

        IsRegistered = !string.IsNullOrEmpty(_token);
        return IsRegistered;
    }

    public void SetToken(string token)
    {
        _token = token;
        IsRegistered = true;
        TokenRefreshed?.Invoke(token);
    }

    public DeviceInstallation GetDeviceInstallation(params string[] tags)
    {
        return new DeviceInstallation
        {
            InstallationId = GetDeviceId(),
            Platform = Platform,
            PushChannel = Token ?? string.Empty,
            Tags = tags.ToList()
        };
    }

    public event Action<string>? TokenRefreshed;
}
```

**AppDelegate.cs (iOS):**

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
        // Convert token to string
        var token = deviceToken.Description
            .Trim('<', '>')
            .Replace(" ", string.Empty);

        // Set token in service
        var service = IPlatformApplication.Current.Services
            .GetService<IDeviceInstallationService>() as MauiDeviceInstallationService;

        service?.SetToken(token);
    }

    [Export("application:didFailToRegisterForRemoteNotificationsWithError:")]
    public void FailedToRegisterForRemoteNotifications(
        UIApplication application,
        NSError error)
    {
        Debug.WriteLine($"Failed to register for remote notifications: {error}");
    }
}
```

**Local Notification Service (Cross-Platform):**

```csharp
public class MauiLocalNotificationService : ILocalNotificationService
{
    public async Task ShowNotificationAsync(
        string title,
        string body,
        Dictionary<string, string>? data = null)
    {
        try
        {
            var request = new NotificationRequest
            {
                NotificationId = Random.Shared.Next(),
                Title = title,
                Description = body,
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = DateTime.Now
                }
            };

            await LocalNotificationCenter.Current.Show(request);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to show local notification: {ex.Message}");
        }
    }

    public async Task<bool> IsPermittedAsync()
    {
        return await LocalNotificationCenter.Current.AreNotificationsEnabled();
    }
}
```

---

#### Photino Setup

**Program.cs:**

```csharp
using CheapHelpers.Blazor.Hybrid.Extensions;
using Photino.Blazor;

var builder = PhotinoBlazorAppBuilder.CreateDefault(args);

// Register platform services
builder.Services.AddSingleton<IDeviceInstallationService, PhotinoDeviceInstallationService>();
builder.Services.AddSingleton<ILocalNotificationService, PhotinoLocalNotificationService>();
builder.Services.AddSingleton<IPreferencesService, PhotinoPreferencesService>();
builder.Services.AddSingleton<IPushNotificationBackend, CustomPushNotificationBackend>();

// Add push notification services
builder.Services.AddBlazorHybridPushNotifications(options =>
{
    options.SmartPermissionFlow = true;
    options.ForegroundNotifications = true;
});

var app = builder.Build();

app.MainWindow
    .SetTitle("My Photino App")
    .SetSize(1200, 800)
    .SetUseOsDefaultLocation(false)
    .SetLeft(100)
    .SetTop(100);

AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
{
    app.MainWindow.ShowMessage("Fatal exception", error.ExceptionObject.ToString());
};

app.Run();
```

**Platform Implementation:**

```csharp
public class PhotinoDeviceInstallationService : IDeviceInstallationService
{
    private string? _subscription;

    public string Platform => "webpush";
    public string? Token => _subscription;
    public bool NotificationsSupported => true;
    public bool IsRegistered { get; private set; }

    public string GetDeviceId()
    {
        // Use machine name + user combination
        return $"{Environment.MachineName}_{Environment.UserName}";
    }

    public string GetDeviceFingerprint()
    {
        // Create stable fingerprint from hardware
        var machineName = Environment.MachineName;
        var userName = Environment.UserName;
        var osVersion = Environment.OSVersion.Version.ToString();

        return $"{machineName}_{userName}_{osVersion}";
    }

    public async Task<bool> RequestPermissionsAsync()
    {
        // Desktop doesn't need permission request
        // Could show a dialog asking user if they want notifications
        return true;
    }

    public async Task<bool> CheckPermissionsAsync()
    {
        return true; // Desktop always has permission
    }

    public async Task<bool> RegisterDeviceAsync(string userId)
    {
        try
        {
            // For desktop, you might use Web Push API or a custom solution
            // This is a simplified example
            _subscription = $"webpush_{GetDeviceId()}";
            IsRegistered = true;
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Registration failed: {ex.Message}");
            return false;
        }
    }

    public DeviceInstallation GetDeviceInstallation(params string[] tags)
    {
        return new DeviceInstallation
        {
            InstallationId = GetDeviceId(),
            Platform = Platform,
            PushChannel = Token ?? string.Empty,
            Tags = tags.ToList()
        };
    }

    public event Action<string>? TokenRefreshed;
}
```

---

## WebView Bridge

Extract data from JavaScript storage (localStorage, sessionStorage, cookies) in WebView-hosted Blazor apps.

### IWebViewBridge<TData>

```csharp
public interface IWebViewBridge<TData> where TData : class
{
    // Extract specific data
    Task<TData?> ExtractDataAsync(string key);

    // Get all storage
    Task<Dictionary<string, string>> GetAllStorageAsync(StorageType storageType);

    // Monitor for changes
    Task StartMonitoringAsync(TimeSpan pollingInterval);
    void StopMonitoring();

    // Change notification
    event Action<TData?>? DataChanged;
}

public enum StorageType
{
    LocalStorage,
    SessionStorage,
    Cookies
}
```

---

### Data Extraction

**WebViewJsonParser** - Handles WebView's quirky JSON escaping:

```csharp
public static class WebViewJsonParser
{
    // Parse with automatic escaping handling
    T? ParseJson<T>(string? jsonData);

    // Parse to JsonElement
    JsonElement? ParseJsonElement(string? jsonData);

    // Unescape multiple levels
    string UnescapeWebViewJson(string jsonData);

    // Extract specific properties
    string? ExtractStringProperty(string? jsonData, string propertyName);
    Dictionary<string, string> ExtractProperties(string? jsonData, params string[] propertyNames);
    DateTime? ExtractDateTimeProperty(string? jsonData, string propertyName);
    bool? ExtractBooleanProperty(string? jsonData, string propertyName);

    // Deep unescape (for heavily nested JSON)
    string DeepUnescape(string jsonData, int maxLevels = 5);
}
```

**Why WebViewJsonParser?**

WebView has quirks with JSON escaping:
- Multiple levels of escaping (`\\"` becomes `\\\\"`)
- Outer quotes wrapping JSON strings
- Platform-specific escaping differences

WebViewJsonParser abstracts these issues away.

---

### Storage Monitoring

Monitor localStorage/sessionStorage for changes:

```csharp
public class AuthTokenData
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
}

// Create bridge
var bridge = new WebViewStorageBridge<AuthTokenData>(webView);

// Monitor for changes
bridge.DataChanged += (newData) =>
{
    if (newData != null)
    {
        Debug.WriteLine($"Token updated: {newData.AccessToken}");
        // Update your app's authentication state
    }
};

await bridge.StartMonitoringAsync(TimeSpan.FromSeconds(5));
```

---

### Implementation Examples

#### MAUI WebView Bridge

```csharp
using Microsoft.Maui.Controls;

public class MauiWebViewBridge<TData> : IWebViewBridge<TData> where TData : class
{
    private readonly WebView _webView;
    private Timer? _monitoringTimer;

    public event Action<TData?>? DataChanged;

    public MauiWebViewBridge(WebView webView)
    {
        _webView = webView;
    }

    public async Task<TData?> ExtractDataAsync(string key)
    {
        var script = $"localStorage.getItem('{key}')";
        var result = await _webView.EvaluateJavaScriptAsync(script);

        return WebViewJsonParser.ParseJson<TData>(result?.ToString());
    }

    public async Task<Dictionary<string, string>> GetAllStorageAsync(StorageType storageType)
    {
        var script = storageType switch
        {
            StorageType.LocalStorage => "JSON.stringify(localStorage)",
            StorageType.SessionStorage => "JSON.stringify(sessionStorage)",
            StorageType.Cookies => "document.cookie",
            _ => throw new ArgumentException("Invalid storage type")
        };

        var result = await _webView.EvaluateJavaScriptAsync(script);
        var jsonData = result?.ToString() ?? "{}";

        var parsed = WebViewJsonParser.ParseJson<Dictionary<string, string>>(jsonData);
        return parsed ?? new Dictionary<string, string>();
    }

    public Task StartMonitoringAsync(TimeSpan pollingInterval)
    {
        _monitoringTimer = new Timer(
            async _ => await CheckForChangesAsync(),
            null,
            TimeSpan.Zero,
            pollingInterval);

        return Task.CompletedTask;
    }

    public void StopMonitoring()
    {
        _monitoringTimer?.Dispose();
        _monitoringTimer = null;
    }

    private TData? _lastData;

    private async Task CheckForChangesAsync()
    {
        try
        {
            // Check localStorage for specific key or scan all
            var allData = await GetAllStorageAsync(StorageType.LocalStorage);

            // Parse and compare
            // This is simplified - implement your own comparison logic
            var currentData = WebViewJsonParser.ParseJson<TData>(
                allData.FirstOrDefault().Value);

            if (!Equals(currentData, _lastData))
            {
                _lastData = currentData;
                DataChanged?.Invoke(currentData);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Monitoring error: {ex.Message}");
        }
    }
}
```

**Usage in MAUI:**

```razor
@inject IWebViewBridge<AuthTokenData> WebViewBridge

<WebView x:Name="myWebView"
         Source="https://myapp.com/login"
         Navigated="OnNavigated" />

@code {
    protected override async Task OnInitializedAsync()
    {
        // Start monitoring after WebView loads
        WebViewBridge.DataChanged += HandleTokenChange;
        await WebViewBridge.StartMonitoringAsync(TimeSpan.FromSeconds(3));
    }

    private void HandleTokenChange(AuthTokenData? data)
    {
        if (data != null)
        {
            // Store token in secure storage
            SecureStorage.SetAsync("access_token", data.AccessToken);

            // Navigate to app
            NavigationManager.NavigateTo("/dashboard");
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            WebViewBridge.StopMonitoring();
            WebViewBridge.DataChanged -= HandleTokenChange;
        }

        base.Dispose(disposing);
    }
}
```

---

## Platform Implementation Guide

### Creating Platform Implementation

1. **Define Your Models** (in shared Blazor project):

```csharp
public class MyAppData
{
    public string Token { get; set; }
    public string UserId { get; set; }
}
```

2. **Implement IDeviceInstallationService** (in platform project):

```csharp
public class MyPlatformDeviceService : IDeviceInstallationService
{
    // Implement all interface members
    // See MAUI examples above
}
```

3. **Implement ILocalNotificationService** (in platform project):

```csharp
public class MyPlatformNotificationService : ILocalNotificationService
{
    // Show platform-specific notifications
}
```

4. **Implement IPushNotificationBackend** (backend/API project):

```csharp
public class MyCustomBackend : IPushNotificationBackend
{
    private readonly HttpClient _http;
    private readonly string _apiUrl;

    public async Task<bool> RegisterDeviceAsync(DeviceInstallation device)
    {
        var response = await _http.PostAsJsonAsync($"{_apiUrl}/devices", device);
        return response.IsSuccessStatusCode;
    }

    // Implement other methods
}
```

5. **Register Services**:

```csharp
builder.Services.AddSingleton<IDeviceInstallationService, MyPlatformDeviceService>();
builder.Services.AddSingleton<ILocalNotificationService, MyPlatformNotificationService>();
builder.Services.AddSingleton<IPushNotificationBackend, MyCustomBackend>();
builder.Services.AddBlazorHybridPushNotifications();
```

---

## Complete Integration Examples

### End-to-End MAUI Example

**1. Create Backend API**

```csharp
// Controllers/NotificationsController.cs
[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly IDeviceRepository _deviceRepo;
    private readonly INotificationService _notificationService;

    [HttpPost("register")]
    public async Task<IActionResult> RegisterDevice([FromBody] DeviceInstallation device)
    {
        await _deviceRepo.UpsertDeviceAsync(device);
        return Ok();
    }

    [HttpGet("device/{deviceId}")]
    public async Task<ActionResult<DeviceInfo>> GetDevice(string deviceId)
    {
        var device = await _deviceRepo.GetDeviceAsync(deviceId);
        return device != null ? Ok(device) : NotFound();
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendNotification([FromBody] NotificationPayload payload)
    {
        var result = await _notificationService.SendNotificationAsync(payload);
        return Ok(result);
    }
}
```

**2. Implement Backend Service**

```csharp
public class CustomPushNotificationBackend : IPushNotificationBackend
{
    private readonly HttpClient _http;

    public CustomPushNotificationBackend(HttpClient http)
    {
        _http = http;
        _http.BaseAddress = new Uri("https://api.myapp.com/");
    }

    public async Task<bool> RegisterDeviceAsync(DeviceInstallation device)
    {
        var response = await _http.PostAsJsonAsync("api/notifications/register", device);
        return response.IsSuccessStatusCode;
    }

    public async Task<DeviceInfo?> GetDeviceAsync(string deviceId)
    {
        var response = await _http.GetAsync($"api/notifications/device/{deviceId}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<DeviceInfo>();
    }

    public async Task<List<DeviceInfo>> GetUserDevicesAsync(string userId)
    {
        var response = await _http.GetAsync($"api/notifications/user/{userId}/devices");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<DeviceInfo>>() ?? new();
    }

    public async Task<bool> DeactivateDeviceAsync(string deviceId)
    {
        var response = await _http.DeleteAsync($"api/notifications/device/{deviceId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<SendNotificationResult> SendNotificationAsync(NotificationPayload payload)
    {
        var response = await _http.PostAsJsonAsync("api/notifications/send", payload);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SendNotificationResult>()
            ?? new SendNotificationResult { Success = false };
    }

    public async Task<SendNotificationResult> SendTestNotificationAsync(string deviceId)
    {
        var payload = new NotificationPayload
        {
            Title = "Test Notification",
            Body = "This is a test notification",
            DeviceIds = new List<string> { deviceId }
        };

        return await SendNotificationAsync(payload);
    }
}
```

**3. Configure MAUI App**

```csharp
// MauiProgram.cs
var builder = MauiApp.CreateBuilder();

builder
    .UseMauiApp<App>()
    .ConfigureFonts(fonts =>
    {
        fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
    });

builder.Services.AddMauiBlazorWebView();

// HTTP client for backend
builder.Services.AddHttpClient<IPushNotificationBackend, CustomPushNotificationBackend>();

// Platform services
builder.Services.AddSingleton<IDeviceInstallationService, MauiDeviceInstallationService>();
builder.Services.AddSingleton<ILocalNotificationService, MauiLocalNotificationService>();
builder.Services.AddSingleton<IPreferencesService, MauiPreferencesService>();

// Hybrid services
builder.Services.AddBlazorHybridPushNotifications(options =>
{
    options.SmartPermissionFlow = true;
    options.ForegroundNotifications = true;
});

return builder.Build();
```

**4. Use in Blazor Component**

```razor
@page "/notifications"
@inject DeviceRegistrationManager RegistrationManager
@inject IPushNotificationBackend Backend
@inject AuthenticationStateProvider AuthState

<MudText Typo="Typo.h4">Notification Settings</MudText>

@if (_registrationState == DeviceRegistrationState.Registered)
{
    <MudAlert Severity="Severity.Success">
        Notifications are enabled for this device.
    </MudAlert>

    <MudButton Color="Color.Primary"
               Variant="Variant.Filled"
               OnClick="SendTestNotification">
        Send Test Notification
    </MudButton>
}
else if (_registrationState == DeviceRegistrationState.PermissionDenied)
{
    <MudAlert Severity="Severity.Warning">
        Notification permissions were denied. Please enable them in settings.
    </MudAlert>
}
else
{
    <MudButton Color="Color.Primary"
               Variant="Variant.Filled"
               OnClick="EnableNotifications">
        Enable Notifications
    </MudButton>
}

@code {
    private DeviceRegistrationState _registrationState;
    private string? _userId;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthState.GetAuthenticationStateAsync();
        _userId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(_userId))
        {
            _registrationState = await RegistrationManager.CheckDeviceStatusAsync(_userId);
        }
    }

    private async Task EnableNotifications()
    {
        if (string.IsNullOrEmpty(_userId))
            return;

        var success = await RegistrationManager.RegisterDeviceIfNeededAsync(_userId);

        if (success)
        {
            _registrationState = DeviceRegistrationState.Registered;
            await RegistrationManager.CleanupOldDevicesAsync(_userId);
        }
        else
        {
            _registrationState = DeviceRegistrationState.Failed;
        }

        StateHasChanged();
    }

    private async Task SendTestNotification()
    {
        var deviceId = RegistrationManager.GetDeviceIdentifier();
        await Backend.SendTestNotificationAsync(deviceId);
    }
}
```

---

## See Also

- [Components](../Components.md) - Blazor UI components and base classes
- [Download Helper](../DownloadHelper.md) - Client-side file downloads
- [Clipboard Service](../ClipboardService.md) - Async clipboard operations
