# Local Notifications - CheapHelpers.MAUI

Comprehensive guide for displaying local notifications in the foreground when your MAUI app is active.

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [ILocalNotificationService Interface](#ilocalnotificationservice-interface)
4. [Platform Implementations](#platform-implementations)
5. [Usage Examples](#usage-examples)
6. [Foreground Notification Flow](#foreground-notification-flow)
7. [Custom Data Handling](#custom-data-handling)
8. [Android Notification Channels](#android-notification-channels)
9. [Best Practices](#best-practices)
10. [Troubleshooting](#troubleshooting)

## Overview

Local notifications allow your app to display notifications to users even when the app is in the foreground. The CheapHelpers.MAUI package automatically converts push notifications received while the app is active into local notifications for immediate display.

### Why Local Notifications?

**Background Notifications:**
- iOS and Android automatically display push notifications when app is backgrounded or closed
- No additional code needed for background display

**Foreground Problem:**
- By default, push notifications are NOT displayed when app is in foreground
- Users miss important updates while actively using the app

**Solution:**
- Local notification service converts push notifications to local notifications
- Provides consistent notification experience regardless of app state

### Key Features

- **Automatic Conversion**: Push notifications automatically shown as local when app is active
- **Custom Data**: Support for custom data payloads
- **Permission Management**: Unified permission handling
- **Platform-Specific**: Native iOS and Android implementations
- **Blazor Integration**: Works seamlessly with Blazor Hybrid apps

## Architecture

```
┌────────────────────────────────────────────────────┐
│  Push Notification Received (from APNS/FCM)        │
└────────────────────────┬───────────────────────────┘
                         │
            ┌────────────▼────────────┐
            │  App State Check        │
            └────────────┬────────────┘
                         │
        ┌────────────────┴────────────────┐
        │                                 │
   ┌────▼─────┐                   ┌──────▼────────┐
   │Background│                   │  Foreground   │
   │/Inactive │                   │    Active     │
   └────┬─────┘                   └──────┬────────┘
        │                                │
        │ OS displays                    │ Convert to local
        │ automatically                  │ notification
        │                                │
        ▼                         ┌──────▼──────────────────┐
   User sees                      │ ILocalNotificationService│
   notification                   │ ShowNotificationAsync()  │
                                  └──────┬──────────────────┘
                                         │
                                  ┌──────▼──────────────────┐
                                  │ Platform Implementation │
                                  ├─────────────────────────┤
                                  │ iOS: UNNotificationCenter│
                                  │ Android: NotificationCompat│
                                  └──────┬──────────────────┘
                                         │
                                         ▼
                                  User sees notification
```

## ILocalNotificationService Interface

The core abstraction defined in CheapHelpers.Blazor.Hybrid:

```csharp
public interface ILocalNotificationService
{
    /// <summary>
    /// Display a local notification with title and body
    /// </summary>
    /// <param name="title">Notification title</param>
    /// <param name="body">Notification body text</param>
    /// <param name="data">Optional custom data dictionary for handling notification clicks</param>
    Task ShowNotificationAsync(string title, string body, Dictionary<string, string>? data = null);

    /// <summary>
    /// Check if local notifications are supported and permitted on this device
    /// </summary>
    Task<bool> IsPermittedAsync();
}
```

### ShowNotificationAsync

Displays a local notification immediately:

```csharp
await localNotificationService.ShowNotificationAsync(
    title: "New Message",
    body: "You have a new message from John",
    data: new Dictionary<string, string>
    {
        { "messageId", "12345" },
        { "senderId", "user-789" },
        { "action", "open_chat" }
    }
);
```

**Parameters:**
- `title`: Notification title (shown prominently)
- `body`: Notification body text (main content)
- `data`: Optional dictionary of custom key-value pairs for handling taps

**Behavior:**
- **iOS**: Creates `UNNotificationRequest` with immediate delivery
- **Android**: Creates `NotificationCompat` notification in specified channel
- Both platforms: Shown with sound, badge, and banner (if permitted)

### IsPermittedAsync

Checks if notifications are permitted:

```csharp
var permitted = await localNotificationService.IsPermittedAsync();

if (!permitted)
{
    // Guide user to enable notifications
}
```

**Returns:**
- `true`: Notifications are enabled and will be displayed
- `false`: Notifications are disabled or permissions not granted

## Platform Implementations

### iOS Implementation

File: `CheapHelpers.MAUI/Platforms/iOS/LocalNotificationService.cs`

#### How It Works

Uses the iOS `UserNotifications` framework:

```csharp
public async Task ShowNotificationAsync(string title, string body, Dictionary<string, string>? data = null)
{
    // Check permissions
    if (!await IsPermittedAsync())
        return;

    // Create notification content
    var content = new UNMutableNotificationContent
    {
        Title = title,
        Body = body,
        Sound = UNNotificationSound.Default
    };

    // Add custom data
    if (data != null && data.Any())
    {
        var userInfo = new NSMutableDictionary();
        foreach (var kvp in data)
        {
            userInfo[new NSString(kvp.Key)] = new NSString(kvp.Value);
        }
        content.UserInfo = userInfo;
    }

    // Create request with unique identifier - NO TRIGGER for immediate display
    var requestId = $"local_notification_{DateTime.Now.Ticks}";
    var request = UNNotificationRequest.FromIdentifier(requestId, content, null);

    // Add to notification center
    await UNUserNotificationCenter.Current.AddNotificationRequestAsync(request);
}
```

**Key Points:**
- `null` trigger means immediate delivery
- Unique request ID prevents duplicates
- Sound is set to default system sound
- Custom data stored in `UserInfo` dictionary

#### Foreground Display

The `ApnsDelegate` handles foreground presentation:

```csharp
[Export("userNotificationCenter:willPresentNotification:withCompletionHandler:")]
public void WillPresentNotification(
    UNUserNotificationCenter center,
    UNNotification notification,
    Action<UNNotificationPresentationOptions> completionHandler)
{
    var isPushNotification = notification.Request.Trigger is UNPushNotificationTrigger;

    if (isPushNotification)
    {
        // Push notification - convert to local
        var title = notification.Request.Content.Title;
        var body = notification.Request.Content.Body;
        var data = ExtractNotificationData(notification.Request.Content.UserInfo);

        await LocalNotificationService.ShowNotificationAsync(title, body, data);

        // Don't show push notification since we're converting to local
        completionHandler(UNNotificationPresentationOptions.None);
    }
    else
    {
        // Already a local notification - show with banner and sound
        if (UIDevice.CurrentDevice.CheckSystemVersion(14, 0))
        {
            completionHandler(
                UNNotificationPresentationOptions.Banner |
                UNNotificationPresentationOptions.Sound |
                UNNotificationPresentationOptions.List
            );
        }
        else
        {
            completionHandler(
                UNNotificationPresentationOptions.Alert |
                UNNotificationPresentationOptions.Sound |
                UNNotificationPresentationOptions.Badge
            );
        }
    }
}
```

**iOS 14+ Presentation Options:**
- `Banner`: Shows banner at top of screen
- `Sound`: Plays notification sound
- `List`: Shows in notification center
- `Badge`: Updates app badge count

**iOS 13 and Earlier:**
- `Alert`: Shows alert (equivalent to Banner)
- `Sound`: Plays notification sound
- `Badge`: Updates app badge count

#### Permission Check

```csharp
public async Task<bool> IsPermittedAsync()
{
    var tcs = new TaskCompletionSource<bool>();

    UNUserNotificationCenter.Current.GetNotificationSettings(settings =>
    {
        var isEnabled = settings.AuthorizationStatus == UNAuthorizationStatus.Authorized;
        tcs.SetResult(isEnabled);
    });

    return await tcs.Task;
}
```

### Android Implementation

File: `CheapHelpers.MAUI/Platforms/Android/LocalNotificationService.cs`

#### How It Works

Uses Android `NotificationCompat` and notification channels:

```csharp
public async Task ShowNotificationAsync(string title, string body, Dictionary<string, string>? data = null)
{
    // Check permissions
    if (!await IsPermittedAsync())
        return;

    var activity = Platform.CurrentActivity;

    // Create intent for when notification is tapped
    var intent = new Intent(activity, activity.GetType());
    intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);

    // Add custom data to intent
    if (data != null)
    {
        foreach (var kvp in data)
        {
            intent.PutExtra(kvp.Key, kvp.Value);
        }
    }

    var pendingIntent = PendingIntent.GetActivity(
        activity,
        _notificationId,
        intent,
        PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
    );

    // Get app icon
    var iconResourceId = GetAppIconResourceId(activity);

    // Build notification
    var notification = new NotificationCompat.Builder(activity, _channelId)
        .SetContentTitle(title)
        .SetContentText(body)
        .SetSmallIcon(iconResourceId)
        .SetAutoCancel(true)
        .SetPriority(NotificationCompat.PriorityDefault)
        .SetContentIntent(pendingIntent)
        .SetDefaults(NotificationCompat.DefaultSound | NotificationCompat.DefaultVibrate)
        .Build();

    // Show notification
    var notificationManager = NotificationManagerCompat.From(activity);
    notificationManager.Notify(_notificationId++, notification);
}
```

**Key Points:**
- Each notification gets unique ID (`_notificationId++`)
- `SetAutoCancel(true)`: Removes notification when tapped
- `PendingIntent`: Defines action when user taps notification
- Custom data added to Intent extras

#### Notification Channel Setup

Android 8.0+ requires notification channels:

```csharp
private void CreateNotificationChannel()
{
    if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
    {
        var notificationManager = Platform.CurrentActivity
            ?.GetSystemService(Context.NotificationService) as NotificationManager;

        var channel = new NotificationChannel(
            _channelId,
            _channelName,
            NotificationImportance.Default
        )
        {
            Description = _channelDescription
        };

        channel.EnableLights(true);
        channel.EnableVibration(true);

        notificationManager?.CreateNotificationChannel(channel);
    }
}
```

**Channel Properties:**
- `NotificationImportance.Default`: Normal priority, makes sound
- `EnableLights(true)`: LED notification light
- `EnableVibration(true)`: Vibrate on notification

#### Permission Check

```csharp
public async Task<bool> IsPermittedAsync()
{
    if (Platform.CurrentActivity is not Activity activity)
        return false;

    if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu) // Android 13+
    {
        var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
        return status == PermissionStatus.Granted;
    }

    // For older Android versions, check if notifications are enabled
    var notificationManager = NotificationManagerCompat.From(activity);
    return notificationManager.AreNotificationsEnabled();
}
```

#### Foreground Display

The `FcmService` handles push-to-local conversion:

```csharp
public override void OnMessageReceived(RemoteMessage message)
{
    var title = message.GetNotification()?.Title ?? "Notification";
    var body = message.GetNotification()?.Body ?? "";

    // Extract custom data
    var data = new Dictionary<string, string>();
    if (message.Data != null)
    {
        foreach (var kvp in message.Data)
        {
            data[kvp.Key] = kvp.Value;
        }
    }

    // Show as local notification
    Task.Run(async () =>
    {
        var localNotificationService = IPlatformApplication.Current?.Services
            ?.GetService<ILocalNotificationService>();

        await localNotificationService?.ShowNotificationAsync(title, body, data);
    });
}
```

## Usage Examples

### Basic Notification

```csharp
@inject ILocalNotificationService NotificationService

private async Task ShowBasicNotificationAsync()
{
    await NotificationService.ShowNotificationAsync(
        title: "Hello!",
        body: "This is a basic local notification"
    );
}
```

### Notification with Custom Data

```csharp
private async Task ShowDataNotificationAsync()
{
    var customData = new Dictionary<string, string>
    {
        { "type", "message" },
        { "messageId", "msg-12345" },
        { "senderId", "user-789" },
        { "timestamp", DateTime.UtcNow.ToString("o") }
    };

    await NotificationService.ShowNotificationAsync(
        title: "New Message",
        body: "John sent you a message",
        data: customData
    );
}
```

### Permission-Aware Notification

```csharp
private async Task ShowNotificationSafelyAsync(string title, string body)
{
    // Check permissions first
    var permitted = await NotificationService.IsPermittedAsync();

    if (!permitted)
    {
        // Inform user
        await Shell.Current.DisplayAlert(
            "Notifications Disabled",
            "Please enable notifications to receive updates.",
            "OK"
        );
        return;
    }

    // Show notification
    await NotificationService.ShowNotificationAsync(title, body);
}
```

### Scheduled Reminders

```csharp
public class ReminderService
{
    private readonly ILocalNotificationService _notificationService;

    public async Task ScheduleReminderAsync(string message, TimeSpan delay)
    {
        // Wait for specified delay
        await Task.Delay(delay);

        // Show reminder notification
        await _notificationService.ShowNotificationAsync(
            title: "Reminder",
            body: message,
            data: new Dictionary<string, string>
            {
                { "type", "reminder" },
                { "scheduledAt", DateTime.UtcNow.ToString("o") }
            }
        );
    }
}

// Usage
await reminderService.ScheduleReminderAsync(
    "Time to take a break!",
    TimeSpan.FromMinutes(30)
);
```

### Progress Notifications (Android)

While the basic interface doesn't support progress, you can extend it:

```csharp
// Custom implementation for Android progress notifications
public class ExtendedLocalNotificationService : LocalNotificationService
{
    public void ShowProgressNotification(string title, int progress, int maxProgress)
    {
#if ANDROID
        var activity = Platform.CurrentActivity;

        var notification = new NotificationCompat.Builder(activity, _channelId)
            .SetContentTitle(title)
            .SetContentText($"{progress}/{maxProgress}")
            .SetSmallIcon(GetAppIconResourceId(activity))
            .SetProgress(maxProgress, progress, false)
            .SetOngoing(true) // Can't be dismissed
            .Build();

        var notificationManager = NotificationManagerCompat.From(activity);
        notificationManager.Notify(PROGRESS_NOTIFICATION_ID, notification);
#endif
    }

    public void DismissProgressNotification()
    {
#if ANDROID
        var activity = Platform.CurrentActivity;
        var notificationManager = NotificationManagerCompat.From(activity);
        notificationManager.Cancel(PROGRESS_NOTIFICATION_ID);
#endif
    }
}
```

## Foreground Notification Flow

### Complete Flow Diagram

```
User Device                 Your App                    Push Service
─────────────────────────────────────────────────────────────────────

                           App Running
                           (Foreground)
                                │
    Push sent ─────────────────►│
    (APNS/FCM)                  │
                                │
                          ┌─────▼──────┐
                          │  Platform  │
                          │  Delegate  │
                          └─────┬──────┘
                                │
                    ┌───────────▼──────────┐
                    │ Check App State      │
                    └───────────┬──────────┘
                                │
                           Foreground
                                │
                    ┌───────────▼──────────────┐
                    │ Extract title, body, data│
                    └───────────┬──────────────┘
                                │
                    ┌───────────▼───────────────┐
                    │ ILocalNotificationService │
                    │ ShowNotificationAsync()   │
                    └───────────┬───────────────┘
                                │
                    ┌───────────▼────────────────┐
                    │ Create Local Notification  │
                    │ • iOS: UNNotification      │
                    │ • Android: NotificationCompat
                    └───────────┬────────────────┘
                                │
                                ▼
                          Display to User
                          (Banner + Sound)
```

### iOS Detailed Flow

```csharp
// 1. Push notification arrives while app is foreground
// AppDelegate.WillPresentNotification() is called

public void WillPresentNotification(...)
{
    var content = notification.Request.Content;
    var title = content.Title;
    var body = content.Body;

    // 2. Check if this is a push notification
    bool isPush = notification.Request.Trigger is UNPushNotificationTrigger;

    if (isPush)
    {
        // 3. Extract custom data
        var data = ExtractNotificationData(content.UserInfo);

        // 4. Convert to local notification
        Task.Run(async () =>
        {
            await LocalNotificationService.ShowNotificationAsync(title, body, data);
        });

        // 5. Don't show original push (we're showing local instead)
        completionHandler(UNNotificationPresentationOptions.None);
    }
    else
    {
        // 6. This is already a local notification - show it
        completionHandler(
            UNNotificationPresentationOptions.Banner |
            UNNotificationPresentationOptions.Sound
        );
    }
}
```

### Android Detailed Flow

```csharp
// 1. FCM message arrives while app is foreground
// FcmService.OnMessageReceived() is called

public override void OnMessageReceived(RemoteMessage message)
{
    // 2. Extract notification content
    var title = message.GetNotification()?.Title ?? "Notification";
    var body = message.GetNotification()?.Body ?? "";

    // 3. Extract custom data
    var data = new Dictionary<string, string>();
    if (message.Data != null)
    {
        foreach (var kvp in message.Data)
            data[kvp.Key] = kvp.Value;
    }

    // 4. Convert to local notification
    Task.Run(async () =>
    {
        var localService = GetService<ILocalNotificationService>();
        await localService.ShowNotificationAsync(title, body, data);
    });
}
```

## Custom Data Handling

### Sending Data from Backend

**iOS APNS Payload:**

```json
{
  "aps": {
    "alert": {
      "title": "New Order",
      "body": "Order #12345 has been placed"
    },
    "sound": "default"
  },
  "orderId": "12345",
  "orderStatus": "pending",
  "customerId": "user-789",
  "action": "open_order_details"
}
```

**Android FCM Payload:**

```json
{
  "notification": {
    "title": "New Order",
    "body": "Order #12345 has been placed"
  },
  "data": {
    "orderId": "12345",
    "orderStatus": "pending",
    "customerId": "user-789",
    "action": "open_order_details"
  }
}
```

### Accessing Data in App

**Override notification handler to process custom data:**

```csharp
// iOS - AppDelegate.cs
public class AppDelegate : ApnsDelegate
{
    protected override void OnNotificationReceived(
        string title,
        string body,
        Dictionary<string, string> data)
    {
        // Process custom data
        if (data.TryGetValue("action", out var action))
        {
            switch (action)
            {
                case "open_order_details":
                    if (data.TryGetValue("orderId", out var orderId))
                    {
                        NavigateToOrderDetails(orderId);
                    }
                    break;

                case "open_chat":
                    if (data.TryGetValue("chatId", out var chatId))
                    {
                        NavigateToChat(chatId);
                    }
                    break;
            }
        }

        base.OnNotificationReceived(title, body, data);
    }

    private void NavigateToOrderDetails(string orderId)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Shell.Current.GoToAsync($"orders/{orderId}");
        });
    }
}
```

```csharp
// Android - MyFirebaseMessagingService.cs
public class MyFirebaseMessagingService : FcmService
{
    protected override void OnNotificationReceived(
        string title,
        string body,
        Dictionary<string, string> data)
    {
        // Same processing logic as iOS
        if (data.TryGetValue("action", out var action))
        {
            ProcessAction(action, data);
        }

        base.OnNotificationReceived(title, body, data);
    }
}
```

### Data Structure Best Practices

**1. Use consistent keys across platforms:**

```csharp
public static class NotificationDataKeys
{
    public const string ACTION = "action";
    public const string ENTITY_ID = "entityId";
    public const string ENTITY_TYPE = "entityType";
    public const string TIMESTAMP = "timestamp";
    public const string PRIORITY = "priority";
}
```

**2. Serialize complex objects as JSON:**

```csharp
// Sending
var orderData = new
{
    orderId = "12345",
    items = new[] { "item1", "item2" },
    total = 99.99
};

var data = new Dictionary<string, string>
{
    { "action", "new_order" },
    { "orderData", JsonSerializer.Serialize(orderData) }
};
```

```csharp
// Receiving
if (data.TryGetValue("orderData", out var orderJson))
{
    var order = JsonSerializer.Deserialize<OrderData>(orderJson);
    ProcessOrder(order);
}
```

**3. Include type information for routing:**

```csharp
var data = new Dictionary<string, string>
{
    { "type", "message" },      // Determines handler
    { "subtype", "chat" },      // Determines UI
    { "messageId", "msg123" },  // Entity reference
    { "senderId", "user789" }   // Related entity
};
```

## Android Notification Channels

### Default Channel Configuration

The service creates a default channel automatically:

```csharp
public LocalNotificationService()
    : this(
        channelId: "cheaphelpers_notifications",
        channelName: "App Notifications",
        channelDescription: "Notifications from this app"
    )
{
}
```

### Custom Channel Configuration

Configure custom channel in `MauiProgram.cs`:

```csharp
builder.Services.AddMauiPushNotifications(options =>
{
    options.ConfigureAndroidChannel(
        channelId: "important_updates",
        channelName: "Important Updates",
        channelDescription: "Critical app notifications"
    );
});
```

### Multiple Channels

For apps with different notification types, create multiple service instances:

```csharp
// Register multiple notification services
builder.Services.AddSingleton<ILocalNotificationService>(sp =>
    new LocalNotificationService(
        "default_channel",
        "General Notifications",
        "Standard app notifications"
    )
);

builder.Services.AddSingleton<ILocalNotificationService>(sp =>
    new LocalNotificationService(
        "urgent_channel",
        "Urgent Notifications",
        "Time-sensitive notifications"
    )
);

builder.Services.AddSingleton<ILocalNotificationService>(sp =>
    new LocalNotificationService(
        "messages_channel",
        "Messages",
        "Chat and message notifications"
    )
);
```

Use named services:

```csharp
public class NotificationManager
{
    private readonly IEnumerable<ILocalNotificationService> _notificationServices;

    public NotificationManager(IEnumerable<ILocalNotificationService> notificationServices)
    {
        _notificationServices = notificationServices;
    }

    public async Task ShowUrgentNotificationAsync(string title, string body)
    {
        // Get specific channel service
        var urgentService = _notificationServices.ElementAt(1); // urgent_channel
        await urgentService.ShowNotificationAsync(title, body);
    }
}
```

### Channel Importance Levels

Android channel importance affects notification behavior:

- `NotificationImportance.High`: Makes sound and appears as heads-up
- `NotificationImportance.Default`: Makes sound
- `NotificationImportance.Low`: No sound
- `NotificationImportance.Min`: No sound or visual interruption

## Best Practices

### 1. Always Check Permissions

```csharp
private async Task ShowNotificationAsync(string title, string body)
{
    if (!await _notificationService.IsPermittedAsync())
    {
        // Don't attempt to show
        return;
    }

    await _notificationService.ShowNotificationAsync(title, body);
}
```

### 2. Provide Meaningful Titles and Bodies

```csharp
// Bad
await NotificationService.ShowNotificationAsync("Update", "New data");

// Good
await NotificationService.ShowNotificationAsync(
    "Order Shipped",
    "Your order #12345 has been shipped and will arrive in 2 days"
);
```

### 3. Use Data for Navigation

```csharp
// Send actionable data
var data = new Dictionary<string, string>
{
    { "action", "navigate" },
    { "route", "orders/12345" }
};

await NotificationService.ShowNotificationAsync(
    "Order Update",
    "Your order has shipped",
    data
);
```

### 4. Limit Notification Frequency

```csharp
private DateTime _lastNotificationTime = DateTime.MinValue;
private readonly TimeSpan _minimumInterval = TimeSpan.FromSeconds(5);

private async Task ShowRateLimitedNotificationAsync(string title, string body)
{
    if (DateTime.Now - _lastNotificationTime < _minimumInterval)
    {
        Debug.WriteLine("Rate limiting notification");
        return;
    }

    await NotificationService.ShowNotificationAsync(title, body);
    _lastNotificationTime = DateTime.Now;
}
```

### 5. Handle Errors Gracefully

```csharp
private async Task SafeShowNotificationAsync(string title, string body)
{
    try
    {
        await NotificationService.ShowNotificationAsync(title, body);
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Failed to show notification: {ex.Message}");
        // Log to analytics
        // Don't crash app
    }
}
```

### 6. Test on Multiple Android Versions

Android notification behavior varies by version:
- Android 13+: Requires runtime permission
- Android 8+: Requires notification channels
- Android 7 and below: Different notification UI

### 7. Respect User Preferences

```csharp
public class NotificationPreferences
{
    private const string PREF_NOTIFICATIONS_ENABLED = "notifications_enabled";

    public static bool AreNotificationsEnabled
    {
        get => Preferences.Get(PREF_NOTIFICATIONS_ENABLED, true);
        set => Preferences.Set(PREF_NOTIFICATIONS_ENABLED, value);
    }
}

// Check before showing
if (NotificationPreferences.AreNotificationsEnabled)
{
    await NotificationService.ShowNotificationAsync(title, body);
}
```

## Troubleshooting

### iOS Issues

**Notifications not appearing:**

1. Check authorization status:
```csharp
var settings = await UNUserNotificationCenter.Current.GetNotificationSettingsAsync();
Debug.WriteLine($"Auth: {settings.AuthorizationStatus}");
Debug.WriteLine($"Alert: {settings.AlertSetting}");
Debug.WriteLine($"Sound: {settings.SoundSetting}");
```

2. Verify delegate is set:
```csharp
// In AppDelegate.FinishedLaunching
UNUserNotificationCenter.Current.Delegate = this;
```

3. Check notification service registration:
```csharp
var service = IPlatformApplication.Current?.Services
    ?.GetService<ILocalNotificationService>();

Debug.WriteLine($"Service registered: {service != null}");
```

**Notifications appearing but no sound:**

- Check device is not in silent mode
- Verify `UNNotificationSound.Default` is set
- Check notification settings allow sound

### Android Issues

**Notifications not appearing:**

1. Check channel exists:
```csharp
var manager = context.GetSystemService(Context.NotificationService) as NotificationManager;
var channel = manager?.GetNotificationChannel(channelId);
Debug.WriteLine($"Channel exists: {channel != null}");
Debug.WriteLine($"Importance: {channel?.Importance}");
```

2. Verify permission (Android 13+):
```csharp
var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
Debug.WriteLine($"Permission: {status}");
```

3. Check notifications enabled:
```csharp
var manager = NotificationManagerCompat.From(context);
Debug.WriteLine($"Enabled: {manager.AreNotificationsEnabled()}");
```

**Notifications appear but can't be tapped:**

- Check `PendingIntent` is created correctly
- Verify `PendingIntentFlags.Immutable` is set (required on Android 12+)
- Ensure activity exists in manifest

**Icon not showing:**

```csharp
// Ensure app icon exists
var resourceId = activity.Resources?.GetIdentifier("ic_launcher", "mipmap", packageName);
if (resourceId == 0)
{
    Debug.WriteLine("App icon not found - using default");
}
```

### Common Issues

**Service not registered:**

```csharp
// Verify in MauiProgram.cs
builder.Services.AddMauiPushNotifications();

// Or manually:
builder.Services.AddSingleton<ILocalNotificationService, LocalNotificationService>();
```

**Permissions not requested:**

```csharp
// Request via IDeviceInstallationService
await deviceInstallationService.RequestPermissionsAsync();
```

**Foreground notifications not converting:**

- iOS: Ensure `ApnsDelegate` is base class of your AppDelegate
- Android: Ensure `FcmService` is base class of your messaging service
- Both: Verify `ILocalNotificationService` is registered

## Next Steps

- [Push Notifications Setup Guide](PushNotifications.md)
- [Device Installation Documentation](DeviceInstallation.md)
- [CheapHelpers.Blazor Documentation](../Blazor/README.md)
