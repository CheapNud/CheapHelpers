using CheapHelpers.Blazor.Hybrid.Models;

namespace CheapHelpers.Blazor.Hybrid.Abstractions;

/// <summary>
/// Platform-specific service interface for managing device push notification setup and registration.
/// Handles platform-specific token acquisition (FCM for Android, APNS for iOS, Web Push for Desktop),
/// device identification, and backend registration. Implemented separately for each target platform.
/// </summary>
public interface IDeviceInstallationService
{
    /// <summary>
    /// Gets the platform identifier (e.g., "apns", "fcm", "webpush")
    /// </summary>
    string Platform { get; }

    /// <summary>
    /// Gets the current push notification token/subscription
    /// </summary>
    string? Token { get; }

    /// <summary>
    /// Indicates whether push notifications are supported on this platform/device
    /// </summary>
    bool NotificationsSupported { get; }

    /// <summary>
    /// Indicates whether the device is currently registered with the backend
    /// </summary>
    bool IsRegistered { get; }

    /// <summary>
    /// Gets a unique identifier for this device
    /// </summary>
    string GetDeviceId();

    /// <summary>
    /// Creates a device installation object with optional tags for targeting
    /// </summary>
    DeviceInstallation GetDeviceInstallation(params string[] tags);

    /// <summary>
    /// Registers the device with the backend for the specified user
    /// </summary>
    Task<bool> RegisterDeviceAsync(string userId);

    /// <summary>
    /// Requests push notification permissions from the user
    /// </summary>
    Task<bool> RequestPermissionsAsync();

    /// <summary>
    /// Gets a unique, persistent device fingerprint for this device
    /// Used for device identification across app reinstalls
    /// </summary>
    string GetDeviceFingerprint();

    /// <summary>
    /// Checks if permissions have been granted without requesting them
    /// </summary>
    Task<bool> CheckPermissionsAsync();

    /// <summary>
    /// Event triggered when the push token is refreshed or updated
    /// </summary>
    [Obsolete("Use OnTokenReceived for initial token or OnTokenUpdated for refreshes")]
    event Action<string>? TokenRefreshed;

    /// <summary>
    /// Event triggered when a push token is received for the first time
    /// </summary>
    event Action<string>? OnTokenReceived;

    /// <summary>
    /// Event triggered when an existing push token is updated/refreshed
    /// </summary>
    event Action<string>? OnTokenUpdated;

    /// <summary>
    /// Gets comprehensive diagnostic information about the device's notification status
    /// </summary>
    /// <returns>DeviceNotificationStatus containing platform-specific diagnostics</returns>
    DeviceNotificationStatus GetNotificationStatus();
}
