# CheapHelpers Notification System

## Table of Contents
- [Overview](#overview)
- [Quick Start](#quick-start)
- [Core Concepts](#core-concepts)
- [Multi-Channel Delivery](#multi-channel-delivery)
- [Subscription System](#subscription-system)
- [Real-Time Notifications](#real-time-notifications)
- [In-App Notifications](#in-app-notifications)
- [Configuration](#configuration)
- [Usage Examples](#usage-examples)
- [Advanced Topics](#advanced-topics)
- [API Reference](#api-reference)
- [Troubleshooting](#troubleshooting)

## Overview

The CheapHelpers notification system provides a unified, multi-channel notification delivery platform for .NET applications. It enables sending notifications through multiple channels (in-app, email, SMS, push) from a single unified API, with intelligent subscription resolution and real-time delivery capabilities.

### Key Features

- **Multi-Channel Delivery**: Send notifications through in-app, email, SMS, and push channels simultaneously
- **Subscription-Based**: Flexible subscription system respects user preferences and channel availability
- **Real-Time Delivery**: SignalR-powered real-time notifications for connected users
- **Priority-Based**: Support for Low, Normal, High, and Urgent priority levels
- **Do Not Disturb**: Time-based DND periods to suppress noisy channels
- **Extensible**: Custom subscription providers for entity-specific notification preferences
- **Production-Ready**: Built with Entity Framework Core, ASP.NET Identity, and comprehensive logging

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                       Notification System                            │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  ┌──────────────────────┐         ┌──────────────────────┐          │
│  │  UnifiedNotification │────────>│ NotificationDispatcher│          │
│  │  (Your Application)  │         └──────────┬───────────┘          │
│  └──────────────────────┘                    │                      │
│                                               │                      │
│                          ┌────────────────────┴────────────┐         │
│                          │                                 │         │
│                          ▼                                 ▼         │
│              ┌─────────────────────┐         ┌──────────────────┐   │
│              │ Subscription        │         │ Channel          │   │
│              │ Resolver            │         │ Selection        │   │
│              │ ┌─────────────────┐ │         └────────┬─────────┘   │
│              │ │ Provider Chain  │ │                  │             │
│              │ │ (Priority Order)│ │                  │             │
│              │ │                 │ │                  │             │
│              │ │ 1. Custom       │ │                  │             │
│              │ │ 2. Global Prefs │ │                  │             │
│              │ └─────────────────┘ │                  │             │
│              │                     │                  │             │
│              │ Apply DND Filters   │                  │             │
│              └──────────┬──────────┘                  │             │
│                         │                             │             │
│                         │      Channels Per User      │             │
│                         └─────────────────────────────┘             │
│                                                        │             │
│                 ┌──────────────┬───────────┬──────────┴───────────┐ │
│                 │              │           │                      │ │
│                 ▼              ▼           ▼                      ▼ │
│         ┌──────────────┐ ┌─────────┐ ┌────────┐ ┌──────────────┐  │
│         │ InApp Channel│ │Email Ch.│ │SMS Ch. │ │ Push Channel │  │
│         │              │ │         │ │        │ │              │  │
│         │ + DB Storage │ │ + Email │ │ + SMS  │ │ + Push       │  │
│         │ + SignalR    │ │ Service │ │Service │ │  Backend     │  │
│         └──────────────┘ └─────────┘ └────────┘ └──────────────┘  │
│                 │                                                   │
│                 ▼                                                   │
│         ┌──────────────┐                                            │
│         │ Real-Time    │                                            │
│         │ Delivery     │                                            │
│         │ (SignalR Hub)│                                            │
│         └──────────────┘                                            │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

## Quick Start

### Prerequisites

**IMPORTANT**: The following services MUST be registered BEFORE calling `AddCheapNotifications()`:

- `IEmailService` - For email notifications (required if using Email channel)
- `ISmsService` - For SMS notifications (required if using SMS channel)
- `IPushNotificationBackend` - For push notifications (required if using Push channel)

If these dependencies are not registered, the respective channels will fail at runtime when attempting to send notifications.

### Basic Registration

```csharp
// In Program.cs or Startup.cs

// 1. Register required dependencies FIRST
services.AddScoped<IEmailService, YourEmailService>();
services.AddScoped<ISmsService, YourSmsService>();
services.AddScoped<IPushNotificationBackend, YourPushBackend>();

// 2. Register CheapHelpers notification system
services.AddCheapNotifications<ApplicationUser>(options =>
{
    options.RetentionDays = 30;
    options.MaxStoredPerUser = 500;
    options.EnableAutoCleanup = true;
    options.EnabledChannels = NotificationChannelFlags.All;
});

// 3. For Blazor applications with real-time notifications
services.AddCheapNotificationsBlazor();

// 4. Map SignalR hub endpoint (in app.MapXXX() section)
app.MapCheapNotificationsHub();
```

### Sending Your First Notification

```csharp
public class OrderService
{
    private readonly NotificationDispatcher _notificationDispatcher;

    public OrderService(NotificationDispatcher notificationDispatcher)
    {
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task ProcessOrderAsync(Order order)
    {
        // Process the order...

        // Send notification
        var notification = new UnifiedNotification
        {
            NotificationType = "OrderConfirmation",
            Title = "Order Confirmed",
            Body = $"Your order #{order.Id} has been confirmed and will ship soon.",
            RecipientUserIds = new List<string> { order.CustomerId },
            Channels = NotificationChannelFlags.InApp | NotificationChannelFlags.Email,
            Priority = NotificationPriority.Normal,
            ActionUrl = $"/orders/{order.Id}"
        };

        await _notificationDispatcher.SendAsync(notification);
    }
}
```

## Core Concepts

### UnifiedNotification

The `UnifiedNotification` record is the central data structure for sending notifications. It supports all delivery channels and contains all necessary information for multi-channel delivery.

```csharp
public record UnifiedNotification
{
    // Required properties
    public required string NotificationType { get; init; }      // e.g., "OrderShipped", "CommentAdded"
    public required string Title { get; init; }                 // Notification title/subject
    public required string Body { get; init; }                  // Plain text body
    public required List<string> RecipientUserIds { get; init; }// User IDs to notify

    // Channel configuration
    public NotificationChannelFlags Channels { get; init; }     // Which channels to use (default: InApp)
    public NotificationPriority Priority { get; init; }         // Priority level (default: Normal)

    // Optional properties
    public string? HtmlBody { get; init; }                      // HTML version for email
    public string? ActionUrl { get; init; }                     // URL to navigate when clicked
    public string? IconUrl { get; init; }                       // Icon/image URL
    public Dictionary<string, string>? Data { get; init; }      // Additional structured data

    // Channel-specific overrides
    public List<string>? EmailRecipients { get; init; }         // Email addresses (instead of user IDs)
    public List<string>? SmsRecipients { get; init; }           // Phone numbers (instead of user IDs)
    public string? EmailTemplateName { get; init; }             // Email template name
    public Dictionary<string, object>? EmailTemplateData { get; init; } // Template data

    // Behavior modifiers
    public DateTime? ExpiresAt { get; init; }                   // Expiration time
    public bool Silent { get; init; }                           // Silent delivery (no sound/vibration)
    public ISubscriptionContext? SubscriptionContext { get; init; } // Subscription context
}
```

### NotificationChannels

The `NotificationChannelFlags` enum defines which channels should deliver the notification. It uses flags so multiple channels can be combined.

```csharp
[Flags]
public enum NotificationChannelFlags
{
    None = 0,
    InApp = 1 << 0,  // In-app notifications (stored in database)
    Email = 1 << 1,  // Email notifications
    Sms = 1 << 2,    // SMS notifications
    Push = 1 << 3,   // Push notifications
    All = InApp | Email | Sms | Push
}

// Usage examples:
var channels = NotificationChannelFlags.InApp | NotificationChannelFlags.Email;
var allChannels = NotificationChannelFlags.All;
var inAppOnly = NotificationChannelFlags.InApp;
```

### NotificationPriority

The `NotificationPriority` enum defines how urgent the notification is.

```csharp
public enum NotificationPriority
{
    Low = 0,      // Non-critical notifications
    Normal = 1,   // Standard notifications (default)
    High = 2,     // Important notifications
    Urgent = 3    // Critical notifications requiring immediate attention
}
```

### Subscription Resolution

The subscription system determines which channels each user should receive notifications on. The resolution process:

1. **Provider Chain**: Registered `INotificationSubscriptionProvider` instances are evaluated in priority order (highest first)
2. **First Match Wins**: The first provider that returns a non-null result determines the enabled channels
3. **DND Filtering**: After determining enabled channels, Do Not Disturb settings are applied from ALL providers
4. **Fallback**: If no provider returns channels, the notification's default `Channels` property is used

### Subscription Providers

Subscription providers implement `INotificationSubscriptionProvider` and determine which channels are enabled for a user.

**Built-in Provider**:
- `GlobalUserPreferencesProvider` - Uses the `UserNotificationPreferences` table (priority: 0, always handles requests)

**Custom Providers**:
You can register custom providers to override global preferences with entity-specific subscriptions (e.g., per-project, per-conversation).

```csharp
public class ChatSubscriptionProvider : INotificationSubscriptionProvider
{
    public int Priority => 500; // Higher than global preferences (0)
    public string Name => "ChatSubscriptions";

    public bool CanHandle(ISubscriptionContext? context)
    {
        return context is ChatSubscriptionContext;
    }

    public async Task<NotificationChannelFlags?> GetEnabledChannelsAsync(
        string userId,
        string notificationType,
        ISubscriptionContext? context,
        CancellationToken ct = default)
    {
        if (context is not ChatSubscriptionContext chatContext)
            return null; // Try next provider

        // Check database for user's chat-specific preferences
        var subscription = await _db.ChatSubscriptions
            .Where(s => s.UserId == userId && s.ChatId == chatContext.ChatId)
            .FirstOrDefaultAsync(ct);

        if (subscription != null)
            return subscription.EnabledChannels;

        return null; // No chat-specific preference, try next provider
    }
}
```

## Multi-Channel Delivery

### How Channels Work

Each channel is an implementation of `INotificationChannel` that handles delivery through a specific medium:

1. **InApp Channel**: Stores notifications in database and delivers via SignalR in real-time
2. **Email Channel**: Sends emails using registered `IEmailService`
3. **SMS Channel**: Sends SMS using registered `ISmsService`
4. **Push Channel**: Sends push notifications using registered `IPushNotificationBackend`

### Channel Adapters

Each channel implements the `INotificationChannel` interface:

```csharp
public interface INotificationChannel
{
    string ChannelName { get; }
    bool SupportsNotificationType(string notificationType);
    Task<NotificationChannelResult> SendAsync(
        UnifiedNotification notification,
        CancellationToken cancellationToken = default);
}
```

**Example: Email Channel Implementation**

```csharp
public class EmailNotificationChannel : INotificationChannel
{
    public string ChannelName => "email";
    public bool SupportsNotificationType(string notificationType) => true;

    public async Task<NotificationChannelResult> SendAsync(
        UnifiedNotification notification,
        CancellationToken cancellationToken = default)
    {
        // Look up email addresses from user IDs
        var emailRecipients = new List<string>();
        foreach (var userId in notification.RecipientUserIds)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user?.Email != null)
                emailRecipients.Add(user.Email);
        }

        // Use HTML body if available, otherwise plain text
        var body = notification.HtmlBody ?? notification.Body;

        // Send email
        await _emailService.SendEmailAsync(
            emailRecipients.ToArray(),
            notification.Title,
            body);

        return NotificationChannelResult.Success(ChannelName, emailRecipients.Count);
    }
}
```

### Channel Flags

Channels are enabled/disabled using bitwise flags:

```csharp
// Enable multiple channels
var channels = NotificationChannelFlags.InApp | NotificationChannelFlags.Email;

// Check if channel is enabled
if (channels.HasFlag(NotificationChannelFlags.Email))
{
    // Email is enabled
}

// Remove a channel (DND filtering)
channels &= ~NotificationChannelFlags.Push; // Remove Push channel
```

### Sending to Multiple Channels

```csharp
var notification = new UnifiedNotification
{
    NotificationType = "OrderShipped",
    Title = "Your Order Has Shipped",
    Body = "Your order #12345 is on its way!",
    HtmlBody = "<h2>Order Shipped</h2><p>Your order #12345 is on its way!</p>",
    RecipientUserIds = new List<string> { userId },

    // Send through all channels
    Channels = NotificationChannelFlags.All,

    // Priority affects display and DND behavior
    Priority = NotificationPriority.High,

    // Action when user clicks notification
    ActionUrl = "/orders/12345",

    // Additional data for client-side handling
    Data = new Dictionary<string, string>
    {
        ["orderId"] = "12345",
        ["trackingNumber"] = "1Z999AA10123456784"
    }
};

var result = await _dispatcher.SendAsync(notification);

// Check results
if (result.IsSuccess)
{
    Console.WriteLine($"Sent via {result.SuccessfulChannels} channels");
    Console.WriteLine($"Failed on {result.FailedChannels} channels");
}
```

## Subscription System

### Global User Preferences

Users can set global notification preferences stored in the `UserNotificationPreferences` table:

```csharp
// Entity: UserNotificationPreference
public class UserNotificationPreference
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string NotificationType { get; set; }
    public NotificationChannelFlags EnabledChannels { get; set; }

    // Do Not Disturb settings
    public int? DoNotDisturbStartHour { get; set; } // 0-23
    public int? DoNotDisturbEndHour { get; set; }   // 0-23
}
```

**Updating User Preferences**:

```csharp
// Get current preferences
var preferences = await _inAppNotificationService.GetUserPreferencesAsync(
    userId,
    "OrderNotifications");

// Modify channels
preferences.EnabledChannels = NotificationChannelFlags.InApp | NotificationChannelFlags.Email;

// Set DND period (10 PM to 8 AM)
preferences.DoNotDisturbStartHour = 22;
preferences.DoNotDisturbEndHour = 8;

// Save
await _inAppNotificationService.UpdateUserPreferencesAsync(preferences);
```

### Custom Subscription Providers

Custom providers allow entity-specific notification preferences. For example, users might want different notification settings for different chat conversations or projects.

**Step 1: Create Subscription Context**

```csharp
public record ProjectSubscriptionContext(int ProjectId) : ISubscriptionContext
{
    public string EntityType => "Project";
}
```

**Step 2: Create Custom Provider**

```csharp
public class ProjectSubscriptionProvider : INotificationSubscriptionProvider
{
    private readonly ApplicationDbContext _db;

    // Higher priority = checked first (500 > 0 for global preferences)
    public int Priority => 500;
    public string Name => "ProjectSubscriptions";

    public bool CanHandle(ISubscriptionContext? context)
    {
        return context is ProjectSubscriptionContext;
    }

    public async Task<NotificationChannelFlags?> GetEnabledChannelsAsync(
        string userId,
        string notificationType,
        ISubscriptionContext? context,
        CancellationToken ct = default)
    {
        if (context is not ProjectSubscriptionContext projectContext)
            return null; // Not our context, try next provider

        var subscription = await _db.ProjectSubscriptions
            .Where(s => s.UserId == userId && s.ProjectId == projectContext.ProjectId)
            .Select(s => s.EnabledChannels)
            .FirstOrDefaultAsync(ct);

        if (subscription != default(NotificationChannelFlags))
            return subscription;

        // No project-specific preference found, let next provider handle it
        return null;
    }
}
```

**Step 3: Register Provider**

```csharp
services.AddScoped<INotificationSubscriptionProvider, ProjectSubscriptionProvider>();
```

**Step 4: Use Context When Sending**

```csharp
var notification = new UnifiedNotification
{
    NotificationType = "TaskAssigned",
    Title = "New Task Assigned",
    Body = "You have been assigned a task in Project Alpha",
    RecipientUserIds = new List<string> { userId },
    Channels = NotificationChannelFlags.All, // Default if no subscription exists

    // Pass context to enable project-specific preferences
    SubscriptionContext = new ProjectSubscriptionContext(ProjectId: 42)
};

await _dispatcher.SendAsync(notification);
```

### Subscription Resolution Flow

The resolver queries providers in **descending priority order**:

```
Priority 1000: AdminOverrideProvider  ──> Returns channels or null
                                           │
                                           ├──> If null, try next
                                           ▼
Priority 500:  ProjectSubscriptionProvider ──> Returns channels or null
                                           │
                                           ├──> If null, try next
                                           ▼
Priority 0:    GlobalUserPreferencesProvider ──> Returns channels or null
                                           │
                                           ├──> If null, use notification default
                                           ▼
                                     Final Channels
                                           │
                                           ▼
                                   Apply DND Filters (from ALL providers)
                                           │
                                           ▼
                                   Final Filtered Channels
```

**IMPORTANT**: Returning `null` from a provider means "I don't have preferences for this, try the next provider". This allows the chain to fall through to lower-priority providers.

### Provider Chain Pattern

```csharp
// Provider registration order doesn't matter - priority values determine order
services.AddScoped<INotificationSubscriptionProvider, GlobalUserPreferencesProvider<User>>();
services.AddScoped<INotificationSubscriptionProvider, ProjectSubscriptionProvider>(); // Priority 500
services.AddScoped<INotificationSubscriptionProvider, ChatSubscriptionProvider>();    // Priority 600
services.AddScoped<INotificationSubscriptionProvider, AdminOverrideProvider>();       // Priority 1000

// Resolver automatically orders by Priority (descending)
// Evaluation order: Admin (1000) -> Chat (600) -> Project (500) -> Global (0)
```

### Do Not Disturb Support

DND settings suppress noisy channels (SMS, Push) during specified hours:

```csharp
public async Task<NotificationChannelFlags?> GetDoNotDisturbChannelsAsync(
    string userId,
    CancellationToken ct = default)
{
    var preferences = await _db.UserNotificationPreferences
        .Where(p => p.UserId == userId
                 && p.DoNotDisturbStartHour.HasValue
                 && p.DoNotDisturbEndHour.HasValue)
        .FirstOrDefaultAsync(ct);

    if (preferences == null)
        return null; // No DND settings

    var currentHour = DateTime.UtcNow.Hour;
    var startHour = preferences.DoNotDisturbStartHour.Value;
    var endHour = preferences.DoNotDisturbEndHour.Value;

    bool isInDndPeriod;
    if (startHour < endHour)
    {
        // DND period doesn't cross midnight (e.g., 9 AM to 5 PM)
        isInDndPeriod = currentHour >= startHour && currentHour < endHour;
    }
    else
    {
        // DND period crosses midnight (e.g., 10 PM to 6 AM)
        isInDndPeriod = currentHour >= startHour || currentHour < endHour;
    }

    if (isInDndPeriod)
    {
        // Disable noisy channels during DND
        return NotificationChannelFlags.Push | NotificationChannelFlags.Sms;
    }

    return null;
}
```

**DND Application**:
1. All providers are queried for DND settings (not just the one that provided enabled channels)
2. DND channels from all providers are combined
3. DND channels are removed from enabled channels using bitwise AND NOT: `enabledChannels &= ~dndChannels`

## Real-Time Notifications

### SignalR Integration

The notification system uses SignalR for real-time delivery of in-app notifications to connected users.

**Server-Side Setup**:

```csharp
// In Program.cs
services.AddCheapNotificationsBlazor();

// Map hub endpoint
app.MapCheapNotificationsHub(); // Maps to /hubs/notifications
```

### NotificationHub

The `NotificationHub` manages client connections and group membership:

```csharp
[Authorize]
public class NotificationHub : Hub
{
    // Automatically adds users to their user-specific group
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }
        await base.OnConnectedAsync();
    }
}
```

### Client-Side Connection Example

**JavaScript/TypeScript**:

```javascript
import * as signalR from "@microsoft/signalr";

// Create connection
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/notifications")
    .withAutomaticReconnect()
    .build();

// Handle incoming notifications
connection.on("ReceiveNotification", (notification) => {
    console.log("New notification:", notification);

    // Update UI
    displayNotification(notification);
    updateUnreadCount();
});

// Start connection
await connection.start();
console.log("Connected to notification hub");
```

**C# (Blazor)**:

```csharp
@inject NavigationManager Navigation
@implements IAsyncDisposable

<div>Unread: @_unreadCount</div>

@code {
    private HubConnection? _hubConnection;
    private int _unreadCount = 0;

    protected override async Task OnInitializedAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri("/hubs/notifications"))
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<InAppNotification>("ReceiveNotification", notification =>
        {
            _unreadCount++;
            StateHasChanged();

            // Show toast notification
            Snackbar.Add(notification.Title, Severity.Info);
        });

        await _hubConnection.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}
```

### Handling ReceiveNotification Events

The `ReceiveNotification` event is triggered when a notification is sent to the user. The notification object contains:

```typescript
interface InAppNotification {
    id: number;
    notificationType: string;
    title: string;
    body?: string;
    htmlBody?: string;
    userId: string;
    priority: number; // 0=Low, 1=Normal, 2=High, 3=Urgent
    isRead: boolean;
    readAt?: Date;
    actionUrl?: string;
    iconUrl?: string;
    dataJson?: string; // JSON string of additional data
    expiresAt?: Date;
    isArchived: boolean;
    createdAt: Date;
    updatedAt: Date;
}
```

## In-App Notifications

### InAppNotification Entity

In-app notifications are stored in the database using the `InAppNotification` entity:

```csharp
public class InAppNotification : IAuditable
{
    public int Id { get; set; }
    public string NotificationType { get; set; }
    public string Title { get; set; }
    public string? Body { get; set; }
    public string? HtmlBody { get; set; }
    public string UserId { get; set; }
    public NotificationPriority Priority { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? ActionUrl { get; set; }
    public string? IconUrl { get; set; }
    public string? DataJson { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public void MarkAsRead() { /* ... */ }
    public void Archive() { /* ... */ }
    public void Unarchive() { /* ... */ }
}
```

### CRUD Operations

**Get Unread Count**:

```csharp
int unreadCount = await _inAppNotificationService.GetUnreadCountAsync(
    userId,
    notificationType: "OrderNotifications"); // Optional filter
```

**Get User Notifications**:

```csharp
var filter = new InAppNotificationFilter
{
    IsRead = false,                    // Only unread
    NotificationType = "OrderUpdates", // Optional type filter
    MinimumPriority = 2,               // High priority and above
    Since = DateTimeOffset.UtcNow.AddDays(-7), // Last 7 days
    IncludeArchived = false,           // Exclude archived
    Skip = 0,                          // Pagination offset
    Take = 20,                         // Page size
    SortBy = "CreatedAt",              // Sort field
    SortDescending = true              // Newest first
};

List<InAppNotification> notifications = await _inAppNotificationService
    .GetUserNotificationsAsync(userId, filter);
```

**Mark as Read**:

```csharp
// Single notification
bool success = await _inAppNotificationService.MarkAsReadAsync(notificationId, userId);

// All notifications
int markedCount = await _inAppNotificationService.MarkAllAsReadAsync(
    userId,
    notificationType: null); // null = all types
```

**Archive Notification**:

```csharp
bool success = await _inAppNotificationService.ArchiveAsync(notificationId, userId);
```

**Delete Notification**:

```csharp
bool success = await _inAppNotificationService.DeleteAsync(notificationId, userId);
```

### Filtering and Pagination

The `InAppNotificationFilter` class provides comprehensive filtering:

```csharp
public class InAppNotificationFilter
{
    public bool? IsRead { get; set; }           // true/false/null (all)
    public string? NotificationType { get; set; } // Filter by type
    public int? MinimumPriority { get; set; }    // 0=Low, 1=Normal, 2=High, 3=Urgent
    public DateTimeOffset? Since { get; set; }   // Only notifications after this date
    public bool IncludeArchived { get; set; }    // Default: false
    public int Skip { get; set; }                // Pagination offset
    public int? Take { get; set; }               // Page size (null = all)
    public string? SortBy { get; set; }          // "CreatedAt", "Priority", "ReadAt"
    public bool SortDescending { get; set; }     // Default: false
}
```

### Read/Unread Tracking

Notifications automatically track read status:

```csharp
var notification = new InAppNotification { /* ... */ };

// Initially unread
notification.IsRead; // false
notification.ReadAt; // null

// Mark as read
notification.MarkAsRead();

// Now read
notification.IsRead; // true
notification.ReadAt; // DateTime.UtcNow
notification.UpdatedAt; // DateTime.UtcNow
```

### Archiving and Expiration

**Archiving**:
```csharp
notification.Archive();
notification.IsArchived; // true

notification.Unarchive();
notification.IsArchived; // false
```

**Expiration**:
```csharp
var notification = new UnifiedNotification
{
    // ...
    ExpiresAt = DateTime.UtcNow.AddDays(7) // Auto-cleanup after 7 days
};

// Check if expired
bool expired = notification.IsExpired;
```

**Cleanup Expired Notifications**:
```csharp
int deletedCount = await _inAppNotificationService.DeleteExpiredAsync();
```

## Configuration

### NotificationOptions Properties

```csharp
public class NotificationOptions
{
    // Retention and cleanup
    public int RetentionDays { get; set; } = 30;           // Days to keep notifications
    public int MaxStoredPerUser { get; set; } = 500;       // Max notifications per user
    public bool EnableAutoCleanup { get; set; } = true;    // Enable background cleanup

    // Real-time delivery
    public string RealTimeProvider { get; set; } = "SignalR"; // "SignalR" or "RabbitMQ"

    // Caching (future)
    public bool EnableRedisCache { get; set; } = false;
    public string? RedisConnectionString { get; set; }

    // Message queue (future)
    public string? RabbitMQConnectionString { get; set; }

    // Channel control
    public NotificationChannelFlags EnabledChannels { get; set; } = NotificationChannelFlags.All;
}
```

### appsettings.json Example

```json
{
  "Notifications": {
    "RetentionDays": 30,
    "MaxStoredPerUser": 500,
    "EnableAutoCleanup": true,
    "RealTimeProvider": "SignalR",
    "EnableRedisCache": false,
    "EnabledChannels": 15
  }
}
```

**Channel Flag Values**:
- `1` = InApp only
- `2` = Email only
- `4` = SMS only
- `8` = Push only
- `3` = InApp + Email
- `15` = All channels (1 + 2 + 4 + 8)

### Enabling/Disabling Channels

**Globally**:
```csharp
services.AddCheapNotifications<User>(options =>
{
    // Disable SMS globally
    options.EnabledChannels = NotificationChannelFlags.InApp
                            | NotificationChannelFlags.Email
                            | NotificationChannelFlags.Push;
});
```

**Per Notification**:
```csharp
var notification = new UnifiedNotification
{
    // ...
    Channels = NotificationChannelFlags.InApp | NotificationChannelFlags.Email
};
```

### Cleanup Service Configuration

The auto-cleanup service runs in the background and removes:
- Notifications older than `RetentionDays`
- Expired notifications (past `ExpiresAt` timestamp)
- Excess notifications beyond `MaxStoredPerUser` per user

**TODO**: Background cleanup service implementation pending.

## Usage Examples

### Basic Notification

```csharp
var notification = new UnifiedNotification
{
    NotificationType = "SystemAnnouncement",
    Title = "System Maintenance",
    Body = "The system will undergo maintenance tonight at 2 AM.",
    RecipientUserIds = new List<string> { userId },
    Channels = NotificationChannelFlags.InApp,
    Priority = NotificationPriority.Normal
};

await _dispatcher.SendAsync(notification);
```

### Multi-Channel with Subscription Context

```csharp
var notification = new UnifiedNotification
{
    NotificationType = "ProjectUpdate",
    Title = "Project Milestone Reached",
    Body = "Project Alpha has reached milestone 3!",
    HtmlBody = "<h2>Milestone Reached</h2><p>Project Alpha completed milestone 3.</p>",
    RecipientUserIds = new List<string> { userId1, userId2, userId3 },

    // Request all channels (subscription system will filter based on user preferences)
    Channels = NotificationChannelFlags.All,

    Priority = NotificationPriority.High,
    ActionUrl = "/projects/alpha/milestones/3",

    // Enable project-specific subscription filtering
    SubscriptionContext = new ProjectSubscriptionContext(ProjectId: 123)
};

var result = await _dispatcher.SendAsync(notification);
```

### Chat Integration

```csharp
public class ChatService
{
    private readonly ChatNotificationSource _chatNotifications;

    public async Task SendMessageAsync(int chatId, string senderId, string message)
    {
        // Save message to database...

        // Get chat participants
        var recipientIds = await GetChatParticipantIds(chatId);

        // Send notification (automatically excludes sender)
        await _chatNotifications.NotifyNewChatMessageAsync(
            chatType: "user_chat",
            senderId: senderId,
            senderName: "John Doe",
            recipientIds: recipientIds,
            messagePreview: message.Substring(0, Math.Min(100, message.Length)),
            chatUrl: $"/chat/{chatId}",
            subscriptionContext: new ChatSubscriptionContext(chatId)
        );
    }
}
```

### Order Confirmation Notification

```csharp
public class OrderNotificationService
{
    private readonly NotificationDispatcher _dispatcher;

    public async Task NotifyOrderConfirmedAsync(Order order, User customer)
    {
        var notification = new UnifiedNotification
        {
            NotificationType = "OrderConfirmation",
            Title = "Order Confirmed",
            Body = $"Your order #{order.OrderNumber} has been confirmed. " +
                   $"Total: {order.Total:C}. Expected delivery: {order.EstimatedDelivery:d}",
            HtmlBody = RenderEmailTemplate("OrderConfirmation", order),

            RecipientUserIds = new List<string> { customer.Id },

            // Email for official confirmation, InApp for convenience
            Channels = NotificationChannelFlags.InApp | NotificationChannelFlags.Email,

            Priority = NotificationPriority.Normal,
            ActionUrl = $"/orders/{order.Id}",
            IconUrl = "/images/order-confirmed.png",

            // Additional data for client-side handling
            Data = new Dictionary<string, string>
            {
                ["orderId"] = order.Id.ToString(),
                ["orderNumber"] = order.OrderNumber,
                ["total"] = order.Total.ToString("F2")
            },

            // Email-specific
            EmailTemplateName = "OrderConfirmation",
            EmailTemplateData = new Dictionary<string, object>
            {
                ["Order"] = order,
                ["Customer"] = customer
            }
        };

        await _dispatcher.SendAsync(notification);
    }
}
```

### Custom Notification Types

```csharp
// Define notification type constants
public static class NotificationTypes
{
    public const string OrderShipped = "OrderShipped";
    public const string OrderDelivered = "OrderDelivered";
    public const string PaymentReceived = "PaymentReceived";
    public const string TaskAssigned = "TaskAssigned";
    public const string CommentAdded = "CommentAdded";
    public const string SystemAlert = "SystemAlert";
}

// Use in notifications
var notification = new UnifiedNotification
{
    NotificationType = NotificationTypes.TaskAssigned,
    Title = "New Task Assigned",
    Body = "You have been assigned to: Update documentation",
    RecipientUserIds = new List<string> { assigneeId },
    Channels = NotificationChannelFlags.InApp | NotificationChannelFlags.Push,
    Priority = NotificationPriority.High,
    ActionUrl = $"/tasks/{taskId}"
};
```

## Advanced Topics

### Custom Subscription Contexts

Subscription contexts enable entity-specific notification preferences:

```csharp
// Define context
public record OrderSubscriptionContext(int OrderId) : ISubscriptionContext
{
    public string EntityType => "Order";
}

// Create provider
public class OrderSubscriptionProvider : INotificationSubscriptionProvider
{
    public int Priority => 600;
    public string Name => "OrderSubscriptions";

    public bool CanHandle(ISubscriptionContext? context)
    {
        return context is OrderSubscriptionContext;
    }

    public async Task<NotificationChannelFlags?> GetEnabledChannelsAsync(
        string userId,
        string notificationType,
        ISubscriptionContext? context,
        CancellationToken ct = default)
    {
        var orderContext = (OrderSubscriptionContext)context!;

        // Example: VIP customers get all channels for their orders
        var order = await _db.Orders.FindAsync(orderContext.OrderId);
        if (order.Customer.IsVip)
        {
            return NotificationChannelFlags.All;
        }

        // Regular customers: InApp + Email only
        return NotificationChannelFlags.InApp | NotificationChannelFlags.Email;
    }
}

// Register
services.AddScoped<INotificationSubscriptionProvider, OrderSubscriptionProvider>();

// Use
await _dispatcher.SendAsync(new UnifiedNotification
{
    // ...
    SubscriptionContext = new OrderSubscriptionContext(orderId)
});
```

### Entity-Specific Subscriptions

Store entity-specific subscriptions in database:

```csharp
// Entity
public class ProjectSubscription
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public int ProjectId { get; set; }
    public NotificationChannelFlags EnabledChannels { get; set; }
}

// Provider implementation
public async Task<NotificationChannelFlags?> GetEnabledChannelsAsync(
    string userId,
    string notificationType,
    ISubscriptionContext? context,
    CancellationToken ct = default)
{
    if (context is not ProjectSubscriptionContext projectContext)
        return null;

    var subscription = await _db.ProjectSubscriptions
        .Where(s => s.UserId == userId && s.ProjectId == projectContext.ProjectId)
        .Select(s => s.EnabledChannels)
        .FirstOrDefaultAsync(ct);

    // Return null if no subscription exists (fall through to next provider)
    return subscription != default(NotificationChannelFlags) ? subscription : null;
}
```

### Priority Ordering of Providers

Providers are evaluated in descending priority order:

```csharp
// High priority: Admin overrides
public class AdminOverrideProvider : INotificationSubscriptionProvider
{
    public int Priority => 1000; // Evaluated first
    public string Name => "AdminOverrides";

    public async Task<NotificationChannelFlags?> GetEnabledChannelsAsync(...)
    {
        // Check if admin has forced specific channels for this user
        var adminOverride = await _db.AdminNotificationOverrides
            .Where(o => o.UserId == userId)
            .FirstOrDefaultAsync(ct);

        if (adminOverride != null)
            return adminOverride.ForcedChannels; // Override all other preferences

        return null; // No override, try next provider
    }
}

// Medium priority: Entity-specific
public class ProjectSubscriptionProvider : INotificationSubscriptionProvider
{
    public int Priority => 500; // Evaluated second
    // ...
}

// Low priority: Global defaults
public class GlobalUserPreferencesProvider : INotificationSubscriptionProvider
{
    public int Priority => 0; // Evaluated last
    // ...
}
```

### DND Channel Filtering

The DND filtering mechanism allows providers to suppress noisy channels during quiet hours:

```csharp
public async Task<NotificationChannelFlags?> GetDoNotDisturbChannelsAsync(
    string userId,
    CancellationToken ct = default)
{
    // Check user's DND schedule
    var schedule = await _db.UserDndSchedules
        .Where(s => s.UserId == userId)
        .FirstOrDefaultAsync(ct);

    if (schedule == null)
        return null; // No DND settings

    var now = DateTime.UtcNow;
    var currentHour = now.Hour;

    // Check if current time is in DND period
    bool isQuietHours = IsInTimeRange(currentHour, schedule.StartHour, schedule.EndHour);

    if (isQuietHours)
    {
        // Suppress Push and SMS during quiet hours
        // Email and InApp are typically silent so we allow them
        return NotificationChannelFlags.Push | NotificationChannelFlags.Sms;
    }

    return null; // Not in DND period
}

private bool IsInTimeRange(int current, int start, int end)
{
    if (start < end)
        return current >= start && current < end;
    else
        return current >= start || current < end; // Crosses midnight
}
```

**Multiple DND Sources**:

All providers are checked for DND settings and combined:

```csharp
// Provider 1: Global user DND (10 PM - 6 AM, suppress Push + SMS)
Provider1.GetDoNotDisturbChannelsAsync() -> Push | Sms

// Provider 2: Project-specific quiet period (suppress all channels)
Provider2.GetDoNotDisturbChannelsAsync() -> All

// Combined DND channels (bitwise OR)
finalDndChannels = Push | Sms | All = All

// Applied to enabled channels (bitwise AND NOT)
enabledChannels = InApp | Email | Push | Sms
filteredChannels = enabledChannels & ~finalDndChannels
                 = (InApp | Email | Push | Sms) & ~All
                 = None
```

## API Reference

### NotificationDispatcher

The main service for sending notifications.

```csharp
public class NotificationDispatcher
{
    /// <summary>
    /// Sends a notification across multiple channels based on resolved user subscriptions
    /// </summary>
    /// <param name="notification">The notification to send</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Dispatch result containing channel-specific results and overall status</returns>
    public async Task<NotificationDispatchResult> SendAsync(
        UnifiedNotification notification,
        CancellationToken ct = default)
}
```

**Returns**: `NotificationDispatchResult`
```csharp
public class NotificationDispatchResult
{
    public bool IsSuccess { get; }
    public string NotificationType { get; }
    public int SuccessfulChannels { get; }
    public int FailedChannels { get; }
    public int TotalSentCount { get; }
    public int TotalFailedCount { get; }
    public Dictionary<string, NotificationChannelResult> ChannelResults { get; }
    public string? ErrorSummary { get; }
}
```

### IInAppNotificationService

Service for managing in-app notifications.

```csharp
public interface IInAppNotificationService
{
    Task<InAppNotificationResult> CreateAsync(InAppNotification notification, CancellationToken ct = default);

    Task<int> GetUnreadCountAsync(string userId, string? notificationType = null, CancellationToken ct = default);

    Task<List<InAppNotification>> GetUserNotificationsAsync(
        string userId,
        InAppNotificationFilter filter,
        CancellationToken ct = default);

    Task<bool> MarkAsReadAsync(int notificationId, string userId, CancellationToken ct = default);

    Task<int> MarkAllAsReadAsync(string userId, string? notificationType = null, CancellationToken ct = default);

    Task<bool> ArchiveAsync(int notificationId, string userId, CancellationToken ct = default);

    Task<bool> DeleteAsync(int notificationId, string userId, CancellationToken ct = default);

    Task<int> DeleteExpiredAsync(CancellationToken ct = default);

    Task<UserNotificationPreference> GetUserPreferencesAsync(
        string userId,
        string notificationType,
        CancellationToken ct = default);

    Task<bool> UpdateUserPreferencesAsync(
        UserNotificationPreference preferences,
        CancellationToken ct = default);
}
```

### INotificationSubscriptionProvider

Interface for custom subscription providers.

```csharp
public interface INotificationSubscriptionProvider
{
    /// <summary>Priority (higher = evaluated first)</summary>
    int Priority { get; }

    /// <summary>Provider name for logging</summary>
    string Name { get; }

    /// <summary>Can this provider handle the given context?</summary>
    bool CanHandle(ISubscriptionContext? subscriptionContext);

    /// <summary>
    /// Get enabled channels for user/notification type.
    /// Return null to try next provider.
    /// </summary>
    Task<NotificationChannelFlags?> GetEnabledChannelsAsync(
        string userId,
        string notificationType,
        ISubscriptionContext? context,
        CancellationToken ct = default);

    /// <summary>
    /// Get DND channels to suppress.
    /// Return null if no DND settings.
    /// </summary>
    Task<NotificationChannelFlags?> GetDoNotDisturbChannelsAsync(
        string userId,
        CancellationToken ct = default);
}
```

### INotificationSubscriptionResolver

Resolves subscriptions by querying providers.

```csharp
public interface INotificationSubscriptionResolver
{
    /// <summary>
    /// Resolves subscriptions for a notification.
    /// Returns one notification per user with resolved channels.
    /// Users with no enabled channels are excluded.
    /// </summary>
    Task<List<UnifiedNotification>> ResolveSubscriptionsAsync(
        UnifiedNotification notification,
        ISubscriptionContext? context,
        CancellationToken ct = default);
}
```

## Troubleshooting

### Common Issues and Solutions

**Issue: Email/SMS/Push notifications fail with "dependency not registered" error**

Solution: Register required dependencies BEFORE calling `AddCheapNotifications()`:
```csharp
services.AddScoped<IEmailService, YourEmailService>();
services.AddScoped<ISmsService, YourSmsService>();
services.AddScoped<IPushNotificationBackend, YourPushBackend>();

services.AddCheapNotifications<User>(); // Now dependencies exist
```

**Issue: Notifications not delivered in real-time**

Checklist:
1. Did you call `services.AddCheapNotificationsBlazor()`?
2. Did you call `app.MapCheapNotificationsHub()`?
3. Is the client connected to `/hubs/notifications`?
4. Is the user authenticated (hub requires `[Authorize]`)?
5. Check browser console for SignalR connection errors

**Issue: Custom subscription provider not being called**

Checklist:
1. Is the provider registered in DI? `services.AddScoped<INotificationSubscriptionProvider, YourProvider>()`
2. Does `CanHandle()` return true for your context?
3. Is `SubscriptionContext` set on the notification?
4. Check logs for provider evaluation order

**Issue: All users getting same notification channels despite different preferences**

Cause: Provider returning same value for all users

Solution: Ensure provider queries user-specific data:
```csharp
// WRONG: Returns same for all users
public async Task<NotificationChannelFlags?> GetEnabledChannelsAsync(...)
{
    return NotificationChannelFlags.All; // Same for everyone!
}

// CORRECT: Queries per-user preferences
public async Task<NotificationChannelFlags?> GetEnabledChannelsAsync(
    string userId, ...)
{
    var prefs = await _db.Preferences.FindAsync(userId);
    return prefs?.EnabledChannels;
}
```

**Issue: Notifications still sent during DND hours**

Checklist:
1. Is `GetDoNotDisturbChannelsAsync()` implemented?
2. Does it return the correct channel flags (e.g., `Push | Sms`)?
3. Is the current hour calculation correct (mind timezone issues)?
4. Check if DND period crosses midnight (requires special logic)

### Debugging Tips

**Enable Detailed Logging**:

```json
{
  "Logging": {
    "LogLevel": {
      "CheapHelpers.Services.Notifications": "Debug",
      "CheapHelpers.Blazor.Services": "Debug"
    }
  }
}
```

**Inspect Dispatch Results**:

```csharp
var result = await _dispatcher.SendAsync(notification);

if (!result.IsSuccess)
{
    _logger.LogWarning("Notification failed: {ErrorSummary}", result.ErrorSummary);

    foreach (var channelResult in result.ChannelResults)
    {
        if (!channelResult.Value.IsSuccess)
        {
            _logger.LogError(
                "Channel {ChannelName} failed: {Error}",
                channelResult.Key,
                channelResult.Value.ErrorMessage);
        }
    }
}
```

**Test Subscription Resolution**:

```csharp
var resolved = await _subscriptionResolver.ResolveSubscriptionsAsync(
    notification,
    context,
    CancellationToken.None);

foreach (var userNotification in resolved)
{
    _logger.LogInformation(
        "User {UserId} will receive via channels: {Channels}",
        userNotification.RecipientUserIds.First(),
        userNotification.Channels);
}
```

### Logging Recommendations

The notification system includes comprehensive logging at these levels:

- **Debug**: Provider evaluation, subscription resolution, DND checks
- **Information**: Notification dispatch, channel delivery success, user preference updates
- **Warning**: Partial failures, missing recipients, channel errors
- **Error**: Complete failures, exceptions during delivery

**Recommended Configuration**:

Development:
```json
{
  "Logging": {
    "LogLevel": {
      "CheapHelpers.Services.Notifications": "Debug"
    }
  }
}
```

Production:
```json
{
  "Logging": {
    "LogLevel": {
      "CheapHelpers.Services.Notifications": "Information"
    }
  }
}
```

---

## Support and Contributing

For issues, feature requests, or contributions, please visit the CheapHelpers repository.

**Author**: CheapNud
**License**: MIT
**Version**: 1.0.0
