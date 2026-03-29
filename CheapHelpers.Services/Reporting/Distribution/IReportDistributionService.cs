namespace CheapHelpers.Services.Reporting.Distribution;

/// <summary>
/// Service for distributing generated reports to recipients via email
/// </summary>
public interface IReportDistributionService
{
    /// <summary>
    /// Sends a completed report to the specified email recipients as an attachment
    /// </summary>
    Task<bool> DistributeAsync(int reportId, string[] recipients, string? subject = null, string? body = null, CancellationToken ct = default);
}
