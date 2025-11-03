# NetworkScanner

The `NetworkScanner` is the core component for discovering and monitoring devices on your network. It provides automated device discovery, background scanning capabilities, and real-time event notifications.

## Overview

The scanner performs ICMP ping sweeps across specified subnets, resolves hostnames and MAC addresses, and uses configurable detectors to identify device types. It supports both one-time scans and continuous background monitoring.

### Key Features

- Subnet-based device discovery with configurable IP ranges
- Concurrent scanning with throttling to prevent network flooding
- Automatic hostname resolution via DNS
- Cross-platform MAC address resolution
- Device type detection using pluggable detector architecture
- Background scanning with configurable intervals
- Real-time progress and state change events
- Thread-safe device list management

## Dependency Injection Setup

### Basic Setup

```csharp
using CheapHelpers.Networking.Extensions;

services.AddNetworkScanning();
services.AddDefaultDetectors(); // HTTP, SSH, Windows Services
```

### Full Setup with All Features

```csharp
services.AddNetworkScanning(scanner =>
{
    scanner.SubnetBase = "auto"; // Auto-detect local subnet
    scanner.StartIp = 1;
    scanner.EndIp = 254;
    scanner.ScanIntervalMinutes = 5;
    scanner.MaxConcurrentConnections = 20;
    scanner.PingTimeoutMs = 2000;
    scanner.EnableContinuousScanning = true;
    scanner.NetworkThrottleDelayMs = 50;
    scanner.DevicesBeforeThrottle = 10;
},
ports =>
{
    ports.HttpTimeoutMs = 2000;
    ports.SshTimeoutMs = 2000;
    ports.PortConnectionTimeoutMs = 1000;
    ports.CustomIoTPorts = [5000, 8000, 8080, 8443];
    ports.ServiceEndpoints = new()
    {
        { 8974, "Custom IoT Device" },
        { 8975, "Automation Hub" }
    };
});

// Add all detectors for maximum discovery
services.AddAllDetectors();

// Optional: Add JSON storage for device persistence
services.AddJsonStorage("MyNetworkApp");
```

### Available Detector Options

```csharp
// Add only default detectors (lighter weight)
services.AddDefaultDetectors();

// Add only enhanced detectors (UPnP/mDNS)
services.AddEnhancedDetectors();

// Add all detectors
services.AddAllDetectors();

// Add individual detectors
services.AddUpnpDetector();
services.AddMdnsDetector();

// Add custom detector
services.AddCustomDetector<MyCustomDetector>();
```

## Configuration Options

### NetworkScannerOptions

| Property | Default | Description |
|----------|---------|-------------|
| `SubnetBase` | `"auto"` | Subnet to scan (e.g., "192.168.1") or "auto" to detect |
| `StartIp` | `1` | Starting IP address (last octet) |
| `EndIp` | `254` | Ending IP address (last octet) |
| `ScanIntervalMinutes` | `5` | Minutes between automatic scans |
| `MaxConcurrentConnections` | `20` | Maximum parallel connections |
| `PingTimeoutMs` | `2000` | ICMP ping timeout |
| `HttpTimeoutMs` | `2000` | HTTP detection timeout |
| `SshTimeoutMs` | `2000` | SSH detection timeout |
| `NetworkThrottleDelayMs` | `50` | Delay after N devices to prevent flooding |
| `DevicesBeforeThrottle` | `10` | Number of devices before throttle delay |
| `EnableContinuousScanning` | `true` | Enable automatic background scanning |

## Basic Usage

### Injecting the Scanner

```csharp
public class NetworkMonitorService
{
    private readonly INetworkScanner _scanner;
    private readonly ILogger<NetworkMonitorService> _logger;

    public NetworkMonitorService(
        INetworkScanner scanner,
        ILogger<NetworkMonitorService> logger)
    {
        _scanner = scanner;
        _logger = logger;
    }
}
```

### One-Time Network Scan

```csharp
public async Task<List<NetworkDevice>> DiscoverDevicesAsync()
{
    var devices = await _scanner.ScanNetworkAsync();

    foreach (var device in devices.Where(d => d.IsOnline))
    {
        _logger.LogInformation(
            "Found: {Name} at {IP} - {Type} (MAC: {Mac}, RTT: {ResponseTime}ms)",
            device.Name,
            device.IPv4Address,
            device.Type,
            device.MacAddress,
            device.ResponseTime.TotalMilliseconds);
    }

    return devices;
}
```

### Scanning a Single Device

```csharp
public async Task<NetworkDevice?> CheckDeviceAsync(string ipAddress)
{
    var devices = await _scanner.ScanSingleDeviceAsync(ipAddress);
    var device = devices.FirstOrDefault();

    if (device?.IsOnline == true)
    {
        _logger.LogInformation("Device is online: {Name} - {Type}",
            device.Name, device.Type);
        return device;
    }

    _logger.LogWarning("Device at {IP} is not responding", ipAddress);
    return null;
}
```

## Background Scanning

### Starting Continuous Scanning

```csharp
public void StartMonitoring()
{
    // Subscribe to events first
    _scanner.DeviceDiscovered += OnDeviceDiscovered;
    _scanner.ScanProgress += OnScanProgress;
    _scanner.ScanningStateChanged += OnScanningStateChanged;
    _scanner.NextScanTimeChanged += OnNextScanTimeChanged;
    _scanner.LastScanTimeChanged += OnLastScanTimeChanged;

    // Start continuous scanning
    _scanner.StartScanning();

    _logger.LogInformation("Background scanning started");
}

public void StopMonitoring()
{
    _scanner.PauseScanning();

    // Unsubscribe from events
    _scanner.DeviceDiscovered -= OnDeviceDiscovered;
    _scanner.ScanProgress -= OnScanProgress;
    _scanner.ScanningStateChanged -= OnScanningStateChanged;
    _scanner.NextScanTimeChanged -= OnNextScanTimeChanged;
    _scanner.LastScanTimeChanged -= OnLastScanTimeChanged;

    _logger.LogInformation("Background scanning stopped");
}

public void ToggleScanning(bool shouldScan)
{
    if (shouldScan)
        _scanner.ResumeScanning();
    else
        _scanner.PauseScanning();
}
```

## Event Handling

### Device Discovery Events

```csharp
private void OnDeviceDiscovered(NetworkDevice device)
{
    if (device.IsOnline)
    {
        _logger.LogInformation(
            "Device online: {Name} ({IP}) - {Type}",
            device.Name, device.IPv4Address, device.Type);
    }
    else
    {
        _logger.LogWarning(
            "Device offline: {Name} ({IP})",
            device.Name, device.IPv4Address);
    }

    // Update UI or trigger notifications
    NotifyDeviceStatusChanged(device);
}
```

### Progress Events

```csharp
private void OnScanProgress(string message)
{
    _logger.LogDebug("Scan progress: {Message}", message);
    // Update UI progress indicator
}

private void OnScanningStateChanged(bool isScanning)
{
    if (isScanning)
        _logger.LogInformation("Scan started");
    else
        _logger.LogInformation("Scan completed");
}

private void OnNextScanTimeChanged(DateTime? nextScanTime)
{
    if (nextScanTime.HasValue)
    {
        var timeUntilScan = nextScanTime.Value - DateTime.Now;
        _logger.LogDebug("Next scan in {Minutes} minutes",
            timeUntilScan.TotalMinutes);
    }
}

private void OnLastScanTimeChanged(DateTime? lastScanTime)
{
    if (lastScanTime.HasValue)
    {
        _logger.LogInformation("Last scan completed at {Time}",
            lastScanTime.Value);
    }
}
```

## Advanced Scenarios

### Device Filtering and Grouping

```csharp
public Dictionary<string, List<NetworkDevice>> GroupDevicesByType()
{
    var devices = _scanner.DiscoveredDevices;

    return devices
        .Where(d => d.IsOnline)
        .GroupBy(d => d.Type)
        .ToDictionary(g => g.Key, g => g.ToList());
}

public List<NetworkDevice> GetWindowsDevices()
{
    return _scanner.DiscoveredDevices
        .Where(d => d.IsOnline && d.Type.Contains("Windows"))
        .ToList();
}

public List<NetworkDevice> GetIoTDevices()
{
    return _scanner.DiscoveredDevices
        .Where(d => d.IsOnline &&
            (d.Type.Contains("IoT") ||
             d.Type.Contains("UPnP") ||
             d.Type.Contains("mDNS")))
        .ToList();
}
```

### Device Status Monitoring

```csharp
public async Task<DeviceStatusReport> GenerateStatusReportAsync()
{
    var devices = _scanner.DiscoveredDevices;

    return new DeviceStatusReport
    {
        TotalDevices = devices.Count,
        OnlineDevices = devices.Count(d => d.IsOnline),
        OfflineDevices = devices.Count(d => !d.IsOnline),
        DevicesByType = devices
            .Where(d => d.IsOnline)
            .GroupBy(d => d.Type)
            .ToDictionary(g => g.Key, g => g.Count()),
        LastScanTime = _scanner.LastScanTime,
        NextScanTime = _scanner.NextScanTime,
        IsScanning = _scanner.IsScanning
    };
}
```

### Custom Subnet Scanning

```csharp
// Create custom subnet provider
public class MultiSubnetProvider : ISubnetProvider
{
    public Task<List<string>> GetSubnetsToScanAsync()
    {
        return Task.FromResult(new List<string>
        {
            "192.168.1",
            "192.168.2",
            "10.0.0"
        });
    }
}

// Register in DI
services.AddNetworkScanning();
services.AddCustomSubnetProvider<MultiSubnetProvider>();
```

## Blazor Integration

### Blazor Server Component

```csharp
@page "/network-monitor"
@inject INetworkScanner Scanner
@implements IDisposable

<MudPaper Class="pa-4">
    <MudStack Spacing="3">
        <MudText Typo="Typo.h5">Network Devices</MudText>

        <MudStack Row="true" Spacing="2">
            <MudButton OnClick="StartScanning"
                       Disabled="Scanner.IsScanning"
                       Color="Color.Primary">
                Start Scanning
            </MudButton>
            <MudButton OnClick="PauseScanning"
                       Disabled="!Scanner.IsScanning"
                       Color="Color.Warning">
                Pause Scanning
            </MudButton>
        </MudStack>

        @if (Scanner.IsScanning)
        {
            <MudProgressLinear Indeterminate="true" Color="Color.Primary" />
            <MudText Typo="Typo.body2">@_scanProgress</MudText>
        }

        @if (Scanner.NextScanTime.HasValue)
        {
            <MudText Typo="Typo.body2">
                Next scan: @GetTimeUntilNextScan()
            </MudText>
        }

        <MudTable T="NetworkDevice" Items="Scanner.DiscoveredDevices" Dense="true">
            <HeaderContent>
                <MudTh>Status</MudTh>
                <MudTh>Name</MudTh>
                <MudTh>IP Address</MudTh>
                <MudTh>Type</MudTh>
                <MudTh>MAC Address</MudTh>
                <MudTh>Response Time</MudTh>
            </HeaderContent>
            <RowTemplate>
                <MudTd>
                    <MudChip T="string" Size="Size.Small"
                             Color="@(context.IsOnline ? Color.Success : Color.Error)">
                        @(context.IsOnline ? "Online" : "Offline")
                    </MudChip>
                </MudTd>
                <MudTd>@context.Name</MudTd>
                <MudTd>@context.IPv4Address</MudTd>
                <MudTd>@context.Type</MudTd>
                <MudTd>@context.MacAddress</MudTd>
                <MudTd>@(context.IsOnline ? $"{context.ResponseTime.TotalMilliseconds:F0} ms" : "-")</MudTd>
            </RowTemplate>
        </MudTable>
    </MudStack>
</MudPaper>

@code {
    private string _scanProgress = string.Empty;

    protected override void OnInitialized()
    {
        Scanner.DeviceDiscovered += OnDeviceDiscovered;
        Scanner.ScanProgress += OnScanProgress;
        Scanner.ScanningStateChanged += OnScanningStateChanged;
        Scanner.NextScanTimeChanged += OnNextScanTimeChanged;
    }

    private void StartScanning()
    {
        Scanner.StartScanning();
    }

    private void PauseScanning()
    {
        Scanner.PauseScanning();
    }

    private void OnDeviceDiscovered(NetworkDevice device)
    {
        InvokeAsync(StateHasChanged);
    }

    private void OnScanProgress(string message)
    {
        _scanProgress = message;
        InvokeAsync(StateHasChanged);
    }

    private void OnScanningStateChanged(bool isScanning)
    {
        InvokeAsync(StateHasChanged);
    }

    private void OnNextScanTimeChanged(DateTime? nextScanTime)
    {
        InvokeAsync(StateHasChanged);
    }

    private string GetTimeUntilNextScan()
    {
        if (!Scanner.NextScanTime.HasValue)
            return "-";

        var timeUntil = Scanner.NextScanTime.Value - DateTime.Now;
        return timeUntil.TotalSeconds > 0
            ? $"{timeUntil.TotalMinutes:F1} minutes"
            : "Starting soon...";
    }

    public void Dispose()
    {
        Scanner.DeviceDiscovered -= OnDeviceDiscovered;
        Scanner.ScanProgress -= OnScanProgress;
        Scanner.ScanningStateChanged -= OnScanningStateChanged;
        Scanner.NextScanTimeChanged -= OnNextScanTimeChanged;
    }
}
```

## NetworkDevice Model

```csharp
public class NetworkDevice
{
    public string Name { get; set; }           // Hostname or DEVICE_xxx
    public string Type { get; set; }           // Device type from detectors
    public string IPv4Address { get; set; }    // IPv4 address
    public string IPv6Address { get; set; }    // IPv6 address (future)
    public bool IsOnline { get; set; }         // Current online status
    public DateTime LastSeen { get; set; }     // Last successful ping
    public string MacAddress { get; set; }     // MAC address (XX:XX:XX:XX:XX:XX)
    public TimeSpan ResponseTime { get; set; } // Ping round-trip time
}
```

## Performance Considerations

### Network Throttling

The scanner includes built-in throttling to prevent overwhelming the network:

```csharp
services.AddNetworkScanning(scanner =>
{
    // Process 10 devices, then pause 50ms
    scanner.DevicesBeforeThrottle = 10;
    scanner.NetworkThrottleDelayMs = 50;

    // Maximum 20 concurrent connections
    scanner.MaxConcurrentConnections = 20;
});
```

### Scan Interval Tuning

For different use cases, adjust the scan interval:

```csharp
// Home network - scan every 5 minutes
scanner.ScanIntervalMinutes = 5;

// Enterprise network - scan every 15 minutes
scanner.ScanIntervalMinutes = 15;

// IoT monitoring - scan every 1 minute
scanner.ScanIntervalMinutes = 1;
```

### Timeout Configuration

Adjust timeouts based on network conditions:

```csharp
services.AddNetworkScanning(scanner =>
{
    // Fast local network
    scanner.PingTimeoutMs = 1000;
    scanner.HttpTimeoutMs = 1000;
    scanner.SshTimeoutMs = 1000;
},
ports =>
{
    ports.PortConnectionTimeoutMs = 500;
});

// OR slow/remote network
services.AddNetworkScanning(scanner =>
{
    scanner.PingTimeoutMs = 5000;
    scanner.HttpTimeoutMs = 5000;
    scanner.SshTimeoutMs = 5000;
},
ports =>
{
    ports.PortConnectionTimeoutMs = 3000;
});
```

## Thread Safety

All public methods and properties of `INetworkScanner` are thread-safe. The `DiscoveredDevices` property returns a copy of the internal list to prevent concurrent modification issues.

```csharp
// Safe to call from multiple threads
var devices1 = _scanner.DiscoveredDevices;
var devices2 = _scanner.DiscoveredDevices;

// These are separate copies
devices1.Add(new NetworkDevice()); // Does not affect devices2
```

## Disposal

The scanner implements `IDisposable` and should be properly disposed:

```csharp
// With DI, disposal is automatic when scope ends

// Manual disposal
using var scanner = serviceProvider.GetRequiredService<INetworkScanner>();
scanner.StartScanning();
// ... use scanner
// Automatically disposed at end of using block
```

## See Also

- [Detectors](Detectors.md) - Device type detection strategies
- [MACResolution](MACResolution.md) - Cross-platform MAC address resolution
