using CheapHelpers.Models.Entities;
using CheapHelpers.Services.Email;
using CheapHelpers.Services.Notifications.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Services.Notifications.Channels;

/// <summary>
/// Notification channel for email delivery
/// </summary>
public class EmailNotificationChannel(
    IEmailService emailService,
    UserManager<CheapUser> userManager,
    ILogger<EmailNotificationChannel> logger) : INotificationChannel
{
    /// <inheritdoc/>
    public string ChannelName => "email";

    /// <inheritdoc/>
    public bool SupportsNotificationType(string notificationType) => true;

    /// <inheritdoc/>
    public async Task<NotificationChannelResult> SendAsync(
        UnifiedNotification notification,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var emailRecipients = new List<string>();

            // Use provided email recipients if available
            if (notification.EmailRecipients != null && notification.EmailRecipients.Count > 0)
            {
                emailRecipients.AddRange(notification.EmailRecipients);
            }
            else
            {
                // Look up email addresses from user IDs
                foreach (var userId in notification.RecipientUserIds)
                {
                    var user = await userManager.FindByIdAsync(userId);
                    if (user?.Email != null)
                    {
                        emailRecipients.Add(user.Email);
                    }
                    else
                    {
                        logger.LogWarning("User {UserId} not found or has no email address", userId);
                    }
                }
            }

            if (emailRecipients.Count == 0)
            {
                return NotificationChannelResult.Failure(
                    ChannelName,
                    "No email recipients found");
            }

            // Use HTML body if available, otherwise use plain text
            var body = notification.HtmlBody ?? notification.Body;

            await emailService.SendEmailAsync(
                emailRecipients.ToArray(),
                notification.Title,
                body);

            return NotificationChannelResult.Success(ChannelName, emailRecipients.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending email notifications");
            return NotificationChannelResult.Failure(ChannelName, ex.Message);
        }
    }
}
