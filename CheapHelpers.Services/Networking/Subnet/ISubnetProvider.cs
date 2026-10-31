namespace CheapHelpers.Networking.Subnet;

/// <summary>
/// Interface for providing subnets to scan
/// </summary>
public interface ISubnetProvider
{
    /// <summary>
    /// Gets the list of subnet bases to scan (e.g., "192.168.1" for 192.168.1.x)
    /// </summary>
    /// <returns>List of subnet bases</returns>
    Task<List<string>> GetSubnetsToScanAsync();
}
