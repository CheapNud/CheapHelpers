# Job queue

Persistent job queue on EF Core. The jobs live in a table inside your own `DbContext`, so they survive a restart. A hosted worker claims Pending jobs, runs them through your processor, retries failures with a backoff, moves jobs that keep failing to a dead letter state, and resumes jobs that a crashed process left behind. Processors report progress that is throttled before it reaches the database or your callbacks, with an estimate of the time left.

## Mapping the table

CheapHelpers.EF does not own a `DbContext`. Map the job table into yours and add one migration:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyJobQueue(); // table "Jobs"; pass a name to change it
}
```

```bash
dotnet ef migrations add AddJobQueue
```

On SQLite, which cannot compare or order `DateTimeOffset` columns, add a `DateTimeOffsetToBinaryConverter` for `DateTimeOffset` properties in `ConfigureConventions`.

## Registration

Register an `IDbContextFactory<TContext>` yourself, then the queue:

```csharp
builder.Services.AddDbContextFactory<MyContext>(o => o.UseSqlServer(connectionString));
builder.Services.AddCheapJobs<MyPayload, MyContext, MyProcessor>(o => o.MaxConcurrentJobs = 2);
```

This registers the options, `TimeProvider.System` when no clock is registered, `JobQueue<MyPayload, MyContext>` as a singleton, the processor as scoped (one scope per job) and `JobWorker<MyPayload, MyContext>` as a hosted service. The payload is stored as JSON with `System.Text.Json`.

## Processor

```csharp
public sealed class MyProcessor(IExporter exporter) : IJobProcessor<MyPayload>
{
    public async Task ProcessAsync(JobContext<MyPayload> job, CancellationToken cancellationToken)
    {
        await exporter.ExportAsync(job.Payload.ReportId, cancellationToken);
        await job.ReportProgressAsync(50, "Exported", cancellationToken);
        await exporter.UploadAsync(job.Payload.ReportId, cancellationToken);
        await job.ReportProgressAsync(100, "Uploaded", cancellationToken);
    }
}
```

Returning completes the job. Throwing fails the attempt. Honour the token: it is cancelled when the job is cancelled or the host stops.

## Queue API

| Member | What it does |
|--------|--------------|
| `EnqueueAsync(payload, kind, ct)` | Adds a Pending job and wakes the worker; returns its id. `kind` is a free label of up to 100 characters. |
| `GetAsync(id, ct)` | Returns one job row, or null. |
| `ListAsync(status, ct)` | Lists jobs oldest first, optionally filtered by status. |
| `CancelAsync(id, ct)` | Cancels a Pending job, or cancels the token of a job running in this process; the worker then records Cancelled. |
| `RetryAsync(id, ct)` | Puts a DeadLetter or Cancelled job back to Pending with its retry count reset. Also accepts a Running job that this process is not running, to rescue rows whose owner is gone; the job runs twice if that owner is still alive. |
| `ClearFinishedAsync(ct)` | Deletes Completed, Cancelled and DeadLetter jobs and returns the count. |
| `GetStatisticsAsync(ct)` | Counts jobs per status. |
| `Pause()` / `Resume()` / `IsPaused` | Stops and restarts claiming new jobs; running jobs carry on. |
| `ProgressChanged` | Event of `Action<JobProgress>` raised for throttled progress reports. |
| `StatusChanged` | Event of `Action<JobRecord>` raised with the fresh row after every status change. |
| `PausedChanged` | Event of `Action<bool>` raised on pause and resume. |

Subscribers are called one by one, so a subscriber that throws is logged and the others still run.

## Lifecycle

- **Claim**: the worker picks the oldest Pending job whose `AvailableAt` has passed and sets it Running with one conditional update. When another worker got there first, zero rows change and it looks for the next job. It wakes on every enqueue and otherwise checks every `PollInterval`.
- **Retry with backoff**: a throwing processor increases `RetryCount`, stores the message in `LastError` (up to 2000 characters) and puts the job back to Pending with `AvailableAt` set `2^RetryCount` seconds ahead.
- **Dead letter**: a job that fails again after `MaxRetries` retries goes to DeadLetter. Only `RetryAsync` brings it back.
- **Recovery after restart**: at startup the worker requeues Running jobs claimed on this machine by a process that no longer exists. Each counts as a used retry, so a job that keeps crashing the process ends in DeadLetter. Jobs claimed on other machines are left alone, because their owner may still be running them. When a host name never comes back (a container recreated under a new name), rescue its rows with `RetryAsync`.
- **Graceful stop**: on shutdown the worker cancels running jobs and waits up to `ShutdownTimeout`. Jobs that do not finish stay Running and are recovered on the next start.
- **Progress**: `ReportProgressAsync` writes to the database at most once per `ProgressWriteInterval` and raises `ProgressChanged` at most once per `ProgressEventInterval`. The first report and any report at 100 percent always go through. Above 2 percent the estimate is `elapsed / (percent / 100) - elapsed`, measured from the first report.

## Options

| Option | Default | Meaning |
|--------|---------|---------|
| `MaxConcurrentJobs` | 1 | Jobs one worker runs at the same time |
| `PollInterval` | 5 s | Longest wait between two checks of the table |
| `ProgressWriteInterval` | 1 s | Minimum time between two progress writes |
| `ProgressEventInterval` | 100 ms | Minimum time between two progress callbacks |
| `ShutdownTimeout` | 5 s | How long a stopping worker waits for running jobs |
| `MaxRetries` | 3 | Retries after the first attempt that new jobs get |
| `StartPaused` | false | Start without claiming until `Resume()` |
| `TableName` | "Jobs" | Documentation only; pass the name to `ApplyJobQueue` |

## What it does not do

It does not pause or suspend a job that is already running; pausing only stops new claims. There are no priorities, no jobs scheduled for a set time or on a cron, and one queue handles one payload type. An app runs one queue: the job table has no payload type column and the options are shared, so `AddCheapJobs` throws when it is called a second time. `Kind` is a label for your own use and does not route jobs.
