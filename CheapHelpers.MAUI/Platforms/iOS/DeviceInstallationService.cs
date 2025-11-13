using CheapHelpers.Blazor.Hybrid.Abstractions;
using CheapHelpers.Blazor.Hybrid.Models;
using Foundation;
using Microsoft.Maui.Storage;
using System.Diagnostics;
using UIKit;
using UserNotifications;

namespace CheapHelpers.MAUI.Platforms.iOS;

/// <summary>
/// iOS implementation of device installation service using APNS
/// </summary>
public class DeviceInstallationService : IDeviceInstallationService
{
    private const int SUPPORTED_VERSION_MAJOR = 14;
    private const int SUPPORTED_VERSION_MINOR = 2;

    private string _token = string.Empty;

    /// <summary>
    /// Platform identifier for APNS
    /// </summary>
    public string Platform => "apns";

    /// <summary>
    /// APNS device token (hex string)
    /// </summary>
    public string? Token => string.IsNullOrEmpty(_token) ? null : _token;

    /// <summary>
    /// Whether notifications are supported on this device
    /// </summary>
    public bool NotificationsSupported
    {
        get
        {
            try
            {
                var device = UIDevice.CurrentDevice;
                if (device == null)
                {
                    return false;
                }
                return device.CheckSystemVersion(SUPPORTED_VERSION_MAJOR, SUPPORTED_VERSION_MINOR);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking notification support: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Whether the device has been registered (has a token)
    /// </summary>
    public bool IsRegistered => !string.IsNullOrEmpty(_token);

    /// <summary>
    /// Event fired when APNS token is refreshed
    /// </summary>
    [Obsolete("Use OnTokenReceived for initial token or OnTokenUpdated for refreshes")]
    public event Action<string>? TokenRefreshed;

    /// <summary>
    /// Event triggered when a push token is received for the first time
    /// </summary>
    public event Action<string>? OnTokenReceived;

    /// <summary>
    /// Event triggered when an existing push token is updated/refreshed
    /// </summary>
    public event Action<string>? OnTokenUpdated;

    /// <summary>
    /// Set the APNS token (called from AppDelegate after registration)
    /// </summary>
    /// <param name="token">APNS device token in hex format</param>
    public void SetToken(string token)
    {
        var oldToken = _token;
        var wasEmpty = string.IsNullOrEmpty(oldToken);
        _token = token ?? string.Empty;

        if (!string.IsNullOrEmpty(_token) && _token != oldToken)
        {
            Debug.WriteLine($"APNS token set: {_token[..Math.Min(8, _token.Length)]}...");

            // Fire the appropriate new event
            if (wasEmpty)
            {
                Debug.WriteLine("APNS token received for the first time");
                OnTokenReceived?.Invoke(_token);
            }
            else
            {
                Debug.WriteLine("APNS token updated/refreshed");
                OnTokenUpdated?.Invoke(_token);
            }

            // Keep backward compatibility
#pragma warning disable CS0618 // Type or member is obsolete
            TokenRefreshed?.Invoke(_token);
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }

    /// <summary>
    /// Get the iOS vendor identifier (persistent per app installation)
    /// </summary>
    public string GetDeviceId()
    {
        try
        {
            var device = UIDevice.CurrentDevice;
            if (device == null)
            {
                return string.Empty;
            }
            return device.IdentifierForVendor?.ToString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting device ID: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Get device installation for push notification registration
    /// </summary>
    public DeviceInstallation GetDeviceInstallation(params string[] tags)
    {
        if (!NotificationsSupported)
            throw new Exception(GetNotificationsSupportError());

        if (string.IsNullOrWhiteSpace(Token))
            throw new Exception("Unable to resolve token for APNS");

        var installation = new DeviceInstallation
        {
            InstallationId = GetDeviceId(),
            Platform = Platform,
            PushChannel = Token
        };

        installation.Tags.AddRange(tags);

        // Add automatic tags based on device state
        installation.Tags.Add("platform_ios");
        installation.Tags.Add($"device_{installation.InstallationId}");

        if (NotificationsSupported)
        {
            installation.Tags.Add("notifications_supported");
        }

        if (!string.IsNullOrEmpty(Token))
        {
            installation.Tags.Add("has_valid_token");
        }

        return installation;
    }

    private string GetNotificationsSupportError()
    {
        if (!NotificationsSupported)
            return $"This app only supports notifications on iOS {SUPPORTED_VERSION_MAJOR}.{SUPPORTED_VERSION_MINOR} and above. You are running {UIDevice.CurrentDevice.SystemVersion}.";

        if (string.IsNullOrWhiteSpace(Token))
            return "This app can support notifications but you must enable this in your settings.";

        return "An error occurred preventing the use of push notifications";
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
                Debug.WriteLine("Cannot register iOS device: No APNS token available");
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
                Debug.WriteLine("iOS device registered successfully");
            }
            else
            {
                Debug.WriteLine("iOS device registration failed");
            }

            return success;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"iOS device registration failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Request notification permissions from the user
    /// </summary>
    public async Task<bool> RequestPermissionsAsync()
    {
        try
        {
            if (!NotificationsSupported)
            {
                return false;
            }

            var tcs = new TaskCompletionSource<bool>();

            // Request permission (this will show the popup)
            UNUserNotificationCenter.Current.RequestAuthorization(
                UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound,
                (approvalGranted, error) =>
                {
                    if (approvalGranted && error == null)
                    {
                        // Register for remote notifications to get APNS token
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            UIApplication.SharedApplication.RegisterForRemoteNotifications();
                        });
                        tcs.SetResult(true);
                    }
                    else
                    {
                        if (error != null)
                        {
                            Debug.WriteLine($"iOS permission error: {error.LocalizedDescription}");
                        }
                        tcs.SetResult(false);
                    }
                });

            var granted = await tcs.Task;

            return granted;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"iOS permission request failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get a unique device fingerprint (persisted across app launches)
    /// </summary>
    public string GetDeviceFingerprint()
    {
        try
        {
            const string DEVICE_FINGERPRINT_KEY = "ios_device_fingerprint";

            // Check if we have a stored fingerprint
            var storedFingerprint = Preferences.Get(DEVICE_FINGERPRINT_KEY, string.Empty);
            if (!string.IsNullOrEmpty(storedFingerprint))
                return storedFingerprint;

            // Generate a new fingerprint
            var deviceId = GetDeviceId();
            var bundleId = NSBundle.MainBundle?.BundleIdentifier ?? "unknown";
            var fingerprint = $"ios_{deviceId}_{bundleId}_{Guid.NewGuid():N}";

            // Store it for future use
            Preferences.Set(DEVICE_FINGERPRINT_KEY, fingerprint);

            return fingerprint;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error generating device fingerprint: {ex.Message}");
            return $"ios_fallback_{Guid.NewGuid():N}";
        }
    }

    /// <summary>
    /// Check if notification permissions are currently granted
    /// </summary>
    public async Task<bool> CheckPermissionsAsync()
    {
        try
        {
            var tcs = new TaskCompletionSource<bool>();

            UNUserNotificationCenter.Current.GetNotificationSettings(settings =>
            {
                var isEnabled = settings.AuthorizationStatus == UNAuthorizationStatus.Authorized;
                tcs.SetResult(isEnabled);
            });

            return await tcs.Task;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to check iOS permissions: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets comprehensive diagnostic information about the device's notification status
    /// </summary>
    /// <returns>DeviceNotificationStatus containing platform-specific diagnostics</returns>
    public DeviceNotificationStatus GetNotificationStatus()
    {
        var status = new DeviceNotificationStatus
        {
            DeviceId = GetDeviceId(),
            Platform = Platform,
            IsSupported = NotificationsSupported,
            HasToken = !string.IsNullOrEmpty(Token),
            TokenLength = Token?.Length ?? 0,
            LastChecked = DateTime.UtcNow
        };

        try
        {
            var device = UIDevice.CurrentDevice;
            if (device != null)
            {
                // Device information
                status.DeviceModel = device.Model ?? "Unknown";
                status.DeviceManufacturer = "Apple";
                status.PlatformSpecificData["SystemVersion"] = device.SystemVersion ?? "Unknown";
                status.PlatformSpecificData["SystemName"] = device.SystemName ?? "Unknown";
                status.PlatformSpecificData["DeviceName"] = device.Name ?? "Unknown";
                status.PlatformSpecificData["IdentifierForVendor"] = device.IdentifierForVendor?.ToString() ?? "Unknown";

                // iOS version support information
                status.PlatformSpecificData["SupportedVersionMajor"] = SUPPORTED_VERSION_MAJOR;
                status.PlatformSpecificData["SupportedVersionMinor"] = SUPPORTED_VERSION_MINOR;
                status.PlatformSpecificData["MeetsVersionRequirement"] = device.CheckSystemVersion(SUPPORTED_VERSION_MAJOR, SUPPORTED_VERSION_MINOR);
            }

            // Token information
            if (!string.IsNullOrEmpty(Token))
            {
                status.PlatformSpecificData["TokenPreview"] = Token.Length > 16 ? Token[..16] + "..." : Token;
                status.PlatformSpecificData["TokenFormat"] = "APNS Hex";
            }

            // Get authorization status asynchronously
            var tcs = new TaskCompletionSource<UNNotificationSettings>();
            UNUserNotificationCenter.Current.GetNotificationSettings(settings =>
            {
                tcs.SetResult(settings);
            });

            if (tcs.Task.Wait(TimeSpan.FromSeconds(1)))
            {
                var settings = tcs.Task.Result;
                status.PlatformSpecificData["AuthorizationStatus"] = settings.AuthorizationStatus.ToString();
                status.PlatformSpecificData["AlertSetting"] = settings.AlertSetting.ToString();
                status.PlatformSpecificData["BadgeSetting"] = settings.BadgeSetting.ToString();
                status.PlatformSpecificData["SoundSetting"] = settings.SoundSetting.ToString();
                status.PlatformSpecificData["NotificationCenterSetting"] = settings.NotificationCenterSetting.ToString();
                status.PlatformSpecificData["LockScreenSetting"] = settings.LockScreenSetting.ToString();
                status.PlatformSpecificData["IsAuthorized"] = settings.AuthorizationStatus == UNAuthorizationStatus.Authorized;
            }
            else
            {
                status.PlatformSpecificData["AuthorizationStatus"] = "Timeout retrieving settings";
            }

            // Bundle information
            var mainBundle = NSBundle.MainBundle;
            if (mainBundle != null)
            {
                status.PlatformSpecificData["BundleIdentifier"] = mainBundle.BundleIdentifier ?? "Unknown";
                status.PlatformSpecificData["BundleVersion"] = mainBundle.InfoDictionary?["CFBundleVersion"]?.ToString() ?? "Unknown";
                status.PlatformSpecificData["BundleShortVersion"] = mainBundle.InfoDictionary?["CFBundleShortVersionString"]?.ToString() ?? "Unknown";
            }

            // Application state
            if (UIApplication.SharedApplication != null)
            {
                status.PlatformSpecificData["ApplicationState"] = UIApplication.SharedApplication.ApplicationState.ToString();
                status.PlatformSpecificData["IsRegisteredForRemoteNotifications"] = UIApplication.SharedApplication.IsRegisteredForRemoteNotifications;
            }

        }
        catch (Exception ex)
        {
            status.Error = $"Error collecting diagnostics: {ex.Message}";
            Debug.WriteLine($"Error in GetNotificationStatus: {ex}");
        }

        return status;
    }
}
