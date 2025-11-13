# CheapHelpers.Blazor

Blazor and Blazor Hybrid utilities for building modern web and MAUI applications with enhanced UI components, clipboard operations, file downloads, and push notification abstractions.

## Installation

```bash
dotnet add package CheapHelpers.Blazor
```

## Features

### Hybrid Features
- **[Blazor Hybrid Integration](Docs/Hybrid/Hybrid.md)** - Complete guide for MAUI, Photino, and Avalonia integration
- **[App Bar Component](Docs/Hybrid/AppBar.md)** - Customizable app bar with RenderFragment support and automatic status bar adjustment
- **Push Notification Abstractions** - Platform-agnostic push notification management with smart permission flow
- **WebView JSON Parser** - Handle WebView JSON escaping quirks automatically
- **Device Registration Manager** - Intelligent device registration with backend checks

### UI Components
- **[Components](Docs/Components.md)** - Base classes and reusable UI components
- **Progress indicators and spinners**
- **Responsive layouts with Material Design principles**

### Utilities
- **[Download Helper](Docs/DownloadHelper.md)** - Client-side file downloads for Blazor WebAssembly
- **[Clipboard Service](Docs/ClipboardService.md)** - Asynchronous clipboard operations

## Quick Start

### App Bar with Actions

```razor
@using CheapHelpers.Blazor.Hybrid.Components

<AppBar Title="My Application">
    <Actions>
        <button @onclick="OnSearch">
            <i class="fas fa-search"></i>
        </button>
        <button @onclick="OnMenu">
            <i class="fas fa-bars"></i>
        </button>
    </Actions>
</AppBar>

<div class="cheap-appbar-offset">
    <!-- Your page content -->
</div>

@code {
    private void OnSearch() { /* Search logic */ }
    private void OnMenu() { /* Menu logic */ }
}
```

### Programmatic App Bar Control

```razor
@inject IAppBarService AppBarService

@code {
    protected override void OnInitialized()
    {
        // Set title dynamically
        AppBarService.SetTitle("Dynamic Page");

        // Change colors
        AppBarService.SetBackgroundColor("#03DAC6");
        AppBarService.SetTextColor("#000000");

        // Show/hide
        AppBarService.SetVisible(true);
    }
}
```

### Push Notifications (Blazor Hybrid)

```csharp
// In MauiProgram.cs
builder.Services.AddBlazorHybridPushNotifications(options =>
{
    options.SmartPermissionFlow = true;
    options.ForegroundNotifications = true;
});
```

```razor
@inject DeviceRegistrationManager RegistrationManager

@code {
    private async Task EnableNotifications()
    {
        var status = await RegistrationManager.CheckDeviceStatusAsync(userId);

        if (status == DeviceRegistrationState.NotRegistered)
        {
            await RegistrationManager.RegisterDeviceIfNeededAsync(userId);
        }
    }
}
```

### File Download

```razor
@inject IDownloadHelper DownloadHelper

@code {
    private async Task DownloadFile()
    {
        var fileData = await GenerateReport();
        await DownloadHelper.DownloadFileAsync(fileData, "report.pdf", "application/pdf");
    }
}
```

### Clipboard Operations

```razor
@inject IClipboardService ClipboardService

@code {
    private async Task CopyToClipboard()
    {
        await ClipboardService.CopyToClipboardAsync("Text to copy");
    }

    private async Task PasteFromClipboard()
    {
        var text = await ClipboardService.ReadFromClipboardAsync();
        Console.WriteLine($"Pasted: {text}");
    }
}
```

## Documentation

### Hybrid Features
- [Blazor Hybrid Integration](Docs/Hybrid/Hybrid.md) - Platform integration guide
- [App Bar Component](Docs/Hybrid/AppBar.md) - Complete app bar documentation

### Components & Services
- [UI Components](Docs/Components.md) - Component library reference
- [Download Helper](Docs/DownloadHelper.md) - File download utilities
- [Clipboard Service](Docs/ClipboardService.md) - Clipboard API wrapper

## Platform Support

- **Blazor WebAssembly** - Full support for web-based components
- **Blazor Server** - Full support for server-side rendering
- **Blazor Hybrid (MAUI)** - Complete integration with platform features
- **Photino.NET** - Desktop application support
- **Avalonia** - Cross-platform desktop support

## Requirements

- .NET 10.0 or later
- For Blazor Hybrid: MAUI, Photino, or Avalonia host application

## Related Packages

- [CheapHelpers.MAUI](../CheapHelpers.MAUI/README.md) - MAUI platform implementations
- [CheapHelpers](../CheapHelpers/README.md) - Core utilities and extensions
- [CheapHelpers.Services](../CheapHelpers.Services/README.md) - Business services

## License

MIT License - See [LICENSE.txt](../LICENSE.txt) for details
