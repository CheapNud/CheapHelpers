# Device Type Detectors

CheapHelpers.Networking provides a pluggable detector architecture for identifying device types on your network. Detectors run in priority order and use various protocols and techniques to determine what type of device is at each IP address.

## Overview

Device type detection uses multiple strategies:

- **UPnP/SSDP Discovery** - Discovers smart devices, media servers, printers, routers
- **mDNS/Zeroconf/Bonjour** - Discovers Apple devices, IoT devices, network services
- **HTTP/HTTPS Headers** - Identifies web servers and their underlying OS
- **SSH Banner Detection** - Identifies Linux/Unix systems
- **Windows Service Ports** - Identifies Windows clients and servers
- **Custom Service Endpoints** - Detects application-specific ports

## Detector Priority System

Detectors execute in priority order (highest first). When a detector successfully identifies a device, lower-priority detectors are skipped.

| Detector | Priority | Description |
|----------|----------|-------------|
| UPnP/SSDP | 90 | Most reliable for IoT/smart devices |
| mDNS/Zeroconf | 85 | Excellent for Apple and network services |
| Service Endpoints | 60 | Custom application ports |
| HTTP Detection | 50 | Web server identification |
| SSH Detection | 40 | Linux/Unix systems |
| Windows Services | 30 | Windows systems |

## DI Configuration

### Add All Detectors

```csharp
services.AddNetworkScanning();
services.AddAllDetectors(); // Includes all detectors
```

### Selective Detectors

```csharp
// Default detectors only (no UPnP/mDNS)
services.AddDefaultDetectors();

// Enhanced detectors only (UPnP + mDNS)
services.AddEnhancedDetectors();

// Individual detectors
services.AddUpnpDetector();
services.AddMdnsDetector();
```

### Custom Detector

```csharp
public class MyCustomDetector : IDeviceTypeDetector
{
    public int Priority => 95; // Higher than UPnP

    public async Task<string?> DetectDeviceTypeAsync(string ipAddress)
    {
        // Your detection logic
        return "Custom Device Type";
    }
}

// Register
services.AddCustomDetector<MyCustomDetector>();
```

## UPnP/SSDP Detector

The UPnP detector uses the SSDP (Simple Service Discovery Protocol) to discover devices that advertise themselves via multicast UDP.

### What It Detects

- Smart TVs (Samsung, LG, Sony)
- Media Servers (Plex, Kodi, Windows Media Player)
- Media Renderers (Roku, Chromecast alternatives)
- Network Storage (NAS devices)
- Routers and Gateways
- Printers and Scanners
- Smart Home Devices (Philips Hue bridges, etc.)
- IoT Devices with UPnP support

### How It Works

1. Sends M-SEARCH multicast requests to 239.255.255.250:1900
2. Listens for SSDP responses from devices
3. Fetches device description XML from the LOCATION header
4. Parses friendly name, manufacturer, model, and device type
5. Caches results for fast subsequent lookups

### Example Detection Results

```text
Samsung Smart TV - Smart TV (UPnP)
Synology DS918+ - Network Storage (UPnP)
HP LaserJet Pro - Printer (UPnP)
Philips Hue Bridge - Smart Light (UPnP)
ASUS RT-AC68U - Router/Gateway (UPnP)
Plex Media Server (UPnP)
```

### Technical Details

```csharp
// UPnP detector runs continuously in background
// It maintains a cache of discovered devices
// Sends periodic searches every 30 seconds
// Auto-starts on first detection request

// M-SEARCH request format
M-SEARCH * HTTP/1.1
HOST: 239.255.255.250:1900
MAN: "ssdp:discover"
MX: 3
ST: ssdp:all
```

### Configuration

```csharp
// UPnP uses PortDetectionOptions for HTTP timeout
services.AddNetworkScanning(
    configurePorts: ports =>
    {
        ports.PortConnectionTimeoutMs = 2000; // For fetching device XML
    });

services.AddUpnpDetector();
```

## mDNS/Zeroconf Detector

The mDNS detector uses multicast DNS (also known as Bonjour or Zeroconf) to discover network services.

### What It Detects

- Apple Devices (MacBooks, iPhones, iPads, Apple TVs)
- Printers (especially network printers)
- Scanners
- IoT Devices (Home Assistant, OctoPrint)
- Network Services (SSH, HTTP, SMB, VNC)
- Smart Speakers (Sonos, AirPlay devices)
- Streaming Devices (Chromecast, Spotify Connect)
- File Servers (SMB, AFP)
- Smart Home Hubs

### Service Types Discovered

```text
_http._tcp          - HTTP servers
_https._tcp         - HTTPS servers
_ssh._tcp           - SSH servers
_printer._tcp       - Printers
_scanner._tcp       - Scanners
_smb._tcp           - SMB/Samba file shares
_airplay._tcp       - AirPlay devices
_homekit._tcp       - HomeKit devices
_googlecast._tcp    - Chromecast
_spotify-connect._tcp - Spotify Connect
_sonos._tcp         - Sonos speakers
_hue._tcp           - Philips Hue
_homeassistant._tcp - Home Assistant
_octoprint._tcp     - OctoPrint (3D printing)
_mqtt._tcp          - MQTT brokers
_rfb._tcp           - VNC servers
```

### Example Detection Results

```text
MacBook Pro - Workstation (mDNS)
Living Room Sonos - Sonos Speaker (mDNS)
HP OfficeJet - Printer (mDNS)
Raspberry Pi - SSH Server (mDNS)
Home Assistant - Home Assistant (mDNS)
OctoPrint - OctoPrint (mDNS)
Chromecast - Chromecast (mDNS)
```

### Technical Details

```csharp
// mDNS uses multicast DNS queries
// Listens on 224.0.0.251:5353
// Discovers services by querying service types
// Auto-starts on first detection request
// Maintains cache of discovered services
```

### Configuration

```csharp
// mDNS detector has no configuration options
// It uses standard mDNS/Zeroconf protocol
services.AddMdnsDetector();
```

## HTTP Detector

Detects devices by probing HTTP/HTTPS ports and analyzing server headers.

### What It Detects

- Windows Servers (IIS versions)
- Linux Servers (Apache, Nginx, Lighttpd)
- .NET Applications (Kestrel)
- Web Applications on custom ports

### Ports Probed

```csharp
services.AddNetworkScanning(
    configurePorts: ports =>
    {
        // Custom IoT ports (probed first)
        ports.CustomIoTPorts = [5000, 8000, 8080, 8443];

        // Standard HTTP ports (probed second)
        ports.StandardHttpPorts = [80, 443];
    });
```

### Example Detection Results

```text
Windows Server 2019 (HTTP)
Windows Server (.NET) (HTTP)
Linux Server (HTTP)
Unknown (.NET App) (HTTP)
Unknown (Web App) (HTTP)
```

### Server Header Patterns

```csharp
// IIS versions
"microsoft-iis/10.0" → "Windows Server 2016/2019/2022 (HTTP)"
"microsoft-iis/8.5"  → "Windows Server 2012 R2 (HTTP)"
"microsoft-iis/8.0"  → "Windows Server 2012 (HTTP)"

// Web servers
"kestrel"    → "Windows/Linux (.NET) (HTTP)"
"apache"     → "Linux Server (HTTP)"
"nginx"      → "Linux Server (HTTP)"
"lighttpd"   → "Linux Server (HTTP)"

// Microsoft headers
"asp.net"           → "Windows Server (.NET) (HTTP)"
"microsoft-httpapi" → "Windows Server (HTTP)"
```

### Configuration

```csharp
services.AddNetworkScanning(
    scanner =>
    {
        scanner.HttpTimeoutMs = 2000; // HTTP request timeout
    },
    ports =>
    {
        ports.CustomIoTPorts = [5000, 8000, 8080, 8443];
        ports.StandardHttpPorts = [80, 443];
        ports.PortConnectionTimeoutMs = 1000;
    });
```

## SSH Detector

Detects Linux/Unix systems by reading SSH banners on port 22.

### What It Detects

- Ubuntu Linux
- Debian Linux
- Raspberry Pi (Raspbian)
- Generic Linux/Unix systems

### Example Detection Results

```text
Ubuntu Linux (SSH)
Debian Linux (SSH)
Raspberry Pi (SSH)
Linux/Unix (SSH)
Unknown (SSH)
```

### Banner Patterns

```csharp
"ubuntu"    → "Ubuntu Linux (SSH)"
"debian"    → "Debian Linux (SSH)"
"raspbian"  → "Raspberry Pi (SSH)"
"openssh"   → "Linux/Unix (SSH)"
```

### Configuration

```csharp
services.AddNetworkScanning(
    scanner =>
    {
        scanner.SshTimeoutMs = 2000;
    },
    ports =>
    {
        ports.SshPort = 22; // Standard SSH port
        ports.PortConnectionTimeoutMs = 1000;
    });
```

## Windows Services Detector

Detects Windows systems by probing common Windows service ports.

### Ports Detected

```csharp
services.AddNetworkScanning(
    configurePorts: ports =>
    {
        ports.WindowsServicePorts = new()
        {
            { 3389, "Remote Desktop Protocol" },
            { 5985, "WinRM HTTP" },
            { 5986, "WinRM HTTPS" },
            { 445, "SMB" },
            { 139, "NetBIOS" },
            { 135, "RPC" }
        };
    });
```

### Example Detection Results

```text
Windows Server (Remote Desktop Protocol)
Windows Server (WinRM HTTP)
Windows Client (SMB)
Windows Client (NetBIOS)
```

### Server vs Client Detection

```csharp
// These ports indicate likely server
3389  → RDP server
5985  → WinRM server
5986  → WinRM server

// These could be client or server
135   → RPC
139   → NetBIOS
445   → SMB
```

### Configuration

```csharp
services.AddNetworkScanning(
    configurePorts: ports =>
    {
        ports.WindowsServicePorts = new()
        {
            { 3389, "RDP" },
            { 5985, "WinRM" },
            // Add custom Windows ports
        };
        ports.PortConnectionTimeoutMs = 1000;
    });
```

## Service Endpoint Detector

Detects custom application-specific ports for proprietary systems.

### Configuration

```csharp
services.AddNetworkScanning(
    configurePorts: ports =>
    {
        ports.ServiceEndpoints = new()
        {
            { 8974, "Home Automation Hub" },
            { 8975, "Security Camera System" },
            { 12050, "Building Management System" },
            { 9000, "Custom Application Server" }
        };
    });
```

### Example Detection Results

```text
Home Automation Hub
Security Camera System
Building Management System
Custom Application Server
```

## Creating Custom Detectors

### Basic Custom Detector

```csharp
public class DatabaseDetector : IDeviceTypeDetector
{
    private readonly ILogger<DatabaseDetector> _logger;

    public int Priority => 70; // Between Service Endpoints and HTTP

    public DatabaseDetector(ILogger<DatabaseDetector> logger)
    {
        _logger = logger;
    }

    public async Task<string?> DetectDeviceTypeAsync(string ipAddress)
    {
        try
        {
            // Try SQL Server
            if (await IsPortOpen(ipAddress, 1433))
            {
                _logger.LogInformation("SQL Server detected at {IP}", ipAddress);
                return "SQL Server Database";
            }

            // Try MySQL
            if (await IsPortOpen(ipAddress, 3306))
            {
                _logger.LogInformation("MySQL detected at {IP}", ipAddress);
                return "MySQL Database";
            }

            // Try PostgreSQL
            if (await IsPortOpen(ipAddress, 5432))
            {
                _logger.LogInformation("PostgreSQL detected at {IP}", ipAddress);
                return "PostgreSQL Database";
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Database detection failed for {IP}", ipAddress);
            return null;
        }
    }

    private async Task<bool> IsPortOpen(string ipAddress, int port)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(ipAddress, port);

            if (await Task.WhenAny(connectTask, Task.Delay(1000)) != connectTask)
                return false;

            return client.Connected;
        }
        catch
        {
            return false;
        }
    }
}

// Register
services.AddCustomDetector<DatabaseDetector>();
```

### Advanced Custom Detector with Caching

```csharp
public class SnmpDetector : IDeviceTypeDetector
{
    private readonly ILogger<SnmpDetector> _logger;
    private readonly Dictionary<string, string> _cache = new();
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    public int Priority => 80; // High priority

    public SnmpDetector(ILogger<SnmpDetector> logger)
    {
        _logger = logger;
    }

    public async Task<string?> DetectDeviceTypeAsync(string ipAddress)
    {
        // Check cache first
        await _cacheLock.WaitAsync();
        try
        {
            if (_cache.TryGetValue(ipAddress, out var cachedType))
            {
                _logger.LogDebug("SNMP cache hit for {IP}", ipAddress);
                return cachedType;
            }
        }
        finally
        {
            _cacheLock.Release();
        }

        // Perform SNMP query
        var deviceType = await QuerySnmpDeviceTypeAsync(ipAddress);

        if (!string.IsNullOrEmpty(deviceType))
        {
            // Cache result
            await _cacheLock.WaitAsync();
            try
            {
                _cache[ipAddress] = deviceType;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        return deviceType;
    }

    private async Task<string?> QuerySnmpDeviceTypeAsync(string ipAddress)
    {
        // Implement SNMP query logic
        // This is a simplified example
        return null;
    }
}
```

### Protocol-Based Detector

```csharp
public class ModbusDetector : IDeviceTypeDetector
{
    public int Priority => 75;

    public async Task<string?> DetectDeviceTypeAsync(string ipAddress)
    {
        if (!await IsModbusDeviceAsync(ipAddress))
            return null;

        // Try to read device identification
        var deviceInfo = await ReadModbusDeviceInfoAsync(ipAddress);
        return $"{deviceInfo} (Modbus)";
    }

    private async Task<bool> IsModbusDeviceAsync(string ipAddress)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(ipAddress, 502); // Modbus TCP port

            if (!client.Connected)
                return false;

            // Send Modbus query
            using var stream = client.GetStream();
            var query = BuildModbusQuery();
            await stream.WriteAsync(query);

            var response = new byte[256];
            var bytesRead = await stream.ReadAsync(response);

            return IsValidModbusResponse(response, bytesRead);
        }
        catch
        {
            return false;
        }
    }

    private byte[] BuildModbusQuery()
    {
        // Build Modbus TCP query
        return new byte[] { /* Modbus query bytes */ };
    }

    private bool IsValidModbusResponse(byte[] response, int length)
    {
        // Validate Modbus response
        return length > 0;
    }

    private async Task<string> ReadModbusDeviceInfoAsync(string ipAddress)
    {
        // Read device-specific information
        return "Industrial Controller";
    }
}
```

## Detector Best Practices

### Priority Assignment

- **90-100**: Very reliable protocol-based detection (UPnP, SNMP)
- **80-89**: Reliable service discovery (mDNS, SSDP)
- **60-79**: Application-specific detection (custom endpoints)
- **40-59**: Generic protocol detection (HTTP, SSH)
- **20-39**: Port-based heuristics (Windows services)

### Performance

```csharp
// Use connection timeouts
var connectTask = client.ConnectAsync(ipAddress, port);
if (await Task.WhenAny(connectTask, Task.Delay(1000)) != connectTask)
    return null;

// Cache results when possible
private readonly Dictionary<string, string> _cache = new();

// Use SemaphoreSlim for thread-safe caching
private readonly SemaphoreSlim _cacheLock = new(1, 1);
```

### Error Handling

```csharp
public async Task<string?> DetectDeviceTypeAsync(string ipAddress)
{
    try
    {
        // Detection logic
    }
    catch (Exception ex)
    {
        // Log at Debug level, not Error
        _logger.LogDebug(ex, "Detection failed for {IP}", ipAddress);
        return null;
    }
}
```

### Return Values

```csharp
// Good: Specific and descriptive
return "Ubuntu Linux (SSH)";
return "Philips Hue Bridge - Smart Light (UPnP)";

// Acceptable: Generic but informative
return "Linux/Unix (SSH)";
return "UPnP Device";

// Poor: Too vague
return "Unknown";
return "Device";

// Return null when detection fails
return null;
```

## Combining Detectors

Multiple detectors can identify the same device using different protocols:

```text
Priority 90: UPnP → "Synology DS918+ - Network Storage (UPnP)"
Priority 85: mDNS → "Synology NAS - File Server (mDNS)"
Priority 50: HTTP → "Linux Server (HTTP)"
Priority 40: SSH  → "Linux/Unix (SSH)"
```

The highest priority match (UPnP in this case) will be used.

## Troubleshooting

### No Devices Detected

```csharp
// Enable debug logging
services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Debug);
});

// Check if detectors are registered
services.AddAllDetectors();

// Verify network configuration
scanner.SubnetBase = "192.168.1"; // Explicit subnet
```

### UPnP/mDNS Not Working

```csharp
// Check firewall allows multicast
// UPnP: 239.255.255.250:1900
// mDNS: 224.0.0.251:5353

// Verify devices support protocols
// Not all devices advertise via UPnP/mDNS

// Add fallback detectors
services.AddAllDetectors(); // Includes HTTP, SSH, Windows
```

### Slow Detection

```csharp
// Reduce timeouts
services.AddNetworkScanning(
    scanner =>
    {
        scanner.HttpTimeoutMs = 1000;
        scanner.SshTimeoutMs = 1000;
    },
    ports =>
    {
        ports.PortConnectionTimeoutMs = 500;
    });

// Use fewer detectors
services.AddDefaultDetectors(); // Skip UPnP/mDNS
```

## See Also

- [Scanner](Scanner.md) - NetworkScanner usage and configuration
- [MACResolution](MACResolution.md) - Cross-platform MAC address resolution
