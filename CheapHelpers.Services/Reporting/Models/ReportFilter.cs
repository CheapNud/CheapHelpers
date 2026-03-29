using CheapHelpers.Models.Enums;

namespace CheapHelpers.Services.Reporting.Models;

/// <summary>
/// Filter criteria for querying stored reports
/// </summary>
public record ReportFilter
{
    public string? GeneratedById { get; init; }
    public ReportFormat? Format { get; init; }
    public ReportStatus? Status { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int Take { get; init; } = 50;
    public int Skip { get; init; }
}
