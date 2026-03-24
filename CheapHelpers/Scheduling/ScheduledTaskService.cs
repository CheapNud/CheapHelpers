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
                    _ = RunTaskAsync(taskName, entry, stoppingToken);
                }
            }
        }
    }

    private bool ShouldRun(ScheduledTaskEntry entry)
    {
        var now = DateTimeOffset.UtcNow;
        var elapsed = now - entry.LastRun;

        return entry.Type switch
        {
            ScheduleType.Interval => elapsed >= entry.Interval,
            ScheduleType.Daily => elapsed >= TimeSpan.FromMinutes(1)
                && now.Hour == entry.RunAt!.Value.Hour
                && now.Minute == entry.RunAt.Value.Minute
                && (now.Date != entry.LastRun.Date || entry.LastRun == DateTimeOffset.MinValue),
            ScheduleType.Monthly => elapsed >= TimeSpan.FromMinutes(1)
                && now.Day == entry.DayOfMonth
                && now.Hour == entry.RunAt!.Value.Hour
                && now.Minute == entry.RunAt.Value.Minute
                && (now.Month != entry.LastRun.Month || now.Year != entry.LastRun.Year || entry.LastRun == DateTimeOffset.MinValue),
            _ => false
        };
    }

    private async Task RunTaskAsync(string taskName, ScheduledTaskEntry entry, CancellationToken stoppingToken)
    {
        if (_runningTasks.ContainsKey(taskName))
            return; // Already running, skip overlapping execution

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        _runningTasks[taskName] = cts;

        try
        {
            _logger.LogDebug("Executing scheduled task '{TaskName}'", taskName);
            entry.LastRun = DateTimeOffset.UtcNow;
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
        public DateTimeOffset LastRun { get; set; } = DateTimeOffset.MinValue;
    }
}
