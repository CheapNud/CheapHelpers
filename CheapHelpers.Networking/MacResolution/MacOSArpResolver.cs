using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;

namespace CheapHelpers.Networking.MacResolution;

/// <summary>
/// macOS-specific MAC address resolver using the 'arp -a' command
/// </summary>
public class MacOSArpResolver(ILogger<MacOSArpResolver> logger) : IMacAddressResolver
{
    public async Task<Dictionary<string, string>> GetArpTableAsync()
    {
        var arpTable = new Dictionary<string, string>();

        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "arp",
                Arguments = "-a",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var arpProcess = Process.Start(processInfo);
            if (arpProcess != null)
            {
                var arpOutput = await arpProcess.StandardOutput.ReadToEndAsync();
                await arpProcess.WaitForExitAsync();

                logger.LogDebug("ARP table output received");

                var outputLines = arpOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in outputLines)
                {
                    if (line.Contains('(') && line.Contains(')') && line.Contains(" at "))
                    {
                        var startIp = line.IndexOf('(') + 1;
                        var endIp = line.IndexOf(')');
                        var ipAddress = line.Substring(startIp, endIp - startIp);

                        var atIndex = line.IndexOf(" at ");
                        if (atIndex > 0 && atIndex + 4 < line.Length)
                        {
                            var macPart = line[(atIndex + 4)..];
                            var macAddress = macPart.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

                            if (!string.IsNullOrEmpty(macAddress) &&
                                IPAddress.TryParse(ipAddress, out _) &&
                                IsValidMacAddress(macAddress))
                            {
                                macAddress = NormalizeMacAddress(macAddress);
                                arpTable[ipAddress] = macAddress;
                                logger.LogDebug("ARP entry: {IpAddress} -> {MacAddress}", ipAddress, macAddress);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing ARP command");
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
