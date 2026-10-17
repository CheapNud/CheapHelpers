using CheapHelpers.Models.Enums;
using CheapHelpers.Services.Notifications.Configuration;
using CheapHelpers.Services.Notifications.Models;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CheapHelpers.Services.Notifications.Channels;

/// <summary>
/// Notification channel that publishes to a self-hosted ntfy topic: one HTTP POST,
/// the message as the body, metadata in headers.
/// See https://docs.ntfy.sh/publish/ for the header contract.
/// </summary>
public class NtfyNotificationChannel(
    IHttpClientFactory httpClientFactory,
    IOptions<NotificationOptions> options,
    ILogger<NtfyNotificationChannel> logger) : INotificationChannel
{
    /// <summary>
    /// Name of the named HttpClient this channel uses
    /// </summary>
    public const string NtfyHttpClientName = "ntfy";

    /// <inheritdoc/>
    public string ChannelName => "ntfy";

    /// <inheritdoc/>
    public bool SupportsNotificationType(string notificationType) => true;

    /// <inheritdoc/>
    public async Task<NotificationChannelResult> SendAsync(
        UnifiedNotification notification,
        CancellationToken cancellationToken = default)
    {
        var ntfyOptions = options.Value.Ntfy;
        if (string.IsNullOrEmpty(ntfyOptions.TopicUrl))
            return NotificationChannelResult.Failure(ChannelName, "ntfy topic URL not configured", failedCount: 0);

        try
        {
            using var ntfyRequest = new HttpRequestMessage(HttpMethod.Post, ntfyOptions.TopicUrl)
            {
                Content = new StringContent(notification.Body)
            };
            ntfyRequest.Headers.Add("X-Title", EncodeHeader(notification.Title));
            ntfyRequest.Headers.Add("X-Priority", MapPriority(notification.Priority));
            if (notification.Data?.TryGetValue("tags", out var tags) == true && !string.IsNullOrEmpty(tags))
                ntfyRequest.Headers.Add("X-Tags", EncodeHeader(tags));
            if (!string.IsNullOrEmpty(notification.ActionUrl))
                ntfyRequest.Headers.Add("X-Click", notification.ActionUrl);
            if (!string.IsNullOrEmpty(notification.IconUrl))
                ntfyRequest.Headers.Add("X-Icon", notification.IconUrl);
            if (!string.IsNullOrEmpty(ntfyOptions.Token))
                ntfyRequest.Headers.Add("Authorization", $"Bearer {ntfyOptions.Token}");

            // Fresh client per publish: notifications are rare, and a long-lived handler
            // would pin stale DNS after the ntfy container is recreated.
            using var ntfyResponse = await httpClientFactory.CreateClient(NtfyHttpClientName)
                .SendAsync(ntfyRequest, cancellationToken);
            if (!ntfyResponse.IsSuccessStatusCode)
            {
                var status = (int)ntfyResponse.StatusCode;
                logger.LogWarning("ntfy publish failed: HTTP {Status} for '{Title}'", status, notification.Title);
                return NotificationChannelResult.Failure(ChannelName, $"HTTP {status}");
            }

            return NotificationChannelResult.Success(ChannelName);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "ntfy publish failed for '{Title}'", notification.Title);
            return NotificationChannelResult.Failure(ChannelName, ex.Message);
        }
    }

    private static string MapPriority(NotificationPriority priority) => priority switch
    {
        NotificationPriority.Low => "low",
        NotificationPriority.Normal => "default",
        NotificationPriority.High => "high",
        NotificationPriority.Urgent => "urgent",
        _ => "default"
    };

    // HttpClient rejects non-ASCII header values, so accents and emoji go out as an
    // RFC 2047 encoded word, which ntfy decodes.
    private static string EncodeHeader(string headerValue) =>
        Ascii.IsValid(headerValue)
            ? headerValue
            : $"=?UTF-8?B?{Convert.ToBase64String(Encoding.UTF8.GetBytes(headerValue))}?=";
}
