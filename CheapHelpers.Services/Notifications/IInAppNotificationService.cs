using CheapHelpers.Models.Entities;
using CheapHelpers.Services.Notifications.Models;

namespace CheapHelpers.Services.Notifications;

/// <summary>
/// Service for managing in-app notifications for users.
/// </summary>
public interface IInAppNotificationService
{
    /// <summary>
    /// Creates a new in-app notification for a user.
    /// </summary>
    /// <param name="notification">The notification to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure of the operation.</returns>
    Task<InAppNotificationResult> CreateAsync(InAppNotification notification, CancellationToken ct = default);

    /// <summary>
    /// Gets the count of unread notifications for a user.
    /// </summary>
    /// <param name="userId">ID of the user.</param>
    /// <param name="notificationType">Optional notification type to filter by.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Count of unread notifications.</returns>
    Task<int> GetUnreadCountAsync(string userId, string? notificationType = null, CancellationToken ct = default);

    /// <summary>
    /// Gets a filtered list of notifications for a user.
    /// </summary>
    /// <param name="userId">ID of the user.</param>
    /// <param name="filter">Filter and pagination options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of notifications matching the filter criteria.</returns>
    Task<List<InAppNotification>> GetUserNotificationsAsync(string userId, InAppNotificationFilter filter, CancellationToken ct = default);

    /// <summary>
    /// Marks a specific notification as read.
    /// </summary>
    /// <param name="notificationId">ID of the notification to mark as read.</param>
    /// <param name="userId">ID of the user (for authorization check).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the notification was marked as read, false if not found or unauthorized.</returns>
    Task<bool> MarkAsReadAsync(int notificationId, string userId, CancellationToken ct = default);

    /// <summary>
    /// Marks all notifications as read for a user.
    /// </summary>
    /// <param name="userId">ID of the user.</param>
    /// <param name="notificationType">Optional notification type to filter by.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Count of notifications marked as read.</returns>
    Task<int> MarkAllAsReadAsync(string userId, string? notificationType = null, CancellationToken ct = default);

    /// <summary>
    /// Archives a specific notification.
    /// </summary>
    /// <param name="notificationId">ID of the notification to archive.</param>
    /// <param name="userId">ID of the user (for authorization check).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the notification was archived, false if not found or unauthorized.</returns>
    Task<bool> ArchiveAsync(int notificationId, string userId, CancellationToken ct = default);

    /// <summary>
    /// Deletes a specific notification.
    /// </summary>
    /// <param name="notificationId">ID of the notification to delete.</param>
    /// <param name="userId">ID of the user (for authorization check).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the notification was deleted, false if not found or unauthorized.</returns>
    Task<bool> DeleteAsync(int notificationId, string userId, CancellationToken ct = default);

    /// <summary>
    /// Deletes all expired notifications from the database.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Count of notifications deleted.</returns>
    Task<int> DeleteExpiredAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a user's notification preferences for a specific notification type.
    /// </summary>
    /// <param name="userId">ID of the user.</param>
    /// <param name="notificationType">Type of notification.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>User's notification preferences, or default preferences if not found.</returns>
    Task<UserNotificationPreference> GetUserPreferencesAsync(string userId, string notificationType, CancellationToken ct = default);

    /// <summary>
    /// Updates or creates a user's notification preferences.
    /// </summary>
    /// <param name="preferences">The preferences to update or create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the preferences were updated successfully, false otherwise.</returns>
    Task<bool> UpdateUserPreferencesAsync(UserNotificationPreference preferences, CancellationToken ct = default);
}
