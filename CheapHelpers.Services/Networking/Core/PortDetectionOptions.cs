namespace CheapHelpers.Networking.Core;

/// <summary>
/// Configuration options for port-based device detection
/// </summary>
public class PortDetectionOptions
{
    /// <summary>
    /// Gets or sets custom IoT device ports to probe
    /// </summary>
    public List<int> CustomIoTPorts { get; set; } = [5000, 8000, 8080, 8443];

    /// <summary>
    /// Gets or sets standard HTTP ports to probe
    /// </summary>
    public List<int> StandardHttpPorts { get; set; } = [80, 443];

    /// <summary>
    /// Gets or sets custom service endpoints with their port numbers and descriptions
    /// </summary>
    public Dictionary<int, string> ServiceEndpoints { get; set; } = new()
    {
        { 8974, "IoT Service Endpoint 3" },
        { 8975, "IoT Service Endpoint 1" },
        { 12050, "IoT Service Endpoint 2" }
    };

    /// <summary>
    /// Gets or sets Windows-specific service ports to probe
    /// </summary>
    public Dictionary<int, string> WindowsServicePorts { get; set; } = new()
    {
        { 3389, "Remote Desktop Protocol" },
        { 5985, "WinRM HTTP" },
        { 5986, "WinRM HTTPS" },
        { 445, "SMB" },
        { 139, "NetBIOS" },
        { 135, "RPC" }
    };

    /// <summary>
    /// Gets or sets the SSH port for Linux/Unix detection
    /// </summary>
    public int SshPort { get; set; } = 22;

    /// <summary>
    /// Gets or sets the timeout in milliseconds for port connection attempts
    /// </summary>
    public int PortConnectionTimeoutMs { get; set; } = 1000;
}
