using System.Diagnostics;
using CheapHelpers.Blazor.Hybrid.Abstractions;
using CheapHelpers.Blazor.Hybrid.Models;

namespace CheapHelpers.Blazor.Hybrid.Notifications.Core;

/// <summary>
/// Manages device registration state with smart permission flow
/// Checks with backend before requesting permissions to avoid spamming users
/// </summary>
public class DeviceRegistrationManager
{
    private readonly IPushNotificationBackend _backend;
    private readonly IDeviceInstallationService _deviceService;
    private readonly IPreferencesService _preferences;
    private const string DEVICE_ID_KEY = "device_unique_id";
    private const string LAST_PERMISSION_STATE_KEY = "last_permission_state";
    private string? _cachedDeviceId;

    public DeviceRegistrationManager(
        IPushNotificationBackend backend,
        IDeviceInstallationService deviceService,
        IPreferencesService preferences)
    {
        _backend = backend;
        _deviceService = deviceService;
        _preferences = preferences;
    }

    /// <summary>
    /// Check the current registration status of this device
    /// </summary>
    public async Task<DeviceRegistrationState> CheckDeviceStatusAsync(string userId)
    {
        try
        {
            // Small delay to ensure platform services are ready
            await Task.Delay(100);

            // Check if user previously denied permissions
            var lastPermissionState = _preferences.Get(LAST_PERMISSION_STATE_KEY, string.Empty);
            if (lastPermissionState == "denied")
            {
                Debug.WriteLine("User previously denied permissions");
                return DeviceRegistrationState.PermissionDenied;
            }

            // Get device identifier
            var deviceId = GetDeviceIdentifier();

            // Check backend for device registration
            var deviceInfo = await _backend.GetDeviceAsync(deviceId);

            if (deviceInfo == null)
            {
                return DeviceRegistrationState.NotRegistered;
            }

            if (!deviceInfo.IsActive)
            {
                Debug.WriteLine("Device registration exists but is inactive");
                return DeviceRegistrationState.NotRegistered;
            }

            // Check if registration is recent (within last 30 days)
            var tokenAge = DateTime.UtcNow - deviceInfo.LastUpdated;
            if (tokenAge.TotalDays > 30)
            {
                Debug.WriteLine($"Device registration expired (age: {tokenAge.TotalDays:F0} days)");
                return DeviceRegistrationState.NotRegistered;
            }

            // Store the device ID for future reference
            _preferences.Set($"device_token_id_{userId}", deviceInfo.DeviceId);

            return DeviceRegistrationState.Registered;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error checking device status: {ex.Message}");
            return DeviceRegistrationState.Failed;
        }
    }

    /// <summary>
    /// Determine if we should request permissions
    /// Only returns true if device is not registered and permissions not previously denied
    /// </summary>
    public async Task<bool> ShouldRequestPermissionsAsync(string userId)
    {
        var state = await CheckDeviceStatusAsync(userId);

        return state switch
        {
            DeviceRegistrationState.NotRegistered => true,
            DeviceRegistrationState.Failed => true, // Fail open - request permissions on error
            _ => false // Registered or PermissionDenied
        };
    }

    /// <summary>
    /// Register the device if needed (smart flow - checks backend first)
    /// </summary>
    public async Task<bool> RegisterDeviceIfNeededAsync(string userId)
    {
        try
        {
            var state = await CheckDeviceStatusAsync(userId);

            if (state == DeviceRegistrationState.Registered)
            {
                return true;
            }

            if (state == DeviceRegistrationState.PermissionDenied)
            {
                Debug.WriteLine("Cannot register device - permissions denied");
                return false;
            }

            // Attempt registration for NotRegistered or Failed states
            var registered = await _deviceService.RegisterDeviceAsync(userId);

            if (registered)
            {
                // Clear any previous permission denied state
                _preferences.Remove(LAST_PERMISSION_STATE_KEY);
            }
            else
            {
                Debug.WriteLine("Device registration failed");
            }

            return registered;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error during device registration: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Clean up old/inactive device registrations for this user
    /// </summary>
    public async Task CleanupOldDevicesAsync(string userId)
    {
        try
        {
            var userDevices = await _backend.GetUserDevicesAsync(userId);
            var currentPlatform = _deviceService.Platform;

            // Find old/inactive devices for the same platform
            var oldDevices = userDevices.Where(d =>
                d.Platform.Equals(currentPlatform, StringComparison.OrdinalIgnoreCase) &&
                (!d.IsActive || (DateTime.UtcNow - d.RegisteredAt).TotalDays > 90))
                .ToList();

            foreach (var device in oldDevices)
            {
                Debug.WriteLine($"Deactivating old device (ID: {device.DeviceId}, Age: {(DateTime.UtcNow - device.RegisteredAt).TotalDays:F0} days)");
                await _backend.DeactivateDeviceAsync(device.DeviceId);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error during device cleanup: {ex.Message}");
            // Non-critical error, don't throw
        }
    }

    /// <summary>
    /// Get or generate a unique device identifier
    /// </summary>
    public string GetDeviceIdentifier()
    {
        try
        {
            if (!string.IsNullOrEmpty(_cachedDeviceId))
                return _cachedDeviceId;

            // Check if we have a stored device ID
            var storedId = _preferences.Get(DEVICE_ID_KEY, string.Empty);
            if (!string.IsNullOrEmpty(storedId))
            {
                _cachedDeviceId = storedId;
                return storedId;
            }

            // Generate a new unique device ID using device fingerprint
            var deviceFingerprint = _deviceService.GetDeviceFingerprint();
            var deviceId = $"{_deviceService.Platform}_{deviceFingerprint}_{Guid.NewGuid():N}";

            // Store it for future use
            try
            {
                _preferences.Set(DEVICE_ID_KEY, deviceId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Could not store device ID: {ex.Message}");
            }

            _cachedDeviceId = deviceId;
            return deviceId;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting device identifier: {ex.Message}");
            // Return a fallback identifier
            return $"fallback_{Guid.NewGuid():N}";
        }
    }

    /// <summary>
    /// Store the permission state for future reference
    /// </summary>
    public void StorePermissionState(bool granted)
    {
        _preferences.Set(LAST_PERMISSION_STATE_KEY, granted ? "granted" : "denied");
    }
}

/// <summary>
/// Abstraction for platform-specific preferences storage
/// </summary>
public interface IPreferencesService
{
    string Get(string key, string defaultValue);
    void Set(string key, string value);
    void Remove(string key);
}
