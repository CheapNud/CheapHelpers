namespace CheapHelpers.Networking.Detection;

/// <summary>
/// Interface for detecting device types on the network
/// </summary>
public interface IDeviceTypeDetector
{
    /// <summary>
    /// Attempts to detect the device type for the given IP address
    /// </summary>
    /// <param name="ipAddress">The IP address to check</param>
    /// <returns>The detected device type, or null if detection failed</returns>
    Task<string?> DetectDeviceTypeAsync(string ipAddress);

    /// <summary>
    /// Gets the priority of this detector. Higher priority detectors run first.
    /// </summary>
    int Priority { get; }
}
