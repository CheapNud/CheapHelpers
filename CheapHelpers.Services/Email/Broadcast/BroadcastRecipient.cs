namespace CheapHelpers.Services.Email.Broadcast;

/// <summary>
/// Represents a single recipient in a broadcast email operation.
/// </summary>
/// <param name="Email">Recipient email address.</param>
/// <param name="DisplayName">Optional display name for personalization.</param>
/// <param name="TemplateData">Optional per-recipient data for Liquid template rendering.
/// Keys become top-level variables in the template context (e.g., "FirstName" → {{ FirstName }}).</param>
public sealed record BroadcastRecipient(
    string Email,
    string? DisplayName = null,
    Dictionary<string, object>? TemplateData = null);
