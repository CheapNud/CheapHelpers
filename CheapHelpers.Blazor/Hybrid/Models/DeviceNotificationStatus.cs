namespace CheapHelpers.Blazor.Hybrid.Models;

/// <summary>
/// Platform-agnostic device notification status information for diagnostics and troubleshooting.
/// Provides a unified view of notification capabilities across Android, iOS, Windows, and macOS platforms.
/// Platform-specific details (like FCM/APNS status) are stored in PlatformSpecificData dictionary.
/// </summary>
public class DeviceNotificationStatus
{
    public string DeviceId { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public bool IsSupported { get; set; }
    public bool HasToken { get; set; }
    public int TokenLength { get; set; }
    public string DeviceModel { get; set; } = string.Empty;
    public string DeviceManufacturer { get; set; } = string.Empty;
    public string? Error { get; set; }
    public DateTime LastChecked { get; set; }
    public Dictionary<string, object> PlatformSpecificData { get; set; } = new();
}
