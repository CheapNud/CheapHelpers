namespace CheapHelpers.Networking.Core;

/// <summary>
/// Configuration options for the network scanner
/// </summary>
public class NetworkScannerOptions
{
    /// <summary>
    /// Gets or sets the interval in minutes for continuous scanning
    /// </summary>
    public int ScanIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum number of concurrent connections during scanning
    /// </summary>
    public int MaxConcurrentConnections { get; set; } = 20;

    /// <summary>
    /// Gets or sets the ping timeout in milliseconds
    /// </summary>
    public int PingTimeoutMs { get; set; } = 2000;

    /// <summary>
    /// Gets or sets the HTTP request timeout in milliseconds
    /// </summary>
    public int HttpTimeoutMs { get; set; } = 2000;

    /// <summary>
    /// Gets or sets the SSH connection timeout in milliseconds
    /// </summary>
    public int SshTimeoutMs { get; set; } = 2000;

    /// <summary>
    /// Gets or sets the subnet base to scan (e.g., "192.168.1" for 192.168.1.x)
    /// Set to "auto" to auto-detect local subnet
    /// </summary>
    public string SubnetBase { get; set; } = "auto";

    /// <summary>
    /// Gets or sets the starting IP address (last octet) for scanning
    /// </summary>
    public int StartIp { get; set; } = 1;

    /// <summary>
    /// Gets or sets the ending IP address (last octet) for scanning
    /// </summary>
    public int EndIp { get; set; } = 254;

    /// <summary>
    /// Gets or sets the delay in milliseconds to pause every N devices during scanning
    /// to prevent network flooding
    /// </summary>
    public int NetworkThrottleDelayMs { get; set; } = 50;

    /// <summary>
    /// Gets or sets how many devices to scan before applying the throttle delay
    /// </summary>
    public int DevicesBeforeThrottle { get; set; } = 10;

    /// <summary>
    /// Gets or sets whether to enable continuous automatic scanning
    /// </summary>
    public bool EnableContinuousScanning { get; set; } = true;
}
