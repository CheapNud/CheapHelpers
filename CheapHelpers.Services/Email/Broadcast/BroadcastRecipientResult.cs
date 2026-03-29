namespace CheapHelpers.Services.Email.Broadcast;

/// <summary>
/// Result of sending a broadcast email to a single recipient.
/// </summary>
public sealed record BroadcastRecipientResult
{
    public required string Email { get; init; }
    public bool IsSuccess { get; init; }
    public bool IsSkipped { get; init; }
    public string? ErrorMessage { get; init; }

    public static BroadcastRecipientResult Success(string email) =>
        new() { Email = email, IsSuccess = true };

    public static BroadcastRecipientResult Failed(string email, string errorMessage) =>
        new() { Email = email, ErrorMessage = errorMessage };

    public static BroadcastRecipientResult Skipped(string email, string reason) =>
        new() { Email = email, IsSkipped = true, ErrorMessage = reason };
}
