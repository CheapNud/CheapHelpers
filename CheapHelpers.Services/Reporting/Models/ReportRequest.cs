using CheapHelpers.Models.Enums;
using CheapHelpers.Services.DataExchange.Pdf.Templates;

namespace CheapHelpers.Services.Reporting.Models;

/// <summary>
/// Describes a report to be generated, including format, data, and distribution options
/// </summary>
public record ReportRequest<T>
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public ReportFormat Format { get; init; }
    public IEnumerable<T> Data { get; init; } = [];
    public PdfDocumentTemplate? Template { get; init; }
    public string? GeneratedById { get; init; }
    public int? RetentionDays { get; init; }
    public string[]? DistributeToEmails { get; init; }
    public string? EmailSubject { get; init; }
}
