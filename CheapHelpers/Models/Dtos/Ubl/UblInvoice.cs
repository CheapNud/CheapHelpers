namespace CheapHelpers.Models.Dtos.Ubl;

/// <summary>
/// Simplified invoice model for creating UBL/PEPPOL BIS 3.0 invoice documents.
/// InvoiceTypeCode 380 = commercial invoice.
/// </summary>
public record UblInvoice
{
    public string Id { get; init; } = string.Empty;
    public DateTime IssueDate { get; init; } = DateTime.Now;
    public DateTime? DueDate { get; init; }
    public string Currency { get; init; } = "EUR";
    public string? Note { get; init; }
    public string InvoiceTypeCode { get; init; } = "380";
    public string? AccountingCostCode { get; init; }
    public string? BillingReference { get; init; }

    public UblParty Seller { get; init; } = new();
    public UblParty Buyer { get; init; } = new();
    public UblDelivery? Delivery { get; init; }
    public UblPaymentMeans? PaymentMeans { get; init; }
    public UblTaxTotal? TaxTotal { get; init; }
    public UblMonetaryTotals Totals { get; init; } = new();
    public List<UblInvoiceLine> Lines { get; init; } = [];
    public List<UblAllowanceCharge> AllowancesCharges { get; init; } = [];

    /// <summary>
    /// Invoice period (e.g., the billing month this invoice covers).
    /// </summary>
    public DateTime? InvoicePeriodStart { get; init; }
    public DateTime? InvoicePeriodEnd { get; init; }
}
