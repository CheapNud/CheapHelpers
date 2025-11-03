using Android.App;
using CheapHelpers.Blazor.Hybrid.Abstractions;
using Firebase.Messaging;
using System.Diagnostics;

namespace CheapHelpers.MAUI.Platforms.Android;

/// <summary>
/// Base Firebase Cloud Messaging service for handling push notifications on Android
/// IMPORTANT: Your app must create a derived class and register it in AndroidManifest.xml
/// </summary>
/// <remarks>
/// To use this in your MAUI app, create a class that inherits from FcmService:
/// <code>
/// [Service(Exported = false)]
/// [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
/// public class MyFirebaseMessagingService : FcmService
/// {
///     // Optional: Override OnNotificationReceived to handle custom actions
///     protected override void OnNotificationReceived(string title, string body, Dictionary&lt;string, string&gt; data)
///     {
///         // Handle notification data
///         base.OnNotificationReceived(title, body, data);
///     }
/// }
/// </code>
/// </remarks>
public abstract class FcmService : FirebaseMessagingService
{
    /// <summary>
    /// Called when a message is received while app is in foreground
    /// </summary>
    public override void OnMessageReceived(RemoteMessage message)
    {
        Debug.WriteLine("FCM message received while app is in foreground");

        try
        {
            var title = message.GetNotification()?.Title ?? "Notification";
            var body = message.GetNotification()?.Body ?? "";

            // Extract custom data
            var data = new Dictionary<string, string>();
            if (message.Data != null)
            {
                foreach (var kvp in message.Data)
                {
                    data[kvp.Key] = kvp.Value;
                }
            }

            // Convert to local notification
            Task.Run(async () =>
            {
                try
                {
                    var serviceProvider = IPlatformApplication.Current?.Services;
                    var localNotificationService = serviceProvider?.GetService<ILocalNotificationService>();

                    if (localNotificationService != null)
                    {
                        await localNotificationService.ShowNotificationAsync(title, body, data);
                    }
                    else
                    {
                        Debug.WriteLine("WARNING: ILocalNotificationService not registered - cannot show notification");
                    }

                    // Call virtual handler
                    OnNotificationReceived(title, body, data);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to show local notification: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to process FCM message: {ex.Message}");
        }

        base.OnMessageReceived(message);
    }

    /// <summary>
    /// Called when FCM token is refreshed
    /// </summary>
    public override void OnNewToken(string token)
    {
        Debug.WriteLine($"FCM token refreshed: {token[..Math.Min(8, token.Length)]}...");

        try
        {
            var serviceProvider = IPlatformApplication.Current?.Services;
            var deviceInstallationService = serviceProvider?.GetService<IDeviceInstallationService>();

            if (deviceInstallationService is DeviceInstallationService service)
            {
                service.SetToken(token);
            }
            else
            {
                Debug.WriteLine("WARNING: DeviceInstallationService not registered or not the expected type");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to update FCM token: {ex.Message}");
        }

        base.OnNewToken(token);
    }

    /// <summary>
    /// Override this method to handle notification received events
    /// </summary>
    protected virtual void OnNotificationReceived(string title, string body, Dictionary<string, string> data)
    {
        Debug.WriteLine($"Notification received - Title: {title}, Body: {body}");
    }
}
