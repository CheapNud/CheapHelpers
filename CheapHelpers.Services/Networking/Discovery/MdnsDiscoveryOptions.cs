namespace CheapHelpers.Networking.Discovery;

/// <summary>
/// Configuration options for <see cref="IMdnsDiscoveryService"/>.
/// </summary>
public class MdnsDiscoveryOptions
{
    /// <summary>Default timeout for one-shot <see cref="IMdnsDiscoveryService.DiscoverAsync"/> calls.</summary>
    public TimeSpan ScanTimeout { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>How often to re-send the mDNS query during a scan to catch late responders.</summary>
    public TimeSpan QueryInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Whether to process IPv6 (AAAA) records. Default true.</summary>
    public bool UseIPv6 { get; set; } = true;

    /// <summary>Whether to process IPv4 (A) records. Default true.</summary>
    public bool UseIPv4 { get; set; } = true;

    /// <summary>
    /// Optional filter: only return devices whose InstanceName contains this
    /// substring (case-insensitive). Null means no filter.
    /// </summary>
    public string? InstanceNameFilter { get; set; }
}
