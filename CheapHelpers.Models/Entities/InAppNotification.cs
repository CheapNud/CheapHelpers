using CheapHelpers.Models.Contracts;
using CheapHelpers.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CheapHelpers.Models.Entities;

/// <summary>
/// Represents an in-app notification for a user
/// </summary>
public class InAppNotification : IAuditable
{
    /// <summary>
    /// Unique identifier for the notification
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Type of notification (e.g., "OrderConfirmation", "SystemAlert", "UserMessage")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string NotificationType { get; set; } = string.Empty;

    /// <summary>
    /// Title of the notification
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Plain text body of the notification
    /// </summary>
    [MaxLength(1000)]
    public string? Body { get; set; }

    /// <summary>
    /// HTML body of the notification (for rich content)
    /// </summary>
    [MaxLength(4000)]
    public string? HtmlBody { get; set; }

    /// <summary>
    /// ID of the user who should receive this notification
    /// </summary>
    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Priority level of the notification
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// Whether the notification has been read
    /// </summary>
    public bool IsRead { get; set; } = false;

    /// <summary>
    /// Timestamp when the notification was read
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// URL to navigate to when the notification is clicked
    /// </summary>
    [MaxLength(500)]
    public string? ActionUrl { get; set; }

    /// <summary>
    /// URL to an icon to display with the notification
    /// </summary>
    [MaxLength(500)]
    public string? IconUrl { get; set; }

    /// <summary>
    /// JSON data for custom notification payload
    /// </summary>
    public string? DataJson { get; set; }

    /// <summary>
    /// When the notification expires and should no longer be shown
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether the notification has been archived by the user
    /// </summary>
    public bool IsArchived { get; set; } = false;

    /// <summary>
    /// When the notification was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the notification was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Helper property to check if the notification has expired
    /// </summary>
    [NotMapped]
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

    /// <summary>
    /// Marks the notification as read
    /// </summary>
    public void MarkAsRead()
    {
        IsRead = true;
        ReadAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Archives the notification
    /// </summary>
    public void Archive()
    {
        IsArchived = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Unarchives the notification
    /// </summary>
    public void Unarchive()
    {
        IsArchived = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
