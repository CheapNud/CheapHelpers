namespace CheapHelpers.Blazor.Hybrid.Abstractions;

/// <summary>
/// Service for displaying local notifications when the app is in the foreground.
/// Converts push notifications to local notifications so users see them even when the app is active.
/// Platform-specific implementations handle the actual notification display.
/// </summary>
public interface ILocalNotificationService
{
    /// <summary>
    /// Display a local notification with title and body
    /// </summary>
    /// <param name="title">Notification title</param>
    /// <param name="body">Notification body text</param>
    /// <param name="data">Optional custom data dictionary for handling notification clicks</param>
    Task ShowNotificationAsync(string title, string body, Dictionary<string, string>? data = null);

    /// <summary>
    /// Check if local notifications are supported and permitted on this device
    /// </summary>
    Task<bool> IsPermittedAsync();
}
