using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using CheapHelpers.Blazor.Hybrid.Abstractions;
using System.Diagnostics;
using Activity = Android.App.Activity;
using Debug = System.Diagnostics.Debug;

namespace CheapHelpers.MAUI.Platforms.Android;

/// <summary>
/// Android implementation of local notification service using Android notification channels
/// </summary>
public class LocalNotificationService : ILocalNotificationService
{
    private const string DEFAULT_CHANNEL_ID = "cheaphelpers_notifications";
    private const string DEFAULT_CHANNEL_NAME = "App Notifications";
    private const string DEFAULT_CHANNEL_DESCRIPTION = "Notifications from this app";

    private readonly string _channelId;
    private readonly string _channelName;
    private readonly string _channelDescription;
    private int _notificationId = 1000;

    /// <summary>
    /// Create a local notification service with default channel settings
    /// </summary>
    public LocalNotificationService()
        : this(DEFAULT_CHANNEL_ID, DEFAULT_CHANNEL_NAME, DEFAULT_CHANNEL_DESCRIPTION)
    {
    }

    /// <summary>
    /// Create a local notification service with custom channel settings
    /// </summary>
    public LocalNotificationService(string channelId, string channelName, string channelDescription)
    {
        _channelId = channelId;
        _channelName = channelName;
        _channelDescription = channelDescription;

        CreateNotificationChannel();
    }

    /// <summary>
    /// Check if notification permissions are granted
    /// </summary>
    public async Task<bool> IsPermittedAsync()
    {
        try
        {
            if (Platform.CurrentActivity is not Activity activity)
                return false;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            {
                // Android 13+ requires POST_NOTIFICATIONS permission
                var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
                return status == PermissionStatus.Granted;
            }

            // For older Android versions, check if notifications are enabled
            var notificationManager = NotificationManagerCompat.From(activity);
            return notificationManager.AreNotificationsEnabled();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to check Android notification permissions: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Show a local notification immediately
    /// </summary>
    public async Task ShowNotificationAsync(string title, string body, Dictionary<string, string>? data = null)
    {
        try
        {
            if (!await IsPermittedAsync())
            {
                Debug.WriteLine("Cannot show notification: permissions not granted");
                return;
            }

            if (Platform.CurrentActivity is not Activity activity)
            {
                Debug.WriteLine("Cannot show notification: current activity is null");
                return;
            }

            // Create intent for when notification is tapped
            var intent = new Intent(activity, activity.GetType());
            intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);

            // Add custom data to intent
            if (data != null)
            {
                foreach (var kvp in data)
                {
                    intent.PutExtra(kvp.Key, kvp.Value);
                }
            }

            var pendingIntent = PendingIntent.GetActivity(
                activity,
                _notificationId,
                intent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            // Get app icon resource ID
            var iconResourceId = GetAppIconResourceId(activity);

            // Build notification
            var notification = new NotificationCompat.Builder(activity, _channelId)
                .SetContentTitle(title)
                .SetContentText(body)
                .SetSmallIcon(iconResourceId)
                .SetAutoCancel(true) // Remove notification when tapped
                .SetPriority(NotificationCompat.PriorityDefault)
                .SetContentIntent(pendingIntent)
                .SetDefaults(NotificationCompat.DefaultSound | NotificationCompat.DefaultVibrate)
                .Build();

            // Show notification
            var notificationManager = NotificationManagerCompat.From(activity);
            notificationManager.Notify(_notificationId++, notification);

            Debug.WriteLine($"Local notification shown: {title}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to show local notification: {ex.Message}");
        }
    }

    private void CreateNotificationChannel()
    {
        try
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                if (Platform.CurrentActivity?.GetSystemService(Context.NotificationService) is NotificationManager notificationManager)
                {
                    var channel = new NotificationChannel(_channelId, _channelName, NotificationImportance.Default)
                    {
                        Description = _channelDescription
                    };
                    channel.EnableLights(true);
                    channel.EnableVibration(true);

                    notificationManager.CreateNotificationChannel(channel);
                    Debug.WriteLine($"Notification channel created: {_channelId}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to create notification channel: {ex.Message}");
        }
    }

    private int GetAppIconResourceId(global::Android.App.Activity activity)
    {
        try
        {
            // Try to get app icon from application info
            var appInfo = activity.ApplicationInfo;
            if (appInfo != null && appInfo.Icon != 0)
            {
                return appInfo.Icon;
            }

            // Fallback: try common icon names
            var packageName = activity.PackageName;
            var resourceId = activity.Resources?.GetIdentifier("icon", "mipmap", packageName) ?? 0;
            if (resourceId != 0)
                return resourceId;

            resourceId = activity.Resources?.GetIdentifier("ic_launcher", "mipmap", packageName) ?? 0;
            if (resourceId != 0)
                return resourceId;

            // Last resort: use system default
            return global::Android.Resource.Drawable.IcDialogInfo;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to get app icon: {ex.Message}");
            return global::Android.Resource.Drawable.IcDialogInfo;
        }
    }
}
