namespace CheapHelpers.Services.Notifications;

/// <summary>
/// Marker interface for subscription context objects that define what entity a notification relates to
/// </summary>
public interface ISubscriptionContext
{
    /// <summary>
    /// Type of entity this subscription context represents (e.g., "Order", "Project", "Task")
    /// </summary>
    string EntityType { get; }
}
