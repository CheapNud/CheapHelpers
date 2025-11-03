using CheapHelpers.Networking.Core;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml.Linq;

namespace CheapHelpers.Networking.Detection;

/// <summary>
/// Detects devices using UPnP/SSDP discovery protocol (custom implementation)
/// </summary>
/// <remarks>
/// Implements SSDP (Simple Service Discovery Protocol) for UPnP device discovery.
/// Sends M-SEARCH multicast requests to 239.255.255.250:1900 and parses responses.
/// </remarks>
public class UpnpDetector : IDeviceTypeDetector, IDisposable
{
    private readonly ILogger<UpnpDetector> _logger;
    private readonly PortDetectionOptions _portOptions;
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, string> _deviceCache = new();
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private readonly CancellationTokenSource _cts = new();
    private readonly List<UdpClient> _udpClients = new();
    private bool _isStarted;

    // SSDP Constants
    private const string SsdpMulticastAddress = "239.255.255.250";
    private const int SsdpPort = 1900;
    private const int DiscoveryTimeoutMs = 3000;

    public int Priority => 90; // Higher priority than HTTP - UPnP is very reliable

    public UpnpDetector(ILogger<UpnpDetector> logger, Microsoft.Extensions.Options.IOptions<PortDetectionOptions> portOptions)
    {
        _logger = logger;
        _portOptions = portOptions.Value;
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

            // Start discovery if not already running
            if (!_isStarted)
            {
                await StartDiscoveryAsync();
            }

            // Trigger a fresh search
            await TriggerSearchAsync();

            // Wait for discoveries to come in
            await Task.Delay(2000);

            // Check cache again
            await _cacheLock.WaitAsync();
            try
            {
                if (_deviceCache.TryGetValue(ipAddress, out var discoveredType))
                {
                    _logger.LogInformation("UPnP device detected: {DeviceInfo}", discoveredType);
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
            _logger.LogDebug(ex, "UPnP detection failed for {IpAddress}", ipAddress);
            return null;
        }
    }

    private async Task StartDiscoveryAsync()
    {
        if (_isStarted)
            return;

        _isStarted = true;

        try
        {
            // Get all local IP addresses
            var localAddresses = GetLocalIpAddresses();

            foreach (var localAddress in localAddresses)
            {
                try
                {
                    // Create UDP client for this interface
                    var udpClient = CreateUdpClient(localAddress);
                    _udpClients.Add(udpClient);

                    // Start listening for responses
                    _ = Task.Run(() => ListenForResponsesAsync(udpClient), _cts.Token);

                    _logger.LogDebug("Started UPnP listener on {IpAddress}", localAddress);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to start UPnP listener on {IpAddress}", localAddress);
                }
            }

            // Background periodic search every 30 seconds
            _ = Task.Run(async () =>
            {
                try
                {
                    while (!_cts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(30), _cts.Token);
                        await TriggerSearchAsync();
                    }
                }
                catch (OperationCanceledException)
                {
                    // Normal shutdown
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "UPnP background search failed");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting UPnP discovery");
        }
    }

    private UdpClient CreateUdpClient(IPAddress localAddress)
    {
        var udpClient = new UdpClient();
        udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udpClient.Client.Bind(new IPEndPoint(localAddress, 0)); // Bind to any available port

        // Join multicast group
        var multicastAddress = IPAddress.Parse(SsdpMulticastAddress);
        udpClient.JoinMulticastGroup(multicastAddress, localAddress);

        return udpClient;
    }

    private async Task TriggerSearchAsync()
    {
        var mSearchMessage = BuildMSearchMessage();

        foreach (var udpClient in _udpClients)
        {
            try
            {
                var messageBytes = Encoding.UTF8.GetBytes(mSearchMessage);
                var multicastEndpoint = new IPEndPoint(IPAddress.Parse(SsdpMulticastAddress), SsdpPort);

                await udpClient.SendAsync(messageBytes, messageBytes.Length, multicastEndpoint);
                _logger.LogDebug("Sent M-SEARCH request");
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error sending M-SEARCH request");
            }
        }
    }

    private string BuildMSearchMessage()
    {
        // SSDP M-SEARCH request format (HTTP over UDP)
        var sb = new StringBuilder();
        sb.AppendLine("M-SEARCH * HTTP/1.1");
        sb.AppendLine($"HOST: {SsdpMulticastAddress}:{SsdpPort}");
        sb.AppendLine("MAN: \"ssdp:discover\"");
        sb.AppendLine("MX: 3"); // Maximum wait time in seconds
        sb.AppendLine("ST: ssdp:all"); // Search for all devices
        sb.AppendLine(); // Empty line marks end of headers
        return sb.ToString();
    }

    private async Task ListenForResponsesAsync(UdpClient udpClient)
    {
        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    // Set receive timeout
                    udpClient.Client.ReceiveTimeout = DiscoveryTimeoutMs;

                    var result = await udpClient.ReceiveAsync(_cts.Token);
                    var responseText = Encoding.UTF8.GetString(result.Buffer);

                    await ProcessSsdpResponseAsync(responseText);
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
                {
                    // Timeout is normal, continue listening
                }
                catch (OperationCanceledException)
                {
                    // Normal shutdown
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error receiving SSDP response");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UPnP listener crashed");
        }
    }

    private async Task ProcessSsdpResponseAsync(string responseText)
    {
        try
        {
            // Parse HTTP-style headers
            var headers = ParseHttpHeaders(responseText);

            if (!headers.TryGetValue("LOCATION", out var location) || string.IsNullOrEmpty(location))
                return;

            _logger.LogDebug("UPnP device discovered: {Location}", location);

            // Extract IP from location URL
            if (Uri.TryCreate(location, UriKind.Absolute, out var locationUri))
            {
                var ipAddress = locationUri.Host;

                // Fetch and parse device description asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var serverInfo = headers.TryGetValue("SERVER", out var server) ? server : null;
                        var deviceInfo = await GetDeviceInfoAsync(location, serverInfo);

                        if (!string.IsNullOrEmpty(deviceInfo))
                        {
                            await _cacheLock.WaitAsync();
                            try
                            {
                                _deviceCache[ipAddress] = deviceInfo;
                                _logger.LogDebug("Cached UPnP info for {IpAddress}: {Info}", ipAddress, deviceInfo);
                            }
                            finally
                            {
                                _cacheLock.Release();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error processing UPnP device at {IpAddress}", ipAddress);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error parsing SSDP response");
        }
    }

    private Dictionary<string, string> ParseHttpHeaders(string httpText)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lines = httpText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines.Skip(1)) // Skip status line (HTTP/1.1 200 OK)
        {
            var colonIndex = line.IndexOf(':');
            if (colonIndex > 0)
            {
                var key = line[..colonIndex].Trim();
                var value = line[(colonIndex + 1)..].Trim();
                headers[key] = value;
            }
        }

        return headers;
    }

    private async Task<string?> GetDeviceInfoAsync(string location, string? serverInfo)
    {
        try
        {
            // Fetch the device description XML
            var descriptionXml = await _httpClient.GetStringAsync(location);

            // Parse XML to extract device info
            var xmlDoc = XDocument.Parse(descriptionXml);
            XNamespace upnpNs = "urn:schemas-upnp-org:device-1-0";
            var deviceElement = xmlDoc.Descendants(upnpNs + "device").FirstOrDefault();

            if (deviceElement == null)
            {
                // Fallback to server info
                return !string.IsNullOrEmpty(serverInfo)
                    ? $"{serverInfo} (UPnP)"
                    : null;
            }

            var friendlyName = deviceElement.Element(upnpNs + "friendlyName")?.Value?.Trim();
            var manufacturer = deviceElement.Element(upnpNs + "manufacturer")?.Value?.Trim();
            var modelName = deviceElement.Element(upnpNs + "modelName")?.Value?.Trim();
            var deviceType = deviceElement.Element(upnpNs + "deviceType")?.Value?.Trim();

            var deviceInfo = BuildDeviceDescription(friendlyName, manufacturer, modelName, deviceType);
            return deviceInfo;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error fetching UPnP device description from {Location}", location);

            // Fallback: use server info
            return !string.IsNullOrEmpty(serverInfo)
                ? $"{serverInfo} (UPnP)"
                : null;
        }
    }

    private string? BuildDeviceDescription(string? friendlyName, string? manufacturer, string? modelName, string? deviceType)
    {
        var typeHint = ExtractDeviceTypeHint(deviceType);
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

        return descriptionParts.Count == 0
            ? "UPnP Device"
            : string.Join(" - ", descriptionParts) + " (UPnP)";
    }

    private string? ExtractDeviceTypeHint(string? deviceTypeUrn)
    {
        if (string.IsNullOrEmpty(deviceTypeUrn))
            return null;

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

    private List<IPAddress> GetLocalIpAddresses()
    {
        var addresses = new List<IPAddress>();
        try
        {
            var hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var address in hostEntry.AddressList)
            {
                // Only IPv4 addresses
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    addresses.Add(address);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting local IP addresses");
        }

        return addresses;
    }

    public void Dispose()
    {
        _cts?.Cancel();

        foreach (var udpClient in _udpClients)
        {
            try
            {
                udpClient?.Close();
                udpClient?.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }

        _httpClient?.Dispose();
        _cacheLock?.Dispose();
        _cts?.Dispose();
    }
}
