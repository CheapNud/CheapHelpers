namespace CheapHelpers.Models.Ubl;

/// <summary>
/// Monetary totals for the order
/// </summary>
public record UblMonetaryTotals
{
    public decimal LineExtensionAmount { get; init; }
    public decimal? AllowanceTotalAmount { get; init; }
    public decimal? ChargeTotalAmount { get; init; }
    public decimal? TaxAmount { get; init; }
    public decimal PayableAmount { get; init; }
}
