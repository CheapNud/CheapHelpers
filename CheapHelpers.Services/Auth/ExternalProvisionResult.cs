namespace CheapHelpers.Services.Auth;

/// <summary>
/// Result of an external user provisioning attempt.
/// </summary>
public sealed record ExternalProvisionResult
{
    public bool Success { get; init; }
    public bool IsNewUser { get; init; }
    public string? UserId { get; init; }
    public string? UserName { get; init; }
    public bool SignInRequired { get; init; }
    public string? ErrorMessage { get; init; }

    public static ExternalProvisionResult Succeeded(string userId, string userName, bool isNew) =>
        new() { Success = true, UserId = userId, UserName = userName, IsNewUser = isNew, SignInRequired = true };

    public static ExternalProvisionResult Failed(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };
}
