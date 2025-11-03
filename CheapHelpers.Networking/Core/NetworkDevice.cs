namespace CheapHelpers.Networking.Core;

/// <summary>
/// Represents a network device discovered during scanning
/// </summary>
public class NetworkDevice
{
    /// <summary>
    /// Gets or sets the device name (hostname)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the device type (e.g., "Windows Server", "Linux", "IoT Device")
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the IPv4 address
    /// </summary>
    public string IPv4Address { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the IPv6 address
    /// </summary>
    public string IPv6Address { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the device is currently online
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Gets or sets the last time the device was seen
    /// </summary>
    public DateTime LastSeen { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets or sets the MAC address
    /// </summary>
    public string MacAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ping response time
    /// </summary>
    public TimeSpan ResponseTime { get; set; }
}
