namespace CheapHelpers.Services.ApiKeys;

/// <summary>
/// Configuration options for the API key distribution system.
/// </summary>
public class ApiKeyOptions
{
    /// <summary>
    /// Configuration section name for binding from appsettings.json.
    /// </summary>
    public const string SectionName = "ApiKeys";

    /// <summary>
    /// HTTP header name to extract the API key from (default "X-Api-Key").
    /// </summary>
    public string HeaderName { get; set; } = "X-Api-Key";

    /// <summary>
    /// Prefix prepended to generated keys (e.g., "ch_"). Helps identify key origin.
    /// </summary>
    public string KeyPrefix { get; set; } = "ch_";

    /// <summary>
    /// How long validated keys are cached in memory before re-checking the database.
    /// </summary>
    public TimeSpan CacheTtl { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Default rate limit per minute for new keys. 0 means unlimited.
    /// </summary>
    public int DefaultRateLimitPerMinute { get; set; } = 60;

    /// <summary>
    /// Default rate limit per day for new keys. 0 means unlimited.
    /// </summary>
    public int DefaultRateLimitPerDay { get; set; } = 10_000;

    /// <summary>
    /// Length in bytes of the random portion of generated keys.
    /// 32 bytes produces a 43-character base64url string.
    /// </summary>
    public int KeyLengthBytes { get; set; } = 32;

    /// <summary>
    /// URL path prefixes that require API key authentication.
    /// Empty list means all paths are protected (except <see cref="ExcludedPaths"/>).
    /// </summary>
    public List<string> ProtectedPaths { get; set; } = [];

    /// <summary>
    /// URL path prefixes that are excluded from API key authentication.
    /// </summary>
    public List<string> ExcludedPaths { get; set; } = [];
}
