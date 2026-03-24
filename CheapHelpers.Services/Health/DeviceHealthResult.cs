namespace CheapHelpers.Services.Health;

/// <summary>
/// Result of a device health check.
/// </summary>
public record DeviceHealthResult(
    bool IsHealthy,
    string DeviceName,
    string? ErrorMessage,
    TimeSpan ResponseTime,
    DateTimeOffset CheckedAt);
