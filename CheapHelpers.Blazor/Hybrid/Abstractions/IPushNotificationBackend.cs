using CheapHelpers.Blazor.Hybrid.Models;

namespace CheapHelpers.Blazor.Hybrid.Abstractions;

/// <summary>
/// Abstraction for push notification backend communication
/// Allows different implementations (Azure Notification Hubs, Firebase Admin SDK, OneSignal, custom API)
/// </summary>
public interface IPushNotificationBackend
{
    /// <summary>
    /// Register or update a device installation with the backend
    /// </summary>
    Task<bool> RegisterDeviceAsync(DeviceInstallation device);

    /// <summary>
    /// Get device information from the backend
    /// </summary>
    Task<DeviceInfo?> GetDeviceAsync(string deviceId);

    /// <summary>
    /// Get all devices registered for a specific user
    /// </summary>
    Task<List<DeviceInfo>> GetUserDevicesAsync(string userId);

    /// <summary>
    /// Deactivate/unregister a device
    /// </summary>
    Task<bool> DeactivateDeviceAsync(string deviceId);

    /// <summary>
    /// Send a push notification
    /// </summary>
    Task<SendNotificationResult> SendNotificationAsync(NotificationPayload payload);

    /// <summary>
    /// Send a test notification to verify setup
    /// </summary>
    Task<SendNotificationResult> SendTestNotificationAsync(string deviceId);
}

/// <summary>
/// Result of a notification send operation
/// </summary>
public class SendNotificationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
}
