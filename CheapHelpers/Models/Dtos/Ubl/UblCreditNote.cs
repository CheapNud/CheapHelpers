namespace CheapHelpers.Models.Dtos.Ubl;

/// <summary>
/// Simplified credit note model for creating UBL/PEPPOL BIS 3.0 credit note documents.
/// CreditNoteTypeCode 381 = credit note.
/// </summary>
public record UblCreditNote
{
    public string Id { get; init; } = string.Empty;
    public DateTime IssueDate { get; init; } = DateTime.Now;
    public string Currency { get; init; } = "EUR";
    public string? Note { get; init; }
    public string CreditNoteTypeCode { get; init; } = "381";
    public string? CreditReasonCode { get; init; }
    public string? CreditReason { get; init; }

    /// <summary>
    /// Original invoice IDs being credited.
    /// </summary>
    public List<string> BillingReferences { get; init; } = [];

    public UblParty Seller { get; init; } = new();
    public UblParty Buyer { get; init; } = new();
    public UblPaymentMeans? PaymentMeans { get; init; }
    public UblTaxTotal? TaxTotal { get; init; }
    public UblMonetaryTotals Totals { get; init; } = new();
    public List<UblInvoiceLine> Lines { get; init; } = [];
    public List<UblAllowanceCharge> AllowancesCharges { get; init; } = [];
}
