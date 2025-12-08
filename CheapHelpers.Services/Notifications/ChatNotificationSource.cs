using CheapHelpers.Models.Enums;
using CheapHelpers.Services.Notifications.Models;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Services.Notifications;

/// <summary>
/// Notification source for chat messages that creates and dispatches chat-related notifications
/// </summary>
public class ChatNotificationSource(
    NotificationDispatcher dispatcher,
    ILogger<ChatNotificationSource> logger)
{
    /// <summary>
    /// Sends a notification for a new chat message to all recipients except the sender
    /// </summary>
    /// <param name="chatType">Notification type (e.g., "user_chat", "supplier_chat")</param>
    /// <param name="senderId">ID of message sender</param>
    /// <param name="senderName">Display name of sender</param>
    /// <param name="recipientIds">List of recipient user IDs</param>
    /// <param name="messagePreview">Preview text (first ~100 chars of message)</param>
    /// <param name="chatUrl">URL to navigate to chat</param>
    /// <param name="subscriptionContext">Optional context for subscription resolution</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    public async Task NotifyNewChatMessageAsync(
        string chatType,
        string senderId,
        string senderName,
        List<string> recipientIds,
        string messagePreview,
        string chatUrl,
        ISubscriptionContext? subscriptionContext = null,
        CancellationToken cancellationToken = default)
    {
        UnifiedNotification notification = new()
        {
            NotificationType = chatType,
            Title = $"New message from {senderName}",
            Body = messagePreview,
            RecipientUserIds = recipientIds.Where(id => id != senderId).ToList(),
            Priority = NotificationPriority.Normal,
            ActionUrl = chatUrl,
            IconUrl = "/images/chat-icon.png",
            Channels = NotificationChannelFlags.InApp | NotificationChannelFlags.Push,
            SubscriptionContext = subscriptionContext,
            Data = new()
            {
                ["senderId"] = senderId,
                ["senderName"] = senderName,
                ["chatType"] = chatType
            }
        };

        NotificationDispatchResult dispatchResult = await dispatcher.SendAsync(notification, cancellationToken);

        if (!dispatchResult.IsSuccess || dispatchResult.FailedChannels > 0)
        {
            logger.LogWarning(
                "Chat notification dispatch completed with partial failures: Type={ChatType}, Sender={SenderName}, SuccessfulChannels={SuccessfulChannels}, FailedChannels={FailedChannels}, ErrorSummary={ErrorSummary}",
                chatType,
                senderName,
                dispatchResult.SuccessfulChannels,
                dispatchResult.FailedChannels,
                dispatchResult.ErrorSummary);
        }
    }
}
