namespace CheapHelpers.Models.Dtos.Ubl;

/// <summary>
/// Tax category for invoice lines and tax subtotals.
/// Common codes: "S" = standard rate, "Z" = zero rate, "AE" = reverse charge, "E" = exempt.
/// </summary>
public record UblTaxCategory
{
    public string Id { get; init; } = "S";
    public decimal Percent { get; init; } = 21.00m;
    public string TaxSchemeId { get; init; } = "VAT";
    public string? TaxExemptionReason { get; init; }
}
