using CheapHelpers.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace CheapHelpers.Models.Entities;

/// <summary>
/// Represents a user's notification preferences for a specific notification type
/// </summary>
public class UserNotificationPreference
{
    /// <summary>
    /// Unique identifier for the preference
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// ID of the user who owns this preference
    /// </summary>
    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Type of notification this preference applies to (e.g., "OrderConfirmation", "SystemAlert")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string NotificationType { get; set; } = string.Empty;

    /// <summary>
    /// Bitwise flags indicating which notification channels are enabled for this notification type
    /// </summary>
    public NotificationChannelFlags EnabledChannels { get; set; } = NotificationChannelFlags.InApp;

    /// <summary>
    /// Start hour (0-23) for Do Not Disturb period, null if not set
    /// </summary>
    [Range(0, 23)]
    public int? DoNotDisturbStartHour { get; set; }

    /// <summary>
    /// End hour (0-23) for Do Not Disturb period, null if not set
    /// </summary>
    [Range(0, 23)]
    public int? DoNotDisturbEndHour { get; set; }

    /// <summary>
    /// Checks if a specific channel is enabled for this notification type
    /// </summary>
    /// <param name="channel">The channel to check</param>
    /// <returns>True if the channel is enabled, false otherwise</returns>
    public bool IsChannelEnabled(NotificationChannelFlags channel)
    {
        return EnabledChannels.HasFlag(channel);
    }

    /// <summary>
    /// Enables a specific channel for this notification type
    /// </summary>
    /// <param name="channel">The channel to enable</param>
    public void EnableChannel(NotificationChannelFlags channel)
    {
        EnabledChannels |= channel;
    }

    /// <summary>
    /// Disables a specific channel for this notification type
    /// </summary>
    /// <param name="channel">The channel to disable</param>
    public void DisableChannel(NotificationChannelFlags channel)
    {
        EnabledChannels &= ~channel;
    }

    /// <summary>
    /// Checks if the current time falls within the Do Not Disturb period
    /// </summary>
    /// <returns>True if currently in DND period, false otherwise</returns>
    public bool IsInDoNotDisturbPeriod()
    {
        if (!DoNotDisturbStartHour.HasValue || !DoNotDisturbEndHour.HasValue)
            return false;

        var currentHour = DateTime.Now.Hour;
        var startHour = DoNotDisturbStartHour.Value;
        var endHour = DoNotDisturbEndHour.Value;

        if (startHour < endHour)
        {
            // DND period doesn't cross midnight
            return currentHour >= startHour && currentHour < endHour;
        }
        else
        {
            // DND period crosses midnight
            return currentHour >= startHour || currentHour < endHour;
        }
    }
}
