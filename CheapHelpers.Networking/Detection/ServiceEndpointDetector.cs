using CheapHelpers.Networking.Core;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace CheapHelpers.Networking.Detection;

/// <summary>
/// Detects devices based on custom service endpoint ports
/// </summary>
public class ServiceEndpointDetector(ILogger<ServiceEndpointDetector> logger, PortDetectionOptions portOptions) : IDeviceTypeDetector
{
    public int Priority => 60;

    public async Task<string?> DetectDeviceTypeAsync(string ipAddress)
    {
        try
        {
            foreach (var (endpointPort, description) in portOptions.ServiceEndpoints)
            {
                if (await IsPortOpen(ipAddress, endpointPort))
                {
                    logger.LogInformation("Service endpoint detected on {IpAddress}:{Port} - {Description}", ipAddress, endpointPort, description);
                    return description;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Service endpoint detection failed for {IpAddress}", ipAddress);
            return null;
        }
    }

    private async Task<bool> IsPortOpen(string ipAddress, int endpointPort)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(ipAddress, endpointPort);

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
