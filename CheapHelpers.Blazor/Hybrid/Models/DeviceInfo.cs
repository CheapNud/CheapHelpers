using System.Text.Json.Serialization;

namespace CheapHelpers.Blazor.Hybrid.Models;

/// <summary>
/// Information about a registered device from the backend
/// </summary>
public class DeviceInfo
{
    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("platform")]
    public string Platform { get; set; } = string.Empty;

    [JsonPropertyName("pushToken")]
    public string PushToken { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string? UserId { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("registeredAt")]
    public DateTime RegisteredAt { get; set; }

    [JsonPropertyName("lastUpdated")]
    public DateTime LastUpdated { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];
}
