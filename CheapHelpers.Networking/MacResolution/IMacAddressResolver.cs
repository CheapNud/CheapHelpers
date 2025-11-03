namespace CheapHelpers.Networking.MacResolution;

/// <summary>
/// Interface for resolving MAC addresses from IP addresses
/// </summary>
public interface IMacAddressResolver
{
    /// <summary>
    /// Gets the ARP table mapping IP addresses to MAC addresses
    /// </summary>
    /// <returns>Dictionary mapping IP addresses to MAC addresses</returns>
    Task<Dictionary<string, string>> GetArpTableAsync();
}
