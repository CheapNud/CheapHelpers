namespace CheapHelpers.Networking.Discovery;

/// <summary>
/// Represents a device discovered via mDNS/Zeroconf service advertisement.
/// Both IP fields are nullable to support split A/AAAA record caching —
/// a device may arrive with only an AAAA initially, then get its A record merged later.
/// </summary>
public sealed record MdnsDevice
{
    /// <summary>Instance name from PTR record (e.g., "HomeWizard P1 Meter").</summary>
    public required string InstanceName { get; init; }

    /// <summary>The service type that was queried (e.g., "_hwenergy._tcp").</summary>
    public required string ServiceType { get; init; }

    /// <summary>Hostname from SRV record, without trailing dot or .local suffix.</summary>
    public string? HostName { get; init; }

    /// <summary>IPv4 address from A record.</summary>
    public string? IPv4Address { get; init; }

    /// <summary>IPv6 address from AAAA record.</summary>
    public string? IPv6Address { get; init; }

    /// <summary>Port from SRV record.</summary>
    public int Port { get; init; }

    /// <summary>Parsed TXT record key-value pairs (case-insensitive keys).</summary>
    public IReadOnlyDictionary<string, string> TxtRecords { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>When this device was first seen in the current listener session.</summary>
    public DateTimeOffset FirstSeen { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When this device's records were last updated (e.g., a late A record merged in).</summary>
    public DateTimeOffset LastUpdated { get; init; } = DateTimeOffset.UtcNow;
}
