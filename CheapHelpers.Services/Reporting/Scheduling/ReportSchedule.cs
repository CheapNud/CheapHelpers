namespace CheapHelpers.Services.Reporting.Scheduling;

/// <summary>
/// Defines when and how a recurring report should be generated
/// </summary>
public record ReportSchedule
{
    /// <summary>
    /// Factory delegate that produces the report when the schedule triggers
    /// </summary>
    public Func<CancellationToken, Task> ReportFactory { get; init; } = _ => Task.CompletedTask;

    /// <summary>
    /// Type of schedule (daily, monthly, or interval-based)
    /// </summary>
    public ScheduleType ScheduleType { get; init; }

    /// <summary>
    /// Time of day to run (for Daily and Monthly schedules, UTC)
    /// </summary>
    public TimeOnly? RunAt { get; init; }

    /// <summary>
    /// Day of the month to run (for Monthly schedules)
    /// </summary>
    public int? DayOfMonth { get; init; }

    /// <summary>
    /// Interval between runs (for Interval schedules)
    /// </summary>
    public TimeSpan? Interval { get; init; }
}

/// <summary>
/// Determines how frequently a scheduled report runs
/// </summary>
public enum ScheduleType
{
    /// <summary>
    /// Runs once per day at the specified time
    /// </summary>
    Daily,

    /// <summary>
    /// Runs once per month on the specified day and time
    /// </summary>
    Monthly,

    /// <summary>
    /// Runs at a fixed interval
    /// </summary>
    Interval
}
