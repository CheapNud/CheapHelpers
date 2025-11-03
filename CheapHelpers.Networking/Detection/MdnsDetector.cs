using CheapHelpers.Networking.Core;
using Makaretu.Dns;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Networking.Detection;

/// <summary>
/// Detects devices using mDNS/Zeroconf/Bonjour service discovery
/// </summary>
public class MdnsDetector : IDeviceTypeDetector, IDisposable
{
    private readonly ILogger<MdnsDetector> _logger;
    private readonly ServiceDiscovery _serviceDiscovery;
    private readonly MulticastService _mdns;
    private readonly Dictionary<string, string> _deviceCache = new();
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private bool _isScanning;

    public int Priority => 85; // High priority, slightly below UPnP

    // Common mDNS service types to discover
    private static readonly string[] ServiceTypes =
    [
        "_http._tcp",           // HTTP servers
        "_https._tcp",          // HTTPS servers
        "_ssh._tcp",            // SSH servers
        "_sftp-ssh._tcp",       // SFTP servers
        "_printer._tcp",        // Printers
        "_ipp._tcp",            // Internet Printing Protocol
        "_scanner._tcp",        // Scanners
        "_smb._tcp",            // SMB/Samba file shares
        "_afpovertcp._tcp",     // Apple File Protocol
        "_device-info._tcp",    // Device information
        "_workstation._tcp",    // Workstations
        "_airplay._tcp",        // AirPlay devices
        "_homekit._tcp",        // HomeKit devices
        "_hap._tcp",            // HomeKit Accessory Protocol
        "_raop._tcp",           // Remote Audio Output Protocol
        "_googlecast._tcp",     // Chromecast
        "_spotify-connect._tcp", // Spotify Connect
        "_sonos._tcp",          // Sonos speakers
        "_hue._tcp",            // Philips Hue
        "_homeassistant._tcp",  // Home Assistant
        "_octoprint._tcp",      // OctoPrint (3D printing)
        "_mqtt._tcp",           // MQTT brokers
        "_rfb._tcp",            // VNC (Remote Frame Buffer)
        "_daap._tcp",           // Digital Audio Access Protocol
        "_radicale._tcp"        // Radicale CalDAV/CardDAV
    ];

    public MdnsDetector(ILogger<MdnsDetector> logger)
    {
        _logger = logger;
        _mdns = new MulticastService();
        _serviceDiscovery = new ServiceDiscovery(_mdns);

        // Subscribe to service announcements
        _serviceDiscovery.ServiceInstanceDiscovered += OnServiceInstanceDiscovered;
    }

    public async Task<string?> DetectDeviceTypeAsync(string ipAddress)
    {
        try
        {
            // Check cache first
            await _cacheLock.WaitAsync();
            try
            {
                if (_deviceCache.TryGetValue(ipAddress, out var cachedType))
                {
                    _logger.LogDebug("mDNS cache hit for {IpAddress}: {Type}", ipAddress, cachedType);
                    return cachedType;
                }
            }
            finally
            {
                _cacheLock.Release();
            }

            // Start discovery if not already running
            if (!_isScanning)
            {
                StartDiscovery();
            }

            // Wait a bit for discoveries to come in
            await Task.Delay(1500);

            // Check cache again after discovery
            await _cacheLock.WaitAsync();
            try
            {
                if (_deviceCache.TryGetValue(ipAddress, out var discoveredType))
                {
                    _logger.LogInformation("mDNS device detected: {Type} at {IpAddress}", discoveredType, ipAddress);
                    return discoveredType;
                }
            }
            finally
            {
                _cacheLock.Release();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "mDNS detection failed for {IpAddress}", ipAddress);
            return null;
        }
    }

    private void StartDiscovery()
    {
        if (_isScanning)
            return;

        _isScanning = true;

        try
        {
            _mdns.Start();

            // Query for each service type
            foreach (var serviceType in ServiceTypes)
            {
                try
                {
                    _serviceDiscovery.QueryServiceInstances(serviceType);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error querying mDNS service type {ServiceType}", serviceType);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting mDNS discovery");
        }
    }

    private void OnServiceInstanceDiscovered(object? sender, ServiceInstanceDiscoveryEventArgs e)
    {
        try
        {
            var serviceInstance = e.ServiceInstanceName;
            var serviceInstanceStr = serviceInstance.ToString();

            _logger.LogDebug("mDNS service instance discovered: {ServiceInstance}", serviceInstanceStr);

            // Resolve the service to get more details
            Task.Run(async () =>
            {
                try
                {
                    // Query for the service instance details
                    var message = e.Message;

                    // Extract IP addresses and other info from the message
                    var ipAddresses = new List<string>();
                    string? instanceName = null;
                    string? hostName = null;

                    foreach (var answer in message.Answers)
                    {
                        if (answer is ARecord aRecord)
                        {
                            ipAddresses.Add(aRecord.Address.ToString());
                        }
                        else if (answer is SRVRecord srvRecord)
                        {
                            hostName = srvRecord.Target.ToString().TrimEnd('.');
                        }
                        else if (answer is PTRRecord ptrRecord)
                        {
                            instanceName = ExtractInstanceName(ptrRecord.DomainName.ToString());
                        }
                    }

                    // Additional records might have more info
                    foreach (var additional in message.AdditionalRecords)
                    {
                        if (additional is ARecord aRecord && !ipAddresses.Contains(aRecord.Address.ToString()))
                        {
                            ipAddresses.Add(aRecord.Address.ToString());
                        }
                    }

                    if (ipAddresses.Count == 0)
                        return;

                    var deviceInfo = BuildDeviceInfo(instanceName, serviceInstanceStr, hostName);

                    foreach (var ipAddress in ipAddresses)
                    {
                        await _cacheLock.WaitAsync();
                        try
                        {
                            // Only cache if we don't already have info for this IP
                            // or if the new info is more specific
                            if (!_deviceCache.ContainsKey(ipAddress) ||
                                deviceInfo.Length > _deviceCache[ipAddress].Length)
                            {
                                _deviceCache[ipAddress] = deviceInfo;
                                _logger.LogDebug("Cached mDNS info for {IpAddress}: {Info}", ipAddress, deviceInfo);
                            }
                        }
                        finally
                        {
                            _cacheLock.Release();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error processing discovered mDNS service instance");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error in mDNS service instance discovery handler");
        }
    }

    private string? ExtractInstanceName(string fullName)
    {
        // Instance names are typically like: "My Printer._printer._tcp.local"
        // We want to extract "My Printer"
        var firstDot = fullName.IndexOf('.');
        if (firstDot > 0)
        {
            return fullName[..firstDot];
        }

        return fullName;
    }

    private string BuildDeviceInfo(string? instanceName, string serviceNameStr, string? hostName)
    {
        var serviceType = ExtractServiceType(serviceNameStr);

        // Build description
        var infoParts = new List<string>();

        if (!string.IsNullOrEmpty(instanceName))
        {
            infoParts.Add(instanceName);
        }

        if (!string.IsNullOrEmpty(serviceType))
        {
            infoParts.Add(serviceType);
        }
        else if (!string.IsNullOrEmpty(hostName))
        {
            infoParts.Add(hostName);
        }

        var deviceInfo = infoParts.Count > 0
            ? string.Join(" - ", infoParts) + " (mDNS)"
            : "mDNS Device";

        return deviceInfo;
    }

    private string? ExtractServiceType(string serviceNameStr)
    {
        var lowerService = serviceNameStr.ToLowerInvariant();

        if (lowerService.Contains("_printer"))
            return "Printer";
        if (lowerService.Contains("_scanner"))
            return "Scanner";
        if (lowerService.Contains("_airplay"))
            return "AirPlay Device";
        if (lowerService.Contains("_homekit") || lowerService.Contains("_hap"))
            return "HomeKit Device";
        if (lowerService.Contains("_googlecast"))
            return "Chromecast";
        if (lowerService.Contains("_spotify"))
            return "Spotify Connect";
        if (lowerService.Contains("_sonos"))
            return "Sonos Speaker";
        if (lowerService.Contains("_hue"))
            return "Philips Hue";
        if (lowerService.Contains("_homeassistant"))
            return "Home Assistant";
        if (lowerService.Contains("_octoprint"))
            return "OctoPrint";
        if (lowerService.Contains("_mqtt"))
            return "MQTT Broker";
        if (lowerService.Contains("_smb") || lowerService.Contains("_afp"))
            return "File Server";
        if (lowerService.Contains("_ssh") || lowerService.Contains("_sftp"))
            return "SSH Server";
        if (lowerService.Contains("_http"))
            return "Web Server";
        if (lowerService.Contains("_workstation"))
            return "Workstation";
        if (lowerService.Contains("_rfb"))
            return "VNC Server";
        if (lowerService.Contains("_raop"))
            return "Audio Receiver";

        return null;
    }

    public void Dispose()
    {
        try
        {
            _mdns?.Stop();
            _mdns?.Dispose();
        }
        catch
        {
            // Ignore disposal errors
        }

        _serviceDiscovery?.Dispose();
        _cacheLock?.Dispose();
    }
}
