using CheapHelpers.Models.Entities;

namespace CheapHelpers.Services.ApiKeys;

/// <summary>
/// Result of an API key validation attempt.
/// </summary>
public sealed record ApiKeyValidationResult(
    bool IsValid,
    ApiKey? Key = null,
    string? UserId = null,
    List<string>? Scopes = null,
    string? FailureReason = null)
{
    public static ApiKeyValidationResult Valid(ApiKey apiKey) =>
        new(true, apiKey, apiKey.UserId, apiKey.Scopes);

    public static ApiKeyValidationResult Invalid(string reason) =>
        new(false, FailureReason: reason);
}
