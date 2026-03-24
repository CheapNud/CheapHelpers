namespace CheapHelpers.Services.Polling;

/// <summary>
/// Configuration options for <see cref="IHttpPollingService{TResponse}"/>.
/// </summary>
public class HttpPollingOptions
{
    /// <summary>
    /// The endpoint to poll.
    /// </summary>
    public Uri Endpoint { get; set; } = default!;

    /// <summary>
    /// Polling interval between requests.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum consecutive retries on transient failure before backing off.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Base delay between retries. Doubles on each consecutive failure (exponential backoff).
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// HTTP request timeout per individual poll.
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(10);
}
