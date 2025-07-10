namespace CheapHelpers.Models.Ubl;

/// <summary>
/// Simplified order model for creating UBL documents
/// </summary>
public record UblOrder
{
    public string Id { get; init; } = string.Empty;
    public DateTime IssueDate { get; init; } = DateTime.Now;
    public string Currency { get; init; } = "EUR";
    public string? Note { get; init; }
    public string? AccountingCostCode { get; init; }
    public string? QuotationId { get; init; }
    public DateTime? ValidityEndDate { get; init; }

    public UblParty Buyer { get; init; } = new();
    public UblParty Seller { get; init; } = new();
    public UblDelivery? Delivery { get; init; }
    public UblMonetaryTotals? Totals { get; init; }
    public List<UblOrderLine> Lines { get; init; } = [];
    public List<UblAllowanceCharge> AllowancesCharges { get; init; } = [];
}
