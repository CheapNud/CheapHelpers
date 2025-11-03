using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;

namespace CheapHelpers.Networking.MacResolution;

/// <summary>
/// Linux-specific MAC address resolver using the 'ip neigh show' command
/// </summary>
public class LinuxArpResolver(ILogger<LinuxArpResolver> logger) : IMacAddressResolver
{
    public async Task<Dictionary<string, string>> GetArpTableAsync()
    {
        var arpTable = new Dictionary<string, string>();

        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "ip",
                Arguments = "neigh show",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var ipProcess = Process.Start(processInfo);
            if (ipProcess != null)
            {
                var ipOutput = await ipProcess.StandardOutput.ReadToEndAsync();
                await ipProcess.WaitForExitAsync();

                logger.LogDebug("IP neighbor table output received");

                var outputLines = ipOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in outputLines)
                {
                    var lineParts = line.Trim().Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);

                    if (lineParts.Length >= 5 && lineParts[2] == "lladdr")
                    {
                        var ipAddress = lineParts[0];
                        var macAddress = lineParts[4];

                        if (IPAddress.TryParse(ipAddress, out _) && IsValidMacAddress(macAddress))
                        {
                            macAddress = NormalizeMacAddress(macAddress);
                            arpTable[ipAddress] = macAddress;
                            logger.LogDebug("ARP entry: {IpAddress} -> {MacAddress}", ipAddress, macAddress);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing IP neigh command");
        }

        return arpTable;
    }

    private static bool IsValidMacAddress(string macAddress)
    {
        return macAddress.Length >= 12 &&
               (macAddress.Contains('-') || macAddress.Contains(':') ||
                (macAddress.Length == 12 && macAddress.All(char.IsLetterOrDigit)));
    }

    private static string NormalizeMacAddress(string macAddress)
    {
        var cleanMac = macAddress.Replace("-", "").Replace(":", "").ToUpper();

        if (cleanMac.Length == 12)
        {
            return string.Join(":",
                Enumerable.Range(0, 6)
                .Select(i => cleanMac.Substring(i * 2, 2)));
        }

        return macAddress;
    }
}
