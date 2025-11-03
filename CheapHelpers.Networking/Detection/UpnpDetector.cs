using CheapHelpers.Networking.Core;
using Microsoft.Extensions.Logging;
using Rssdp;
using System.Xml.Linq;

namespace CheapHelpers.Networking.Detection;

/// <summary>
/// Detects devices using UPnP/SSDP discovery protocol
/// </summary>
public class UpnpDetector : IDeviceTypeDetector, IDisposable
{
    private readonly ILogger<UpnpDetector> _logger;
    private readonly PortDetectionOptions _portOptions;
    private readonly SsdpDeviceLocator _deviceLocator;
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, string> _deviceCache = new();
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    public int Priority => 90; // Higher priority than HTTP - UPnP is very reliable

    public UpnpDetector(ILogger<UpnpDetector> logger, Microsoft.Extensions.Options.IOptions<PortDetectionOptions> portOptions)
    {
        _logger = logger;
        _portOptions = portOptions.Value;
        _deviceLocator = new SsdpDeviceLocator();
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMilliseconds(_portOptions.PortConnectionTimeoutMs)
        };
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
                    _logger.LogDebug("UPnP cache hit for {IpAddress}: {Type}", ipAddress, cachedType);
                    return cachedType;
                }
            }
            finally
            {
                _cacheLock.Release();
            }

            // Search for devices (with timeout)
            var searchTask = _deviceLocator.SearchAsync(TimeSpan.FromSeconds(2));
            var devices = await searchTask;

            foreach (var foundDevice in devices)
            {
                try
                {
                    // Check if this device matches our target IP
                    if (foundDevice.DescriptionLocation?.Host == ipAddress)
                    {
                        var deviceInfo = await GetDeviceInfoAsync(foundDevice);
                        if (!string.IsNullOrEmpty(deviceInfo))
                        {
                            // Cache the result
                            await _cacheLock.WaitAsync();
                            try
                            {
                                _deviceCache[ipAddress] = deviceInfo;
                            }
                            finally
                            {
                                _cacheLock.Release();
                            }

                            _logger.LogInformation("UPnP device detected: {DeviceInfo}", deviceInfo);
                            return deviceInfo;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error processing UPnP device for {IpAddress}", ipAddress);
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "UPnP detection failed for {IpAddress}", ipAddress);
            return null;
        }
    }

    private async Task<string?> GetDeviceInfoAsync(DiscoveredSsdpDevice discoveredDevice)
    {
        try
        {
            if (discoveredDevice.DescriptionLocation == null)
                return null;

            // Fetch the device description XML
            var descriptionXml = await _httpClient.GetStringAsync(discoveredDevice.DescriptionLocation);
            var xmlDoc = XDocument.Parse(descriptionXml);

            // Parse UPnP device description
            XNamespace ns = "urn:schemas-upnp-org:device-1-0";
            var deviceElement = xmlDoc.Descendants(ns + "device").FirstOrDefault();

            if (deviceElement == null)
                return null;

            var friendlyName = deviceElement.Element(ns + "friendlyName")?.Value?.Trim();
            var manufacturer = deviceElement.Element(ns + "manufacturer")?.Value?.Trim();
            var modelName = deviceElement.Element(ns + "modelName")?.Value?.Trim();
            var deviceType = deviceElement.Element(ns + "deviceType")?.Value?.Trim();

            // Build a descriptive string
            var deviceInfo = BuildDeviceDescription(friendlyName, manufacturer, modelName, deviceType);
            return deviceInfo;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error fetching UPnP device description from {Location}", discoveredDevice.DescriptionLocation);
            return null;
        }
    }

    private string? BuildDeviceDescription(string? friendlyName, string? manufacturer, string? modelName, string? deviceType)
    {
        // Extract meaningful device type from UPnP deviceType URN
        var typeHint = ExtractDeviceTypeHint(deviceType);

        // Build description with available information
        var descriptionParts = new List<string>();

        if (!string.IsNullOrEmpty(friendlyName) && friendlyName != manufacturer)
        {
            descriptionParts.Add(friendlyName);
        }
        else if (!string.IsNullOrEmpty(modelName))
        {
            if (!string.IsNullOrEmpty(manufacturer))
                descriptionParts.Add($"{manufacturer} {modelName}");
            else
                descriptionParts.Add(modelName);
        }
        else if (!string.IsNullOrEmpty(manufacturer))
        {
            descriptionParts.Add(manufacturer);
        }

        if (!string.IsNullOrEmpty(typeHint))
        {
            descriptionParts.Add(typeHint);
        }

        if (descriptionParts.Count == 0)
            return "UPnP Device";

        return string.Join(" - ", descriptionParts) + " (UPnP)";
    }

    private string? ExtractDeviceTypeHint(string? deviceTypeUrn)
    {
        if (string.IsNullOrEmpty(deviceTypeUrn))
            return null;

        // UPnP device types look like: urn:schemas-upnp-org:device:MediaServer:1
        var deviceTypeLower = deviceTypeUrn.ToLowerInvariant();

        if (deviceTypeLower.Contains("mediaserver"))
            return "Media Server";
        if (deviceTypeLower.Contains("mediarenderer"))
            return "Media Renderer";
        if (deviceTypeLower.Contains("printer"))
            return "Printer";
        if (deviceTypeLower.Contains("scanner"))
            return "Scanner";
        if (deviceTypeLower.Contains("router") || deviceTypeLower.Contains("gateway"))
            return "Router/Gateway";
        if (deviceTypeLower.Contains("tv") || deviceTypeLower.Contains("television"))
            return "Smart TV";
        if (deviceTypeLower.Contains("light"))
            return "Smart Light";
        if (deviceTypeLower.Contains("thermostat"))
            return "Thermostat";
        if (deviceTypeLower.Contains("camera"))
            return "Camera";
        if (deviceTypeLower.Contains("storage"))
            return "Network Storage";

        return null;
    }

    public void Dispose()
    {
        _deviceLocator?.Dispose();
        _httpClient?.Dispose();
        _cacheLock?.Dispose();
    }
}
