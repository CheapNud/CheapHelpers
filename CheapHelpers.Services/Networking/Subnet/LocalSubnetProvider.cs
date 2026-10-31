using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;

namespace CheapHelpers.Networking.Subnet;

/// <summary>
/// Provides local subnet information by detecting the local machine's IP address
/// </summary>
public class LocalSubnetProvider(ILogger<LocalSubnetProvider> logger) : ISubnetProvider
{
    public Task<List<string>> GetSubnetsToScanAsync()
    {
        var subnets = new List<string>();

        try
        {
            var hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            var localIp = hostEntry.AddressList
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                .FirstOrDefault();

            if (localIp != null)
            {
                var ipString = localIp.ToString();
                var networkBase = GetNetworkBase(ipString);
                subnets.Add(networkBase);

                logger.LogInformation("Local IP: {LocalIp}, Subnet: {Subnet}.x", ipString, networkBase);
            }
            else
            {
                logger.LogWarning("Could not determine local IP address");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error determining local subnet");
        }

        return Task.FromResult(subnets);
    }

    private static string GetNetworkBase(string ipAddress)
    {
        var ipParts = ipAddress.Split('.');
        return $"{ipParts[0]}.{ipParts[1]}.{ipParts[2]}";
    }
}
