namespace CheapHelpers.Scheduling;

/// <summary>
/// Service for registering and managing named scheduled tasks.
/// Tasks run on interval, daily, or monthly schedules within a hosted service.
/// </summary>
public interface IScheduledTaskService
{
    /// <summary>
    /// Registers a task that runs at a fixed interval.
    /// </summary>
    void RegisterTask(string taskName, TimeSpan interval, Func<CancellationToken, Task> work);

    /// <summary>
    /// Registers a task that runs once daily at the specified time (UTC).
    /// </summary>
    void RegisterDailyTask(string taskName, TimeOnly runAt, Func<CancellationToken, Task> work);

    /// <summary>
    /// Registers a task that runs once monthly on the specified day and time (UTC).
    /// </summary>
    void RegisterMonthlyTask(string taskName, int dayOfMonth, TimeOnly runAt, Func<CancellationToken, Task> work);

    /// <summary>
    /// Unregisters a previously registered task by name.
    /// </summary>
    bool UnregisterTask(string taskName);

    /// <summary>
    /// Gets the names of all currently registered tasks.
    /// </summary>
    IReadOnlyCollection<string> RegisteredTasks { get; }
}
