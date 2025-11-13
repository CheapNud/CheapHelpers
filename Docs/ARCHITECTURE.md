# CheapHelpers Architecture: Separation of Concerns

## Overview

This document explains the clean separation between system UI, application UI, and background services in CheapHelpers.

## Architecture Layers

```
┌───────────────────────────────────────────────────────────┐
│                    USER INTERFACE                         │
├───────────────────────────────────────────────────────────┤
│                                                           │
│  ┌─────────────────────────────────────────────────┐    │
│  │     System Status Bar (OS Level)                │    │
│  │     Platform: Android / iOS                     │    │
│  │     Configured by: AndroidSystemBarsHelper      │    │
│  │     Concern: Transparent/opaque, light/dark     │    │
│  └─────────────────────────────────────────────────┘    │
│                                                           │
│  ┌─────────────────────────────────────────────────┐    │
│  │     App Bar (Application Level)                 │    │
│  │     Platform: Blazor Component                  │    │
│  │     Configured by: AppBar.razor + IAppBarService│    │
│  │     Concern: Navigation, branding, actions      │    │
│  └─────────────────────────────────────────────────┘    │
│                                                           │
│  ┌─────────────────────────────────────────────────┐    │
│  │     Page Content (Application)                  │    │
│  │     Platform: Blazor Pages/Components           │    │
│  │     Configured by: Developer                    │    │
│  │     Concern: Business logic, user interaction   │    │
│  └─────────────────────────────────────────────────┘    │
│                                                           │
├───────────────────────────────────────────────────────────┤
│                 BACKGROUND SERVICES                       │
├───────────────────────────────────────────────────────────┤
│                                                           │
│  ┌─────────────────────────────────────────────────┐    │
│  │     Push Notifications                          │    │
│  │     Platform: Firebase / APNS                   │    │
│  │     Configured by: FirebaseTokenHelper          │    │
│  │     Concern: FCM token retrieval, registration  │    │
│  └─────────────────────────────────────────────────┘    │
│                                                           │
│  ┌─────────────────────────────────────────────────┐    │
│  │     Device Installation                         │    │
│  │     Platform: IDeviceInstallationService        │    │
│  │     Configured by: DeviceInstallationService    │    │
│  │     Concern: Device registration with backend   │    │
│  └─────────────────────────────────────────────────┘    │
│                                                           │
└───────────────────────────────────────────────────────────┘
```

## Component Responsibilities

### 1. AndroidSystemBarsHelper (System UI)

**Location:** `CheapHelpers.MAUI/Helpers/AndroidSystemBarsHelper.cs`

**Purpose:** Configure Android system-level UI elements (status bar, navigation bar)

**Responsibilities:**
- Set status bar transparency
- Configure status bar icon color (light/dark)
- Set navigation bar color
- Manage window insets for edge-to-edge layouts
- Handle API level differences

**Does NOT:**
- Retrieve Firebase tokens ❌
- Manage application navigation ❌
- Handle push notifications ❌

**Usage:**
```csharp
// In MainActivity.OnCreate
AndroidSystemBarsHelper.ConfigureEdgeToEdge(this);
```

### 2. AppBar Component (Application UI)

**Location:** `CheapHelpers.Blazor/Hybrid/Components/AppBar.razor`

**Purpose:** Provide application-level navigation bar

**Responsibilities:**
- Display app title and branding
- Provide navigation actions
- Adjust for status bar height
- Support custom layouts via RenderFragment
- Manage visibility and styling

**Does NOT:**
- Configure system status bar ❌
- Retrieve Firebase tokens ❌
- Handle push notifications ❌

**Usage:**
```razor
<AppBar Title="My App">
    <Actions>
        <button @onclick="OnMenu">Menu</button>
    </Actions>
</AppBar>
```

### 3. IAppBarService (Application State)

**Location:** `CheapHelpers.Blazor/Hybrid/Services/IAppBarService.cs`

**Purpose:** Programmatic control of app bar from anywhere in the application

**Responsibilities:**
- Set app bar title dynamically
- Show/hide app bar
- Change colors programmatically
- Notify subscribers of state changes

**Does NOT:**
- Configure system status bar ❌
- Retrieve Firebase tokens ❌
- Handle device registration ❌

**Usage:**
```csharp
@inject IAppBarService AppBarService

AppBarService.SetTitle("Dynamic Title");
AppBarService.SetVisible(true);
```

### 4. FirebaseTokenHelper (Background Service)

**Location:** `CheapHelpers.MAUI/Helpers/FirebaseTokenHelper.cs`

**Purpose:** Safely retrieve Firebase Cloud Messaging tokens

**Responsibilities:**
- Check Firebase availability
- Validate device notification support
- Request FCM token asynchronously
- Handle token retrieval errors
- Refresh tokens on demand

**Does NOT:**
- Configure status bar ❌
- Manage app bar ❌
- Register device with backend (that's IDeviceInstallationService) ❌

**Usage:**
```csharp
// In MainActivity.OnCreate
FirebaseTokenHelper.GetFirebaseTokenSafely(
    this,
    deviceService,
    () => MainApplication.IsFirebaseAvailable
);
```

### 5. IDeviceInstallationService (Device Management)

**Location:** `CheapHelpers.Blazor/Hybrid/Abstractions/IDeviceInstallationService.cs`

**Purpose:** Manage device registration with push notification backend

**Responsibilities:**
- Store FCM/APNS token
- Generate device installation model
- Register with backend service
- Check notification support
- Provide device diagnostics

**Does NOT:**
- Configure system UI ❌
- Manage app bar ❌
- Retrieve Firebase token (uses FirebaseTokenHelper) ❌

**Usage:**
```csharp
@inject IDeviceInstallationService DeviceService

var installation = DeviceService.GetDeviceInstallation("user_123");
await backend.RegisterDeviceAsync(installation);
```

## Data Flow

### Push Notification Setup Flow

```
1. App Launch
   ↓
2. MainActivity.OnCreate()
   ↓
   ├─→ [UI] AndroidSystemBarsHelper.ConfigureEdgeToEdge(this)
   │   └─→ Configure transparent status bar
   │       Configure black navigation bar
   │       Setup window insets
   │
   └─→ [Service] FirebaseTokenHelper.GetFirebaseTokenSafely(this, deviceService)
       └─→ Check Firebase availability
           Check notification support
           Request FCM token
           ↓
3. OnSuccess(token) callback
   ↓
4. DeviceInstallationService.SetToken(token)
   ↓
   ├─→ Trigger OnTokenReceived event
   └─→ Store token for registration
       ↓
5. DeviceInstallationService.RegisterDeviceAsync(userId)
   ↓
6. Backend registration complete
```

### App Bar State Management Flow

```
1. App Layout Loads
   ↓
2. <AppBar Title="Home" /> renders
   ↓
3. AppBar component initialized
   ↓
   ├─→ Detect status bar height via JS interop
   └─→ Apply padding-top to accommodate status bar
       ↓
4. User navigates to new page
   ↓
5. Page.OnInitialized()
   ↓
6. AppBarService.SetTitle("New Page")
   ↓
7. OnAppBarChanged event fired
   ↓
8. AppBar component updates via StateHasChanged()
```

## Why This Separation Matters

### Problem: Mixed Concerns (Old Design)

```csharp
// BAD: Status bar helper doing push notification work
AndroidSystemBarsHelper.ConfigureTransparentStatusBar(this);
AndroidSystemBarsHelper.GetFirebaseTokenSafely(this, deviceService); // ❌ Wrong!
```

**Issues:**
1. Status bar helper knows about Firebase (violation of SRP)
2. UI configuration mixed with background services
3. Difficult to test in isolation
4. Confusing for developers (where do I find push notification code?)

### Solution: Clean Separation (New Design)

```csharp
// GOOD: Each helper has a single responsibility
AndroidSystemBarsHelper.ConfigureTransparentStatusBar(this);  // UI only
FirebaseTokenHelper.GetFirebaseTokenSafely(this, deviceService); // Push only
```

**Benefits:**
1. Single Responsibility Principle (SRP)
2. Easy to find and maintain code
3. Can be tested independently
4. Clear mental model for developers

## Testing Strategy

### Unit Testing

```csharp
// Test status bar configuration in isolation
[Test]
public void ConfigureTransparentStatusBar_SetsStatusBarTransparent()
{
    var activity = new MockActivity();
    AndroidSystemBarsHelper.ConfigureTransparentStatusBar(activity);
    Assert.That(activity.Window.StatusBarColor, Is.EqualTo(Color.Transparent));
}

// Test Firebase token retrieval in isolation
[Test]
public void GetFirebaseTokenSafely_ChecksAvailability()
{
    var activity = new MockActivity();
    var deviceService = new MockDeviceService();
    FirebaseTokenHelper.GetFirebaseTokenSafely(activity, deviceService, () => false);
    // Should not call Firebase when availability check returns false
    Assert.That(deviceService.TokenSetCalled, Is.False);
}

// Test app bar service in isolation
[Test]
public void AppBarService_SetTitle_TriggersEvent()
{
    var service = new AppBarService();
    var eventFired = false;
    service.OnAppBarChanged += () => eventFired = true;
    service.SetTitle("Test");
    Assert.That(eventFired, Is.True);
}
```

### Integration Testing

```csharp
// Test complete push notification flow
[Test]
public async Task PushNotificationFlow_RegistersDevice()
{
    // 1. Firebase token retrieved
    var token = await GetFirebaseToken();

    // 2. Token stored in device service
    deviceService.SetToken(token);

    // 3. Device registered with backend
    var success = await deviceService.RegisterDeviceAsync("user123");

    Assert.That(success, Is.True);
}

// Test app bar with status bar integration
[Test]
public async Task AppBar_AdjustsForStatusBar()
{
    // 1. Status bar configured
    AndroidSystemBarsHelper.ConfigureTransparentStatusBar(activity);

    // 2. App bar detects status bar height
    var appBar = new AppBar { AdjustForStatusBar = true };
    await appBar.OnInitializedAsync();

    // 3. App bar applies correct padding
    Assert.That(appBar.GetTotalHeight(), Is.GreaterThan(56));
}
```

## Migration Guide

### Migrating from Old Design

**Before (Mixed Concerns):**
```csharp
protected override void OnCreate(Bundle? savedInstanceState)
{
    base.OnCreate(savedInstanceState);
    AndroidSystemBarsHelper.ConfigureTransparentStatusBar(this);
    AndroidSystemBarsHelper.GetFirebaseTokenSafely(this, deviceService); // Old way
}
```

**After (Clean Separation):**
```csharp
protected override void OnCreate(Bundle? savedInstanceState)
{
    base.OnCreate(savedInstanceState);

    // Configure system UI (one concern)
    AndroidSystemBarsHelper.ConfigureTransparentStatusBar(this);

    // Setup push notifications (separate concern)
    FirebaseTokenHelper.GetFirebaseTokenSafely(this, deviceService);
}
```

### Finding Features

| Feature | Old Location | New Location |
|---------|-------------|--------------|
| System status bar config | `AndroidSystemBarsHelper` | `AndroidSystemBarsHelper` (unchanged) |
| Firebase token retrieval | `AndroidSystemBarsHelper` ❌ | `FirebaseTokenHelper` ✅ |
| App navigation bar | N/A | `AppBar` component ✅ |
| App bar state management | N/A | `IAppBarService` ✅ |
| Device registration | `DeviceInstallationService` | `DeviceInstallationService` (unchanged) |

## Best Practices

### 1. Choose the Right Helper

```csharp
// Need to configure system UI? → AndroidSystemBarsHelper
AndroidSystemBarsHelper.ConfigureTransparentStatusBar(this);

// Need to get Firebase token? → FirebaseTokenHelper
FirebaseTokenHelper.GetFirebaseTokenSafely(this, deviceService);

// Need app navigation bar? → AppBar component
<AppBar Title="My App" />

// Need to control app bar programmatically? → IAppBarService
AppBarService.SetTitle("Dynamic Title");

// Need to register device? → IDeviceInstallationService
await DeviceService.RegisterDeviceAsync("user123");
```

### 2. Don't Mix Concerns

```csharp
// ❌ BAD: Putting UI logic in service classes
public class PushNotificationService
{
    public void Initialize()
    {
        AndroidSystemBarsHelper.ConfigureTransparentStatusBar(activity); // Wrong!
        FirebaseTokenHelper.GetFirebaseTokenSafely(activity, this);
    }
}

// ✅ GOOD: Keep concerns separate
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // UI configuration
        AndroidSystemBarsHelper.ConfigureTransparentStatusBar(this);

        // Service initialization
        InitializePushNotifications();
    }

    private void InitializePushNotifications()
    {
        FirebaseTokenHelper.GetFirebaseTokenSafely(this, deviceService);
    }
}
```

### 3. Use Dependency Injection

```csharp
// Register services properly
builder.Services.AddSingleton<IDeviceInstallationService, DeviceInstallationService>();
builder.Services.AddAppBar();

// Inject where needed
public class MyPage : ComponentBase
{
    [Inject] private IAppBarService AppBarService { get; set; }
    [Inject] private IDeviceInstallationService DeviceService { get; set; }

    protected override void OnInitialized()
    {
        AppBarService.SetTitle("My Page");
        // Don't create instances manually!
    }
}
```

## Summary

The new architecture provides:

1. **Clear Separation**: Each component has a single, well-defined responsibility
2. **Easy Discovery**: Developers know exactly where to find functionality
3. **Testability**: Components can be tested in isolation
4. **Maintainability**: Changes to one concern don't affect others
5. **Scalability**: New features can be added without breaking existing code

Remember: **Status Bar ≠ App Bar ≠ Push Notifications**

They are three separate concerns that should never be mixed!
