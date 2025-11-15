namespace CheapHelpers.Models.Enums;

/// <summary>
/// Priority level for notifications affecting delivery and display behavior
/// </summary>
public enum NotificationPriority
{
    /// <summary>
    /// Low priority notification (non-critical)
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority notification (default)
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority notification (important)
    /// </summary>
    High = 2,

    /// <summary>
    /// Urgent priority notification (critical, requires immediate attention)
    /// </summary>
    Urgent = 3
}
