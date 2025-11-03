using CheapHelpers.Blazor.Hybrid.Abstractions;
using Foundation;
using Microsoft.Maui;
using System.Diagnostics;
using UIKit;
using UserNotifications;

namespace CheapHelpers.MAUI.Platforms.iOS;

/// <summary>
/// Base AppDelegate with APNS push notification handling
/// IMPORTANT: Your MauiProgram must register the services:
/// - services.AddSingleton&lt;IDeviceInstallationService, DeviceInstallationService&gt;();
/// - services.AddSingleton&lt;ILocalNotificationService, LocalNotificationService&gt;();
/// </summary>
/// <remarks>
/// To use this in your MAUI app, make your AppDelegate inherit from this class:
/// <code>
/// [Register("AppDelegate")]
/// public class AppDelegate : ApnsDelegate
/// {
///     protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
///
///     // Optional: Override OnNotificationReceived to handle custom notification actions
///     protected override void OnNotificationReceived(string title, string body, Dictionary&lt;string, string&gt; data)
///     {
///         // Handle notification data
///         base.OnNotificationReceived(title, body, data);
///     }
/// }
/// </code>
/// </remarks>
[Register("ApnsDelegate")]
public abstract class ApnsDelegate : MauiUIApplicationDelegate, IUNUserNotificationCenterDelegate
{
    private IDeviceInstallationService? _deviceInstallationService;
    private ILocalNotificationService? _localNotificationService;

    /// <summary>
    /// Get the device installation service from DI
    /// </summary>
    protected IDeviceInstallationService? DeviceInstallationService =>
        _deviceInstallationService ??= IPlatformApplication.Current?.Services?.GetService<IDeviceInstallationService>();

    /// <summary>
    /// Get the local notification service from DI
    /// </summary>
    protected ILocalNotificationService? LocalNotificationService =>
        _localNotificationService ??= IPlatformApplication.Current?.Services?.GetService<ILocalNotificationService>();

    /// <summary>
    /// Called when app finishes launching
    /// </summary>
    [Export("application:didFinishLaunchingWithOptions:")]
    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        Debug.WriteLine("iOS app started with APNS support");

        // Set up foreground notification handling
        UNUserNotificationCenter.Current.Delegate = this;

        // Process notification if app was launched from one
        using var userInfo = launchOptions?.ObjectForKey(UIApplication.LaunchOptionsRemoteNotificationKey) as NSDictionary;
        if (userInfo != null)
        {
            ProcessNotification(userInfo);
        }

        return base.FinishedLaunching(application, launchOptions ?? new NSDictionary());
    }

    /// <summary>
    /// Called when APNS registration succeeds and device token is received
    /// </summary>
    [Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")]
    public void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
    {
        var token = deviceToken.ToHexString();
        Debug.WriteLine($"APNS token received: {token[..Math.Min(8, token.Length)]}...");

        // Set token in DeviceInstallationService
        if (DeviceInstallationService is DeviceInstallationService service)
        {
            service.SetToken(token);
        }
        else
        {
            Debug.WriteLine("WARNING: DeviceInstallationService not available or not the expected type");
        }
    }

    /// <summary>
    /// Called when APNS registration fails
    /// </summary>
    [Export("application:didFailToRegisterForRemoteNotificationsWithError:")]
    public void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
    {
        Debug.WriteLine($"iOS APNS registration failed: {error.Description}");
    }

    /// <summary>
    /// Called when a remote notification is received (background or inactive state)
    /// </summary>
    [Export("application:didReceiveRemoteNotification:")]
    public void ReceivedRemoteNotification(UIApplication application, NSDictionary userInfo)
    {
        ProcessNotification(userInfo);
    }

    /// <summary>
    /// Called when a notification arrives while app is in foreground
    /// </summary>
    [Export("userNotificationCenter:willPresentNotification:withCompletionHandler:")]
    public void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
    {
        var content = notification.Request.Content;
        var title = content.Title ?? "Notification";
        var body = content.Body ?? "";
        var userInfo = content.UserInfo;

        // Check if this is a push notification (has a trigger) or local notification (no trigger)
        bool isPushNotification = notification.Request.Trigger is UNPushNotificationTrigger;

        if (isPushNotification)
        {
            // Extract custom data
            var data = ExtractNotificationData(userInfo);

            // Convert push to local notification for immediate display
            Task.Run(async () =>
            {
                try
                {
                    if (LocalNotificationService != null)
                    {
                        await LocalNotificationService.ShowNotificationAsync(title, body, data);
                    }

                    // Call virtual handler
                    OnNotificationReceived(title, body, data);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to show local notification: {ex.Message}");
                }
            });

            // Don't show the push notification since we're converting it to local
            completionHandler(UNNotificationPresentationOptions.None);
        }
        else
        {
            // This is already a local notification - show it immediately with banner and sound
            if (UIDevice.CurrentDevice.CheckSystemVersion(14, 0))
            {
                completionHandler(UNNotificationPresentationOptions.Banner |
                                 UNNotificationPresentationOptions.Sound |
                                 UNNotificationPresentationOptions.List);
            }
            else
            {
                // For older iOS versions
                completionHandler(UNNotificationPresentationOptions.Alert |
                                 UNNotificationPresentationOptions.Sound |
                                 UNNotificationPresentationOptions.Badge);
            }
        }
    }

    /// <summary>
    /// Called when user taps a notification
    /// </summary>
    [Export("userNotificationCenter:didReceiveNotificationResponse:withCompletionHandler:")]
    public void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
    {
        var userInfo = response.Notification.Request.Content.UserInfo;
        ProcessNotification(userInfo);
        completionHandler();
    }

    /// <summary>
    /// Process notification data and extract custom fields
    /// </summary>
    private void ProcessNotification(NSDictionary userInfo)
    {
        if (userInfo == null) return;

        try
        {
            var title = userInfo.ObjectForKey(new NSString("title")) as NSString;
            var body = userInfo.ObjectForKey(new NSString("body")) as NSString;
            var data = ExtractNotificationData(userInfo);

            OnNotificationReceived(
                title?.Description ?? "Notification",
                body?.Description ?? "",
                data
            );
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to process iOS notification: {ex.Message}");
        }
    }

    /// <summary>
    /// Extract notification data dictionary from NSDictionary
    /// </summary>
    private Dictionary<string, string> ExtractNotificationData(NSDictionary userInfo)
    {
        var data = new Dictionary<string, string>();
        if (userInfo == null) return data;

        foreach (var key in userInfo.Keys)
        {
            if (key is NSString nsKey && userInfo[key] is NSString nsValue)
            {
                data[nsKey.ToString()] = nsValue.ToString();
            }
        }

        return data;
    }

    /// <summary>
    /// Override this method to handle notification received events
    /// </summary>
    protected virtual void OnNotificationReceived(string title, string body, Dictionary<string, string> data)
    {
        Debug.WriteLine($"Notification received - Title: {title}, Body: {body}");
    }
}
