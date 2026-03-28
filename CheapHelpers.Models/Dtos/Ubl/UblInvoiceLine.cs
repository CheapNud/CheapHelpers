namespace CheapHelpers.Models.Dtos.Ubl;

/// <summary>
/// Invoice line item with tax category support for PEPPOL BIS 3.0.
/// </summary>
public record UblInvoiceLine
{
    public string Id { get; init; } = string.Empty;
    public string? Note { get; init; }
    public UblItem Item { get; init; } = new();
    public decimal Quantity { get; init; }
    public string QuantityUnit { get; init; } = "PCE";
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
    public string? AccountingCostCode { get; init; }
    public UblTaxCategory TaxCategory { get; init; } = new();

    /// <summary>
    /// Billing period this line covers (e.g., monthly subscription period).
    /// </summary>
    public DateTime? InvoicePeriodStart { get; init; }
    public DateTime? InvoicePeriodEnd { get; init; }
}
