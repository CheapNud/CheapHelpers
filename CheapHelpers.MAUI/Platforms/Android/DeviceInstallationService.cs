using Android.Gms.Common;
using CheapHelpers.Blazor.Hybrid.Abstractions;
using CheapHelpers.Blazor.Hybrid.Models;
using System.Diagnostics;
using static Android.Provider.Settings;

namespace CheapHelpers.MAUI.Platforms.Android;

/// <summary>
/// Android implementation of device installation service using Firebase Cloud Messaging (FCM)
/// </summary>
public class DeviceInstallationService : IDeviceInstallationService
{
    private string _token = string.Empty;
    private bool? _notificationsSupportedCache;
    private string? _deviceIdCache;

    /// <summary>
    /// Platform identifier for FCM
    /// </summary>
    public string Platform => "fcmv1";

    /// <summary>
    /// FCM device token
    /// </summary>
    public string? Token => string.IsNullOrEmpty(_token) ? null : _token;

    /// <summary>
    /// Whether the device has been registered (has a token)
    /// </summary>
    public bool IsRegistered => !string.IsNullOrEmpty(_token);

    /// <summary>
    /// Event fired when FCM token is refreshed
    /// </summary>
    public event Action<string>? TokenRefreshed;

    /// <summary>
    /// Set the FCM token (called from FirebaseMessagingService when token is received)
    /// </summary>
    /// <param name="token">FCM device token</param>
    public void SetToken(string token)
    {
        var oldToken = _token;
        _token = token ?? string.Empty;

        if (!string.IsNullOrEmpty(_token) && _token != oldToken)
        {
            Debug.WriteLine($"FCM token set: {_token[..Math.Min(8, _token.Length)]}...");
            TokenRefreshed?.Invoke(_token);
        }
    }

    /// <summary>
    /// Whether notifications are supported on this device
    /// </summary>
    public bool NotificationsSupported
    {
        get
        {
            if (_notificationsSupportedCache.HasValue)
                return _notificationsSupportedCache.Value;

            _notificationsSupportedCache = CheckNotificationSupport();
            return _notificationsSupportedCache.Value;
        }
    }

    /// <summary>
    /// Get the Android device ID (Secure.AndroidId)
    /// </summary>
    public string GetDeviceId()
    {
        if (!string.IsNullOrEmpty(_deviceIdCache))
            return _deviceIdCache;

        try
        {
            _deviceIdCache = Secure.GetString(Microsoft.Maui.ApplicationModel.Platform.AppContext.ContentResolver, Secure.AndroidId);

            if (string.IsNullOrEmpty(_deviceIdCache))
            {
                _deviceIdCache = $"fallback-device-id-{Guid.NewGuid()}";
                Debug.WriteLine($"Android ID not available, using fallback: {_deviceIdCache}");
            }

            return _deviceIdCache;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to get Android ID: {ex.Message}");
            _deviceIdCache = $"error-device-id-{Guid.NewGuid()}";
            return _deviceIdCache;
        }
    }

    /// <summary>
    /// Get device installation for push notification registration
    /// </summary>
    public DeviceInstallation GetDeviceInstallation(params string[] tags)
    {
        var deviceId = GetDeviceId();
        var currentToken = Token;

        if (!NotificationsSupported)
        {
            currentToken = $"no-notifications-supported-{deviceId}";
        }
        else if (string.IsNullOrWhiteSpace(currentToken))
        {
            currentToken = $"development-token-{deviceId}";
        }

        var installation = new DeviceInstallation
        {
            InstallationId = deviceId,
            Platform = Platform,
            PushChannel = currentToken
        };

        installation.Tags.AddRange(tags);

        // Add automatic tags based on device state
        installation.Tags.Add("platform_android");
        installation.Tags.Add($"device_{deviceId}");

        if (NotificationsSupported)
        {
            installation.Tags.Add("notifications_supported");
        }

        if (!string.IsNullOrEmpty(Token) && !Token.StartsWith("development-") && !Token.StartsWith("no-notifications-"))
        {
            installation.Tags.Add("has_valid_token");
        }

        return installation;
    }

    private bool CheckNotificationSupport()
    {
        try
        {
            // Check if Firebase is available globally via MainApplication or custom Firebase initializer
            // Users should implement their own Firebase initialization check
            // For now, we check Google Play Services which is required for FCM

            // Check Google Play Services
            var availability = GoogleApiAvailability.Instance;
            var resultCode = availability.IsGooglePlayServicesAvailable(Microsoft.Maui.ApplicationModel.Platform.AppContext);

            bool isPlayServicesAvailable = resultCode == ConnectionResult.Success;

            if (!isPlayServicesAvailable)
            {
                var errorMessage = availability.IsUserResolvableError(resultCode)
                    ? availability.GetErrorString(resultCode)
                    : $"Google Play Services error code: {resultCode}";

                Debug.WriteLine($"Google Play Services not available: {errorMessage}");
                return false;
            }

            // Check Android API level (FCM requires API 19+)
            var apiLevel = global::Android.OS.Build.VERSION.SdkInt;
            if ((int)apiLevel < 19)
            {
                Debug.WriteLine($"Android API level {apiLevel} too low for FCM (requires 19+)");
                return false;
            }

            // Check if the app has internet permission
            var hasInternetPermission = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.CheckSelfPermission(global::Android.Manifest.Permission.Internet)
                                      == global::Android.Content.PM.Permission.Granted;

            if (!hasInternetPermission)
            {
                Debug.WriteLine("Internet permission not granted");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to check notification support: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Force refresh of the cached notification support status
    /// </summary>
    public void RefreshNotificationSupportStatus()
    {
        _notificationsSupportedCache = null;
        var newStatus = NotificationsSupported;
        Debug.WriteLine($"Notification support status refreshed: {newStatus}");
    }

    /// <summary>
    /// Wait for FCM token with timeout
    /// </summary>
    public async Task<bool> WaitForTokenAsync(int timeoutSeconds = 10)
    {
        if (!string.IsNullOrEmpty(Token) && !Token.StartsWith("development-"))
        {
            return true;
        }

        Debug.WriteLine($"Waiting for FCM token (timeout: {timeoutSeconds}s)...");

        var tokenReceived = false;
        void handler(string token)
        {
            tokenReceived = true;
        }

        TokenRefreshed += handler;

        var waitCount = 0;
        while (!tokenReceived && waitCount < timeoutSeconds * 2) // Check every 500ms
        {
            await Task.Delay(500);
            waitCount++;

            // Also check if token was set directly (not via event)
            if (!string.IsNullOrEmpty(Token) && !Token.StartsWith("development-"))
            {
                tokenReceived = true;
                break;
            }
        }

        TokenRefreshed -= handler;

        Debug.WriteLine($"FCM token wait result: {(tokenReceived ? "SUCCESS" : "TIMEOUT")}");
        return tokenReceived;
    }

    /// <summary>
    /// Register device with push notification backend
    /// NOTE: You must implement this using your backend service (Azure NH, Firebase, custom API)
    /// </summary>
    public async Task<bool> RegisterDeviceAsync(string userId)
    {
        try
        {
            if (string.IsNullOrEmpty(Token))
            {
                Debug.WriteLine("Cannot register Android device: No FCM token available");
                return false;
            }

            // Get backend service from DI container
            var backend = IPlatformApplication.Current?.Services?.GetService<IPushNotificationBackend>();
            if (backend == null)
            {
                Debug.WriteLine("IPushNotificationBackend not registered in DI container");
                return false;
            }

            // Get device installation with tags
            var installation = GetDeviceInstallation($"user_{userId}");

            // Register with backend
            var success = await backend.RegisterDeviceAsync(installation);

            if (success)
            {
                Debug.WriteLine("Android device registered successfully");
            }
            else
            {
                Debug.WriteLine("Android device registration failed");
            }

            return success;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Android device registration failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Request notification permissions (Android 13+)
    /// </summary>
    public async Task<bool> RequestPermissionsAsync()
    {
        try
        {
#if ANDROID
            var status = await Permissions.RequestAsync<Permissions.PostNotifications>();
            var granted = status == PermissionStatus.Granted;
            return granted;
#else
            return true;
#endif
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Permission request failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get a unique device fingerprint (persisted across app launches)
    /// </summary>
    public string GetDeviceFingerprint()
    {
        const string DEVICE_FINGERPRINT_KEY = "android_device_fingerprint";

        // Check if we have a stored fingerprint
        var storedFingerprint = Preferences.Get(DEVICE_FINGERPRINT_KEY, string.Empty);
        if (!string.IsNullOrEmpty(storedFingerprint))
            return storedFingerprint;

        // Generate a new fingerprint
        var deviceId = GetDeviceId();
        var packageName = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.PackageName ?? "unknown";
        var fingerprint = $"android_{deviceId}_{packageName}_{Guid.NewGuid():N}";

        // Store it for future use
        Preferences.Set(DEVICE_FINGERPRINT_KEY, fingerprint);

        Debug.WriteLine($"Generated Android device fingerprint: {fingerprint}");
        return fingerprint;
    }

    /// <summary>
    /// Check if notification permissions are currently granted
    /// </summary>
    public async Task<bool> CheckPermissionsAsync()
    {
        try
        {
#if ANDROID
            var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
            var isEnabled = status == PermissionStatus.Granted;
            Debug.WriteLine($"Android notification permission status: {status}");
            return isEnabled;
#else
            return false;
#endif
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to check Android permissions: {ex.Message}");
            return false;
        }
    }
}
