using System.Diagnostics;
using CheapHelpers.EF;
using CheapHelpers.Models.Entities;
using CheapHelpers.Models.Enums;
using CheapHelpers.Services.DataExchange.Excel;
using CheapHelpers.Services.DataExchange.Pdf;
using CheapHelpers.Services.Reporting.Models;
using CheapHelpers.Services.Reporting.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Services.Reporting;

/// <summary>
/// Orchestrates report generation, storage, retrieval, and lifecycle management
/// </summary>
public class ReportService<TUser>(
    CheapContext<TUser> dbContext,
    IPdfExportService pdfExport,
    IXlsxService xlsxService,
    IReportStorageProvider storageProvider,
    ILogger<ReportService<TUser>> logger) : IReportService
    where TUser : IdentityUser
{
    public async Task<ReportResult> GenerateReportAsync<T>(ReportRequest<T> reportRequest, CancellationToken ct = default)
    {
        // Create the report entity in Generating state
        var report = new Report
        {
            Name = reportRequest.Name,
            Description = reportRequest.Description,
            Format = reportRequest.Format,
            Status = ReportStatus.Generating,
            GeneratedById = reportRequest.GeneratedById,
            ExpiresAt = reportRequest.RetentionDays.HasValue
                ? DateTime.UtcNow.AddDays(reportRequest.RetentionDays.Value)
                : null
        };

        dbContext.Set<Report>().Add(report);
        await dbContext.SaveChangesAsync(ct);

        try
        {
            // Generate the report content based on format
            byte[] contentBytes;
            string mimeType;
            string fileExtension;

            switch (reportRequest.Format)
            {
                case ReportFormat.Pdf:
                    if (reportRequest.Template is null)
                    {
                        throw new InvalidOperationException("PdfDocumentTemplate is required for PDF report generation.");
                    }
                    contentBytes = await pdfExport.ExportToPdfAsync(reportRequest.Data, reportRequest.Template);
                    mimeType = "application/pdf";
                    fileExtension = "pdf";
                    break;

                case ReportFormat.Excel:
                    var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xlsx");
                    try
                    {
                        var dynamicRecords = reportRequest.Data.Cast<dynamic>().ToList();
                        await xlsxService.Generate(tempPath, dynamicRecords);
                        contentBytes = await File.ReadAllBytesAsync(tempPath, ct);
                    }
                    finally
                    {
                        if (File.Exists(tempPath))
                        {
                            File.Delete(tempPath);
                        }
                    }
                    mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    fileExtension = "xlsx";
                    break;

                default:
                    throw new NotSupportedException($"Report format '{reportRequest.Format}' is not supported.");
            }

            // Build storage path and store the file
            var storagePath = $"reports/{DateTime.UtcNow:yyyy-MM}/{report.Id}_{reportRequest.Name}.{fileExtension}";
            await storageProvider.StoreAsync(storagePath, contentBytes, mimeType, ct);

            // Update the report entity with completion details
            report.Status = ReportStatus.Completed;
            report.StoragePath = storagePath;
            report.FileSizeBytes = contentBytes.Length;
            report.MimeType = mimeType;
            report.GeneratedAt = DateTime.UtcNow;
            report.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(ct);

            logger.LogInformation("Report {ReportId} '{ReportName}' generated successfully ({FileSize} bytes)", report.Id, report.Name, contentBytes.Length);

            return new ReportResult(report.Id, true, storagePath, contentBytes.Length);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Report {ReportId} '{ReportName}' generation failed", report.Id, report.Name);

            report.Status = ReportStatus.Failed;
            report.ErrorMessage = ex.Message;
            report.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(ct);

            return new ReportResult(report.Id, false, ErrorMessage: ex.Message);
        }
    }

    public async Task<Report?> GetReportAsync(int reportId, CancellationToken ct = default)
    {
        return await dbContext.Set<Report>().FindAsync([reportId], ct);
    }

    public async Task<List<Report>> GetReportsAsync(ReportFilter filter, CancellationToken ct = default)
    {
        var query = dbContext.Set<Report>().AsQueryable();

        if (!string.IsNullOrEmpty(filter.GeneratedById))
        {
            query = query.Where(r => r.GeneratedById == filter.GeneratedById);
        }

        if (filter.Format.HasValue)
        {
            query = query.Where(r => r.Format == filter.Format.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(r => r.Status == filter.Status.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt <= filter.ToDate.Value);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(filter.Skip)
            .Take(filter.Take)
            .ToListAsync(ct);
    }

    public async Task<(byte[] Content, string FileName, string MimeType)?> DownloadReportAsync(int reportId, CancellationToken ct = default)
    {
        var report = await dbContext.Set<Report>().FindAsync([reportId], ct);

        if (report?.StoragePath is null || report.MimeType is null)
        {
            Debug.WriteLine($"ReportService: Report {reportId} not found or has no storage path");
            return null;
        }

        var contentBytes = await storageProvider.GetAsync(report.StoragePath, ct);
        if (contentBytes is null)
        {
            logger.LogWarning("Report {ReportId} storage file missing at {StoragePath}", reportId, report.StoragePath);
            return null;
        }

        var fileName = Path.GetFileName(report.StoragePath);
        return (contentBytes, fileName, report.MimeType);
    }

    public async Task<bool> DeleteReportAsync(int reportId, CancellationToken ct = default)
    {
        var report = await dbContext.Set<Report>().FindAsync([reportId], ct);
        if (report is null)
        {
            return false;
        }

        // Delete from storage first
        if (!string.IsNullOrEmpty(report.StoragePath))
        {
            await storageProvider.DeleteAsync(report.StoragePath, ct);
        }

        // Remove from database
        dbContext.Set<Report>().Remove(report);
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Report {ReportId} '{ReportName}' deleted", reportId, report.Name);
        return true;
    }

    public async Task<int> CleanupExpiredReportsAsync(CancellationToken ct = default)
    {
        var expiredReports = await dbContext.Set<Report>()
            .Where(r => r.Status == ReportStatus.Completed
                        && r.ExpiresAt.HasValue
                        && r.ExpiresAt.Value < DateTime.UtcNow)
            .ToListAsync(ct);

        foreach (var report in expiredReports)
        {
            if (!string.IsNullOrEmpty(report.StoragePath))
            {
                await storageProvider.DeleteAsync(report.StoragePath, ct);
            }

            report.Status = ReportStatus.Expired;
            report.StoragePath = null;
            report.UpdatedAt = DateTime.UtcNow;
        }

        if (expiredReports.Count > 0)
        {
            await dbContext.SaveChangesAsync(ct);
            logger.LogInformation("Cleaned up {Count} expired reports", expiredReports.Count);
        }

        return expiredReports.Count;
    }
}
