namespace CheapHelpers.Models.Dtos.Ubl;

/// <summary>
/// Order line item
/// </summary>
public record UblOrderLine
{
    public string Id { get; init; } = string.Empty;
    public string? Note { get; init; }
    public UblItem Item { get; init; } = new();
    public decimal Quantity { get; init; }
    public string QuantityUnit { get; init; } = "PCE";
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
    public decimal? TaxAmount { get; init; }
    public string? AccountingCostCode { get; init; }
    public bool AllowPartialDelivery { get; init; } = true;
    public UblDelivery? SpecificDelivery { get; init; }
}
