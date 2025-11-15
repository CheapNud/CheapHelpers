namespace CheapHelpers.Models.Enums;

/// <summary>
/// Flags indicating which notification channels should be used for delivery
/// </summary>
[Flags]
public enum NotificationChannelFlags
{
    /// <summary>
    /// No channels specified
    /// </summary>
    None = 0,

    /// <summary>
    /// In-app notification channel
    /// </summary>
    InApp = 1 << 0,

    /// <summary>
    /// Email notification channel
    /// </summary>
    Email = 1 << 1,

    /// <summary>
    /// SMS notification channel
    /// </summary>
    Sms = 1 << 2,

    /// <summary>
    /// Push notification channel
    /// </summary>
    Push = 1 << 3,

    /// <summary>
    /// Desktop OS notification channel (requires CheapHelpers.Avalonia.Bridge package)
    /// </summary>
    Desktop = 1 << 4,

    /// <summary>
    /// All available notification channels
    /// </summary>
    All = InApp | Email | Sms | Push | Desktop
}
