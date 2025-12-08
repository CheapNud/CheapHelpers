using CheapHelpers.EF;
using CheapHelpers.Models.Entities;
using CheapHelpers.Models.Enums;
using CheapHelpers.Services.Notifications.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Services.Notifications;

/// <summary>
/// Service for managing in-app notifications for users.
/// </summary>
/// <typeparam name="TUser">The type of user entity, must inherit from IdentityUser.</typeparam>
public class InAppNotificationService<TUser>(
    CheapContext<TUser> context,
    ILogger<InAppNotificationService<TUser>> logger) : IInAppNotificationService
    where TUser : IdentityUser
{
    /// <summary>
    /// Creates a new in-app notification for a user.
    /// </summary>
    /// <param name="notification">The notification to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure of the operation.</returns>
    public async Task<InAppNotificationResult> CreateAsync(InAppNotification notification, CancellationToken ct = default)
    {
        try
        {
            notification.CreatedAt = DateTime.UtcNow;
            notification.UpdatedAt = DateTime.UtcNow;

            context.InAppNotifications.Add(notification);
            await context.SaveChangesAsync(ct);

            logger.LogInformation("Created in-app notification {NotificationId} for user {UserId} with type {NotificationType}",
                notification.Id, notification.UserId, notification.NotificationType);

            return InAppNotificationResult.Success(notification.Id.ToString());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create in-app notification for user {UserId}", notification.UserId);
            return InAppNotificationResult.Failure(ex);
        }
    }

    /// <summary>
    /// Gets the count of unread notifications for a user.
    /// </summary>
    /// <param name="userId">ID of the user.</param>
    /// <param name="notificationType">Optional notification type to filter by.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Count of unread notifications.</returns>
    public async Task<int> GetUnreadCountAsync(string userId, string? notificationType = null, CancellationToken ct = default)
    {
        try
        {
            var utcNow = DateTime.UtcNow;
            var query = context.InAppNotifications
                .Where(n => n.UserId == userId && !n.IsRead && !n.IsArchived)
                .Where(n => !n.ExpiresAt.HasValue || n.ExpiresAt > utcNow);

            if (!string.IsNullOrWhiteSpace(notificationType))
            {
                query = query.Where(n => n.NotificationType == notificationType);
            }

            var count = await query.CountAsync(ct);

            logger.LogInformation("Retrieved unread count of {Count} for user {UserId}", count, userId);
            return count;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get unread count for user {UserId}", userId);
            return 0;
        }
    }

    /// <summary>
    /// Gets a filtered list of notifications for a user.
    /// </summary>
    /// <param name="userId">ID of the user.</param>
    /// <param name="filter">Filter and pagination options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of notifications matching the filter criteria.</returns>
    public async Task<List<InAppNotification>> GetUserNotificationsAsync(string userId, InAppNotificationFilter filter, CancellationToken ct = default)
    {
        try
        {
            var utcNow = DateTime.UtcNow;
            var query = context.InAppNotifications
                .Where(n => n.UserId == userId)
                .Where(n => !n.ExpiresAt.HasValue || n.ExpiresAt > utcNow);

            // Apply filter: IsRead
            if (filter.IsRead.HasValue)
            {
                query = query.Where(n => n.IsRead == filter.IsRead.Value);
            }

            // Apply filter: NotificationType
            if (!string.IsNullOrWhiteSpace(filter.NotificationType))
            {
                query = query.Where(n => n.NotificationType == filter.NotificationType);
            }

            // Apply filter: MinimumPriority
            if (filter.MinimumPriority.HasValue)
            {
                query = query.Where(n => (int)n.Priority >= filter.MinimumPriority.Value);
            }

            // Apply filter: Since
            if (filter.Since.HasValue)
            {
                query = query.Where(n => n.CreatedAt >= filter.Since.Value.UtcDateTime);
            }

            // Apply filter: IncludeArchived
            if (!filter.IncludeArchived)
            {
                query = query.Where(n => !n.IsArchived);
            }

            // Apply sorting
            if (!string.IsNullOrWhiteSpace(filter.SortBy))
            {
                query = filter.SortBy.ToLowerInvariant() switch
                {
                    "createdat" => filter.SortDescending
                        ? query.OrderByDescending(n => n.CreatedAt)
                        : query.OrderBy(n => n.CreatedAt),
                    "priority" => filter.SortDescending
                        ? query.OrderByDescending(n => n.Priority)
                        : query.OrderBy(n => n.Priority),
                    "readat" => filter.SortDescending
                        ? query.OrderByDescending(n => n.ReadAt)
                        : query.OrderBy(n => n.ReadAt),
                    _ => query.OrderByDescending(n => n.CreatedAt)
                };
            }
            else
            {
                // Default sort by CreatedAt descending
                query = query.OrderByDescending(n => n.CreatedAt);
            }

            // Apply pagination
            query = query.Skip(filter.Skip);
            if (filter.Take.HasValue)
            {
                query = query.Take(filter.Take.Value);
            }

            var notifications = await query.ToListAsync(ct);

            logger.LogInformation("Retrieved {Count} notifications for user {UserId}", notifications.Count, userId);
            return notifications;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get notifications for user {UserId}", userId);
            return [];
        }
    }

    /// <summary>
    /// Marks a specific notification as read.
    /// </summary>
    /// <param name="notificationId">ID of the notification to mark as read.</param>
    /// <param name="userId">ID of the user (for authorization check).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the notification was marked as read, false if not found or unauthorized.</returns>
    public async Task<bool> MarkAsReadAsync(int notificationId, string userId, CancellationToken ct = default)
    {
        try
        {
            var notification = await context.InAppNotifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, ct);

            if (notification == null)
            {
                logger.LogWarning("Notification {NotificationId} not found for user {UserId}", notificationId, userId);
                return false;
            }

            notification.MarkAsRead();
            await context.SaveChangesAsync(ct);

            logger.LogInformation("Marked notification {NotificationId} as read for user {UserId}", notificationId, userId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to mark notification {NotificationId} as read for user {UserId}", notificationId, userId);
            return false;
        }
    }

    /// <summary>
    /// Marks all notifications as read for a user.
    /// </summary>
    /// <param name="userId">ID of the user.</param>
    /// <param name="notificationType">Optional notification type to filter by.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Count of notifications marked as read.</returns>
    public async Task<int> MarkAllAsReadAsync(string userId, string? notificationType = null, CancellationToken ct = default)
    {
        try
        {
            var query = context.InAppNotifications
                .Where(n => n.UserId == userId && !n.IsRead);

            if (!string.IsNullOrWhiteSpace(notificationType))
            {
                query = query.Where(n => n.NotificationType == notificationType);
            }

            var notifications = await query.ToListAsync(ct);
            var utcNow = DateTime.UtcNow;

            foreach (var notification in notifications)
            {
                notification.MarkAsRead();
            }

            await context.SaveChangesAsync(ct);

            logger.LogInformation("Marked {Count} notifications as read for user {UserId}", notifications.Count, userId);
            return notifications.Count;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to mark all notifications as read for user {UserId}", userId);
            return 0;
        }
    }

    /// <summary>
    /// Archives a specific notification.
    /// </summary>
    /// <param name="notificationId">ID of the notification to archive.</param>
    /// <param name="userId">ID of the user (for authorization check).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the notification was archived, false if not found or unauthorized.</returns>
    public async Task<bool> ArchiveAsync(int notificationId, string userId, CancellationToken ct = default)
    {
        try
        {
            var notification = await context.InAppNotifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, ct);

            if (notification == null)
            {
                logger.LogWarning("Notification {NotificationId} not found for user {UserId}", notificationId, userId);
                return false;
            }

            notification.Archive();
            await context.SaveChangesAsync(ct);

            logger.LogInformation("Archived notification {NotificationId} for user {UserId}", notificationId, userId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to archive notification {NotificationId} for user {UserId}", notificationId, userId);
            return false;
        }
    }

    /// <summary>
    /// Deletes a specific notification.
    /// </summary>
    /// <param name="notificationId">ID of the notification to delete.</param>
    /// <param name="userId">ID of the user (for authorization check).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the notification was deleted, false if not found or unauthorized.</returns>
    public async Task<bool> DeleteAsync(int notificationId, string userId, CancellationToken ct = default)
    {
        try
        {
            var notification = await context.InAppNotifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, ct);

            if (notification == null)
            {
                logger.LogWarning("Notification {NotificationId} not found for user {UserId}", notificationId, userId);
                return false;
            }

            context.InAppNotifications.Remove(notification);
            await context.SaveChangesAsync(ct);

            logger.LogInformation("Deleted notification {NotificationId} for user {UserId}", notificationId, userId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete notification {NotificationId} for user {UserId}", notificationId, userId);
            return false;
        }
    }

    /// <summary>
    /// Deletes all expired notifications from the database.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Count of notifications deleted.</returns>
    public async Task<int> DeleteExpiredAsync(CancellationToken ct = default)
    {
        try
        {
            var utcNow = DateTime.UtcNow;
            var expiredNotifications = await context.InAppNotifications
                .Where(n => n.ExpiresAt.HasValue && n.ExpiresAt <= utcNow)
                .ToListAsync(ct);

            context.InAppNotifications.RemoveRange(expiredNotifications);
            await context.SaveChangesAsync(ct);

            logger.LogInformation("Deleted {Count} expired notifications", expiredNotifications.Count);
            return expiredNotifications.Count;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete expired notifications");
            return 0;
        }
    }

    /// <summary>
    /// Gets a user's notification preferences for a specific notification type.
    /// </summary>
    /// <param name="userId">ID of the user.</param>
    /// <param name="notificationType">Type of notification.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>User's notification preferences, or default preferences if not found.</returns>
    public async Task<UserNotificationPreference> GetUserPreferencesAsync(string userId, string notificationType, CancellationToken ct = default)
    {
        try
        {
            var preferences = await context.UserNotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId && p.NotificationType == notificationType, ct);

            if (preferences == null)
            {
                // Return default preferences
                logger.LogInformation("No preferences found for user {UserId} and type {NotificationType}, returning defaults",
                    userId, notificationType);

                return new UserNotificationPreference
                {
                    UserId = userId,
                    NotificationType = notificationType,
                    EnabledChannels = NotificationChannelFlags.All
                };
            }

            logger.LogInformation("Retrieved preferences for user {UserId} and type {NotificationType}", userId, notificationType);
            return preferences;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get preferences for user {UserId} and type {NotificationType}", userId, notificationType);

            // Return default on error
            return new UserNotificationPreference
            {
                UserId = userId,
                NotificationType = notificationType,
                EnabledChannels = NotificationChannelFlags.All
            };
        }
    }

    /// <summary>
    /// Updates or creates a user's notification preferences.
    /// </summary>
    /// <param name="preferences">The preferences to update or create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the preferences were updated successfully, false otherwise.</returns>
    public async Task<bool> UpdateUserPreferencesAsync(UserNotificationPreference preferences, CancellationToken ct = default)
    {
        try
        {
            var existing = await context.UserNotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == preferences.UserId && p.NotificationType == preferences.NotificationType, ct);

            if (existing == null)
            {
                // Add new preferences
                context.UserNotificationPreferences.Add(preferences);
                logger.LogInformation("Created new preferences for user {UserId} and type {NotificationType}",
                    preferences.UserId, preferences.NotificationType);
            }
            else
            {
                // Update existing preferences
                existing.EnabledChannels = preferences.EnabledChannels;
                existing.DoNotDisturbStartHour = preferences.DoNotDisturbStartHour;
                existing.DoNotDisturbEndHour = preferences.DoNotDisturbEndHour;
                logger.LogInformation("Updated preferences for user {UserId} and type {NotificationType}",
                    preferences.UserId, preferences.NotificationType);
            }

            await context.SaveChangesAsync(ct);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update preferences for user {UserId} and type {NotificationType}",
                preferences.UserId, preferences.NotificationType);
            return false;
        }
    }
}
