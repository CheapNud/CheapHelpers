using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Services.Polling;

/// <summary>
/// Implementation of <see cref="IHttpPollingService{TResponse}"/> using <see cref="IHttpClientFactory"/>,
/// <see cref="PeriodicTimer"/>, and exponential backoff on failure.
/// </summary>
public class HttpPollingService<TResponse>(
    HttpClient httpClient,
    HttpPollingOptions pollingOptions,
    ILogger<HttpPollingService<TResponse>> logger) : IHttpPollingService<TResponse>, IAsyncDisposable
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly HttpPollingOptions _pollingOptions = pollingOptions;
    private readonly ILogger<HttpPollingService<TResponse>> _logger = logger;

    private CancellationTokenSource? _cts;
    private Task? _pollingTask;
    private int _consecutiveFailures;

    public bool IsRunning => _pollingTask is { IsCompleted: false };
    public Func<TResponse, Task>? OnDataReceived { get; set; }
    public Action<Exception>? OnError { get; set; }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
            return Task.CompletedTask;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _pollingTask = PollLoopAsync(_cts.Token);
        _logger.LogInformation("HTTP polling started for {Endpoint} at {Interval}s interval",
            _pollingOptions.Endpoint, _pollingOptions.Interval.TotalSeconds);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_cts is null)
            return;

        await _cts.CancelAsync();

        if (_pollingTask is not null)
        {
            try
            {
                await _pollingTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        _logger.LogInformation("HTTP polling stopped for {Endpoint}", _pollingOptions.Endpoint);
    }

    private async Task PollLoopAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_pollingOptions.Interval);

        // Poll once immediately, then on timer
        await PollOnceAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await PollOnceAsync(stoppingToken);
        }
    }

    private async Task PollOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            timeoutCts.CancelAfter(_pollingOptions.RequestTimeout);

            var pollResponse = await _httpClient.GetFromJsonAsync<TResponse>(
                (string?)null, timeoutCts.Token);

            _consecutiveFailures = 0;

            if (pollResponse is not null && OnDataReceived is not null)
            {
                await OnDataReceived(pollResponse);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _consecutiveFailures++;
            _logger.LogWarning(ex, "Polling failed for {Endpoint} (attempt {Attempt}/{Max})",
                _pollingOptions.Endpoint, _consecutiveFailures, _pollingOptions.MaxRetries);

            OnError?.Invoke(ex);

            if (_consecutiveFailures >= _pollingOptions.MaxRetries)
            {
                var backoff = _pollingOptions.RetryDelay * Math.Pow(2, _consecutiveFailures - _pollingOptions.MaxRetries);
                var maxBackoff = TimeSpan.FromMinutes(5);
                if (backoff > maxBackoff)
                    backoff = maxBackoff;

                _logger.LogWarning("Backing off for {Backoff}s after {Failures} consecutive failures",
                    backoff.TotalSeconds, _consecutiveFailures);

                await Task.Delay(backoff, stoppingToken);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();

            if (_pollingTask is not null)
            {
                try
                {
                    await _pollingTask.WaitAsync(TimeSpan.FromSeconds(5));
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
                catch (TimeoutException)
                {
                    _logger.LogWarning("Polling task did not complete within 5s during dispose");
                }
            }

            _cts.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}
