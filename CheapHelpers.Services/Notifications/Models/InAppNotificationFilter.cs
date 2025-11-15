namespace CheapHelpers.Services.Notifications.Models;

/// <summary>
/// Filtering and pagination options for querying in-app notifications.
/// </summary>
public class InAppNotificationFilter
{
    /// <summary>
    /// Gets or sets a value indicating whether to filter by read status.
    /// </summary>
    /// <value>
    /// - True: Return only read notifications
    /// - False: Return only unread notifications
    /// - Null: Return all notifications regardless of read status (default)
    /// </value>
    public bool? IsRead { get; set; }

    /// <summary>
    /// Gets or sets the notification type to filter by.
    /// </summary>
    /// <value>The notification type string, or null to include all types (default).</value>
    public string? NotificationType { get; set; }

    /// <summary>
    /// Gets or sets the minimum priority level to include.
    /// </summary>
    /// <value>The minimum priority value, or null to include all priorities (default).</value>
    /// <remarks>
    /// Typical priority values:
    /// - 1: Low priority
    /// - 2: Normal priority
    /// - 3: High priority
    /// - 4+: Critical/urgent priority
    /// </remarks>
    public int? MinimumPriority { get; set; }

    /// <summary>
    /// Gets or sets the earliest date/time for notifications to include.
    /// </summary>
    /// <value>The minimum creation date/time, or null to include all dates (default).</value>
    public DateTimeOffset? Since { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include archived notifications.
    /// </summary>
    /// <value>True to include archived notifications; otherwise, false (default).</value>
    public bool IncludeArchived { get; set; }

    /// <summary>
    /// Gets or sets the number of records to skip for pagination.
    /// </summary>
    /// <value>The number of records to skip (default is 0).</value>
    public int Skip { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of records to return.
    /// </summary>
    /// <value>The maximum number of records, or null for no limit (default).</value>
    public int? Take { get; set; }

    /// <summary>
    /// Gets or sets the field to sort by.
    /// </summary>
    /// <value>The sort field name (e.g., "CreatedAt", "Priority"), or null to use default sorting (default).</value>
    /// <remarks>
    /// Common sort fields:
    /// - "CreatedAt": Sort by creation date/time
    /// - "Priority": Sort by notification priority
    /// - "ReadAt": Sort by when notification was read
    /// </remarks>
    public string? SortBy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to sort in descending order.
    /// </summary>
    /// <value>True for descending order; false for ascending order (default).</value>
    public bool SortDescending { get; set; }
}
