namespace CheapHelpers.Services.Notifications.Models;

/// <summary>
/// Represents the result of an in-app notification operation.
/// </summary>
/// <param name="IsSuccess">Indicates whether the operation completed successfully.</param>
/// <param name="NotificationId">The unique identifier of the notification that was operated on, if applicable.</param>
/// <param name="ErrorMessage">A human-readable error message if the operation failed, otherwise null.</param>
/// <param name="Exception">The exception that caused the failure, if applicable, otherwise null.</param>
public record InAppNotificationResult(
    bool IsSuccess,
    string? NotificationId = null,
    string? ErrorMessage = null,
    Exception? Exception = null)
{
    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="notificationId">The unique identifier of the notification that was operated on.</param>
    /// <returns>A successful <see cref="InAppNotificationResult"/> instance.</returns>
    public static InAppNotificationResult Success(string? notificationId = null)
        => new(IsSuccess: true, NotificationId: notificationId);

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="errorMessage">A human-readable error message describing the failure.</param>
    /// <param name="notificationId">The unique identifier of the notification that was operated on, if applicable.</param>
    /// <returns>A failed <see cref="InAppNotificationResult"/> instance.</returns>
    public static InAppNotificationResult Failure(string errorMessage, string? notificationId = null)
        => new(IsSuccess: false, NotificationId: notificationId, ErrorMessage: errorMessage);

    /// <summary>
    /// Creates a failed result with an exception.
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="notificationId">The unique identifier of the notification that was operated on, if applicable.</param>
    /// <returns>A failed <see cref="InAppNotificationResult"/> instance.</returns>
    public static InAppNotificationResult Failure(Exception exception, string? notificationId = null)
        => new(IsSuccess: false, NotificationId: notificationId, ErrorMessage: exception.Message, Exception: exception);

    /// <summary>
    /// Creates a failed result with both an error message and an exception.
    /// </summary>
    /// <param name="errorMessage">A human-readable error message describing the failure.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="notificationId">The unique identifier of the notification that was operated on, if applicable.</param>
    /// <returns>A failed <see cref="InAppNotificationResult"/> instance.</returns>
    public static InAppNotificationResult Failure(string errorMessage, Exception exception, string? notificationId = null)
        => new(IsSuccess: false, NotificationId: notificationId, ErrorMessage: errorMessage, Exception: exception);
}
