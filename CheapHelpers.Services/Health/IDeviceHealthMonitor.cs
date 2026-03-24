namespace CheapHelpers.Services.Health;

/// <summary>
/// Monitors multiple <see cref="IDeviceHealthCheck"/> instances on a configurable interval
/// and fires callbacks when health status transitions.
/// </summary>
public interface IDeviceHealthMonitor
{
    /// <summary>
    /// Callback invoked when a device's health status changes (healthy→unhealthy or back).
    /// </summary>
    Func<DeviceHealthResult, DeviceHealthResult, Task>? OnHealthChanged { get; set; }

    /// <summary>
    /// Gets the most recent health result for each registered device.
    /// </summary>
    IReadOnlyDictionary<string, DeviceHealthResult> CurrentStatus { get; }

    /// <summary>
    /// Runs all health checks immediately and returns their results.
    /// </summary>
    Task<IReadOnlyList<DeviceHealthResult>> CheckAllAsync(CancellationToken cancellationToken = default);
}
