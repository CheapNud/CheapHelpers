namespace CheapHelpers.Services.Health;

/// <summary>
/// Defines a health check for a single device or endpoint.
/// </summary>
public interface IDeviceHealthCheck
{
    /// <summary>
    /// The display name of the device being checked.
    /// </summary>
    string DeviceName { get; }

    /// <summary>
    /// Performs the health check and returns the result.
    /// </summary>
    Task<DeviceHealthResult> CheckAsync(CancellationToken cancellationToken = default);
}
