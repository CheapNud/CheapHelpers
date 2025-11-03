using System.Text.Json.Serialization;

namespace CheapHelpers.Blazor.Hybrid.Models;

/// <summary>
/// Payload for sending a push notification
/// </summary>
public class NotificationPayload
{
    /// <summary>
    /// Notification title
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Notification body text
    /// </summary>
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Target device IDs (if null, uses tags)
    /// </summary>
    [JsonPropertyName("deviceIds")]
    public List<string>? DeviceIds { get; set; }

    /// <summary>
    /// Target tags for filtered delivery
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Custom data dictionary for handling notification clicks
    /// </summary>
    [JsonPropertyName("data")]
    public Dictionary<string, string>? Data { get; set; }

    /// <summary>
    /// Silent notification (no alert, just data delivery)
    /// </summary>
    [JsonPropertyName("silent")]
    public bool Silent { get; set; }
}
