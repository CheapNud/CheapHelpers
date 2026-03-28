namespace CheapHelpers.Models.Dtos.Ubl;

/// <summary>
/// Tax subtotal per tax category (e.g., one entry per VAT rate used in the invoice).
/// </summary>
public record UblTaxSubtotal
{
    public decimal TaxableAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public UblTaxCategory TaxCategory { get; init; } = new();
}
