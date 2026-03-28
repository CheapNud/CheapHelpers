namespace CheapHelpers.Services.Polling;

/// <summary>
/// Typed wrapper for <see cref="HttpPollingOptions"/> keyed by <typeparamref name="TResponse"/>,
/// enabling multiple polling services with different configurations in the same DI container.
/// </summary>
public class HttpPollingOptions<TResponse> : HttpPollingOptions;

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

    /// <summary>
    /// Validates that required options are configured correctly.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="Endpoint"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <see cref="Endpoint"/> is not an absolute URI.</exception>
    public void Validate()
    {
        ArgumentNullException.ThrowIfNull(Endpoint, nameof(Endpoint));
        if (!Endpoint.IsAbsoluteUri)
            throw new ArgumentException("Endpoint must be an absolute URI.", nameof(Endpoint));
    }
}
