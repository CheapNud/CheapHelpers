using CheapHelpers.Networking.Core;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Text;

namespace CheapHelpers.Networking.Detection;

/// <summary>
/// Detects device types based on HTTP/HTTPS headers
/// </summary>
public class HttpDetector(ILogger<HttpDetector> logger, PortDetectionOptions portOptions) : IDeviceTypeDetector
{
    public int Priority => 50;

    public async Task<string?> DetectDeviceTypeAsync(string ipAddress)
    {
        try
        {
            // Try custom IoT ports first
            foreach (var customPort in portOptions.CustomIoTPorts)
            {
                var detectedType = await ProbeHttpPort(ipAddress, customPort);
                if (!string.IsNullOrEmpty(detectedType))
                {
                    logger.LogInformation("Device type detected via HTTP on port {Port}: {Type}", customPort, detectedType);
                    return detectedType;
                }
            }

            // Try standard HTTP ports
            foreach (var standardPort in portOptions.StandardHttpPorts)
            {
                var detectedType = await ProbeHttpPort(ipAddress, standardPort);
                if (!string.IsNullOrEmpty(detectedType))
                {
                    logger.LogInformation("Device type detected via HTTP on port {Port}: {Type}", standardPort, detectedType);
                    return detectedType;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "HTTP detection failed for {IpAddress}", ipAddress);
            return null;
        }
    }

    private async Task<string?> ProbeHttpPort(string ipAddress, int port)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(ipAddress, port);

            if (await Task.WhenAny(connectTask, Task.Delay(portOptions.PortConnectionTimeoutMs)) != connectTask)
            {
                return null;
            }

            if (!client.Connected) return null;

            using var stream = client.GetStream();
            stream.ReadTimeout = portOptions.PortConnectionTimeoutMs;

            var httpRequest = $"HEAD / HTTP/1.1\r\nHost: {ipAddress}\r\nConnection: close\r\n\r\n";
            var requestBytes = Encoding.ASCII.GetBytes(httpRequest);
            await stream.WriteAsync(requestBytes);

            var buffer = new byte[1024];
            var readTask = stream.ReadAsync(buffer).AsTask();

            if (await Task.WhenAny(readTask, Task.Delay(portOptions.PortConnectionTimeoutMs)) != readTask)
            {
                return null;
            }

            var bytesRead = await readTask;
            var httpResponse = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            return ParseHttpOsInfo(httpResponse, port);
        }
        catch (Exception ex)
        {
            logger.LogDebug("HTTP probe failed for {IpAddress}:{Port} - {Message}", ipAddress, port, ex.Message);
            return null;
        }
    }

    private string? ParseHttpOsInfo(string httpResponse, int httpPort)
    {
        var responseLines = httpResponse.Split('\n');

        foreach (var line in responseLines)
        {
            var lowerLine = line.ToLowerInvariant();

            if (lowerLine.StartsWith("server:"))
            {
                if (lowerLine.Contains("microsoft-iis"))
                {
                    if (lowerLine.Contains("iis/10.0"))
                        return "Windows Server 2016/2019/2022 (HTTP)";
                    if (lowerLine.Contains("iis/8.5"))
                        return "Windows Server 2012 R2 (HTTP)";
                    if (lowerLine.Contains("iis/8.0"))
                        return "Windows Server 2012 (HTTP)";

                    return "Windows Server (HTTP)";
                }
                if (lowerLine.Contains("kestrel"))
                    return "Windows/Linux (.NET) (HTTP)";
                if (lowerLine.Contains("apache"))
                    return "Linux Server (HTTP)";
                if (lowerLine.Contains("nginx"))
                    return "Linux Server (HTTP)";
                if (lowerLine.Contains("lighttpd"))
                    return "Linux Server (HTTP)";
            }

            if (lowerLine.Contains("asp.net"))
                return "Windows Server (.NET) (HTTP)";
            if (lowerLine.Contains("microsoft-httpapi"))
                return "Windows Server (HTTP)";
        }

        if (httpResponse.Contains("HTTP/1."))
        {
            return httpPort switch
            {
                5000 => "Unknown (.NET App) (HTTP)",
                8000 or 8080 => "Unknown (Web App) (HTTP)",
                8443 => "Unknown (Secure Web) (HTTP)",
                80 or 443 => "Unknown (HTTP)",
                _ => "Unknown (HTTP)"
            };
        }

        return null;
    }
}
