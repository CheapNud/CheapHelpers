# MAC Address Resolution

CheapHelpers.Networking provides cross-platform MAC address resolution from IP addresses using platform-specific ARP table implementations.

## Overview

MAC (Media Access Control) addresses uniquely identify network interface hardware. The MAC resolver queries the operating system's ARP (Address Resolution Protocol) cache to map IP addresses to MAC addresses.

### Platform Support

- **Windows** - Uses `arp -a` command
- **Linux** - Uses `ip neigh show` command
- **macOS** - Uses `arp -a` command with BSD-style parsing

The appropriate resolver is automatically selected based on the detected operating system during DI registration.

## Automatic Platform Detection

The `AddNetworkScanning` extension method automatically registers the correct MAC resolver:

```csharp
services.AddNetworkScanning();

// Internally selects:
// - WindowsArpResolver on Windows
// - LinuxArpResolver on Linux
// - MacOSArpResolver on macOS
```

## Manual Platform-Specific Registration

You can manually register a specific resolver if needed:

```csharp
// Force Windows resolver
services.AddSingleton<IMacAddressResolver, WindowsArpResolver>();

// Force Linux resolver
services.AddSingleton<IMacAddressResolver, LinuxArpResolver>();

// Force macOS resolver
services.AddSingleton<IMacAddressResolver, MacOSArpResolver>();
```

## IMacAddressResolver Interface

```csharp
public interface IMacAddressResolver
{
    /// <summary>
    /// Gets the ARP table mapping IP addresses to MAC addresses
    /// </summary>
    Task<Dictionary<string, string>> GetArpTableAsync();
}
```

## Basic Usage

### Injecting the Resolver

```csharp
public class NetworkService
{
    private readonly IMacAddressResolver _macResolver;
    private readonly ILogger<NetworkService> _logger;

    public NetworkService(
        IMacAddressResolver macResolver,
        ILogger<NetworkService> logger)
    {
        _macResolver = macResolver;
        _logger = logger;
    }

    public async Task<string?> GetMacAddressAsync(string ipAddress)
    {
        var arpTable = await _macResolver.GetArpTableAsync();

        if (arpTable.TryGetValue(ipAddress, out var macAddress))
        {
            _logger.LogInformation("MAC for {IP}: {MAC}", ipAddress, macAddress);
            return macAddress;
        }

        _logger.LogWarning("No MAC address found for {IP}", ipAddress);
        return null;
    }
}
```

### Getting All ARP Entries

```csharp
public async Task<Dictionary<string, string>> GetAllMacAddressesAsync()
{
    var arpTable = await _macResolver.GetArpTableAsync();

    _logger.LogInformation("Found {Count} ARP entries", arpTable.Count);

    foreach (var (ip, mac) in arpTable)
    {
        _logger.LogDebug("{IP} → {MAC}", ip, mac);
    }

    return arpTable;
}
```

## Platform-Specific Implementations

### Windows (WindowsArpResolver)

Uses the `arp -a` command which outputs in this format:

```text
Interface: 192.168.1.100 --- 0x3
  Internet Address      Physical Address      Type
  192.168.1.1           aa-bb-cc-dd-ee-ff     dynamic
  192.168.1.50          11-22-33-44-55-66     dynamic
  192.168.1.255         ff-ff-ff-ff-ff-ff     static
```

The resolver parses this output and extracts IP-MAC pairs.

#### Implementation Details

```csharp
public class WindowsArpResolver : IMacAddressResolver
{
    public async Task<Dictionary<string, string>> GetArpTableAsync()
    {
        // Execute: arp -a
        // Parse output lines
        // Extract IP and MAC from each line
        // Validate IP format with IPAddress.TryParse
        // Validate MAC format (must contain - or :)
        // Normalize MAC to colon-separated format (XX:XX:XX:XX:XX:XX)
        // Return dictionary
    }
}
```

### Linux (LinuxArpResolver)

Uses the `ip neigh show` command which outputs in this format:

```text
192.168.1.1 dev eth0 lladdr aa:bb:cc:dd:ee:ff REACHABLE
192.168.1.50 dev eth0 lladdr 11:22:33:44:55:66 STALE
192.168.1.100 dev eth0  FAILED
```

The resolver looks for lines containing `lladdr` and extracts the MAC address.

#### Implementation Details

```csharp
public class LinuxArpResolver : IMacAddressResolver
{
    public async Task<Dictionary<string, string>> GetArpTableAsync()
    {
        // Execute: ip neigh show
        // Parse output lines
        // Look for "lladdr" keyword at position [2]
        // Extract IP from position [0]
        // Extract MAC from position [4]
        // Validate and normalize
        // Return dictionary
    }
}
```

### macOS (MacOSArpResolver)

Uses the `arp -a` command which outputs in BSD format:

```text
gateway (192.168.1.1) at aa:bb:cc:dd:ee:ff on en0 ifscope [ethernet]
device-50 (192.168.1.50) at 11:22:33:44:55:66 on en0 ifscope [ethernet]
? (192.168.1.100) at (incomplete) on en0 ifscope [ethernet]
```

The resolver extracts IP addresses from parentheses and MAC addresses after "at".

#### Implementation Details

```csharp
public class MacOSArpResolver : IMacAddressResolver
{
    public async Task<Dictionary<string, string>> GetArpTableAsync()
    {
        // Execute: arp -a
        // Parse output lines
        // Extract IP from within parentheses (IP)
        // Find " at " and extract MAC after it
        // Skip "(incomplete)" entries
        // Validate and normalize
        // Return dictionary
    }
}
```

## MAC Address Normalization

All resolvers normalize MAC addresses to a consistent format:

### Input Formats Accepted

```text
AA-BB-CC-DD-EE-FF    (Windows hyphen format)
aa:bb:cc:dd:ee:ff    (Unix colon format)
AABBCCDDEEFF         (No separators)
```

### Normalized Output Format

```text
AA:BB:CC:DD:EE:FF    (Uppercase colon-separated)
```

### Normalization Code

```csharp
private static string NormalizeMacAddress(string macAddress)
{
    // Remove all separators
    var cleanMac = macAddress.Replace("-", "").Replace(":", "").ToUpper();

    // Must be exactly 12 hex characters
    if (cleanMac.Length == 12)
    {
        // Insert colons every 2 characters
        return string.Join(":",
            Enumerable.Range(0, 6)
            .Select(i => cleanMac.Substring(i * 2, 2)));
    }

    // Return original if normalization fails
    return macAddress;
}
```

## Integration with NetworkScanner

The NetworkScanner automatically uses the MAC resolver during device discovery:

```csharp
public class NetworkScanner
{
    private readonly IMacAddressResolver _macResolver;

    private async Task<string> GetMacAddressForIpAsync(string ipAddress)
    {
        try
        {
            var arpEntries = await _macResolver.GetArpTableAsync();

            if (arpEntries.TryGetValue(ipAddress, out var macAddress))
            {
                return macAddress;
            }

            return "Unknown";
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error getting MAC address for {IP}", ipAddress);
            return "Unknown";
        }
    }
}
```

## Advanced Usage

### Building a MAC Address Cache

```csharp
public class MacAddressCache
{
    private readonly IMacAddressResolver _macResolver;
    private readonly Dictionary<string, MacCacheEntry> _cache = new();
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public async Task<string?> GetMacAddressAsync(string ipAddress)
    {
        await _cacheLock.WaitAsync();
        try
        {
            // Check cache first
            if (_cache.TryGetValue(ipAddress, out var entry))
            {
                if (DateTime.Now - entry.Timestamp < _cacheExpiration)
                {
                    return entry.MacAddress;
                }
            }

            // Refresh from ARP table
            var arpTable = await _macResolver.GetArpTableAsync();

            if (arpTable.TryGetValue(ipAddress, out var macAddress))
            {
                _cache[ipAddress] = new MacCacheEntry
                {
                    MacAddress = macAddress,
                    Timestamp = DateTime.Now
                };
                return macAddress;
            }

            return null;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private class MacCacheEntry
    {
        public string MacAddress { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
```

### MAC Vendor Lookup

```csharp
public class MacVendorService
{
    private readonly IMacAddressResolver _macResolver;
    private readonly Dictionary<string, string> _vendorPrefixes;

    public MacVendorService(IMacAddressResolver macResolver)
    {
        _macResolver = macResolver;
        _vendorPrefixes = LoadVendorPrefixes();
    }

    public async Task<string?> GetDeviceVendorAsync(string ipAddress)
    {
        var arpTable = await _macResolver.GetArpTableAsync();

        if (!arpTable.TryGetValue(ipAddress, out var macAddress))
            return null;

        // Extract OUI (Organizationally Unique Identifier)
        // First 3 octets of MAC address
        var oui = string.Join(":", macAddress.Split(':').Take(3));

        if (_vendorPrefixes.TryGetValue(oui, out var vendor))
        {
            return vendor;
        }

        return "Unknown Vendor";
    }

    private Dictionary<string, string> LoadVendorPrefixes()
    {
        return new Dictionary<string, string>
        {
            { "00:50:56", "VMware" },
            { "00:0C:29", "VMware" },
            { "08:00:27", "VirtualBox" },
            { "52:54:00", "QEMU" },
            { "00:15:5D", "Hyper-V" },
            { "DC:A6:32", "Raspberry Pi" },
            { "B8:27:EB", "Raspberry Pi" },
            { "E4:5F:01", "Raspberry Pi" },
            // Add more vendor prefixes
        };
    }
}
```

### Device Fingerprinting

```csharp
public class DeviceFingerprint
{
    public string IpAddress { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
}

public class DeviceFingerprintService
{
    private readonly IMacAddressResolver _macResolver;
    private readonly INetworkScanner _scanner;
    private readonly MacVendorService _vendorService;
    private readonly Dictionary<string, DeviceFingerprint> _fingerprints = new();

    public async Task<List<DeviceFingerprint>> GetDeviceFingerprintsAsync()
    {
        var devices = await _scanner.ScanNetworkAsync();
        var arpTable = await _macResolver.GetArpTableAsync();

        foreach (var device in devices.Where(d => d.IsOnline))
        {
            var macAddress = device.MacAddress;

            if (macAddress == "Unknown")
            {
                if (arpTable.TryGetValue(device.IPv4Address, out var mac))
                {
                    macAddress = mac;
                }
            }

            var vendor = await _vendorService.GetDeviceVendorAsync(device.IPv4Address);

            if (_fingerprints.TryGetValue(macAddress, out var fingerprint))
            {
                // Update existing fingerprint
                fingerprint.IpAddress = device.IPv4Address;
                fingerprint.Hostname = device.Name;
                fingerprint.DeviceType = device.Type;
                fingerprint.LastSeen = DateTime.Now;
            }
            else
            {
                // Create new fingerprint
                _fingerprints[macAddress] = new DeviceFingerprint
                {
                    IpAddress = device.IPv4Address,
                    MacAddress = macAddress,
                    Vendor = vendor ?? "Unknown",
                    Hostname = device.Name,
                    DeviceType = device.Type,
                    FirstSeen = DateTime.Now,
                    LastSeen = DateTime.Now
                };
            }
        }

        return _fingerprints.Values.ToList();
    }
}
```

## Custom MAC Resolver

You can create a custom MAC resolver for specialized scenarios:

```csharp
public class SnmpMacResolver : IMacAddressResolver
{
    private readonly ILogger<SnmpMacResolver> _logger;
    private readonly string _routerIp;

    public SnmpMacResolver(
        ILogger<SnmpMacResolver> logger,
        string routerIp)
    {
        _logger = logger;
        _routerIp = routerIp;
    }

    public async Task<Dictionary<string, string>> GetArpTableAsync()
    {
        // Query router's ARP table via SNMP
        // This can provide MAC addresses for devices across VLANs
        // that may not be in the local ARP cache

        var arpTable = new Dictionary<string, string>();

        try
        {
            // SNMP query implementation
            // Walk ipNetToMediaPhysAddress (1.3.6.1.2.1.4.22.1.2)
            // Extract IP and MAC from SNMP responses

            _logger.LogInformation("Retrieved {Count} entries via SNMP", arpTable.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SNMP ARP table query failed");
        }

        return arpTable;
    }
}

// Register custom resolver
services.AddCustomMacResolver<SnmpMacResolver>();
```

## Troubleshooting

### No MAC Addresses Found

```csharp
// Ensure devices have been pinged recently
await _scanner.ScanNetworkAsync();

// Then query MAC addresses
var arpTable = await _macResolver.GetArpTableAsync();

// ARP cache has TTL (typically 2-10 minutes)
// Entries expire if device hasn't communicated recently
```

### Permission Issues on Linux

```bash
# The 'ip neigh show' command may require privileges
# for some network configurations

# Run application with appropriate permissions
sudo dotnet run

# Or configure capabilities
sudo setcap cap_net_raw,cap_net_admin=eip ./MyApp
```

### Incomplete MAC Entries on macOS

```csharp
// macOS may show "(incomplete)" for offline devices
// These entries are filtered out automatically

// Force ARP entry by pinging first
using var ping = new Ping();
await ping.SendPingAsync(ipAddress);

// Then query MAC
var arpTable = await _macResolver.GetArpTableAsync();
```

### MAC Address Format Inconsistencies

```csharp
// All MAC addresses are normalized to XX:XX:XX:XX:XX:XX
// No need to handle different formats manually

var mac = "aa-bb-cc-dd-ee-ff";  // Input
// Output: "AA:BB:CC:DD:EE:FF"   // Normalized

var mac2 = "AABBCCDDEEFF";       // Input
// Output: "AA:BB:CC:DD:EE:FF"   // Normalized
```

## Performance Considerations

### ARP Table Query Cost

```csharp
// Querying the ARP table spawns a process (arp/ip command)
// This has overhead, so cache results when possible

// Bad: Query for each device
foreach (var device in devices)
{
    var arpTable = await _macResolver.GetArpTableAsync(); // Slow!
    var mac = arpTable.GetValueOrDefault(device.IPv4Address);
}

// Good: Query once, lookup many
var arpTable = await _macResolver.GetArpTableAsync();
foreach (var device in devices)
{
    var mac = arpTable.GetValueOrDefault(device.IPv4Address);
}
```

### Caching Strategy

```csharp
// Cache ARP table for short duration
private Dictionary<string, string>? _cachedArpTable;
private DateTime _lastArpQuery = DateTime.MinValue;
private readonly TimeSpan _arpCacheDuration = TimeSpan.FromSeconds(30);

public async Task<string?> GetMacWithCacheAsync(string ipAddress)
{
    if (_cachedArpTable == null ||
        DateTime.Now - _lastArpQuery > _arpCacheDuration)
    {
        _cachedArpTable = await _macResolver.GetArpTableAsync();
        _lastArpQuery = DateTime.Now;
    }

    return _cachedArpTable.GetValueOrDefault(ipAddress);
}
```

## Example: MAC Address Tracking

```csharp
public class MacAddressTracker
{
    private readonly IMacAddressResolver _macResolver;
    private readonly IDeviceStorage _storage;
    private readonly Dictionary<string, DeviceHistory> _history = new();

    public async Task TrackDevicesAsync()
    {
        var arpTable = await _macResolver.GetArpTableAsync();

        foreach (var (ip, mac) in arpTable)
        {
            if (!_history.TryGetValue(mac, out var history))
            {
                history = new DeviceHistory
                {
                    MacAddress = mac,
                    FirstSeen = DateTime.Now,
                    IpAddresses = []
                };
                _history[mac] = history;
            }

            if (!history.IpAddresses.Contains(ip))
            {
                history.IpAddresses.Add(ip);
            }

            history.LastSeen = DateTime.Now;
            history.CurrentIp = ip;
        }

        await _storage.SaveAsync(_history.Values.ToList());
    }

    public DeviceHistory? GetDeviceByMac(string macAddress)
    {
        return _history.GetValueOrDefault(macAddress);
    }

    public List<DeviceHistory> GetDevicesWithMultipleIps()
    {
        return _history.Values
            .Where(h => h.IpAddresses.Count > 1)
            .ToList();
    }
}

public class DeviceHistory
{
    public string MacAddress { get; set; } = string.Empty;
    public string CurrentIp { get; set; } = string.Empty;
    public List<string> IpAddresses { get; set; } = [];
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
}
```

## Storage Integration

The MAC resolver integrates seamlessly with device storage:

```csharp
public class DeviceService
{
    private readonly INetworkScanner _scanner;
    private readonly IMacAddressResolver _macResolver;
    private readonly IDeviceStorage _storage;

    public async Task SaveDiscoveredDevicesAsync()
    {
        var devices = await _scanner.ScanNetworkAsync();
        var arpTable = await _macResolver.GetArpTableAsync();

        // Ensure all devices have MAC addresses
        foreach (var device in devices)
        {
            if (device.MacAddress == "Unknown" &&
                arpTable.TryGetValue(device.IPv4Address, out var mac))
            {
                device.MacAddress = mac;
            }
        }

        await _storage.SaveDevicesAsync(devices);
    }
}
```

## See Also

- [Scanner](Scanner.md) - NetworkScanner usage and configuration
- [Detectors](Detectors.md) - Device type detection strategies
