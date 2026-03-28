using CheapHelpers.Scheduling;

namespace CheapHelpers.Services.Reporting.Scheduling;

/// <summary>
/// Bridges report schedules to the underlying scheduled task infrastructure
/// </summary>
public class ScheduledReportService(IScheduledTaskService scheduledTaskService) : IScheduledReportService
{
    public void RegisterRecurringReport(string taskName, ReportSchedule schedule)
    {
        switch (schedule.ScheduleType)
        {
            case ScheduleType.Daily:
                scheduledTaskService.RegisterDailyTask(
                    taskName,
                    schedule.RunAt ?? new TimeOnly(0, 0),
                    schedule.ReportFactory);
                break;

            case ScheduleType.Monthly:
                scheduledTaskService.RegisterMonthlyTask(
                    taskName,
                    schedule.DayOfMonth ?? 1,
                    schedule.RunAt ?? new TimeOnly(0, 0),
                    schedule.ReportFactory);
                break;

            case ScheduleType.Interval:
                scheduledTaskService.RegisterTask(
                    taskName,
                    schedule.Interval ?? TimeSpan.FromHours(24),
                    schedule.ReportFactory);
                break;

            default:
                throw new NotSupportedException($"Schedule type '{schedule.ScheduleType}' is not supported.");
        }
    }

    public bool UnregisterRecurringReport(string taskName)
    {
        return scheduledTaskService.UnregisterTask(taskName);
    }
}
