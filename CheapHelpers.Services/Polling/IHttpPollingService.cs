namespace CheapHelpers.Services.Polling;

/// <summary>
/// Generic HTTP polling service that periodically fetches data from an endpoint
/// and invokes callbacks with the deserialized response.
/// </summary>
/// <typeparam name="TResponse">The type to deserialize the HTTP response into.</typeparam>
public interface IHttpPollingService<TResponse> : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Starts polling the configured endpoint.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops polling.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether the service is currently polling.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Callback invoked when data is successfully received and deserialized.
    /// </summary>
    Func<TResponse, Task>? OnDataReceived { get; set; }

    /// <summary>
    /// Callback invoked when a polling attempt fails.
    /// </summary>
    Action<Exception>? OnError { get; set; }
}
