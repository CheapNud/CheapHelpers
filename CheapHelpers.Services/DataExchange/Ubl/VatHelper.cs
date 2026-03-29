namespace CheapHelpers.Services.DataExchange.Ubl;

/// <summary>
/// Shared helpers for VAT number handling in UBL invoice/credit note builders.
/// </summary>
internal static class VatHelper
{
    /// <summary>
    /// Strips the 2-letter country prefix from a VAT number to derive the PEPPOL endpoint ID.
    /// Handles any EU country prefix (BE, NL, DE, FR, etc.).
    /// </summary>
    internal static string? DeriveEndpointFromVat(string? vatNumber)
    {
        if (string.IsNullOrWhiteSpace(vatNumber))
            return null;

        var trimmed = vatNumber.Trim();

        if (trimmed.Length > 2 && char.IsLetter(trimmed[0]) && char.IsLetter(trimmed[1]))
            return trimmed[2..];

        return trimmed;
    }
}
