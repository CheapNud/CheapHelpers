using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CheapHelpers.EF.Jobs;

/// <summary>The job table as a queue: enqueue, inspect, cancel and retry jobs. Register as a singleton.</summary>
public sealed class JobQueue<TPayload, TContext>(
    IDbContextFactory<TContext> contextFactory,
    IOptions<JobQueueOptions> options,
    TimeProvider timeProvider,
    ILogger<JobQueue<TPayload, TContext>> logger)
    where TContext : DbContext
{
    private const int BackoffBaseSeconds = 2;

    // 2^16 seconds is about 18 hours; a higher exponent soon overflows TimeSpan
    private const int MaxBackoffExponent = 16;

    // Capacity 1: any number of signals while the worker is busy collapse into one wake-up
    private readonly Channel<bool> _signal = Channel.CreateBounded<bool>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });
    private readonly JobQueueOptions _options = options.Value;
    private volatile bool _paused = options.Value.StartPaused;

    /// <summary>Raised for progress reports that pass the event throttle.</summary>
    public event Action<JobProgress>? ProgressChanged;

    /// <summary>Raised with the fresh row after every status change.</summary>
    public event Action<JobRecord>? StatusChanged;

    /// <summary>Raised with the new value when the queue is paused or resumed.</summary>
    public event Action<bool>? PausedChanged;

    /// <summary>True while the worker claims no new jobs; running jobs carry on.</summary>
    public bool IsPaused => _paused;

    /// <summary>Cancellation sources of the jobs running in this process.</summary>
    internal ConcurrentDictionary<Guid, CancellationTokenSource> RunningJobs { get; } = new();

    /// <summary>Adds a Pending job and wakes the worker.</summary>
    public async Task<Guid> EnqueueAsync(TPayload payload, string kind = "", CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow();
        var job = new JobRecord
        {
            Id = Guid.NewGuid(),
            Kind = kind,
            PayloadJson = JsonSerializer.Serialize(payload),
            Status = JobStatus.Pending,
            MaxRetries = _options.MaxRetries,
            CreatedAt = now,
            AvailableAt = now,
            LastUpdatedAt = now,
        };

        await using (var db = await contextFactory.CreateDbContextAsync(ct))
        {
            db.Set<JobRecord>().Add(job);
            await db.SaveChangesAsync(ct);
        }

        Raise(StatusChanged, job);
        Signal();
        return job.Id;
    }

    /// <summary>Gets one job, or null when it does not exist.</summary>
    public async Task<JobRecord?> GetAsync(Guid id, CancellationToken ct)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        return await db.Set<JobRecord>().AsNoTracking().FirstOrDefaultAsync(j => j.Id == id, ct);
    }

    /// <summary>Lists jobs oldest first, optionally only those with <paramref name="status"/>.</summary>
    public async Task<IReadOnlyList<JobRecord>> ListAsync(JobStatus? status, CancellationToken ct)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var jobs = db.Set<JobRecord>().AsNoTracking();
        if (status is { } wanted)
        {
            jobs = jobs.Where(j => j.Status == wanted);
        }

        return await jobs.OrderBy(j => j.CreatedAt).ToListAsync(ct);
    }

    /// <summary>Cancels a Pending job, or signals a job running in this process to stop. False when neither applies.</summary>
    public async Task<bool> CancelAsync(Guid id, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();
        int changed;
        await using (var db = await contextFactory.CreateDbContextAsync(ct))
        {
            changed = await db.Set<JobRecord>()
                .Where(j => j.Id == id && j.Status == JobStatus.Pending)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(j => j.Status, JobStatus.Cancelled)
                    .SetProperty(j => j.CompletedAt, now)
                    .SetProperty(j => j.LastUpdatedAt, now), ct);
        }

        if (changed > 0)
        {
            await RaiseStatusAsync(id, ct);
            return true;
        }

        if (!RunningJobs.TryGetValue(id, out var source))
        {
            return false;
        }

        try
        {
            // The worker sees its token cancelled and records Cancelled
            await source.CancelAsync();
            return true;
        }
        catch (ObjectDisposedException)
        {
            // The job finished between the lookup and the cancel
            return false;
        }
    }

    /// <summary>
    /// Puts a DeadLetter or Cancelled job back to Pending with a fresh retry budget. A Running job that this process is
    /// not running is accepted too, to rescue rows whose owner is gone for good (a container that came back under another
    /// host name); only do that when the owner is known to be dead, or the job runs twice. False for any other state.
    /// </summary>
    public async Task<bool> RetryAsync(Guid id, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();
        var rescueRunning = !RunningJobs.ContainsKey(id);
        int changed;
        await using (var db = await contextFactory.CreateDbContextAsync(ct))
        {
            changed = await db.Set<JobRecord>()
                .Where(j => j.Id == id
                    && (j.Status == JobStatus.DeadLetter || j.Status == JobStatus.Cancelled || (rescueRunning && j.Status == JobStatus.Running)))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(j => j.Status, JobStatus.Pending)
                    .SetProperty(j => j.RetryCount, 0)
                    .SetProperty(j => j.AvailableAt, now)
                    .SetProperty(j => j.CompletedAt, (DateTimeOffset?)null)
                    .SetProperty(j => j.LastUpdatedAt, now), ct);
        }

        if (changed == 0)
        {
            return false;
        }

        await RaiseStatusAsync(id, ct);
        Signal();
        return true;
    }

    /// <summary>Deletes Completed, Cancelled and DeadLetter jobs and returns how many were removed.</summary>
    public async Task<int> ClearFinishedAsync(CancellationToken ct)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        return await db.Set<JobRecord>()
            .Where(j => j.Status == JobStatus.Completed || j.Status == JobStatus.Cancelled || j.Status == JobStatus.DeadLetter)
            .ExecuteDeleteAsync(ct);
    }

    /// <summary>Counts jobs per status.</summary>
    public async Task<JobStatistics> GetStatisticsAsync(CancellationToken ct)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var counts = await db.Set<JobRecord>()
            .GroupBy(j => j.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count, ct);

        return new JobStatistics(
            counts.GetValueOrDefault(JobStatus.Pending),
            counts.GetValueOrDefault(JobStatus.Running),
            counts.GetValueOrDefault(JobStatus.Completed),
            counts.GetValueOrDefault(JobStatus.Cancelled),
            counts.GetValueOrDefault(JobStatus.DeadLetter));
    }

    /// <summary>Stops claiming new jobs; running jobs carry on.</summary>
    public void Pause()
    {
        _paused = true;
        Raise(PausedChanged, true);
    }

    /// <summary>Starts claiming jobs again.</summary>
    public void Resume()
    {
        _paused = false;
        Raise(PausedChanged, false);
        Signal();
    }

    /// <summary>Wakes the worker.</summary>
    internal void Signal() => _signal.Writer.TryWrite(true);

    /// <summary>Waits for a signal or <paramref name="timeout"/>, whichever comes first. Throws only when <paramref name="ct"/> is cancelled.</summary>
    internal async Task WaitForWorkAsync(TimeSpan timeout, CancellationToken ct)
    {
        using var timeoutSource = new CancellationTokenSource(timeout, timeProvider);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutSource.Token);
        try
        {
            await _signal.Reader.WaitToReadAsync(linked.Token);
            _signal.Reader.TryRead(out _);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // Poll interval elapsed without a signal
        }
    }

    /// <summary>
    /// Claims the oldest Pending job whose <see cref="JobRecord.AvailableAt"/> has passed, or returns null. The returned
    /// cancellation source is linked to <paramref name="hostToken"/> and already registered, so <see cref="CancelAsync"/>
    /// can reach the job from the moment its row reads Running.
    /// </summary>
    internal async Task<(JobRecord Job, CancellationTokenSource Cancellation)?> TryClaimNextAsync(CancellationToken hostToken)
    {
        while (true)
        {
            var now = timeProvider.GetUtcNow();
            await using var db = await contextFactory.CreateDbContextAsync(hostToken);
            var job = await db.Set<JobRecord>()
                .AsNoTracking()
                .Where(j => j.Status == JobStatus.Pending && j.AvailableAt <= now)
                .OrderBy(j => j.CreatedAt)
                .FirstOrDefaultAsync(hostToken);

            if (job is null)
            {
                return null;
            }

            var id = job.Id;
            var cancellation = CancellationTokenSource.CreateLinkedTokenSource(hostToken);
            RunningJobs[id] = cancellation;
            try
            {
                // The guard makes the claim atomic and keeps the snapshot current: zero rows means another worker took
                // or changed the row, so look again
                var claimed = await db.Set<JobRecord>()
                    .Where(j => j.Id == id && j.Status == JobStatus.Pending && j.RetryCount == job.RetryCount && j.AvailableAt <= now)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(j => j.Status, JobStatus.Running)
                        .SetProperty(j => j.StartedAt, now)
                        .SetProperty(j => j.MachineName, Environment.MachineName)
                        .SetProperty(j => j.ProcessId, Environment.ProcessId)
                        .SetProperty(j => j.LastUpdatedAt, now), hostToken);

                if (claimed > 0)
                {
                    // Applied in memory rather than read back, so nothing can fail between the claim and the hand-over
                    job.Status = JobStatus.Running;
                    job.StartedAt = now;
                    job.MachineName = Environment.MachineName;
                    job.ProcessId = Environment.ProcessId;
                    job.LastUpdatedAt = now;
                    Raise(StatusChanged, job);
                    return (job, cancellation);
                }
            }
            catch
            {
                RunningJobs.TryRemove(KeyValuePair.Create(id, cancellation));
                cancellation.Dispose();
                throw;
            }

            RunningJobs.TryRemove(KeyValuePair.Create(id, cancellation));
            cancellation.Dispose();
        }
    }

    /// <summary>Marks a job Completed (at 100 percent) or Cancelled.</summary>
    internal async Task FinishAsync(Guid id, JobStatus status, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();
        await using (var db = await contextFactory.CreateDbContextAsync(ct))
        {
            await db.Set<JobRecord>()
                .Where(j => j.Id == id && j.Status == JobStatus.Running)
                .ExecuteUpdateAsync(s =>
                {
                    s.SetProperty(j => j.Status, status)
                        .SetProperty(j => j.CompletedAt, now)
                        .SetProperty(j => j.LastUpdatedAt, now);
                    if (status == JobStatus.Completed)
                    {
                        s.SetProperty(j => j.ProgressPercent, 100)
                            .SetProperty(j => j.EstimatedRemaining, TimeSpan.Zero);
                    }
                }, ct);
        }

        await RaiseStatusAsync(id, ct);
    }

    /// <summary>Records a failed attempt: back to Pending with a backoff of 2^RetryCount seconds, or DeadLetter once the retries are used up.</summary>
    internal async Task FailAttemptAsync(JobRecord job, Exception exception, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();
        var retryCount = job.RetryCount + 1;
        var deadLetter = job.RetryCount >= job.MaxRetries;
        var error = exception.Message.Length > JobRecord.MaxErrorLength ? exception.Message[..JobRecord.MaxErrorLength] : exception.Message;

        await using (var db = await contextFactory.CreateDbContextAsync(ct))
        {
            await db.Set<JobRecord>()
                .Where(j => j.Id == job.Id && j.Status == JobStatus.Running)
                .ExecuteUpdateAsync(s =>
                {
                    s.SetProperty(j => j.RetryCount, retryCount)
                        .SetProperty(j => j.LastError, error)
                        .SetProperty(j => j.LastUpdatedAt, now);
                    if (deadLetter)
                    {
                        s.SetProperty(j => j.Status, JobStatus.DeadLetter)
                            .SetProperty(j => j.CompletedAt, now);
                    }
                    else
                    {
                        s.SetProperty(j => j.Status, JobStatus.Pending)
                            .SetProperty(j => j.AvailableAt, now + TimeSpan.FromSeconds(Math.Pow(BackoffBaseSeconds, Math.Min(retryCount, MaxBackoffExponent))));
                    }
                }, ct);
        }

        logger.LogWarning(exception, "Job {JobId} failed attempt {Attempt}{Outcome}", job.Id, retryCount, deadLetter ? ", moved to dead letter" : "");
        await RaiseStatusAsync(job.Id, ct);
    }

    /// <summary>Stores a progress report on the job row.</summary>
    internal async Task WriteProgressAsync(JobProgress progress, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        await db.Set<JobRecord>()
            .Where(j => j.Id == progress.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(j => j.ProgressPercent, progress.Percent)
                .SetProperty(j => j.ProgressMessage, progress.Message)
                .SetProperty(j => j.EstimatedRemaining, progress.EstimatedRemaining)
                .SetProperty(j => j.LastUpdatedAt, now), ct);
    }

    /// <summary>
    /// Requeues jobs left Running by a process on this machine that no longer exists: back to Pending with one more
    /// retry used, or DeadLetter when no retries are left. Jobs claimed on other machines are left alone.
    /// </summary>
    internal async Task RecoverAsync(CancellationToken ct)
    {
        var machine = Environment.MachineName;
        var self = Environment.ProcessId;
        List<JobRecord> orphans;
        await using (var db = await contextFactory.CreateDbContextAsync(ct))
        {
            var candidates = await db.Set<JobRecord>()
                .AsNoTracking()
                .Where(j => j.Status == JobStatus.Running && j.MachineName == machine)
                .ToListAsync(ct);
            // Recovery runs before this worker claims anything, so a row carrying this process id belongs to an earlier
            // process that got the same id; containers often run the app as process 1 on every start. A dead worker's id
            // taken over by an unrelated live process keeps its rows Running until that process exits, because the table
            // stores no process start time to tell the two apart
            orphans = candidates.Where(j => j.ProcessId is not { } pid || pid == self || !IsProcessAlive(pid)).ToList();
        }

        var now = timeProvider.GetUtcNow();
        foreach (var job in orphans)
        {
            // Same rule as a failed attempt, so an interrupted job gets the same number of tries as a throwing one
            var deadLetter = job.RetryCount >= job.MaxRetries;
            int changed;
            await using (var db = await contextFactory.CreateDbContextAsync(ct))
            {
                // Guarded on the snapshot so a job another worker already requeued and claimed again is not touched
                changed = await db.Set<JobRecord>()
                    .Where(j => j.Id == job.Id && j.Status == JobStatus.Running && j.ProcessId == job.ProcessId && j.RetryCount == job.RetryCount)
                    .ExecuteUpdateAsync(s =>
                    {
                        s.SetProperty(j => j.RetryCount, job.RetryCount + 1)
                            .SetProperty(j => j.Status, deadLetter ? JobStatus.DeadLetter : JobStatus.Pending)
                            .SetProperty(j => j.LastUpdatedAt, now);
                        if (deadLetter)
                        {
                            s.SetProperty(j => j.CompletedAt, now);
                        }
                    }, ct);
            }

            if (changed == 0)
            {
                continue;
            }

            logger.LogInformation("Recovered job {JobId} left running by process {ProcessId}", job.Id, job.ProcessId);
            await RaiseStatusAsync(job.Id, ct);
        }
    }

    /// <summary>Raises <see cref="ProgressChanged"/>.</summary>
    internal void RaiseProgress(JobProgress progress) => Raise(ProgressChanged, progress);

    private async Task RaiseStatusAsync(Guid id, CancellationToken ct)
    {
        if (StatusChanged is null)
        {
            return;
        }

        if (await GetAsync(id, ct) is { } job)
        {
            Raise(StatusChanged, job);
        }
    }

    // Each subscriber runs on its own so one that throws cannot starve the rest
    private void Raise<T>(Action<T>? callback, T argument)
    {
        if (callback is null)
        {
            return;
        }

        foreach (var subscriber in callback.GetInvocationList().Cast<Action<T>>())
        {
            try
            {
                subscriber(argument);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "A job queue subscriber threw");
            }
        }
    }

    private static bool IsProcessAlive(int processId)
    {
        try
        {
            // Fully qualified: inside CheapHelpers.EF.Jobs a bare Process resolves to the CheapHelpers.Process namespace
            using var process = System.Diagnostics.Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
