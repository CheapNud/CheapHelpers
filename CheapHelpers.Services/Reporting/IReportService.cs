using CheapHelpers.Models.Entities;
using CheapHelpers.Services.Reporting.Models;

namespace CheapHelpers.Services.Reporting;

/// <summary>
/// Service for generating, retrieving, downloading, and managing reports
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Generates a report from the provided data and stores it
    /// </summary>
    Task<ReportResult> GenerateReportAsync<T>(ReportRequest<T> reportRequest, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a report entity by its ID
    /// </summary>
    Task<Report?> GetReportAsync(int reportId, CancellationToken ct = default);

    /// <summary>
    /// Queries reports matching the specified filter criteria
    /// </summary>
    Task<List<Report>> GetReportsAsync(ReportFilter filter, CancellationToken ct = default);

    /// <summary>
    /// Downloads report content along with its filename and MIME type
    /// </summary>
    Task<(byte[] Content, string FileName, string MimeType)?> DownloadReportAsync(int reportId, CancellationToken ct = default);

    /// <summary>
    /// Deletes a report from both storage and the database
    /// </summary>
    Task<bool> DeleteReportAsync(int reportId, CancellationToken ct = default);

    /// <summary>
    /// Removes expired reports from storage and marks them as expired. Returns the number of cleaned up reports.
    /// </summary>
    Task<int> CleanupExpiredReportsAsync(CancellationToken ct = default);
}
