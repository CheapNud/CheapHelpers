namespace CheapHelpers.Models.Dtos.Ubl;

/// <summary>
/// Document-level tax total with breakdown by tax category.
/// </summary>
public record UblTaxTotal
{
    public decimal TaxAmount { get; init; }
    public List<UblTaxSubtotal> TaxSubtotals { get; init; } = [];
}
