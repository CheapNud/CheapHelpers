using CheapHelpers.Networking.Core;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Text;

namespace CheapHelpers.Networking.Detection;

/// <summary>
/// Detects Linux/Unix systems based on SSH banner
/// </summary>
public class SshDetector(ILogger<SshDetector> logger, PortDetectionOptions portOptions) : IDeviceTypeDetector
{
    public int Priority => 40;

    public async Task<string?> DetectDeviceTypeAsync(string ipAddress)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(ipAddress, portOptions.SshPort);

            if (await Task.WhenAny(connectTask, Task.Delay(portOptions.PortConnectionTimeoutMs)) != connectTask)
            {
                return null;
            }

            if (!client.Connected) return null;

            using var stream = client.GetStream();
            stream.ReadTimeout = portOptions.PortConnectionTimeoutMs;

            var buffer = new byte[256];
            var bytesRead = await stream.ReadAsync(buffer);
            var sshBanner = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            var detectedType = ParseSshBanner(sshBanner);
            if (!string.IsNullOrEmpty(detectedType))
            {
                logger.LogInformation("Device type detected via SSH: {Type}", detectedType);
            }

            return detectedType;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "SSH detection failed for {IpAddress}", ipAddress);
            return null;
        }
    }

    private string? ParseSshBanner(string sshBanner)
    {
        var lowerBanner = sshBanner.ToLowerInvariant();

        if (lowerBanner.Contains("ubuntu"))
            return "Ubuntu Linux (SSH)";
        if (lowerBanner.Contains("debian"))
            return "Debian Linux (SSH)";
        if (lowerBanner.Contains("raspbian"))
            return "Raspberry Pi (SSH)";
        if (lowerBanner.Contains("openssh"))
            return "Linux/Unix (SSH)";

        return lowerBanner.Contains("ssh") ? "Unknown (SSH)" : null;
    }
}
