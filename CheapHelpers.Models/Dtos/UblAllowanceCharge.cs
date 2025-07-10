namespace CheapHelpers.Models.Ubl;

/// <summary>
/// Allowances (discounts) and charges (fees)
/// </summary>
public record UblAllowanceCharge
{
    public bool IsCharge { get; init; } // true = charge, false = allowance/discount
    public string Reason { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "EUR";
}
