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
        var recovered = false;
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Recovery must succeed before the first claim, so a database that is down at startup is retried here
                    if (!recovered)
                    {
                        await queue.RecoverAsync(stoppingToken);
                        recovered = true;
                    }

                    await RunOnceAsync(stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(ex, "Recovering or claiming jobs failed; trying again after the poll interval");
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
            Exception? failure = null;
            var outcome = JobStatus.Completed;
            try
            {
                // A cancel that arrived between the claim and this point still wins
                jobSource.Token.ThrowIfCancellationRequested();
                // The scope ends before the outcome is stored, so a dependency that throws on disposal cannot undo Completed
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<IJobProcessor<TPayload>>();
                var payload = JsonSerializer.Deserialize<TPayload>(job.PayloadJson)!;
                var context = new JobContext<TPayload>(job.Id, job.Kind, payload, job.RetryCount + 1, _options, timeProvider,
                    queue.WriteProgressAsync, queue.RaiseProgress);
                await processor.ProcessAsync(context, jobSource.Token);
            }
            catch (OperationCanceledException) when (hostToken.IsCancellationRequested)
            {
                // Left Running on purpose; the next start recovers it
                return;
            }
            catch (OperationCanceledException) when (jobSource.IsCancellationRequested)
            {
                outcome = JobStatus.Cancelled;
            }
            catch (Exception ex)
            {
                failure = ex;
            }

            // Stored outside the handlers above, so a failing write is never mistaken for a failed attempt.
            // None: the outcome must be stored even while the host is stopping
            if (failure is not null)
            {
                await queue.FailAttemptAsync(job, failure, CancellationToken.None);
            }
            else
            {
                await queue.FinishAsync(job.Id, outcome, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            // Transient faults are retried by the execution strategy configured on the app's DbContext; past that the
            // row stays Running and the next start recovers it
            logger.LogError(ex, "Recording the outcome of job {JobId} failed", job.Id);
        }
        finally
        {
            // Only this attempt's entry: a retried job may already be registered again under the same id
            queue.RunningJobs.TryRemove(KeyValuePair.Create(job.Id, jobSource));
            jobSource.Dispose();
            _slots.Release();
            queue.Signal();
        }
    }
}
