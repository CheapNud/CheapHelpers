namespace CheapHelpers.Blazor.Hybrid.Models;

/// <summary>
/// State of device registration for push notifications
/// </summary>
public enum DeviceRegistrationState
{
    /// <summary>
    /// Device is not registered
    /// </summary>
    NotRegistered,

    /// <summary>
    /// Permission request is pending
    /// </summary>
    PermissionPending,

    /// <summary>
    /// Permission was denied by the user
    /// </summary>
    PermissionDenied,

    /// <summary>
    /// Device is registered and active
    /// </summary>
    Registered,

    /// <summary>
    /// Registration failed
    /// </summary>
    Failed
}
