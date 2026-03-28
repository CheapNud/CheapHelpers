using CheapHelpers.EF;
using CheapHelpers.Models.Entities;
using CheapHelpers.Models.Enums;
using CheapHelpers.Services.Email;
using CheapHelpers.Services.Reporting.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Services.Reporting.Distribution;

/// <summary>
/// Distributes generated reports to recipients via email with file attachments
/// </summary>
public class ReportDistributionService<TUser>(
    CheapContext<TUser> dbContext,
    IReportStorageProvider storageProvider,
    IEmailService emailService,
    ILogger<ReportDistributionService<TUser>> logger) : IReportDistributionService
    where TUser : IdentityUser
{
    public async Task<bool> DistributeAsync(int reportId, string[] recipients, string? subject = null, string? body = null, CancellationToken ct = default)
    {
        var report = await dbContext.Set<Report>().FindAsync([reportId], ct);
        if (report is null || report.Status != ReportStatus.Completed || report.StoragePath is null)
        {
            logger.LogWarning("Cannot distribute report {ReportId}: not found or not completed", reportId);
            return false;
        }

        var contentBytes = await storageProvider.GetAsync(report.StoragePath, ct);
        if (contentBytes is null)
        {
            logger.LogWarning("Cannot distribute report {ReportId}: storage file missing at {StoragePath}", reportId, report.StoragePath);
            return false;
        }

        // Build attachment from stored report
        var fileExtension = report.Format switch
        {
            ReportFormat.Pdf => ".pdf",
            ReportFormat.Excel => ".xlsx",
            _ => string.Empty
        };
        var fileName = report.Name + fileExtension;

        var emailSubject = subject ?? $"Report: {report.Name}";
        var emailBody = body ?? "Please find your report attached.";

        await emailService.SendEmailAsync(
            recipients,
            emailSubject,
            emailBody,
            [(fileName, contentBytes)]);

        logger.LogInformation("Report {ReportId} '{ReportName}' distributed to {RecipientCount} recipients", reportId, report.Name, recipients.Length);
        return true;
    }
}
