using System.Collections.Concurrent;
using System.Security.Cryptography;
using CheapHelpers.EF;
using CheapHelpers.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Services.ApiKeys;

/// <summary>
/// API key management service with SHA-256 hashing and in-memory validation cache.
/// </summary>
public class ApiKeyService<TUser>(
    CheapContext<TUser> dbContext,
    ApiKeyOptions apiKeyOptions,
    ILogger<ApiKeyService<TUser>> logger) : IApiKeyService where TUser : IdentityUser
{
    private static readonly ConcurrentDictionary<string, (ApiKeyValidationResult Result, DateTime CachedAt)> ValidationCache = new();

    public async Task<ApiKeyCreateResult> GenerateAsync(
        string userId,
        string name,
        List<string>? scopes = null,
        DateTime? expiresAt = null,
        string? description = null,
        string? prefixOverride = null,
        string? createdBy = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var prefix = prefixOverride ?? apiKeyOptions.KeyPrefix;

        var rawKeyBytes = RandomNumberGenerator.GetBytes(apiKeyOptions.KeyLengthBytes);
        var rawKeyBase64 = Convert.ToBase64String(rawKeyBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        var fullKey = $"{prefix}{rawKeyBase64}";
        var keyHash = ComputeHash(fullKey);
        var keyPrefix = fullKey[..Math.Min(fullKey.Length, 12)] + "...";

        var apiKey = new ApiKey
        {
            KeyHash = keyHash,
            KeyPrefix = keyPrefix,
            Name = name,
            Description = description,
            UserId = userId,
            CreatedBy = createdBy ?? userId,
            RateLimitPerMinute = apiKeyOptions.DefaultRateLimitPerMinute,
            RateLimitPerDay = apiKeyOptions.DefaultRateLimitPerDay,
            ExpiresAt = expiresAt,
        };

        if (scopes is { Count: > 0 })
            apiKey.Scopes = scopes;

        dbContext.ApiKeys.Add(apiKey);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Generated API key {KeyPrefix} for user {UserId} (created by {CreatedBy})", keyPrefix, userId, apiKey.CreatedBy);
        return ApiKeyCreateResult.Succeeded(apiKey.Id, fullKey, keyPrefix);
    }

    public async Task<ApiKeyValidationResult> ValidateAsync(string rawKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawKey))
            return ApiKeyValidationResult.Invalid("Empty API key");

        var keyHash = ComputeHash(rawKey);

        // Check cache
        if (ValidationCache.TryGetValue(keyHash, out var cached) &&
            cached.CachedAt + apiKeyOptions.CacheTtl > DateTime.UtcNow)
        {
            return cached.Result;
        }

        // Cache miss — query database
        var apiKey = await dbContext.ApiKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash, cancellationToken);

        if (apiKey is null)
        {
            var invalidResult = ApiKeyValidationResult.Invalid("Invalid API key");
            ValidationCache[keyHash] = (invalidResult, DateTime.UtcNow);
            return invalidResult;
        }

        if (!apiKey.IsValid)
        {
            var reason = apiKey.RevokedAt is not null ? "API key has been revoked"
                : !apiKey.IsActive ? "API key is inactive"
                : "API key has expired";
            var invalidResult = ApiKeyValidationResult.Invalid(reason);
            ValidationCache[keyHash] = (invalidResult, DateTime.UtcNow);
            return invalidResult;
        }

        var validResult = ApiKeyValidationResult.Valid(apiKey);
        ValidationCache[keyHash] = (validResult, DateTime.UtcNow);

        // Update LastUsedAt on cache miss (fire-and-forget to avoid blocking validation)
        _ = UpdateLastUsedAsync(apiKey.Id);

        return validResult;
    }

    public async Task<bool> RevokeAsync(int keyId, CancellationToken cancellationToken = default)
    {
        var apiKey = await dbContext.ApiKeys.FindAsync([keyId], cancellationToken);
        if (apiKey is null)
            return false;

        apiKey.RevokedAt = DateTime.UtcNow;
        apiKey.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);

        InvalidateCache(apiKey.KeyHash);
        logger.LogInformation("Revoked API key {KeyId} ({KeyPrefix})", keyId, apiKey.KeyPrefix);
        return true;
    }

    public async Task<ApiKeyCreateResult> RotateAsync(int keyId, CancellationToken cancellationToken = default)
    {
        var existingKey = await dbContext.ApiKeys.FindAsync([keyId], cancellationToken);
        if (existingKey is null)
            return ApiKeyCreateResult.Failed("API key not found");

        // Revoke the old key
        existingKey.RevokedAt = DateTime.UtcNow;
        existingKey.IsActive = false;
        InvalidateCache(existingKey.KeyHash);

        // Generate a new key with the same config
        var rawKeyBytes = RandomNumberGenerator.GetBytes(apiKeyOptions.KeyLengthBytes);
        var rawKeyBase64 = Convert.ToBase64String(rawKeyBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        var fullKey = $"{apiKeyOptions.KeyPrefix}{rawKeyBase64}";
        var keyHash = ComputeHash(fullKey);
        var keyPrefix = fullKey[..Math.Min(fullKey.Length, 12)] + "...";

        var newKey = new ApiKey
        {
            KeyHash = keyHash,
            KeyPrefix = keyPrefix,
            Name = existingKey.Name,
            Description = existingKey.Description,
            UserId = existingKey.UserId,
            CreatedBy = existingKey.CreatedBy,
            ScopesJson = existingKey.ScopesJson,
            RateLimitPerMinute = existingKey.RateLimitPerMinute,
            RateLimitPerDay = existingKey.RateLimitPerDay,
            ExpiresAt = existingKey.ExpiresAt,
        };

        dbContext.ApiKeys.Add(newKey);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Rotated API key {OldKeyId} → {NewKeyId} ({KeyPrefix})", keyId, newKey.Id, keyPrefix);
        return ApiKeyCreateResult.Succeeded(newKey.Id, fullKey, keyPrefix);
    }

    public async Task<List<ApiKey>> GetUserKeysAsync(string userId, CancellationToken cancellationToken = default)
    {
        var keys = await dbContext.ApiKeys
            .AsNoTracking()
            .Where(k => k.UserId == userId)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(cancellationToken);

        // Redact key hashes — never expose them outside the service
        foreach (var apiKey in keys)
            apiKey.KeyHash = "***";

        return keys;
    }

    public void InvalidateCache(string keyHash)
    {
        ValidationCache.TryRemove(keyHash, out _);
    }

    private async Task UpdateLastUsedAsync(int keyId)
    {
        try
        {
            await dbContext.ApiKeys
                .Where(k => k.Id == keyId)
                .ExecuteUpdateAsync(s => s.SetProperty(k => k.LastUsedAt, DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to update LastUsedAt for API key {KeyId}", keyId);
        }
    }

    private static string ComputeHash(string rawKey)
    {
        var hashBytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexStringLower(hashBytes);
    }
}
