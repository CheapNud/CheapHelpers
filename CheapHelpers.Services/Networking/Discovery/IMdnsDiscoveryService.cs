namespace CheapHelpers.Networking.Discovery;

/// <summary>
/// Generic mDNS/Zeroconf discovery service. Finds devices advertising
/// a given service type on the local network.
/// <para>
/// Supports two modes: one-shot discovery via <see cref="DiscoverAsync"/>
/// and continuous listening via <see cref="StartListeningAsync"/>/<see cref="StopListeningAsync"/>.
/// Both share a single persistent multicast listener that stays alive for the service lifetime.
/// </para>
/// </summary>
public interface IMdnsDiscoveryService : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// One-shot discovery: queries for the given service type, waits for
    /// <see cref="MdnsDiscoveryOptions.ScanTimeout"/>, and returns all devices found.
    /// Re-queries at <see cref="MdnsDiscoveryOptions.QueryInterval"/> to catch late responders.
    /// </summary>
    Task<List<MdnsDevice>> DiscoverAsync(string serviceType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts continuous listening for the given service type.
    /// <paramref name="onDeviceFound"/> fires each time a new device is discovered
    /// or an existing device is updated (e.g., a late A record arrives).
    /// </summary>
    Task StartListeningAsync(
        string serviceType,
        Func<MdnsDevice, Task> onDeviceFound,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops continuous listening. The persistent multicast listener stays alive
    /// for future calls; only the active query subscription is removed.
    /// </summary>
    Task StopListeningAsync(CancellationToken cancellationToken = default);

    /// <summary>Whether continuous listening is currently active.</summary>
    bool IsListening { get; }

    /// <summary>Callback invoked when a non-fatal discovery error occurs.</summary>
    Action<Exception>? OnError { get; set; }
}
