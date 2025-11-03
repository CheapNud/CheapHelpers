using System.Text.Json.Serialization;

namespace CheapHelpers.Blazor.Hybrid.Models;

/// <summary>
/// Represents a device installation for push notification backend registration.
/// Platform-agnostic format that works with Azure Notification Hubs, Firebase, and custom backends.
/// </summary>
public class DeviceInstallation
{
    /// <summary>
    /// Unique identifier for this device installation
    /// </summary>
    [JsonPropertyName("installationId")]
    public string InstallationId { get; set; } = string.Empty;

    /// <summary>
    /// Platform identifier: "apns" (iOS), "fcm" (Android), "webpush" (Desktop)
    /// </summary>
    [JsonPropertyName("platform")]
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// Platform-specific push token/subscription endpoint
    /// </summary>
    [JsonPropertyName("pushChannel")]
    public string PushChannel { get; set; } = string.Empty;

    /// <summary>
    /// Tags for targeted notification delivery (e.g., user IDs, groups, topics)
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];
}
