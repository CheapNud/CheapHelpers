namespace CheapHelpers.Services.Reporting.Scheduling;

/// <summary>
/// Service for registering and managing recurring report generation schedules
/// </summary>
public interface IScheduledReportService
{
    /// <summary>
    /// Registers a recurring report generation task under the given name
    /// </summary>
    void RegisterRecurringReport(string taskName, ReportSchedule schedule);

    /// <summary>
    /// Unregisters a previously registered recurring report task. Returns true if found and removed.
    /// </summary>
    bool UnregisterRecurringReport(string taskName);
}
