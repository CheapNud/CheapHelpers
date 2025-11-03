using CheapHelpers.Networking.Core;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace CheapHelpers.Networking.Detection;

/// <summary>
/// Detects Windows systems based on common Windows service ports
/// </summary>
public class WindowsServicesDetector(ILogger<WindowsServicesDetector> logger, PortDetectionOptions portOptions) : IDeviceTypeDetector
{
    public int Priority => 30;

    public async Task<string?> DetectDeviceTypeAsync(string ipAddress)
    {
        try
        {
            string? detectionMethod = null;
            var isLikelyServer = false;

            foreach (var (windowsPort, method) in portOptions.WindowsServicePorts)
            {
                if (await IsPortOpen(ipAddress, windowsPort))
                {
                    detectionMethod = method;

                    isLikelyServer = windowsPort switch
                    {
                        3389 => true,  // RDP server
                        5985 or 5986 => true,  // WinRM server
                        135 or 139 or 445 => false,  // Could be client or server
                        _ => false
                    };

                    break;
                }
            }

            if (detectionMethod == null)
                return null;

            var deviceType = isLikelyServer
                ? $"Windows Server ({detectionMethod})"
                : $"Windows Client ({detectionMethod})";

            logger.LogInformation("Device type detected via Windows services: {Type}", deviceType);
            return deviceType;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Windows services detection failed for {IpAddress}", ipAddress);
            return null;
        }
    }

    private async Task<bool> IsPortOpen(string ipAddress, int servicePort)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(ipAddress, servicePort);

            if (await Task.WhenAny(connectTask, Task.Delay(portOptions.PortConnectionTimeoutMs)) != connectTask)
            {
                return false;
            }

            return client.Connected;
        }
        catch
        {
            return false;
        }
    }
}
