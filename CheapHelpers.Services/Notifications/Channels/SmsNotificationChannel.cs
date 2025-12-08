using CheapHelpers.Models.Entities;
using CheapHelpers.Services.Communication.Sms;
using CheapHelpers.Services.Notifications.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Services.Notifications.Channels;

/// <summary>
/// Notification channel for SMS delivery
/// </summary>
public class SmsNotificationChannel(
    ISmsService smsService,
    UserManager<CheapUser> userManager,
    ILogger<SmsNotificationChannel> logger) : INotificationChannel
{
    /// <inheritdoc/>
    public string ChannelName => "sms";

    /// <inheritdoc/>
    public bool SupportsNotificationType(string notificationType) => true;

    /// <inheritdoc/>
    public async Task<NotificationChannelResult> SendAsync(
        UnifiedNotification notification,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var phoneRecipients = new List<string>();

            // Use provided phone numbers if available
            if (notification.SmsRecipients != null && notification.SmsRecipients.Count > 0)
            {
                phoneRecipients.AddRange(notification.SmsRecipients);
            }
            else
            {
                // Look up phone numbers from user IDs
                foreach (var userId in notification.RecipientUserIds)
                {
                    var user = await userManager.FindByIdAsync(userId);
                    if (!string.IsNullOrEmpty(user?.PhoneNumber))
                    {
                        phoneRecipients.Add(user.PhoneNumber);
                    }
                    else
                    {
                        logger.LogWarning("User {UserId} not found or has no phone number", userId);
                    }
                }
            }

            if (phoneRecipients.Count == 0)
            {
                return NotificationChannelResult.Failure(
                    ChannelName,
                    "No SMS recipients found");
            }

            // Construct SMS body (plain text only)
            var smsBody = $"{notification.Title}\n\n{notification.Body}";

            // Create dictionary for bulk send
            var recipients = phoneRecipients.ToDictionary(
                phone => phone,
                _ => smsBody);

            var smsResults = await smsService.SendBulkAsync(recipients, cancellationToken);

            var successCount = smsResults.Count(r => r.Value.IsSuccess);
            var failedCount = smsResults.Count - successCount;

            if (successCount > 0)
            {
                return NotificationChannelResult.Partial(ChannelName, successCount, failedCount);
            }

            return NotificationChannelResult.Failure(
                ChannelName,
                "Failed to send SMS to all recipients",
                failedCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending SMS notifications");
            return NotificationChannelResult.Failure(ChannelName, ex.Message);
        }
    }
}
