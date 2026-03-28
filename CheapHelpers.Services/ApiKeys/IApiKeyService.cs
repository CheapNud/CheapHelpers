using CheapHelpers.Models.Entities;

namespace CheapHelpers.Services.ApiKeys;

/// <summary>
/// Service for generating, validating, revoking, and rotating API keys.
/// Keys are SHA-256 hashed — the full key is only available at creation time.
/// </summary>
public interface IApiKeyService
{
    /// <summary>
    /// Generates a new API key for the specified user.
    /// The <see cref="ApiKeyCreateResult.FullKey"/> is only available in this result — store it immediately.
    /// </summary>
    Task<ApiKeyCreateResult> GenerateAsync(
        string userId,
        string name,
        List<string>? scopes = null,
        DateTime? expiresAt = null,
        string? description = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a raw API key string. Returns the associated key entity and user info if valid.
    /// Uses in-memory caching to avoid hitting the database on every request.
    /// </summary>
    Task<ApiKeyValidationResult> ValidateAsync(string rawKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes an API key by setting <see cref="ApiKey.RevokedAt"/>. The key remains in the database for audit.
    /// </summary>
    Task<bool> RevokeAsync(int keyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes the existing key and generates a new one with the same configuration (name, scopes, rate limits).
    /// </summary>
    Task<ApiKeyCreateResult> RotateAsync(int keyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all API keys for a user. Key hashes are redacted from the result.
    /// </summary>
    Task<List<ApiKey>> GetUserKeysAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a specific key hash from the in-memory validation cache.
    /// </summary>
    void InvalidateCache(string keyHash);
}
