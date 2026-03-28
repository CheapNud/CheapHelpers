using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using CheapHelpers.Models.Contracts;

namespace CheapHelpers.Models.Entities;

/// <summary>
/// Represents an API key for authenticating machine-to-machine requests.
/// The key is stored as a SHA-256 hash — the full key is only available at creation time.
/// </summary>
public class ApiKey : IEntityId, IAuditable
{
    public int Id { get; set; }

    /// <summary>
    /// SHA-256 hex hash of the full API key (64 characters). Used for validation lookups.
    /// </summary>
    [Required, MaxLength(64)]
    public string KeyHash { get; set; } = string.Empty;

    /// <summary>
    /// Short prefix of the key for display and identification (e.g., "ch_Ab3xYz12").
    /// </summary>
    [Required, MaxLength(20)]
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name for the key (e.g., "Production API", "CI/CD Pipeline").
    /// </summary>
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the key's purpose.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// ID of the user who owns this key. Maps to IdentityUser.Id.
    /// </summary>
    [Required, MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// JSON array of scope strings (e.g., ["read", "write"]). Null means all scopes.
    /// </summary>
    [MaxLength(2000)]
    public string? ScopesJson { get; set; }

    /// <summary>
    /// Maximum requests per minute for this key. 0 means unlimited.
    /// </summary>
    public int RateLimitPerMinute { get; set; }

    /// <summary>
    /// Maximum requests per day for this key. 0 means unlimited.
    /// </summary>
    public int RateLimitPerDay { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// When the key expires. Null means no expiration.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// When the key was revoked. Null means not revoked.
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Last time the key was used for authentication. Updated on cache miss only.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Whether the key is active. Set to false for soft-delete.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether the key is currently valid (active, not revoked, not expired).
    /// </summary>
    [NotMapped]
    public bool IsValid => IsActive && RevokedAt is null && (ExpiresAt is null || ExpiresAt > DateTime.UtcNow);

    /// <summary>
    /// Deserialized scopes from <see cref="ScopesJson"/>. Empty list if no scopes are set.
    /// </summary>
    [NotMapped]
    public List<string> Scopes
    {
        get => string.IsNullOrEmpty(ScopesJson)
            ? []
            : JsonSerializer.Deserialize<List<string>>(ScopesJson) ?? [];
        set => ScopesJson = value.Count > 0 ? JsonSerializer.Serialize(value) : null;
    }
}
