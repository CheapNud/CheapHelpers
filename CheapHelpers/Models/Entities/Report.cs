using System.ComponentModel.DataAnnotations;
using CheapHelpers.Models.Contracts;
using CheapHelpers.Models.Enums;

namespace CheapHelpers.Models.Entities;

/// <summary>
/// Generated report with storage path and lifecycle tracking.
/// </summary>
public class Report : IEntityId, IAuditable
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public ReportFormat Format { get; set; }

    public ReportStatus Status { get; set; } = ReportStatus.Queued;

    [MaxLength(500)]
    public string? StoragePath { get; set; }

    public long? FileSizeBytes { get; set; }

    [MaxLength(100)]
    public string? MimeType { get; set; }

    /// <summary>
    /// User who requested the report generation.
    /// </summary>
    [MaxLength(450)]
    public string? GeneratedById { get; set; }

    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the report should be automatically cleaned up. Null means no expiration.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    public DateTime? GeneratedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
