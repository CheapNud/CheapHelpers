# CheapHelpers

A collection of C# utility classes and extension methods for common development tasks.

## Latest Additions

### Caching

#### Memory Cache System
Modern, thread-safe memory caching with flexible expiration strategies using `Microsoft.Extensions.Caching.Memory`.

**Features:**
- Three expiration strategies: Absolute, Sliding, and Flexible (both)
- Full async/await support with cancellation
- Generic type-safe implementation
- Factory pattern for cache-miss scenarios
- Proper disposal pattern (IDisposable)
- TryGet pattern for optional retrieval
- Thread-safe operations

**Cache Types:**
- **AbsoluteExpirationCache\<T>**: Items expire at a fixed time after creation
- **SlidingExpirationCache\<T>**: Items expire after inactivity (timer resets on access)
- **FlexibleExpirationCache\<T>**: Supports both absolute and sliding expiration simultaneously

### IO Utilities

#### TemporaryFileManager
Manages temporary files and directories with automatic cleanup. Ensures proper cleanup even on exceptions or cancellation.

**Features:**
- Automatic cleanup via IDisposable pattern
- Track multiple directories and files
- Get directory size with human-readable formatting
- Exception-safe cleanup
- Configurable application name for temp directory organization

### Process Utilities

#### ProcessExecutor
Generic process execution wrapper with progress tracking, timeout handling, and cancellation support.

**Features:**
- Basic process execution with configurable options
- Process piping (stdout to stdin)
- Regex-based progress extraction with predefined patterns
- Timeout handling with automatic cleanup
- Full cancellation support
- Captured output (stdout/stderr)
- Environment variable configuration

## Core Features

## Packages

| Package | Version | Description |
|---------|---------|-------------|
| [CheapHelpers](https://www.nuget.org/packages/CheapHelpers) | ![NuGet](https://img.shields.io/nuget/v/CheapHelpers.svg) | Core utilities, extensions, and helpers |
| [CheapHelpers.Models](https://www.nuget.org/packages/CheapHelpers.Models) | ![NuGet](https://img.shields.io/nuget/v/CheapHelpers.Models.svg) | Shared data models and DTOs |
| [CheapHelpers.EF](https://www.nuget.org/packages/CheapHelpers.EF) | ![NuGet](https://img.shields.io/nuget/v/CheapHelpers.EF.svg) | Entity Framework repository pattern |
| [CheapHelpers.Services](https://www.nuget.org/packages/CheapHelpers.Services) | ![NuGet](https://img.shields.io/nuget/v/CheapHelpers.Services.svg) | Business services and integrations |
| [CheapHelpers.Blazor](https://www.nuget.org/packages/CheapHelpers.Blazor) | ![NuGet](https://img.shields.io/nuget/v/CheapHelpers.Blazor.svg) | Blazor components, UI utilities, and Hybrid features |
| [CheapHelpers.Networking](https://www.nuget.org/packages/CheapHelpers.Networking) | ![NuGet](https://img.shields.io/nuget/v/CheapHelpers.Networking.svg) | Network scanning and device discovery |
| [CheapHelpers.MAUI](https://www.nuget.org/packages/CheapHelpers.MAUI) | ![NuGet](https://img.shields.io/nuget/v/CheapHelpers.MAUI.svg) | MAUI platform implementations (iOS APNS, Android FCM) |

### Services

#### Email Service with Templates
**Package**: CheapHelpers.Services

Full-featured email service with Fluid/Liquid template engine support.

**Features:**
- MailKit-based SMTP with attachment support
- Template engine with master templates and partials
- Development mode (auto-redirect to dev emails)
- Multiple recipients, CC/BCC support
- Embedded resource template loading

#### XmlService
**Package**: CheapHelpers.Services

XML serialization supporting both dynamic objects and strongly-typed models.

**Features:**
- Dynamic object serialization (ExpandoObject support)
- Strongly-typed serialization/deserialization
- File and string-based operations
- Async I/O operations

#### PDF & Document Services
**Package**: CheapHelpers.Services

- **PdfOptimizationService**: Dual optimization (iLovePDF API + iText fallback)
- **UblService**: UBL 2.0 (Universal Business Language) order document generation
- **PdfTemplateService**: Template-based PDF generation

### Networking

#### NetworkScanner
**Package**: CheapHelpers.Networking

Comprehensive network scanning and device discovery with cross-platform support.

**Features:**
- Subnet scanning with configurable IP ranges
- Ping-based host discovery
- Multi-detector device type identification with priority system
- Cross-platform MAC address resolution (Windows/Linux/macOS)
- Device persistence with JSON storage
- Background scanning with configurable intervals
- Thread-safe operations with SemaphoreSlim
- Dependency injection ready

**Device Detectors:**
- **UPnP/SSDP Detector**: Custom implementation for discovering smart devices, media servers, routers, and IoT devices
- **mDNS/Zeroconf Detector**: Bonjour/Avahi service discovery for Apple devices, printers, and network services
- **HTTP Detector**: Web server detection via headers (IIS, Apache, nginx)
- **SSH Detector**: Banner-based detection for Linux/Unix systems
- **Windows Services Detector**: RDP, SMB, WinRM, NetBIOS port scanning
- **Service Endpoint Detector**: Known device endpoints and patterns

**UPnP/SSDP Features:**
- Custom protocol implementation with zero external dependencies
- Proper multicast support (239.255.255.250:1900)
- Multi-interface UDP listening for comprehensive discovery
- XML device description parsing
- Device type classification (Media Server, Smart TV, Printer, etc.)
- Background periodic discovery

**mDNS/Zeroconf Features:**
- 25+ service type discovery (_http, _printer, _airplay, _homekit, etc.)
- Instance name extraction
- Service type classification
- Actively maintained library (Makaretu.Dns.Multicast.New)

### Security

#### EncryptionHelper
**Package**: CheapHelpers

Machine-specific AES-256-CBC encryption with PBKDF2 key derivation.

**Features:**
- Deterministic key generation based on machine/user/OS
- Fixed IV or random IV per operation
- Thread-safe lazy initialization
- Static `Encrypt()` / `Decrypt()` methods

#### FileHelper
**Package**: CheapHelpers

Secure filename generation to prevent overwrite attacks.

**Features:**
- `GetTrustedFileName()`: Generate secure filenames with GUID suffixes, optional timestamp support
- `GetTrustedFileNameFromPath()`: Secure naming from full paths
- `GetTrustedFileNameFromTempPath()`: Secure temp file handling
- **Date-based filename generators:**
  - `GetDailyFilename()`: Daily pattern (yyyy-MM-dd)
  - `GetWeeklyFilename()`: Weekly pattern (yyyy-wN)
  - `GetMonthlyFilename()`: Monthly pattern (yyyy-MM)
  - `GetYearlyFilename()`: Yearly pattern (yyyy)
  - `GetCustomDateFilename()`: Custom date format patterns

### Database

#### BaseRepo
**Package**: CheapHelpers.EF

Generic repository pattern with comprehensive CRUD operations.

**Features:**
- Full async CRUD operations with cancellation support
- Pagination support with `PaginatedList<T>`
- Bulk operations (AddRange, UpdateRange, DeleteRange)
- Query helpers (GetWhere, CountWhere, Exists)
- No-tracking queries for read operations
- Generic constraints (IEntityId, IEntityCode)

### Extension Methods

#### String Extensions
- `Capitalize()`: Capitalize first letter
- `IsDigitsOnly()`: Validate digit-only strings
- `ToInternationalPhoneNumber()`: Convert to international format (NL/BE support)
- `ToShortString()`: Truncate with ellipsis
- `TrimWithEllipsis()`: Trim to max length with ellipsis
- `RemoveSpecialCharacters()`: Keep only alphanumeric characters
- `RemoveSpecialCharactersKeepDash()`: Keep alphanumeric and dashes
- `Sanitize()`: Sanitize for safe usage (spaces to underscores, slashes to dashes)

#### DateTime Extensions
- `GetDateTime()`: Timezone conversion (bidirectional)
- `GetWorkingDays()`: Calculate business days with exclusions

#### DateTimeOffset Extensions
- `Floor()`: Round down to specified interval
- `Round()`: Round to nearest interval
- `Ceiling()`: Round up to specified interval
- `PerMinute()`: Zero out seconds
- `PerHour()`: Zero out minutes and seconds
- `PerDay()`: Zero out time component
- `ToZeroTime()`: Convert to midnight UTC

#### Collection Extensions
- `Replace<T>()`: Replace items with predicates
- `ToBindingList()` / `ToObservableCollection()`: WPF/UI binding
- `IsNullOrEmpty()`: Safe null/empty check
- `OrderByDynamic()`: Dynamic LINQ ordering by property name
- `OrderByDescendingDynamic()`: Dynamic LINQ descending ordering

#### Core Extensions
- `ToJson<T>()` / `FromJson<T>()`: JSON serialization with loop handling
- `DeepClone<T>()`: Deep object cloning via JSON
- `AddQueryParm()`: URI query string building

#### TimeSpan Extensions
- `ToReadableString()`: Human-readable formatting (auto-selects appropriate unit)

#### Uri Extensions
- `GetUrlBase()`: Extract base URL (scheme + authority) from full URL

#### Type Extensions
- `GetCachedAttributes<T>()`: Cached attribute reflection for performance

#### BinaryReader/Writer Extensions
- `ReadDateTimeOffset()` / `WriteDateTimeOffset()`: Binary DateTimeOffset serialization

### Helpers

#### BitHelper
**Package**: CheapHelpers

Bit manipulation and endian conversion utilities.

**Features:**
- `GetBit()`: Extract bit values from byte/short/ushort
- `ConcatBytesToInt()`: Concatenate 1-4 bytes into integer
- Little-endian parsing (Int16, UInt16, Int32, UInt32)
- Bit-level extraction from little-endian values
- Hex string conversions (HexStringToByteArray, ByteArrayToHexString)

#### HashHelper
**Package**: CheapHelpers

Hashing utilities for data integrity and comparison.

**Features:**
- `GetMD5Hash()`: MD5 hashing for strings and byte arrays
- `GetFnvHash()`: FNV-1a 32-bit hashing (fast non-cryptographic)

### Blazor Utilities

#### DownloadHelper
**Package**: CheapHelpers.Blazor

Client-side file downloads with multiple formats.

**Features:**
- HTML-to-image conversion (PNG/JPG via JS interop)
- Base64 image downloads
- File streaming downloads
- Auto-delete after download option

#### ClipboardService
**Package**: CheapHelpers.Blazor

Async clipboard operations via JS interop.

### Blazor Hybrid

**Packages**: CheapHelpers.Blazor + CheapHelpers.MAUI

Complete push notification and WebView bridge solution for Blazor Hybrid apps (MAUI, Photino, Avalonia). Supports iOS APNS, Android FCM, and Desktop Web Push with a unified abstraction layer.

#### Push Notifications

**Architecture:**
- **CheapHelpers.Blazor**: Platform-agnostic abstractions, models, and smart registration manager
- **CheapHelpers.MAUI**: iOS and Android platform-specific implementations

**Core Abstractions:**
- `IDeviceInstallationService`: Platform-specific device registration (APNS/FCM/WebPush)
- `ILocalNotificationService`: Show local notifications when app is in foreground
- `IPushNotificationBackend`: Swappable backend (Azure NH, Firebase, OneSignal, custom API)

**Smart Permission Flow:**
- Checks backend for existing registration before requesting permissions
- Prevents annoying users with unnecessary permission prompts
- Tracks permission denial to never ask again
- Device fingerprinting for persistent identification

**Platform Support:**
- iOS: APNS (Apple Push Notification Service)
- Android: FCM (Firebase Cloud Messaging)
- Desktop: Web Push (Photino, Avalonia)

**Key Features:**
- Unified API across all platforms
- Token refresh handling with events
- Device installation with tags (targeting specific users/groups)
- Multi-device support per user
- Background token waiting with timeout
- Permission status tracking
- Automatic retry on token failure

#### WebView Bridge

**Package**: CheapHelpers.Blazor

Generic JavaScript bridge for extracting data from WebView storage (localStorage, sessionStorage, cookies, DOM).

**Features:**
- Configurable data sources (localStorage, sessionStorage, cookies, DOM elements, URL params, global variables)
- Multi-level JSON escaping handling (WebView-specific)
- Type-safe data extraction
- Automatic JSON parsing with fallback
- Regex key filtering
- Deep object flattening

**Use Cases:**
- Extract authentication tokens from WebView
- Retrieve user data from web applications
- Access session information
- Parse cookies and local storage
- DOM element data extraction

## Installation

### Via NuGet Package Manager

```bash
# Core utilities and extensions
dotnet add package CheapHelpers

# Entity Framework repository pattern
dotnet add package CheapHelpers.EF

# Business services (email, PDF, Azure)
dotnet add package CheapHelpers.Services

# Blazor components and UI utilities
dotnet add package CheapHelpers.Blazor

# Shared models and DTOs
dotnet add package CheapHelpers.Models

# Network scanning and device discovery
dotnet add package CheapHelpers.Networking

# MAUI platform implementations (iOS APNS, Android FCM)
dotnet add package CheapHelpers.MAUI
```

### Via Package Manager Console (Visual Studio)

```powershell
Install-Package CheapHelpers
Install-Package CheapHelpers.EF
Install-Package CheapHelpers.Services
Install-Package CheapHelpers.Blazor
Install-Package CheapHelpers.Models
Install-Package CheapHelpers.Networking
Install-Package CheapHelpers.MAUI
```

### Via Project Reference (for development)

```xml
<ItemGroup>
  <ProjectReference Include="..\CheapHelpers\CheapHelpers\CheapHelpers.csproj" />
</ItemGroup>
```

## Quick Start

### TemporaryFileManager

```csharp
using CheapHelpers.IO;

// Default: uses "CheapHelpers" as application name
using var tempManager = new TemporaryFileManager();

// Or specify custom application name for organization
// using var tempManager = new TemporaryFileManager(applicationName: "MyApp");

var workDir = tempManager.CreateTempDirectory("processing");
var outputFile = tempManager.CreateTempFile("output.mp4");

// Use the paths...
// Cleanup happens automatically on dispose
```

### ProcessExecutor

```csharp
using CheapHelpers.Process;

var result = await ProcessExecutor.ExecuteAsync(
    executable: "ffmpeg",
    arguments: "-i input.mp4 output.mp4",
    options: new ProcessExecutorOptions
    {
        Timeout = TimeSpan.FromMinutes(10),
        ProgressPatterns = ProgressPattern.GetCommonPatterns()
    },
    progress: new Progress<ProcessProgress>(p =>
        Debug.WriteLine($"Progress: {p.Percentage:F2}%")));

if (result.Success)
{
    Debug.WriteLine($"Completed in {result.Duration}");
}
```

### NetworkScanner

```csharp
using CheapHelpers.Networking.Core;
using CheapHelpers.Networking.Extensions;
using Microsoft.Extensions.DependencyInjection;

// Configure services
var services = new ServiceCollection();

// Add network scanning with all detectors
services.AddNetworkScanning(scanner =>
{
    scanner.ScanIntervalMinutes = 5;
    scanner.MaxConcurrentConnections = 20;
})
.AddAllDetectors()  // UPnP, mDNS, HTTP, SSH, Windows Services
.AddJsonStorage();  // Persist discovered devices

// Add logging
services.AddLogging(builder => builder.AddConsole());

var serviceProvider = services.BuildServiceProvider();
var scanner = serviceProvider.GetRequiredService<INetworkScanner>();

// Start background scanning
scanner.Start();

// Listen for device discovery events
scanner.DeviceDiscovered += (device) =>
{
    Console.WriteLine($"Found: {device.Name} ({device.IPv4Address}) - {device.Type}");
};

// Trigger manual scan
await scanner.ScanAsync();

// Get all discovered devices
var devices = await scanner.GetAllDevicesAsync();
foreach (var device in devices)
{
    Console.WriteLine($"{device.Name}: {device.MacAddress}");
}

// Custom detector configuration
services.AddNetworkScanning()
    .AddUpnpDetector()      // Only UPnP/SSDP
    .AddMdnsDetector()      // Only mDNS/Zeroconf
    .AddDefaultDetectors(); // HTTP, SSH, Windows Services
```

### Memory Caching

```csharp
using CheapHelpers.Caching;

// Absolute expiration - items expire exactly 1 hour after creation
using var userCache = new AbsoluteExpirationCache<User>("UserCache", TimeSpan.FromHours(1));

// Fetch user or get from cache
var user = await userCache.GetOrAddAsync("user:123", async key =>
    await database.GetUserAsync(key));

// Sliding expiration - items expire after 30 minutes of inactivity
using var sessionCache = new SlidingExpirationCache<Session>("SessionCache", TimeSpan.FromMinutes(30));
var session = sessionCache.GetOrAdd("session:abc", key => CreateNewSession(key));

// Flexible - max 24 hours absolute, but extends on access (1 hour sliding)
using var apiCache = new FlexibleExpirationCache<ApiResponse>(
    cacheName: "ApiCache",
    absoluteExpiration: TimeSpan.FromHours(24),
    slidingExpiration: TimeSpan.FromHours(1));

var apiData = await apiCache.GetOrAddAsync("endpoint:/users", async key =>
{
    var response = await httpClient.GetAsync(key);
    return await response.Content.ReadFromJsonAsync<ApiResponse>();
});

// Manual operations
apiCache.Set("key", value);
if (apiCache.TryGet("key", out var cachedValue))
{
    Debug.WriteLine($"Found: {cachedValue}");
}
apiCache.Remove("key");
apiCache.Clear(); // Remove all items
```

### XmlService

```csharp
using CheapHelpers.Services.DataExchange.Xml;

// Strongly-typed serialization
var myObject = new MyModel { Name = "Test", Value = 123 };
await xmlService.SerializeAsync("output.xml", myObject);

// Deserialization
var loaded = await xmlService.DeserializeAsync<MyModel>("output.xml");
```

### EncryptionHelper

```csharp
using CheapHelpers.Helpers.Encryption;

// Encrypt sensitive data (machine-specific key)
string encrypted = EncryptionHelper.Encrypt("sensitive data");

// Decrypt
string decrypted = EncryptionHelper.Decrypt(encrypted);

// Enhanced security with random IV per operation
string encryptedSecure = EncryptionHelper.EncryptWithRandomIV("top secret");
string decryptedSecure = EncryptionHelper.DecryptWithRandomIV(encryptedSecure);
```

### BaseRepo

```csharp
using CheapHelpers.EF.Repositories;

public class ProductRepo : BaseRepo<Product, MyDbContext>
{
    public ProductRepo(MyDbContext context) : base(context) { }
}

// Usage
var products = await productRepo.GetAllPaginatedAsync(pageIndex: 1, pageSize: 20);
var activeProducts = await productRepo.GetWhereAsync(p => p.IsActive);
await productRepo.AddAsync(newProduct);
```

### FileHelper - Date-Based Filenames

```csharp
using CheapHelpers.Helpers.Files;

// Generate secure filenames with timestamps
var daily = FileHelper.GetDailyFilename(DateTime.Now, "backup.dat");
// Returns: "backup_2025-01-15_a1b2c3d4.dat"

var weekly = FileHelper.GetWeeklyFilename(DateTime.Now, "report.csv");
// Returns: "report_2025-w3_a1b2c3d4.csv"

var monthly = FileHelper.GetMonthlyFilename(DateTime.Now, "summary.json");
// Returns: "summary_2025-01_a1b2c3d4.json"

// Custom date format
var custom = FileHelper.GetCustomDateFilename(DateTime.Now, "log.txt", "yyyyMMdd-HHmmss");
// Returns: "log_20250115-143022_a1b2c3d4.txt"

// Enhanced GetTrustedFileName with optional timestamp
var trusted = FileHelper.GetTrustedFileName("document.pdf", DateTime.Now, "yyyy-MM-dd");
// Returns: "document_2025-01-15_a1b2c3d4.pdf"
```

### String Extensions

```csharp
using CheapHelpers.Extensions;

"hello world".Capitalize();  // "Hello world"
"0474123456".ToInternationalPhoneNumber("BE");  // "+32474123456"
"Very long text...".ToShortString(10);  // "Very lo..."

// New sanitization methods
"Hello@World!".RemoveSpecialCharacters();  // "HelloWorld"
"Hello--World".RemoveSpecialCharactersKeepDash();  // "Hello-World"
"My File/Name".Sanitize();  // "My_File-Name"
"Very long text here".TrimWithEllipsis(10);  // "Very long ..."
```

### DateTimeOffset Extensions

```csharp
using CheapHelpers.Extensions;

var timestamp = DateTimeOffset.Now;

// Round to intervals
timestamp.Floor(TimeSpan.FromMinutes(15));    // Round down to 15-min intervals
timestamp.Round(TimeSpan.FromMinutes(15));    // Round to nearest 15-min
timestamp.Ceiling(TimeSpan.FromMinutes(15));  // Round up to 15-min intervals

// Truncate to specific precision
timestamp.PerMinute();  // Zero out seconds
timestamp.PerHour();    // Zero out minutes and seconds
timestamp.PerDay();     // Zero out time component
```

### Collection Extensions - Dynamic Ordering

```csharp
using CheapHelpers.Extensions;
using System.Linq;

var products = dbContext.Products.AsQueryable();

// Dynamic ordering by property name
var sorted = products.OrderByDynamic("Name");
var sortedDesc = products.OrderByDescendingDynamic("Price");
```

### Blazor Hybrid - Push Notifications (MAUI)

```csharp
using CheapHelpers.Blazor.Hybrid.Extensions;
using CheapHelpers.MAUI.Extensions;

// In MauiProgram.cs
var builder = MauiApp.CreateBuilder();

// Add Blazor Hybrid push notification abstractions
builder.Services.AddBlazorHybridPushNotifications(options =>
{
    options.CheckBackendBeforeRequestingPermissions = true;
    options.AutoRegisterOnStartup = false;
});

// Add platform-specific implementations (iOS APNS, Android FCM)
builder.Services.AddMauiPushNotifications();

// Register your backend implementation
builder.Services.AddSingleton<IPushNotificationBackend, YourBackendService>();

var app = builder.Build();

// In your Blazor component or service
@inject DeviceRegistrationManager RegistrationManager
@inject IDeviceInstallationService DeviceService

// Smart permission flow - checks backend before requesting permissions
var status = await RegistrationManager.CheckDeviceStatusAsync(userId);

if (status == DeviceRegistrationState.NotRegistered)
{
    // Only request permissions if device is not registered
    var registered = await RegistrationManager.RegisterDeviceAsync(userId);

    if (registered)
    {
        Console.WriteLine($"Device registered with token: {DeviceService.Token}");
    }
}
else if (status == DeviceRegistrationState.Registered)
{
    Console.WriteLine("Device already registered - no permission prompt needed");
}
else if (status == DeviceRegistrationState.PermissionDenied)
{
    Console.WriteLine("User previously denied permissions");
}

// Listen for notification events
DeviceService.TokenRefreshed += (token) =>
{
    Console.WriteLine($"Token refreshed: {token}");
};
```

#### iOS APNS Setup

```csharp
// In Platforms/iOS/AppDelegate.cs
using CheapHelpers.MAUI.Platforms.iOS;

[Register("AppDelegate")]
public class AppDelegate : ApnsDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    // Optional: Handle notification received events
    protected override void OnNotificationReceived(string title, string body, Dictionary<string, string> data)
    {
        Console.WriteLine($"Notification received: {title}");
    }
}

// In Info.plist, ensure you have:
// <key>UIBackgroundModes</key>
// <array>
//   <string>remote-notification</string>
// </array>
```

#### Android FCM Setup

```csharp
// In Platforms/Android/MainApplication.cs
using CheapHelpers.MAUI.Platforms.Android;

[Application]
public class MainApplication : MauiApplication
{
    public override void OnCreate()
    {
        base.OnCreate();

        // Initialize Firebase
        FirebaseInitializer.Initialize(this);

        if (FirebaseInitializer.IsFirebaseAvailable)
        {
            Console.WriteLine("Firebase initialized successfully");
        }
    }
}

// In Platforms/Android/FcmService.cs
using CheapHelpers.MAUI.Platforms.Android;

[Service(Exported = true)]
[IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
public class MyFcmService : FcmService
{
    // Optional: Handle notification received events
    protected override void OnNotificationReceived(string title, string body, Dictionary<string, string> data)
    {
        Console.WriteLine($"FCM notification received: {title}");
    }
}

// Ensure google-services.json is in your Android project with Build Action: GoogleServicesJson
```

### Blazor Hybrid - WebView Bridge

```csharp
// In your Blazor component
@inject IJSRuntime JS

// Configure the bridge
var bridge = await JS.InvokeAsync<IJSObjectReference>(
    "createWebViewBridge",
    new
    {
        sources = new[]
        {
            "localStorage",
            "sessionStorage",
            "cookies"
        },
        keyFilter = "^(auth|user|session).*", // Regex filter for keys
        includeUrl = true,
        includeDOM = new[] { "#user-data", ".auth-token" }
    });

// Extract data from WebView
var extractedData = await bridge.InvokeAsync<Dictionary<string, object>>("extractData");

// Parse authentication data
if (extractedData.TryGetValue("authToken", out var tokenObj))
{
    var token = tokenObj.ToString();
    Console.WriteLine($"Auth token: {token}");
}

// Handle WebView JSON escaping
using CheapHelpers.Blazor.Hybrid.WebView;

var webViewJson = "\\\"escaped\\\\json\\\\data\\\"";
var clean = WebViewJsonParser.UnescapeWebViewJson(webViewJson);
var parsed = JsonSerializer.Deserialize<MyModel>(clean);
```

### Blazor Hybrid - Custom Backend Implementation

```csharp
using CheapHelpers.Blazor.Hybrid.Abstractions;
using CheapHelpers.Blazor.Hybrid.Models;

public class AzureNotificationHubBackend : IPushNotificationBackend
{
    private readonly NotificationHubClient _hubClient;

    public async Task<bool> RegisterDeviceAsync(DeviceInstallation device)
    {
        var installation = new Installation
        {
            InstallationId = device.InstallationId,
            Platform = device.Platform switch
            {
                "apns" => NotificationPlatform.Apns,
                "fcmv1" => NotificationPlatform.FcmV1,
                _ => throw new ArgumentException("Unknown platform")
            },
            PushChannel = device.PushChannel,
            Tags = device.Tags
        };

        await _hubClient.CreateOrUpdateInstallationAsync(installation);
        return true;
    }

    public async Task<SendNotificationResult> SendNotificationAsync(NotificationPayload payload)
    {
        var notification = new Dictionary<string, string>
        {
            { "title", payload.Title },
            { "body", payload.Body }
        };

        if (payload.Data != null)
        {
            foreach (var kvp in payload.Data)
                notification[kvp.Key] = kvp.Value;
        }

        var outcome = payload.Tags?.Any() == true
            ? await _hubClient.SendTemplateNotificationAsync(notification, payload.Tags)
            : await _hubClient.SendTemplateNotificationAsync(notification);

        return new SendNotificationResult
        {
            Success = outcome.State == NotificationOutcomeState.Completed,
            MessageId = outcome.NotificationId
        };
    }

    // Implement other interface members...
}
```

## Requirements

- .NET 10.0 or later
- C# 14.0 or later

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

### Building from Source

```bash
git clone https://github.com/CheapNud/CheapHelpers.git
cd CheapHelpers
dotnet restore
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Creating a Release

Releases are automatically published to NuGet.org when a version tag is pushed:

```bash
git tag v1.0.0
git push origin v1.0.0
```

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.