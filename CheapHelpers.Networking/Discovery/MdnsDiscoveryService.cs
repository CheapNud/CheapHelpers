using System.Collections.Concurrent;
using MeaMod.DNS;
using MeaMod.DNS.Model;
using MeaMod.DNS.Multicast;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CheapHelpers.Networking.Discovery;

/// <summary>
/// Generic mDNS/Zeroconf discovery service using MeaMod.DNS.
/// <para>
/// Uses a persistent multicast listener that stays alive across calls —
/// subsequent queries re-use the running listener instead of tearing down
/// and recreating, eliminating dead windows where multicast traffic is missed.
/// </para>
/// </summary>
public sealed class MdnsDiscoveryService(
    ILogger<MdnsDiscoveryService> logger,
    IOptions<MdnsDiscoveryOptions> options)
    : IMdnsDiscoveryService
{
    private readonly MdnsDiscoveryOptions _options = options.Value;
    private readonly SemaphoreSlim _stateLock = new(1, 1);
    private readonly Lock _initLock = new();

    // Persistent listener — created lazily on first use, never recreated until disposal
    private MulticastService? _mdns;
    private ServiceDiscovery? _serviceDiscovery;

    // Split record cache: keyed by "serviceType|instanceName"
    // Holds partial device records that get merged as A/AAAA/SRV/TXT arrive across messages
    private readonly ConcurrentDictionary<string, MdnsDeviceBuilder> _deviceCache = new();

    // Continuous listening state
    private Func<MdnsDevice, Task>? _onDeviceFound;
    private string? _activeServiceType;
    private bool _isListening;
    private int _disposed;

    public bool IsListening => _isListening;

    public Action<Exception>? OnError { get; set; }

    public async Task<List<MdnsDevice>> DiscoverAsync(string serviceType, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        EnsureStarted();

        // Don't clear the cache — the persistent listener accumulates results across calls.
        // Clearing would corrupt concurrent DiscoverAsync calls for the same service type.

        // Initial query
        _serviceDiscovery!.QueryServiceInstances(serviceType);
        logger.LogDebug("mDNS one-shot discovery started for {ServiceType}", serviceType);

        // Re-query at intervals to catch late responders
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_options.ScanTimeout);

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                await Task.Delay(_options.QueryInterval, cts.Token);
                _serviceDiscovery.QueryServiceInstances(serviceType);
            }
        }
        catch (OperationCanceledException) { /* Expected — timeout reached */ }

        // Collect results: only devices with at least one address
        var results = _deviceCache.Values
            .Where(b => string.Equals(b.ServiceType, serviceType, StringComparison.OrdinalIgnoreCase))
            .Where(b => b.HasAddress)
            .Where(PassesFilter)
            .Select(b => b.ToDevice())
            .ToList();

        logger.LogInformation("mDNS discovery for {ServiceType} found {Count} device(s)", serviceType, results.Count);
        return results;
    }

    public async Task StartListeningAsync(
        string serviceType,
        Func<MdnsDevice, Task> onDeviceFound,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        await _stateLock.WaitAsync(cancellationToken);
        try
        {
            if (_isListening)
                throw new InvalidOperationException("Already listening. Call StopListeningAsync first.");

            EnsureStarted();

            _activeServiceType = serviceType;
            _onDeviceFound = onDeviceFound;
            _isListening = true;

            _serviceDiscovery!.QueryServiceInstances(serviceType);
            logger.LogInformation("mDNS continuous listening started for {ServiceType}", serviceType);
        }
        finally
        {
            try { _stateLock.Release(); }
            catch (ObjectDisposedException) { /* Disposal raced — safe to swallow */ }
        }
    }

    public async Task StopListeningAsync(CancellationToken cancellationToken = default)
    {
        await _stateLock.WaitAsync(cancellationToken);
        try
        {
            _isListening = false;
            _onDeviceFound = null;
            _activeServiceType = null;

            logger.LogDebug("mDNS continuous listening stopped");
        }
        finally
        {
            try { _stateLock.Release(); }
            catch (ObjectDisposedException) { /* Disposal raced — safe to swallow */ }
        }
    }

    /// <summary>
    /// Lazily initializes the persistent multicast listener.
    /// Thread-safe via dedicated init lock — safe to call from multiple paths concurrently.
    /// </summary>
    private void EnsureStarted()
    {
        if (_mdns is not null) return;

        lock (_initLock)
        {
            if (_mdns is not null) return;

            var mdns = new MulticastService();
            var sd = new ServiceDiscovery(mdns);

            sd.ServiceInstanceDiscovered += OnServiceInstanceDiscovered;
            mdns.AnswerReceived += OnAnswerReceived;
            mdns.Start();

            _serviceDiscovery = sd;
            _mdns = mdns; // Publish last — this is the flag other threads check

            logger.LogInformation("mDNS persistent listener started (all interfaces)");
        }
    }

    /// <summary>
    /// Fires when a PTR response names a service instance.
    /// Creates or updates a builder entry in the cache.
    /// </summary>
    private void OnServiceInstanceDiscovered(object? sender, ServiceInstanceDiscoveryEventArgs e)
    {
        try
        {
            var instanceName = ExtractInstanceName(e.ServiceInstanceName.ToString());
            var serviceType = ExtractServiceType(e.ServiceInstanceName.ToString());

            if (string.IsNullOrEmpty(instanceName) || string.IsNullOrEmpty(serviceType))
                return;

            var cacheKey = $"{serviceType}|{instanceName}";

            var builder = _deviceCache.GetOrAdd(cacheKey, _ => new MdnsDeviceBuilder
            {
                InstanceName = instanceName,
                ServiceType = serviceType
            });

            // Merge any records from this message
            MergeRecords(builder, e.Message);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error processing mDNS service instance discovery");
            OnError?.Invoke(ex);
        }
    }

    /// <summary>
    /// Fires for EVERY DNS answer — including A, AAAA, SRV, TXT records
    /// that may arrive in separate messages from the PTR response.
    /// This is critical for the split A/AAAA record caching pattern.
    /// </summary>
    private void OnAnswerReceived(object? sender, MessageEventArgs e)
    {
        try
        {
            // Early exit: nothing to merge into
            if (_deviceCache.IsEmpty) return;

            var allRecords = e.Message.Answers
                .Concat(e.Message.AdditionalRecords)
                .ToList();

            // Early exit: only care about records that carry address/service/text data
            if (!allRecords.Any(r => r is ARecord or AAAARecord or SRVRecord or TXTRecord))
                return;

            // Try to match records to existing builders by hostname
            foreach (var builder in _deviceCache.Values)
            {
                if (builder.HostName is null) continue;

                var matched = false;

                foreach (var record in allRecords)
                {
                    var recordName = record.Name.ToString().TrimEnd('.');

                    // Match A/AAAA/TXT records by hostname
                    if (!string.Equals(recordName, builder.HostName, StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(recordName, builder.HostName + ".local", StringComparison.OrdinalIgnoreCase))
                        continue;

                    lock (builder.Sync)
                    {
                        switch (record)
                        {
                            case ARecord aRecord when _options.UseIPv4:
                                var ipv4 = aRecord.Address.ToString();
                                if (builder.IPv4Address != ipv4)
                                {
                                    builder.IPv4Address = ipv4;
                                    builder.LastUpdated = DateTimeOffset.UtcNow;
                                    matched = true;
                                }
                                break;

                            case AAAARecord aaaaRecord when _options.UseIPv6:
                                var ipv6 = aaaaRecord.Address.ToString();
                                if (builder.IPv6Address != ipv6)
                                {
                                    builder.IPv6Address = ipv6;
                                    builder.LastUpdated = DateTimeOffset.UtcNow;
                                    matched = true;
                                }
                                break;

                            case TXTRecord txtRecord:
                                var parsed = ParseTxtRecords(txtRecord.Strings);
                                if (parsed.Count > 0)
                                {
                                    foreach (var (key, txtValue) in parsed)
                                        builder.TxtRecords[key] = txtValue;
                                    builder.LastUpdated = DateTimeOffset.UtcNow;
                                    matched = true;
                                }
                                break;
                        }
                    }
                }

                if (matched && builder.HasAddress)
                    NotifyDeviceFound(builder);
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error processing mDNS answer");
            OnError?.Invoke(ex);
        }
    }

    /// <summary>
    /// Merges A/AAAA/SRV/TXT records from a message into a builder.
    /// </summary>
    private void MergeRecords(MdnsDeviceBuilder builder, Message message)
    {
        var allRecords = message.Answers
            .Concat(message.AdditionalRecords)
            .ToList();

        var updated = false;

        lock (builder.Sync)
        {
            foreach (var record in allRecords)
            {
                switch (record)
                {
                    case SRVRecord srvRecord:
                        var hostName = srvRecord.Target.ToString().TrimEnd('.');
                        // Strip .local suffix for cleaner display
                        if (hostName.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
                            hostName = hostName[..^6];
                        builder.HostName = hostName;
                        builder.Port = srvRecord.Port;
                        updated = true;
                        break;

                    case ARecord aRecord when _options.UseIPv4:
                        builder.IPv4Address = aRecord.Address.ToString();
                        updated = true;
                        break;

                    case AAAARecord aaaaRecord when _options.UseIPv6:
                        builder.IPv6Address = aaaaRecord.Address.ToString();
                        updated = true;
                        break;

                    case TXTRecord txtRecord:
                        var parsed = ParseTxtRecords(txtRecord.Strings);
                        foreach (var (key, txtValue) in parsed)
                            builder.TxtRecords[key] = txtValue;
                        if (parsed.Count > 0) updated = true;
                        break;
                }
            }

            if (updated)
                builder.LastUpdated = DateTimeOffset.UtcNow;
        }

        if (updated && builder.HasAddress)
            NotifyDeviceFound(builder);
    }

    /// <summary>
    /// Fires the continuous listening callback if active and device passes filters.
    /// Runs on the thread pool to avoid blocking the mDNS receive loop.
    /// </summary>
    private void NotifyDeviceFound(MdnsDeviceBuilder builder)
    {
        if (!_isListening || _onDeviceFound is not { } callback)
            return;

        if (!PassesFilter(builder))
            return;

        var device = builder.ToDevice();

        _ = Task.Run(async () =>
        {
            try
            {
                await callback(device);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Error in mDNS device found callback");
                OnError?.Invoke(ex);
            }
        });
    }

    /// <summary>
    /// Parses TXT record strings into key-value pairs.
    /// Handles both separate entries ["key=val", "key2=val2"] and
    /// comma-separated ["key=val,key2=val2"] formats.
    /// </summary>
    private static Dictionary<string, string> ParseTxtRecords(IList<string> strings)
    {
        var txtEntries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in strings)
        {
            // Handle comma-separated entries: "key=val,key2=val2"
            var parts = entry.Contains(',') && entry.Contains('=')
                ? entry.Split(',')
                : [entry];

            foreach (var part in parts)
            {
                var eqIndex = part.IndexOf('=');
                if (eqIndex <= 0) continue;

                var key = part[..eqIndex].Trim();
                var partValue = part[(eqIndex + 1)..].Trim();
                txtEntries[key] = partValue;
            }
        }

        return txtEntries;
    }

    /// <summary>
    /// Extracts the instance name from a full service instance name.
    /// "My Printer._printer._tcp.local" → "My Printer"
    /// </summary>
    private static string? ExtractInstanceName(string fullName)
    {
        // Instance names are: "InstanceName._serviceType._tcp.local"
        // Find first underscore-prefixed segment to split
        var underscoreIndex = fullName.IndexOf("._", StringComparison.Ordinal);
        return underscoreIndex > 0 ? fullName[..underscoreIndex] : null;
    }

    /// <summary>
    /// Extracts the service type from a full service instance name.
    /// "My Printer._printer._tcp.local" → "_printer._tcp"
    /// </summary>
    private static string? ExtractServiceType(string fullName)
    {
        var underscoreIndex = fullName.IndexOf("._", StringComparison.Ordinal);
        if (underscoreIndex < 0) return null;

        // Skip the dot: "._printer._tcp.local" → "_printer._tcp.local"
        var serviceAndDomain = fullName[(underscoreIndex + 1)..];

        // Remove ".local" suffix
        var localIndex = serviceAndDomain.IndexOf(".local", StringComparison.OrdinalIgnoreCase);
        return localIndex > 0 ? serviceAndDomain[..localIndex] : serviceAndDomain;
    }

    private bool PassesFilter(MdnsDeviceBuilder builder)
    {
        if (_options.InstanceNameFilter is not { } filter)
            return true;

        return builder.InstanceName.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    public ValueTask DisposeAsync()
    {
        ReleaseResources();
        return ValueTask.CompletedTask;
    }

    public void Dispose() => ReleaseResources();

    private void ReleaseResources()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
            return;
        _isListening = false;
        _onDeviceFound = null;

        // MeaMod.DNS Stop/Dispose are synchronous — safe from both paths
        if (_serviceDiscovery is not null)
        {
            _serviceDiscovery.ServiceInstanceDiscovered -= OnServiceInstanceDiscovered;
            _serviceDiscovery.Dispose();
        }

        if (_mdns is not null)
        {
            _mdns.AnswerReceived -= OnAnswerReceived;
            _mdns.Stop();
            _mdns.Dispose();
        }

        _deviceCache.Clear();
        _stateLock.Dispose();

        logger.LogDebug("mDNS discovery service disposed");
    }

    /// <summary>
    /// Mutable builder that accumulates partial mDNS records across messages.
    /// Produces an immutable <see cref="MdnsDevice"/> when complete.
    /// </summary>
    private sealed class MdnsDeviceBuilder
    {
        public required string InstanceName { get; set; }
        public required string ServiceType { get; set; }
        public string? HostName { get; set; }
        public string? IPv4Address { get; set; }
        public string? IPv6Address { get; set; }
        public int Port { get; set; }
        public Dictionary<string, string> TxtRecords { get; } = new(StringComparer.OrdinalIgnoreCase);
        public DateTimeOffset FirstSeen { get; } = DateTimeOffset.UtcNow;
        public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
        public readonly Lock Sync = new();

        public bool HasAddress => IPv4Address is not null || IPv6Address is not null;

        public MdnsDevice ToDevice()
        {
            lock (Sync)
            {
                return new MdnsDevice
                {
                    InstanceName = InstanceName,
                    ServiceType = ServiceType,
                    HostName = HostName,
                    IPv4Address = IPv4Address,
                    IPv6Address = IPv6Address,
                    Port = Port,
                    TxtRecords = new Dictionary<string, string>(TxtRecords, StringComparer.OrdinalIgnoreCase),
                    FirstSeen = FirstSeen,
                    LastUpdated = LastUpdated
                };
            }
        }
    }
}
