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

## Projects

This repository contains multiple NuGet packages, each with its own documentation:

### [CheapHelpers.Blazor](CheapHelpers.Blazor/README.md)
Blazor and Blazor Hybrid utilities including app bar components, WebView helpers, push notification abstractions, and file download utilities.

**Key Features:**
- App Bar Component with programmatic control
- Blazor Hybrid integration (MAUI, Photino, Avalonia)
- Push notification abstractions (platform-agnostic)
- WebView JSON parser and bridge
- Download helper and clipboard service

**Documentation:** [CheapHelpers.Blazor/Docs](CheapHelpers.Blazor/Docs/)

### [CheapHelpers.MAUI](CheapHelpers.MAUI/README.md)
MAUI platform helpers for iOS and Android including status bar configuration, system UI helpers, and push notification implementations.

**Key Features:**
- Cross-platform status bar configuration
- Android system bars (status bar + navigation bar) with edge-to-edge support
- iOS APNS and Android FCM push notification implementations
- Firebase token helper with safe retrieval
- Device installation service for backend registration

**Documentation:** [CheapHelpers.MAUI/Docs](CheapHelpers.MAUI/Docs/)

### [CheapHelpers](Docs/Core/README.md)
Core utilities, extensions, and helpers for everyday .NET development.

**Key Features:**
- String extensions (capitalize, sanitize, phone numbers, truncation)
- DateTime extensions (timezone conversion, business days, rounding)
- Collection extensions (dynamic ordering, replacements, bindings)
- Memory caching with flexible expiration strategies
- Encryption helpers (machine-specific AES-256)
- File helpers (secure filename generation, date-based naming)
- Process execution with progress tracking

**Documentation:** [Docs/Core](Docs/Core/)

### [CheapHelpers.EF](Docs/EF/)
Entity Framework repository pattern and extensions.

**Key Features:**
- BaseRepo with CRUD operations and pagination
- Context extensions for bulk operations
- PaginatedList helper

**Documentation:** [Docs/EF](Docs/EF/)

### [CheapHelpers.Services](Docs/Services/README.md)
Business services and integrations for common development tasks.

**Key Features:**
- Email service with SMTP and Fluid templates
- PDF generation and optimization services
- XML serialization (dynamic and strongly-typed)
- Azure integration (Translation, Vision, Document services)

**Documentation:** [Docs/Services](Docs/Services/)

### [CheapHelpers.Networking](Docs/Networking/)
Network scanning and device discovery utilities.

**Key Features:**
- Network scanner with background discovery
- Device detectors (UPnP, mDNS, HTTP, SSH)
- MAC address resolution (cross-platform)

**Documentation:** [Docs/Networking](Docs/Networking/)

## Architecture

See [Docs/ARCHITECTURE.md](Docs/ARCHITECTURE.md) for overall solution architecture and separation of concerns.

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
