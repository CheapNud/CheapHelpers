using System.Text.Json.Serialization;

namespace CheapHelpers.Blazor.Hybrid.Models;

/// <summary>
/// Browser Web Push subscription triplet as produced by <c>PushManager.subscribe()</c>.
/// Property names match the Azure Notification Hubs browser installation pushChannel contract.
/// </summary>
public class WebPushSubscription
{
    /// <summary>
    /// Push service endpoint URL from the browser subscription
    /// </summary>
    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Client public key (p256dh) from the browser subscription
    /// </summary>
    [JsonPropertyName("p256dh")]
    public string P256dh { get; set; } = string.Empty;

    /// <summary>
    /// Auth secret from the browser subscription
    /// </summary>
    [JsonPropertyName("auth")]
    public string Auth { get; set; } = string.Empty;
}
