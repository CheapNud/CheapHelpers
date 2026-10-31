using CheapHelpers.Networking.Detection;
using CheapHelpers.Networking.MacResolution;
using CheapHelpers.Networking.Subnet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace CheapHelpers.Networking.Core;

/// <summary>
/// Network scanner implementation for discovering and monitoring devices on the network
/// </summary>
public class NetworkScanner : INetworkScanner
{
    private readonly List<NetworkDevice> _discoveredDevices = [];
    private readonly object _devicesLock = new();

    private readonly ILogger<NetworkScanner> _logger;
    private readonly NetworkScannerOptions _options;
    private readonly IMacAddressResolver _macResolver;
    private readonly ISubnetProvider _subnetProvider;
    private readonly IEnumerable<IDeviceTypeDetector> _detectors;

    private bool _isScanning;
    private bool _shouldScan;
    private Timer? _scanTimer;
    private Timer? _uiUpdateTimer;
    private DateTime? _lastScanTime;
    private DateTime? _nextScanTime;

    public event Action<string>? ScanProgress;
    public event Action<NetworkDevice>? DeviceDiscovered;
    public event Action<bool>? ScanningStateChanged;
    public event Action<DateTime?>? NextScanTimeChanged;
    public event Action<DateTime?>? LastScanTimeChanged;

    public bool IsScanning => _isScanning;
    public DateTime? LastScanTime => _lastScanTime;
    public DateTime? NextScanTime => _nextScanTime;

    public List<NetworkDevice> DiscoveredDevices
    {
        get
        {
            lock (_devicesLock)
            {
                return [.. _discoveredDevices];
            }
        }
    }

    public NetworkScanner(
        ILogger<NetworkScanner> logger,
        IOptions<NetworkScannerOptions> options,
        IMacAddressResolver macResolver,
        ISubnetProvider subnetProvider,
        IEnumerable<IDeviceTypeDetector> detectors)
    {
        _logger = logger;
        _options = options.Value;
        _macResolver = macResolver;
        _subnetProvider = subnetProvider;
        _detectors = detectors.OrderByDescending(d => d.Priority);
    }

    public void StartScanning()
    {
        if (_shouldScan) return;

        _logger.LogInformation("Starting continuous scanning");
        _shouldScan = true;

        if (_options.EnableContinuousScanning)
        {
            StartContinuousScanning();
        }
    }

    public void PauseScanning()
    {
        _logger.LogInformation("Pausing continuous scanning");
        _shouldScan = false;

        StopContinuousScanning();

        if (_isScanning)
        {
            _logger.LogInformation("Scan is currently running - it will stop on next check");
        }
    }

    public void ResumeScanning()
    {
        if (_shouldScan) return;

        _logger.LogInformation("Resuming continuous scanning");
        _shouldScan = true;

        if (_options.EnableContinuousScanning)
        {
            StartContinuousScanning();
        }
    }

    public async Task<List<NetworkDevice>> ScanNetworkAsync()
    {
        return await ScanNetworkInternalAsync();
    }

    public async Task<List<NetworkDevice>> ScanSingleDeviceAsync(string ipAddress)
    {
        _logger.LogInformation("Scanning single device at {IpAddress}", ipAddress);
        ScanProgress?.Invoke($"Scanning device at {ipAddress}...");

        var discoveredDevices = new List<NetworkDevice>();

        try
        {
            if (!IPAddress.TryParse(ipAddress, out var parsedAddress))
            {
                _logger.LogWarning("Invalid IP address format: {IpAddress}", ipAddress);
                ScanProgress?.Invoke("Error: Invalid IP address format");
                return discoveredDevices;
            }

            if (parsedAddress.AddressFamily != AddressFamily.InterNetwork)
            {
                _logger.LogWarning("Only IPv4 addresses are supported: {IpAddress}", ipAddress);
                ScanProgress?.Invoke("Error: Only IPv4 addresses are supported");
                return discoveredDevices;
            }

            await ProcessSingleDeviceAsync(ipAddress, discoveredDevices);

            _logger.LogInformation("Single device scan completed for {IpAddress}. Found: {Count} devices", ipAddress, discoveredDevices.Count);
            ScanProgress?.Invoke($"Scan complete - {(discoveredDevices.Count > 0 ? "Device found" : "No device found")}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during single device scan for {IpAddress}", ipAddress);
            ScanProgress?.Invoke($"Scan error: {ex.Message}");
        }

        return discoveredDevices;
    }

    private void StartContinuousScanning()
    {
        _scanTimer?.Dispose();
        _uiUpdateTimer?.Dispose();

        _logger.LogDebug("Starting continuous scanning timers");

        _scanTimer = new Timer(async _ =>
        {
            if (_shouldScan && !_isScanning)
            {
                _logger.LogDebug("Timer triggered - starting scan");
                await ScanNetworkInternalAsync();
            }
            else if (!_shouldScan)
            {
                _logger.LogDebug("Timer triggered but scanning is paused - skipping scan");
            }
            else if (_isScanning)
            {
                _logger.LogDebug("Timer triggered but scan already in progress - skipping scan");
            }
        }, null, TimeSpan.Zero, TimeSpan.FromMinutes(_options.ScanIntervalMinutes));

        UpdateNextScanTime();

        _uiUpdateTimer = new Timer(_ =>
        {
            if (_shouldScan && _nextScanTime.HasValue)
            {
                NextScanTimeChanged?.Invoke(_nextScanTime);
            }
        }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    private void StopContinuousScanning()
    {
        _scanTimer?.Dispose();
        _scanTimer = null;

        _uiUpdateTimer?.Dispose();
        _uiUpdateTimer = null;

        _nextScanTime = null;
        NextScanTimeChanged?.Invoke(null);
    }

    private void UpdateNextScanTime()
    {
        if (_shouldScan)
        {
            _nextScanTime = DateTime.Now.AddMinutes(_options.ScanIntervalMinutes);
            NextScanTimeChanged?.Invoke(_nextScanTime);
        }
        else
        {
            _nextScanTime = null;
            NextScanTimeChanged?.Invoke(null);
        }
    }

    private async Task<List<NetworkDevice>> ScanNetworkInternalAsync()
    {
        if (_isScanning) return DiscoveredDevices;

        _isScanning = true;
        ScanningStateChanged?.Invoke(true);

        try
        {
            _logger.LogInformation("Starting network scan (shouldScan: {ShouldScan})", _shouldScan);

            if (!_shouldScan)
            {
                _logger.LogInformation("Scan cancelled - scanning is paused");
                return DiscoveredDevices;
            }

            ScanProgress?.Invoke("Starting network scan...");

            var subnets = await _subnetProvider.GetSubnetsToScanAsync();
            if (subnets.Count == 0)
            {
                _logger.LogWarning("No subnets to scan");
                ScanProgress?.Invoke("Error: Could not determine network to scan");
                return DiscoveredDevices;
            }

            lock (_devicesLock)
            {
                foreach (var existingDevice in _discoveredDevices)
                {
                    existingDevice.IsOnline = false;
                }
            }

            foreach (var networkBase in subnets)
            {
                _logger.LogInformation("Scanning network: {NetworkBase}.{StartIp}-{EndIp}", networkBase, _options.StartIp, _options.EndIp);
                ScanProgress?.Invoke($"Scanning network {networkBase}.x...");

                var deviceTasks = new List<Task>();
                var semaphore = new SemaphoreSlim(_options.MaxConcurrentConnections, _options.MaxConcurrentConnections);

                for (int i = _options.StartIp; i <= _options.EndIp; i++)
                {
                    if (!_shouldScan)
                    {
                        _logger.LogInformation("Scan cancelled mid-process - scanning was paused");
                        break;
                    }

                    var targetIp = $"{networkBase}.{i}";
                    deviceTasks.Add(ProcessDeviceWithThrottlingAsync(targetIp, semaphore));

                    if (i % _options.DevicesBeforeThrottle == 0)
                    {
                        await Task.Delay(_options.NetworkThrottleDelayMs);
                    }
                }

                await Task.WhenAll(deviceTasks);
            }

            _lastScanTime = DateTime.Now;
            LastScanTimeChanged?.Invoke(_lastScanTime);
            UpdateNextScanTime();

            var onlineCount = _discoveredDevices.Count(d => d.IsOnline);
            var offlineCount = _discoveredDevices.Count(d => !d.IsOnline);
            _logger.LogInformation("Network scan completed. Online: {OnlineCount}, Offline: {OfflineCount}, Total: {TotalCount}",
                onlineCount, offlineCount, _discoveredDevices.Count);
            ScanProgress?.Invoke($"Scan complete - found {onlineCount} online devices, {offlineCount} offline");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during network scan");
            ScanProgress?.Invoke($"Scan error: {ex.Message}");
        }
        finally
        {
            _isScanning = false;
            ScanningStateChanged?.Invoke(false);
        }

        return DiscoveredDevices;
    }

    private async Task ProcessDeviceWithThrottlingAsync(string ipAddress, SemaphoreSlim semaphore)
    {
        await semaphore.WaitAsync();
        try
        {
            await ProcessDeviceAsync(ipAddress);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task ProcessDeviceAsync(string ipAddress)
    {
        try
        {
            if (!_shouldScan)
            {
                _logger.LogDebug("Skipping {IpAddress} - scanning paused", ipAddress);
                return;
            }

            var pingResult = await PingHostAsync(ipAddress);

            NetworkDevice? existingDevice;
            lock (_devicesLock)
            {
                existingDevice = _discoveredDevices.FirstOrDefault(d => d.IPv4Address == ipAddress);
            }

            if (pingResult != null)
            {
                await Task.Delay(25);

                if (!_shouldScan)
                {
                    _logger.LogDebug("Skipping OS detection for {IpAddress} - scanning paused", ipAddress);
                    return;
                }

                pingResult.MacAddress = await GetMacAddressForIpAsync(ipAddress);
                await DetectDeviceTypeAsync(pingResult);

                lock (_devicesLock)
                {
                    if (existingDevice != null)
                    {
                        existingDevice.IsOnline = true;
                        existingDevice.LastSeen = pingResult.LastSeen;
                        existingDevice.ResponseTime = pingResult.ResponseTime;
                        existingDevice.Name = pingResult.Name;
                        existingDevice.Type = pingResult.Type;

                        if (!string.IsNullOrEmpty(pingResult.MacAddress) && pingResult.MacAddress != "Unknown")
                        {
                            existingDevice.MacAddress = pingResult.MacAddress;
                        }

                        _logger.LogDebug("Updated existing device: {Name} ({IpAddress}) - Status: Online", existingDevice.Name, existingDevice.IPv4Address);
                    }
                    else
                    {
                        _discoveredDevices.Add(pingResult);
                        _logger.LogDebug("Added new device: {Name} ({IpAddress}) - {Type} - Status: Online", pingResult.Name, pingResult.IPv4Address, pingResult.Type);
                    }
                }

                DeviceDiscovered?.Invoke(existingDevice ?? pingResult);
            }
            else if (existingDevice != null)
            {
                lock (_devicesLock)
                {
                    existingDevice.IsOnline = false;
                    existingDevice.ResponseTime = TimeSpan.Zero;
                }

                _logger.LogDebug("Device went offline: {Name} ({IpAddress})", existingDevice.Name, existingDevice.IPv4Address);
                DeviceDiscovered?.Invoke(existingDevice);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error processing device {IpAddress}", ipAddress);
        }
    }

    private async Task ProcessSingleDeviceAsync(string ipAddress, List<NetworkDevice> discoveredDevices)
    {
        try
        {
            _logger.LogDebug("Processing single device: {IpAddress}", ipAddress);

            var pingResult = await PingHostAsync(ipAddress);

            if (pingResult != null)
            {
                _logger.LogDebug("Device at {IpAddress} is responsive", ipAddress);

                pingResult.MacAddress = await GetMacAddressForIpAsync(ipAddress);
                await DetectDeviceTypeAsync(pingResult);

                discoveredDevices.Add(pingResult);

                _logger.LogDebug("Single device fully discovered: {Name} ({IpAddress}) - {Type}", pingResult.Name, pingResult.IPv4Address, pingResult.Type);
            }
            else
            {
                var offlineDevice = new NetworkDevice
                {
                    IPv4Address = ipAddress,
                    IsOnline = false,
                    LastSeen = DateTime.Now,
                    ResponseTime = TimeSpan.Zero,
                    Name = await TryGetHostNameAsync(ipAddress),
                    Type = "Unknown",
                    MacAddress = "Unknown"
                };

                discoveredDevices.Add(offlineDevice);
                _logger.LogDebug("Device at {IpAddress} is not responding - added as offline", ipAddress);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing single device {IpAddress}", ipAddress);

            var errorDevice = new NetworkDevice
            {
                IPv4Address = ipAddress,
                IsOnline = false,
                LastSeen = DateTime.Now,
                ResponseTime = TimeSpan.Zero,
                Name = $"ERROR_{ipAddress.Split('.').Last()}",
                Type = "Error",
                MacAddress = "Unknown"
            };

            discoveredDevices.Add(errorDevice);
        }
    }

    private async Task<NetworkDevice?> PingHostAsync(string ipAddress)
    {
        try
        {
            using var ping = new Ping();
            var pingReply = await ping.SendPingAsync(ipAddress, _options.PingTimeoutMs);

            if (pingReply.Status == IPStatus.Success)
            {
                _logger.LogDebug("Host {IpAddress} is reachable (RTT: {RoundtripTime}ms)", ipAddress, pingReply.RoundtripTime);

                var discoveredDevice = new NetworkDevice
                {
                    IPv4Address = ipAddress,
                    IsOnline = true,
                    LastSeen = DateTime.Now,
                    ResponseTime = TimeSpan.FromMilliseconds(pingReply.RoundtripTime),
                    Name = await TryGetHostNameAsync(ipAddress),
                    Type = "Unknown",
                    MacAddress = string.Empty
                };

                return discoveredDevice;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error pinging {IpAddress}", ipAddress);
        }

        return null;
    }

    private async Task<string> TryGetHostNameAsync(string ipAddress)
    {
        try
        {
            var hostEntry = await Dns.GetHostEntryAsync(ipAddress);
            var hostName = hostEntry.HostName;

            if (hostName.Contains('.'))
            {
                var nameParts = hostName.Split('.');
                var computerName = nameParts[0].ToUpper();
                var domainName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

                if (!string.IsNullOrEmpty(domainName) && domainName.Length > 2)
                {
                    return $"{computerName} ({domainName})";
                }

                return computerName;
            }

            return hostName.ToUpper();
        }
        catch
        {
            var lastOctet = ipAddress.Split('.').Last();
            return $"DEVICE_{lastOctet}";
        }
    }

    private async Task<string> GetMacAddressForIpAsync(string ipAddress)
    {
        try
        {
            var arpEntries = await _macResolver.GetArpTableAsync();

            if (arpEntries.TryGetValue(ipAddress, out var macAddress))
            {
                _logger.LogDebug("Found MAC for {IpAddress}: {MacAddress}", ipAddress, macAddress);
                return macAddress;
            }

            _logger.LogDebug("No MAC address found for {IpAddress}", ipAddress);
            return "Unknown";
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error getting MAC address for {IpAddress}", ipAddress);
            return "Unknown";
        }
    }

    private async Task DetectDeviceTypeAsync(NetworkDevice targetDevice)
    {
        try
        {
            _logger.LogDebug("Detecting device type for {IpAddress}...", targetDevice.IPv4Address);

            foreach (var detector in _detectors)
            {
                var detectedType = await detector.DetectDeviceTypeAsync(targetDevice.IPv4Address);
                if (!string.IsNullOrEmpty(detectedType))
                {
                    targetDevice.Type = detectedType;
                    _logger.LogDebug("Device type detected: {Type}", detectedType);
                    return;
                }
            }

            _logger.LogDebug("No device type detected for {IpAddress}, keeping as Unknown", targetDevice.IPv4Address);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error detecting device type for {IpAddress}", targetDevice.IPv4Address);
        }
    }

    public void Dispose()
    {
        _logger.LogDebug("Disposing NetworkScanner");
        StopContinuousScanning();
    }
}
