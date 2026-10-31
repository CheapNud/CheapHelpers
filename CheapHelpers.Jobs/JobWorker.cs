using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CheapHelpers.Jobs;

/// <summary>Hosted service that recovers interrupted jobs at startup, then claims and runs Pending jobs.</summary>
public sealed class JobWorker<TPayload, TContext>(
    JobQueue<TPayload, TContext> queue,
    IServiceScopeFactory scopeFactory,
    IOptions<JobQueueOptions> options,
    TimeProvider timeProvider,
    ILogger<JobWorker<TPayload, TContext>> logger)
    : BackgroundService
    where TContext : DbContext
{
    private readonly JobQueueOptions _options = options.Value;
    private readonly SemaphoreSlim _slots = new(options.Value.MaxConcurrentJobs, options.Value.MaxConcurrentJobs);
    private readonly ConcurrentDictionary<Guid, Task> _jobTasks = new();

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await queue.RecoverAsync(stoppingToken);
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunOnceAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Claiming jobs failed; trying again after the poll interval");
                }

                await queue.WaitForWorkAsync(_options.PollInterval, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host is stopping
        }
    }

    /// <summary>Cancels running jobs and waits up to <see cref="JobQueueOptions.ShutdownTimeout"/>; unfinished rows stay Running for recovery.</summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        try
        {
            await Task.WhenAll(_jobTasks.Values).WaitAsync(_options.ShutdownTimeout, timeProvider, cancellationToken);
        }
        catch (TimeoutException)
        {
            logger.LogWarning("Jobs still running after {Timeout}; they resume on the next start", _options.ShutdownTimeout);
        }
    }

    /// <summary>Claims jobs into every free concurrency slot and starts them; does nothing while the queue is paused.</summary>
    internal async Task RunOnceAsync(CancellationToken hostToken)
    {
        foreach (var finished in _jobTasks.Where(t => t.Value.IsCompleted).ToList())
        {
            _jobTasks.TryRemove(finished);
        }

        while (!queue.IsPaused && _slots.Wait(0))
        {
            (JobRecord Job, CancellationTokenSource Cancellation)? claim;
            try
            {
                claim = await queue.TryClaimNextAsync(hostToken);
            }
            catch
            {
                _slots.Release();
                throw;
            }

            if (claim is not { } claimed)
            {
                _slots.Release();
                return;
            }

            var (job, cancellation) = claimed;

            _jobTasks[job.Id] = Task.Run(() => RunJobAsync(job, cancellation, hostToken), CancellationToken.None);
        }
    }

    /// <summary>Waits for every started job to finish.</summary>
    internal Task WhenIdleAsync() => Task.WhenAll(_jobTasks.Values);

    private async Task RunJobAsync(JobRecord job, CancellationTokenSource jobSource, CancellationToken hostToken)
    {
        try
        {
            // Bookkeeping writes use None: the outcome must be stored even while the host is stopping
            try
            {
                // A cancel that arrived between the claim and this point still wins
                jobSource.Token.ThrowIfCancellationRequested();
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<IJobProcessor<TPayload>>();
                var payload = JsonSerializer.Deserialize<TPayload>(job.PayloadJson)!;
                var context = new JobContext<TPayload>(job.Id, job.Kind, payload, job.RetryCount + 1, _options, timeProvider,
                    queue.WriteProgressAsync, queue.RaiseProgress);
                await processor.ProcessAsync(context, jobSource.Token);
                await queue.FinishAsync(job.Id, JobStatus.Completed, CancellationToken.None);
            }
            catch (OperationCanceledException) when (hostToken.IsCancellationRequested)
            {
                // Left Running on purpose; the next start recovers it
            }
            catch (OperationCanceledException) when (jobSource.IsCancellationRequested)
            {
                await queue.FinishAsync(job.Id, JobStatus.Cancelled, CancellationToken.None);
            }
            catch (Exception ex)
            {
                await queue.FailAttemptAsync(job, ex, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Recording the outcome of job {JobId} failed", job.Id);
        }
        finally
        {
            queue.RunningJobs.TryRemove(job.Id, out _);
            jobSource.Dispose();
            _slots.Release();
            queue.Signal();
        }
    }
}
