namespace CheapHelpers.Networking.Core;

/// <summary>
/// Interface for network scanning functionality
/// </summary>
public interface INetworkScanner : IDisposable
{
    /// <summary>
    /// Scans the entire network for devices
    /// </summary>
    /// <returns>List of discovered network devices</returns>
    Task<List<NetworkDevice>> ScanNetworkAsync();

    /// <summary>
    /// Scans a single device at the specified IP address
    /// </summary>
    /// <param name="ipAddress">The IP address to scan</param>
    /// <returns>List containing the discovered device (or empty if not found)</returns>
    Task<List<NetworkDevice>> ScanSingleDeviceAsync(string ipAddress);

    /// <summary>
    /// Starts continuous automatic scanning based on configured interval
    /// </summary>
    void StartScanning();

    /// <summary>
    /// Pauses continuous automatic scanning
    /// </summary>
    void PauseScanning();

    /// <summary>
    /// Resumes continuous automatic scanning
    /// </summary>
    void ResumeScanning();

    /// <summary>
    /// Event fired when scan progress updates are available
    /// </summary>
    event Action<string>? ScanProgress;

    /// <summary>
    /// Event fired when a device is discovered or updated
    /// </summary>
    event Action<NetworkDevice>? DeviceDiscovered;

    /// <summary>
    /// Event fired when scanning state changes (started/stopped)
    /// </summary>
    event Action<bool>? ScanningStateChanged;

    /// <summary>
    /// Event fired when the next scheduled scan time changes
    /// </summary>
    event Action<DateTime?>? NextScanTimeChanged;

    /// <summary>
    /// Event fired when the last scan completion time changes
    /// </summary>
    event Action<DateTime?>? LastScanTimeChanged;

    /// <summary>
    /// Gets whether a scan is currently in progress
    /// </summary>
    bool IsScanning { get; }

    /// <summary>
    /// Gets the time of the last completed scan
    /// </summary>
    DateTime? LastScanTime { get; }

    /// <summary>
    /// Gets the time of the next scheduled scan
    /// </summary>
    DateTime? NextScanTime { get; }

    /// <summary>
    /// Gets the list of all discovered devices
    /// </summary>
    List<NetworkDevice> DiscoveredDevices { get; }
}
