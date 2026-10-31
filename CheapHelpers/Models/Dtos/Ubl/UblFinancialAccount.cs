namespace CheapHelpers.Models.Dtos.Ubl;

/// <summary>
/// Financial account for payment (IBAN + BIC).
/// </summary>
public record UblFinancialAccount
{
    /// <summary>
    /// Account ID (typically IBAN).
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Account holder name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// BIC/SWIFT code of the financial institution.
    /// </summary>
    public string? FinancialInstitutionBranch { get; init; }
}
