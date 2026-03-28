using CheapHelpers.Models.Entities;

namespace CheapHelpers.Services.ApiKeys;

/// <summary>
/// Generic API key validator for domain-specific validation logic.
/// Implement this for your entity type to add custom validation rules
/// beyond the standard hash lookup and expiry checks.
/// </summary>
/// <typeparam name="TEntity">The entity type the API key grants access to (e.g., a Project, Tenant, Device).</typeparam>
public interface IApiKeyValidator<TEntity> where TEntity : class
{
    /// <summary>
    /// Validates an API key in the context of a specific entity.
    /// Called after the base key validation (hash, expiry, revocation) has passed.
    /// </summary>
    /// <param name="apiKey">The validated API key entity.</param>
    /// <param name="entity">The entity the key is being used to access.</param>
    /// <param name="requiredScope">Optional scope required for this operation (e.g., "read", "write").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Validation result indicating whether access is granted.</returns>
    Task<ApiKeyEntityValidationResult> ValidateForEntityAsync(
        ApiKey apiKey,
        TEntity entity,
        string? requiredScope = null,
        CancellationToken ct = default);
}

/// <summary>
/// Result of an entity-scoped API key validation.
/// </summary>
public sealed record ApiKeyEntityValidationResult(
    bool IsAuthorized,
    string? DenialReason = null)
{
    public static ApiKeyEntityValidationResult Authorized() => new(true);
    public static ApiKeyEntityValidationResult Denied(string reason) => new(false, reason);
}
