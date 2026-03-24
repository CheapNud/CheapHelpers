using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Scheduling;

/// <summary>
/// Hosted service that manages multiple named scheduled tasks using <see cref="PeriodicTimer"/>.
/// Supports interval, daily, and monthly schedules.
/// </summary>
public class ScheduledTaskService(ILogger<ScheduledTaskService> logger) : BackgroundService, IScheduledTaskService
{
    private readonly ILogger<ScheduledTaskService> _logger = logger;
    private readonly ConcurrentDictionary<string, ScheduledTaskEntry> _tasks = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _runningTasks = new();
    private readonly ConcurrentDictionary<string, Task> _activeTasks = new();

    public IReadOnlyCollection<string> RegisteredTasks => _tasks.Keys.ToArray();

    public void RegisterTask(string taskName, TimeSpan interval, Func<CancellationToken, Task> work)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskName);
        ArgumentNullException.ThrowIfNull(work);

        _tasks[taskName] = new ScheduledTaskEntry(ScheduleType.Interval, work, interval);
        _logger.LogInformation("Registered interval task '{TaskName}' every {Interval}", taskName, interval);
    }

    public void RegisterDailyTask(string taskName, TimeOnly runAt, Func<CancellationToken, Task> work)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskName);
        ArgumentNullException.ThrowIfNull(work);

        _tasks[taskName] = new ScheduledTaskEntry(ScheduleType.Daily, work, RunAt: runAt);
        _logger.LogInformation("Registered daily task '{TaskName}' at {RunAt} UTC", taskName, runAt);
    }

    public void RegisterMonthlyTask(string taskName, int dayOfMonth, TimeOnly runAt, Func<CancellationToken, Task> work)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskName);
        ArgumentNullException.ThrowIfNull(work);
        ArgumentOutOfRangeException.ThrowIfLessThan(dayOfMonth, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(dayOfMonth, 28);

        _tasks[taskName] = new ScheduledTaskEntry(ScheduleType.Monthly, work, RunAt: runAt, DayOfMonth: dayOfMonth);
        _logger.LogInformation("Registered monthly task '{TaskName}' on day {Day} at {RunAt} UTC", taskName, dayOfMonth, runAt);
    }

    public bool UnregisterTask(string taskName)
    {
        if (_runningTasks.TryRemove(taskName, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }

        _tasks.TryRemove(taskName, out _);

        // Note: if the task is still running in _activeTasks, it will complete
        // on its own via CTS cancellation. Use UnregisterTaskAsync for awaitable completion.
        return true;
    }

    /// <summary>
    /// Unregisters a task and awaits its completion if it's currently running.
    /// </summary>
    public async Task<bool> UnregisterTaskAsync(string taskName)
    {
        if (_runningTasks.TryRemove(taskName, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }

        if (_activeTasks.TryRemove(taskName, out var activeTask))
        {
            try { await activeTask; }
            catch (OperationCanceledException) { }
        }

        return _tasks.TryRemove(taskName, out _);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scheduled task service starting");

        // Check every 30 seconds for tasks that need to run
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var (taskName, entry) in _tasks)
            {
                if (ShouldRun(entry))
                {
                    var task = RunTaskAsync(taskName, entry, stoppingToken);
                    _activeTasks[taskName] = task;
                    _ = task.ContinueWith(
                        (_, state) =>
                        {
                            var (dict, name) = ((ConcurrentDictionary<string, Task>, string))state!;
                            dict.TryRemove(name, out _);
                        },
                        (_activeTasks, taskName),
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                }
            }
        }

        // Await all running tasks on shutdown
        await Task.WhenAll(_activeTasks.Values);
    }

    private bool ShouldRun(ScheduledTaskEntry entry)
    {
        var now = DateTimeOffset.UtcNow;
        var elapsed = now - entry.LastRun;

        return entry.Type switch
        {
            ScheduleType.Interval => elapsed >= entry.Interval,
            ScheduleType.Daily => HasNotRunToday(entry, now)
                && IsWithinScheduleWindow(now, entry.RunAt!.Value),
            ScheduleType.Monthly => HasNotRunThisMonth(entry, now)
                && now.Day == entry.DayOfMonth
                && IsWithinScheduleWindow(now, entry.RunAt!.Value),
            _ => false
        };
    }

    private async Task RunTaskAsync(string taskName, ScheduledTaskEntry entry, CancellationToken stoppingToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        if (!_runningTasks.TryAdd(taskName, cts))
            return; // Already running, skip overlapping execution

        try
        {
            _logger.LogDebug("Executing scheduled task '{TaskName}'", taskName);
            entry.LastRun = DateTimeOffset.UtcNow; // Stamp before execution to prevent double-fire from polling
            await entry.Work(cts.Token);
            _logger.LogDebug("Scheduled task '{TaskName}' completed", taskName);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scheduled task '{TaskName}' failed", taskName);
        }
        finally
        {
            _runningTasks.TryRemove(taskName, out _);
        }
    }

    /// <summary>
    /// Checks if the current time is within a 2-minute window of the scheduled time,
    /// preventing missed executions due to timer drift. Handles midnight wrap correctly.
    /// </summary>
    private static bool IsWithinScheduleWindow(DateTimeOffset now, TimeOnly scheduledTime)
    {
        var nowMinutes = now.Hour * 60 + now.Minute;
        var schedMinutes = scheduledTime.Hour * 60 + scheduledTime.Minute;
        var diff = nowMinutes - schedMinutes;

        // Handle midnight wrap: e.g. scheduled 23:59, now 00:00 → diff = -1439 → +1440 = 1
        if (diff < 0)
            diff += 1440;

        return diff >= 0 && diff < 2;
    }

    private static bool HasNotRunToday(ScheduledTaskEntry entry, DateTimeOffset now) =>
        now.Date != entry.LastRun.Date || entry.LastRun == DateTimeOffset.MinValue;

    private static bool HasNotRunThisMonth(ScheduledTaskEntry entry, DateTimeOffset now) =>
        now.Month != entry.LastRun.Month || now.Year != entry.LastRun.Year || entry.LastRun == DateTimeOffset.MinValue;

    private enum ScheduleType { Interval, Daily, Monthly }

    private sealed class ScheduledTaskEntry(
        ScheduledTaskService.ScheduleType Type,
        Func<CancellationToken, Task> Work,
        TimeSpan? Interval = null,
        TimeOnly? RunAt = null,
        int? DayOfMonth = null)
    {
        public ScheduleType Type { get; } = Type;
        public Func<CancellationToken, Task> Work { get; } = Work;
        public TimeSpan? Interval { get; } = Interval;
        public TimeOnly? RunAt { get; } = RunAt;
        public int? DayOfMonth { get; } = DayOfMonth;
        private long _lastRunTicks = DateTimeOffset.MinValue.Ticks;

        public DateTimeOffset LastRun
        {
            get => new(Interlocked.Read(ref _lastRunTicks), TimeSpan.Zero);
            set => Interlocked.Exchange(ref _lastRunTicks, value.Ticks);
        }
    }
}
