using CheapHelpers.Networking.Discovery;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Networking.Detection;

/// <summary>
/// Detects device types using mDNS/Zeroconf/Bonjour service discovery.
/// Delegates discovery to <see cref="IMdnsDiscoveryService"/> and maps
/// discovered service types to human-readable device labels.
/// </summary>
public class MdnsDetector(
    IMdnsDiscoveryService discoveryService,
    ILogger<MdnsDetector> logger)
    : IDeviceTypeDetector
{
    public int Priority => 85;

    private static readonly string[] ServiceTypes =
    [
        "_http._tcp",
        "_https._tcp",
        "_ssh._tcp",
        "_sftp-ssh._tcp",
        "_printer._tcp",
        "_ipp._tcp",
        "_scanner._tcp",
        "_smb._tcp",
        "_afpovertcp._tcp",
        "_device-info._tcp",
        "_workstation._tcp",
        "_airplay._tcp",
        "_homekit._tcp",
        "_hap._tcp",
        "_raop._tcp",
        "_googlecast._tcp",
        "_spotify-connect._tcp",
        "_sonos._tcp",
        "_hue._tcp",
        "_homeassistant._tcp",
        "_octoprint._tcp",
        "_mqtt._tcp",
        "_rfb._tcp",
        "_daap._tcp",
        "_radicale._tcp"
    ];

    public async Task<string?> DetectDeviceTypeAsync(string ipAddress)
    {
        try
        {
            foreach (var serviceType in ServiceTypes)
            {
                var devices = await discoveryService.DiscoverAsync(serviceType);

                var match = devices.Find(d =>
                    string.Equals(d.IPv4Address, ipAddress, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(d.IPv6Address, ipAddress, StringComparison.OrdinalIgnoreCase));

                if (match is not null)
                {
                    var deviceInfo = BuildDeviceInfo(match.InstanceName, serviceType);
                    logger.LogInformation("mDNS device detected: {Type} at {IpAddress}", deviceInfo, ipAddress);
                    return deviceInfo;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "mDNS detection failed for {IpAddress}", ipAddress);
            return null;
        }
    }

    private static string BuildDeviceInfo(string? instanceName, string serviceType)
    {
        var label = ClassifyServiceType(serviceType);
        if (!string.IsNullOrEmpty(instanceName) && !string.IsNullOrEmpty(label))
            return $"{instanceName} - {label} (mDNS)";
        if (!string.IsNullOrEmpty(instanceName))
            return $"{instanceName} (mDNS)";
        if (!string.IsNullOrEmpty(label))
            return $"{label} (mDNS)";
        return "mDNS Device";
    }

    private static string? ClassifyServiceType(string serviceType)
    {
        var lowerService = serviceType.ToLowerInvariant();

        if (lowerService.Contains("_printer") || lowerService.Contains("_ipp"))
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
}
