namespace CheapHelpers.Services.Reporting.Models;

/// <summary>
/// Outcome of a report generation operation
/// </summary>
public sealed record ReportResult(
    int ReportId,
    bool IsSuccess,
    string? StoragePath = null,
    long? FileSizeBytes = null,
    string? ErrorMessage = null);
