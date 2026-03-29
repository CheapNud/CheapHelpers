namespace CheapHelpers.Services.Email.Broadcast;

/// <summary>
/// Configuration options for the broadcast email service.
/// </summary>
public class BroadcastEmailOptions
{
    /// <summary>
    /// Configuration section name for binding from appsettings.json.
    /// </summary>
    public const string SectionName = "BroadcastEmail";

    /// <summary>
    /// Number of recipients per chunk (default 200).
    /// </summary>
    public int ChunkSize { get; set; } = 200;

    /// <summary>
    /// Maximum concurrent email sends within a chunk (default 20).
    /// </summary>
    public int MaxConcurrency { get; set; } = 20;

    /// <summary>
    /// Delay in milliseconds between chunks for rate limiting (default 2000).
    /// </summary>
    public int ChunkDelayMs { get; set; } = 2000;

    /// <summary>
    /// Maximum retry attempts per recipient on transient failures (default 3).
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay in milliseconds for exponential backoff between retries (default 1000).
    /// Actual delay = <c>RetryDelayBaseMs * attemptNumber</c>.
    /// </summary>
    public int RetryDelayBaseMs { get; set; } = 1000;

    /// <summary>
    /// Custom from address for broadcast emails. When null, the default <see cref="IEmailService"/> sender is used.
    /// </summary>
    public string? FromAddress { get; set; }
}
