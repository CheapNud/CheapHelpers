# CheapHelpers

A collection of production-ready C# utilities, extensions, and services for .NET 10.0 development. Simplify common development tasks with battle-tested helpers for Blazor, Entity Framework, networking, email, PDF generation, and more.

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

## Installation

```bash
# Core utilities and extensions
dotnet add package CheapHelpers

# Entity Framework repository pattern
dotnet add package CheapHelpers.EF

# Business services (email, PDF, Azure)
dotnet add package CheapHelpers.Services

# Blazor components and Hybrid features
dotnet add package CheapHelpers.Blazor

# Network scanning and device discovery
dotnet add package CheapHelpers.Networking

# MAUI platform implementations
dotnet add package CheapHelpers.MAUI
```

## Quick Start

### String Extensions
```csharp
using CheapHelpers.Extensions;

"hello world".Capitalize();  // "Hello world"
"0474123456".ToInternationalPhoneNumber("BE");  // "+32474123456"
"Very long text...".ToShortString(10);  // "Very lo..."
```

### Memory Caching
```csharp
using CheapHelpers.Caching;

// Sliding expiration - items expire after 30 minutes of inactivity
using var cache = new SlidingExpirationCache<User>("UserCache", TimeSpan.FromMinutes(30));
var user = await cache.GetOrAddAsync("user:123", async key => await database.GetUserAsync(key));
```

### Entity Framework Repository
```csharp
using CheapHelpers.EF.Repositories;

public class ProductRepo : BaseRepo<Product, MyDbContext>
{
    public ProductRepo(MyDbContext context) : base(context) { }
}

var products = await productRepo.GetAllPaginatedAsync(pageIndex: 1, pageSize: 20);
```

### Network Scanning
```csharp
using CheapHelpers.Networking.Extensions;

services.AddNetworkScanning()
    .AddAllDetectors()  // UPnP, mDNS, HTTP, SSH, Windows Services
    .AddJsonStorage();

var scanner = serviceProvider.GetRequiredService<INetworkScanner>();
scanner.Start();  // Background scanning

scanner.DeviceDiscovered += (device) =>
{
    Console.WriteLine($"Found: {device.Name} ({device.IPv4Address}) - {device.Type}");
};
```

### Email with Templates
```csharp
using CheapHelpers.Services.Email;

services.AddEmailService(options =>
{
    options.SmtpServer = "smtp.gmail.com";
    options.SmtpPort = 587;
});

await emailService.SendEmailAsync(
    recipient: "user@example.com",
    subject: "Welcome!",
    body: "<h1>Welcome to our app!</h1>"
);
```

### Status Bar Configuration (MAUI)
```csharp
// In MauiProgram.cs - ONE LINE configuration!
builder.UseMauiApp<App>()
       .UseTransparentStatusBar(StatusBarStyle.DarkContent);

// Or from any page/code
StatusBarHelper.ConfigureForLightBackground(); // Dark icons for light theme
StatusBarHelper.ConfigureForDarkBackground();  // Light icons for dark theme

// Get status bar height for layouts
var height = StatusBarHelper.GetStatusBarHeight();
```

### Blazor Hybrid Push Notifications (MAUI)
```csharp
// In MauiProgram.cs
builder.Services.AddBlazorHybridPushNotifications();
builder.Services.AddMauiPushNotifications();  // iOS APNS + Android FCM

// In your Blazor component
var status = await RegistrationManager.CheckDeviceStatusAsync(userId);
if (status == DeviceRegistrationState.NotRegistered)
{
    await RegistrationManager.RegisterDeviceAsync(userId);
}
```

## Documentation

Detailed documentation for each package:

### Core Package
- [String Extensions](Docs/Core/StringExtensions.md) - Capitalize, sanitize, phone numbers, truncation
- [DateTime Extensions](Docs/Core/DateTimeExtensions.md) - Timezone conversion, business days, rounding
- [Collection Extensions](Docs/Core/CollectionExtensions.md) - Dynamic ordering, replacements, bindings
- [Caching](Docs/Core/Caching.md) - Memory cache with flexible expiration strategies
- [Encryption](Docs/Core/Encryption.md) - Machine-specific AES-256 encryption
- [File Helpers](Docs/Core/FileHelpers.md) - Secure filename generation, date-based naming
- [Process Execution](Docs/Core/ProcessExecution.md) - Process executor with progress tracking

### Entity Framework
- [Repository Pattern](Docs/EF/Repository.md) - BaseRepo with CRUD operations and pagination
- [Context Extensions](Docs/EF/ContextExtensions.md) - Bulk operations and utilities

### Services
- [Email Service](Docs/Services/Email.md) - SMTP with Fluid templates and attachments
- [PDF Services](Docs/Services/PDF.md) - PDF generation and optimization
- [XML Service](Docs/Services/XML.md) - Dynamic and strongly-typed serialization
- [Azure Integration](Docs/Services/Azure.md) - Translation, Vision, Document services

### Blazor
- [Components](Docs/Blazor/Components.md) - UI components and utilities
- [Hybrid Features](Docs/Blazor/Hybrid.md) - WebView bridge and push notification abstractions
- [Download Helper](Docs/Blazor/DownloadHelper.md) - Client-side file downloads
- [Clipboard Service](Docs/Blazor/ClipboardService.md) - Async clipboard operations

### Networking
- [Network Scanner](Docs/Networking/Scanner.md) - Device discovery and scanning
- [Device Detectors](Docs/Networking/Detectors.md) - UPnP, mDNS, HTTP, SSH detection
- [MAC Address Resolution](Docs/Networking/MACResolution.md) - Cross-platform MAC lookup

### MAUI
- [Status Bar Configuration](Docs/MAUI/StatusBarConfiguration.md) - **NEW!** Cross-platform status bar with zero native code
- [Push Notifications](Docs/MAUI/PushNotifications.md) - iOS APNS and Android FCM setup
- [Device Installation](Docs/MAUI/DeviceInstallation.md) - Device registration and management
- [Local Notifications](Docs/MAUI/LocalNotifications.md) - Foreground notification display
- [Android System Bars](Docs/MAUI/AndroidSystemBars.md) - Android-specific system bar configuration and edge-to-edge layouts

## Requirements

- .NET 10.0 or later
- C# 14.0 or later

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

```bash
git clone https://github.com/CheapNud/CheapHelpers.git
cd CheapHelpers
dotnet restore
dotnet build
```

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.
