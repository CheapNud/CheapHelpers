using CheapHelpers.Blazor.Hybrid.Abstractions;
using Foundation;
using System.Diagnostics;
using UIKit;
using UserNotifications;

namespace CheapHelpers.MAUI.Platforms.iOS;

/// <summary>
/// iOS implementation of local notification service using UserNotifications framework
/// </summary>
public class LocalNotificationService : ILocalNotificationService
{
    /// <summary>
    /// Check if notification permissions are granted
    /// </summary>
    public async Task<bool> IsPermittedAsync()
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
            Debug.WriteLine($"Failed to check iOS notification permissions: {ex.Message}");
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

            // Create notification content
            var content = new UNMutableNotificationContent
            {
                Title = title,
                Body = body,
                Sound = UNNotificationSound.Default
            };

            // Add custom data as userInfo
            if (data != null && data.Any())
            {
                var userInfo = new NSMutableDictionary();
                foreach (var kvp in data)
                {
                    userInfo[new NSString(kvp.Key)] = new NSString(kvp.Value);
                }
                content.UserInfo = userInfo;
            }

            // Create request with unique identifier - NO TRIGGER for immediate display
            var requestId = $"local_notification_{DateTime.Now.Ticks}";
            var request = UNNotificationRequest.FromIdentifier(requestId, content, null); // null trigger = immediate

            // Add to notification center
            var center = UNUserNotificationCenter.Current;

            // Remove any pending notifications with same ID to prevent duplicates
            center.RemovePendingNotificationRequests(new[] { requestId });

            // Add the notification request
            await center.AddNotificationRequestAsync(request);

            Debug.WriteLine($"Local notification scheduled: {title}");

            // Note: Foreground presentation is handled by the AppDelegate's IUNUserNotificationCenterDelegate
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to show local notification: {ex.Message}");
        }
    }
}
