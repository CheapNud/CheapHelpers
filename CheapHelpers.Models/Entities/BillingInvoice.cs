using System.ComponentModel.DataAnnotations;
using CheapHelpers.Models.Contracts;
using CheapHelpers.Models.Enums;

namespace CheapHelpers.Models.Entities;

/// <summary>
/// Generated billing invoice tied to API key usage for a billing period.
/// Contains calculated charges and optional UBL XML / PDF storage references.
/// </summary>
public class BillingInvoice : IEntityId, IAuditable
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public int ApiKeyId { get; set; }

    public int BillingPlanId { get; set; }

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public long TotalUnits { get; set; }

    public long IncludedUnits { get; set; }

    public long OverageUnits { get; set; }

    public decimal SubTotal { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "EUR";

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    /// <summary>
    /// Generated PEPPOL BIS 3.0 UBL Invoice XML.
    /// </summary>
    public string? UblXml { get; set; }

    /// <summary>
    /// Storage path to the rendered PDF invoice.
    /// </summary>
    [MaxLength(500)]
    public string? PdfStoragePath { get; set; }

    public DateTime? IssuedAt { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
