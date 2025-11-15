# CheapHelpers.Avalonia.Bridge

Bridge package for integrating **CheapAvaloniaBlazor** desktop OS notifications with the **CheapHelpers** unified notification system.

## Purpose

This package provides an adapter that allows desktop applications built with CheapAvaloniaBlazor (using Photino, MAUI WebView, or other WebView technologies) to participate in the CheapHelpers multi-channel notification system.

**Key Features:**
- Adds Desktop OS notifications as a 5th delivery channel (alongside InApp, Email, SMS, Push)
- Zero coupling between main packages (CheapHelpers and CheapAvaloniaBlazor remain independent)
- Clean adapter pattern following .NET ecosystem conventions
- Minimal overhead (single channel implementation)

## Why a Separate Package?

This bridge package exists to keep dependencies clean:

```
CheapAvaloniaBlazor          (independent - no CheapHelpers dependency)
CheapHelpers.Services        (independent - no Avalonia dependency)
CheapHelpers.Avalonia.Bridge (depends on both ↑)
```

Neither main package needs to know about the other. Only install this bridge if you're using **both** packages together.

## Installation

```bash
dotnet add package CheapHelpers.Avalonia.Bridge
```

**Prerequisites:**
- `CheapHelpers` (with notification system)
- `CheapAvaloniaBlazor` (with desktop interop services)

## Quick Start

### 1. Register Services (Correct Order)

```csharp
// In your desktop app's Program.cs or Startup.cs
builder.Services
    .AddCheapAvaloniaBlazorServices()     // 1. Register CheapAvaloniaBlazor
    .AddCheapNotifications<MyUser>()      // 2. Register CheapHelpers notifications
    .AddDesktopNotificationBridge();      // 3. Register bridge
```

**Important:** Registration order matters. The bridge requires both underlying services to be registered first.

### 2. Send Notifications to Desktop

```csharp
// Inject the notification dispatcher
public class MyService(NotificationDispatcher notificationDispatcher)
{
    public async Task NotifyUserAsync(string userId)
    {
        await notificationDispatcher.SendAsync(new UnifiedNotification
        {
            NotificationType = "OrderShipped",
            Title = "Your order has shipped!",
            Body = "Track your package at...",
            RecipientUserIds = [userId],
            Channels = NotificationChannelFlags.Desktop | NotificationChannelFlags.Email
        });
    }
}
```

### 3. Configure User Preferences

Desktop notifications respect the same subscription system as other channels:

```csharp
// Users can enable/disable desktop notifications per notification type
var preference = new UserNotificationPreference
{
    UserId = userId,
    NotificationType = "OrderShipped",
    EnabledChannels = NotificationChannelFlags.Desktop | NotificationChannelFlags.InApp
};

context.UserNotificationPreferences.Add(preference);
await context.SaveChangesAsync();
```

## How It Works

### Architecture

```
┌─────────────────────────────────────────────┐
│        CheapHelpers Notification System     │
│                                             │
│  ┌─────────────────────────────────────┐   │
│  │   NotificationDispatcher             │   │
│  │   (orchestrates multi-channel send)  │   │
│  └──────────────┬──────────────────────┘   │
│                 │                           │
│                 │ resolves all              │
│                 │ INotificationChannel      │
│                 ▼                           │
│  ┌──────────────────────────────────────┐  │
│  │  InApp │ Email │ SMS │ Push │ Desktop│  │  <-- Desktop added by bridge
│  └──────────────────────────────────────┘  │
└─────────────────────────────────────────────┘
                                      │
                                      │ Desktop channel implemented by bridge
                                      ▼
                        ┌──────────────────────────────┐
                        │  DesktopNotificationChannel  │
                        │  (in bridge package)         │
                        └──────────────┬───────────────┘
                                       │ uses
                                       ▼
                        ┌──────────────────────────────┐
                        │  DesktopInteropService       │
                        │  (CheapAvaloniaBlazor)       │
                        └──────────────┬───────────────┘
                                       │ calls JavaScript
                                       ▼
                        ┌──────────────────────────────┐
                        │  WebView Notification API    │
                        │  (Browser/OS integration)    │
                        └──────────────────────────────┘
```

### Channel Implementation

The `DesktopNotificationChannel` implements `INotificationChannel` and:
1. Receives `UnifiedNotification` from the dispatcher
2. Calls `DesktopInteropService.ShowNotificationAsync()` from CheapAvaloniaBlazor
3. Returns delivery results to the dispatcher

Desktop notifications appear in:
- **Windows**: Action Center
- **macOS**: Notification Center
- **Linux**: Desktop notification daemon (varies by DE)

## Important Notes

### Client-Side Nature

Desktop notifications are **client-side** - they show to whoever is currently using the desktop app, regardless of `RecipientUserIds`. This differs from server-side channels like Email/SMS which can target specific users remotely.

### Notification Permissions

Desktop apps must request notification permission from the OS. CheapAvaloniaBlazor handles this via the browser Notification API:

```javascript
// Handled automatically by CheapAvaloniaBlazor
Notification.requestPermission();
```

### Subscription Provider Support

Desktop notifications work with custom subscription providers just like other channels:

```csharp
public class ProjectNotificationProvider(CheapContext<MyUser> context)
    : INotificationSubscriptionProvider
{
    public int Priority => 10;
    public string Name => "ProjectNotifications";

    public bool CanHandle(ISubscriptionContext? subscriptionContext)
        => subscriptionContext is ProjectSubscriptionContext;

    public async Task<NotificationChannelFlags?> GetEnabledChannelsAsync(
        string userId,
        string notificationType,
        ISubscriptionContext? context,
        CancellationToken cancellationToken)
    {
        var projectContext = (ProjectSubscriptionContext)context!;

        // User might want desktop notifications for high-priority projects only
        if (projectContext.Project.Priority == Priority.High)
            return NotificationChannelFlags.Desktop | NotificationChannelFlags.Email;

        return NotificationChannelFlags.Email; // Lower priority projects: email only
    }
}
```

## Future Extensibility

This bridge package can grow to include additional desktop integrations:

- **Desktop file pickers** - OS file dialogs integration
- **System tray integration** - Notification badges on system tray icons
- **Window management** - Desktop window state helpers
- **Desktop-specific auth flows** - OS credential storage integration

For now, it focuses solely on OS notifications, keeping the package lightweight.

## Examples

### Send Urgent Desktop Notification

```csharp
await dispatcher.SendAsync(new UnifiedNotification
{
    NotificationType = "SecurityAlert",
    Title = "Unusual login detected",
    Body = "Someone logged into your account from a new device",
    RecipientUserIds = [userId],
    Priority = NotificationPriority.Urgent,
    Channels = NotificationChannelFlags.Desktop | NotificationChannelFlags.Email | NotificationChannelFlags.Sms
});
```

### Desktop + InApp Only (Silent Server-Side)

```csharp
await dispatcher.SendAsync(new UnifiedNotification
{
    NotificationType = "MessageReceived",
    Title = "New message from Alice",
    Body = "Hey, are you available?",
    RecipientUserIds = [userId],
    Channels = NotificationChannelFlags.Desktop | NotificationChannelFlags.InApp
    // No Email/SMS - keeps it quiet
});
```

### Conditional Desktop Based on Time

```csharp
// User preferences can include Do Not Disturb hours
var preference = new UserNotificationPreference
{
    UserId = userId,
    NotificationType = "MessageReceived",
    EnabledChannels = NotificationChannelFlags.Desktop | NotificationChannelFlags.InApp,
    DoNotDisturbStartHour = 22, // 10 PM
    DoNotDisturbEndHour = 8      // 8 AM
};

// Desktop notifications automatically respect DND hours
// Falls back to InApp only during quiet hours
```

## Troubleshooting

### Desktop notifications not showing?

1. **Check registration order** - Bridge must be registered AFTER both main packages
2. **Verify OS permissions** - User must grant notification permission to the app
3. **Check user preferences** - Desktop channel must be enabled for the notification type
4. **Inspect logs** - DesktopNotificationChannel logs errors at Error level

### Bridge package not found?

Ensure you have the correct package references:
```xml
<PackageReference Include="CheapHelpers" Version="..." />
<PackageReference Include="CheapAvaloniaBlazor" Version="..." />
<PackageReference Include="CheapHelpers.Avalonia.Bridge" Version="..." />
```

## Related Packages

- **CheapHelpers** - Core helpers and unified notification system
- **CheapHelpers.Services** - Service layer including notification services
- **CheapHelpers.Blazor** - Blazor components including NotificationBell
- **CheapAvaloniaBlazor** - Desktop Blazor hosting (Photino, MAUI WebView)

## License

MIT License - See [LICENSE](https://github.com/CheapNud/CheapHelpers/blob/master/LICENSE)

## Contributing

Contributions welcome! See [CONTRIBUTING.md](https://github.com/CheapNud/CheapHelpers/blob/master/CONTRIBUTING.md)
