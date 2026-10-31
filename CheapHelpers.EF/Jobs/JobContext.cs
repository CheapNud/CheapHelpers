namespace CheapHelpers.EF.Jobs;

/// <summary>The job a processor works on, with a throttled progress reporter.</summary>
public sealed class JobContext<TPayload>
{
    private const double MinPercentForEstimate = 2;
    private const double FullPercent = 100;

    private readonly JobQueueOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly Func<JobProgress, CancellationToken, Task> _writeProgress;
    private readonly Action<JobProgress> _raiseProgress;
    private DateTimeOffset? _firstReportAt;
    private DateTimeOffset _lastWriteAt;
    private DateTimeOffset _lastEventAt;

    internal JobContext(Guid id, string kind, TPayload payload, int attempt, JobQueueOptions options, TimeProvider timeProvider,
        Func<JobProgress, CancellationToken, Task> writeProgress, Action<JobProgress> raiseProgress)
    {
        Id = id;
        Kind = kind;
        Payload = payload;
        Attempt = attempt;
        _options = options;
        _timeProvider = timeProvider;
        _writeProgress = writeProgress;
        _raiseProgress = raiseProgress;
    }

    /// <summary>The job id.</summary>
    public Guid Id { get; }

    /// <summary>The job's label.</summary>
    public string Kind { get; }

    /// <summary>The deserialised payload.</summary>
    public TPayload Payload { get; }

    /// <summary>The attempt number, starting at 1.</summary>
    public int Attempt { get; }

    /// <summary>
    /// Reports progress. The database is written at most once per <see cref="JobQueueOptions.ProgressWriteInterval"/>
    /// and the callback raised at most once per <see cref="JobQueueOptions.ProgressEventInterval"/>; the first report
    /// and any report at 100 percent always go through.
    /// </summary>
    public async ValueTask ReportProgressAsync(double percent, string? message = null, CancellationToken cancellationToken = default)
    {
        percent = Math.Clamp(percent, 0, FullPercent);
        var now = _timeProvider.GetUtcNow();
        var isFirst = _firstReportAt is null;
        _firstReportAt ??= now;

        // Linear estimate from the first report; too noisy to show below a few percent
        var elapsed = now - _firstReportAt.Value;
        TimeSpan? remaining = percent > MinPercentForEstimate ? elapsed / (percent / FullPercent) - elapsed : null;
        var progress = new JobProgress(Id, Kind, percent, message, remaining);
        var mustSend = isFirst || percent >= FullPercent;

        if (mustSend || now - _lastEventAt >= _options.ProgressEventInterval)
        {
            _lastEventAt = now;
            _raiseProgress(progress);
        }

        if (mustSend || now - _lastWriteAt >= _options.ProgressWriteInterval)
        {
            _lastWriteAt = now;
            await _writeProgress(progress, cancellationToken);
        }
    }
}
