using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Services.Health;

/// <summary>
/// Hosted service that periodically runs all registered <see cref="IDeviceHealthCheck"/> instances
/// and fires <see cref="OnHealthChanged"/> when a device transitions between healthy and unhealthy.
/// </summary>
public class DeviceHealthMonitor(
    IEnumerable<IDeviceHealthCheck> healthChecks,
    DeviceHealthMonitorOptions monitorOptions,
    ILogger<DeviceHealthMonitor> logger) : BackgroundService, IDeviceHealthMonitor
{
    private readonly IDeviceHealthCheck[] _healthChecks = healthChecks.ToArray();
    private readonly DeviceHealthMonitorOptions _monitorOptions = monitorOptions;
    private readonly ILogger<DeviceHealthMonitor> _logger = logger;
    private readonly ConcurrentDictionary<string, DeviceHealthResult> _lastResults = new();

    public Func<DeviceHealthResult, DeviceHealthResult, Task>? OnHealthChanged { get; set; }

    public IReadOnlyDictionary<string, DeviceHealthResult> CurrentStatus => _lastResults;

    public async Task<IReadOnlyList<DeviceHealthResult>> CheckAllAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<DeviceHealthResult>(_healthChecks.Length);

        foreach (var check in _healthChecks)
        {
            var checkResult = await RunCheckAsync(check, cancellationToken);
            results.Add(checkResult);
        }

        return results;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_healthChecks.Length == 0)
        {
            _logger.LogInformation("No device health checks registered, monitor idle");
            return;
        }

        _logger.LogInformation("Device health monitor starting with {Count} checks at {Interval}s interval",
            _healthChecks.Length, _monitorOptions.CheckInterval.TotalSeconds);

        using var timer = new PeriodicTimer(_monitorOptions.CheckInterval);

        // Run once immediately
        await CheckAllAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CheckAllAsync(stoppingToken);
        }
    }

    private async Task<DeviceHealthResult> RunCheckAsync(IDeviceHealthCheck check, CancellationToken cancellationToken)
    {
        DeviceHealthResult checkResult;
        var startedAt = DateTimeOffset.UtcNow;

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_monitorOptions.CheckTimeout);

            checkResult = await check.CheckAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var elapsed = DateTimeOffset.UtcNow - startedAt;
            checkResult = new DeviceHealthResult(false, check.DeviceName, ex.Message, elapsed, DateTimeOffset.UtcNow);
            _logger.LogWarning(ex, "Health check failed for {DeviceName}", check.DeviceName);
        }

        // Detect status transition (including initial check — no previous result counts as a transition)
        var hasPrevious = _lastResults.TryGetValue(check.DeviceName, out var previousResult);
        var isTransition = !hasPrevious || previousResult!.IsHealthy != checkResult.IsHealthy;

        if (isTransition && OnHealthChanged is not null)
        {
            try
            {
                // For initial checks, previous is a synthetic "unknown" result
                var previous = previousResult ?? new DeviceHealthResult(
                    !checkResult.IsHealthy, check.DeviceName, null, TimeSpan.Zero, DateTimeOffset.MinValue);
                await OnHealthChanged(previous, checkResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnHealthChanged callback failed for {DeviceName}", check.DeviceName);
            }
        }

        _lastResults[check.DeviceName] = checkResult;
        return checkResult;
    }
}

/// <summary>
/// Configuration options for <see cref="DeviceHealthMonitor"/>.
/// </summary>
public class DeviceHealthMonitorOptions
{
    /// <summary>
    /// Interval between health check rounds. Default: 60 seconds.
    /// </summary>
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Timeout for each individual health check. Default: 10 seconds.
    /// </summary>
    public TimeSpan CheckTimeout { get; set; } = TimeSpan.FromSeconds(10);
}
