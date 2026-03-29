namespace CheapHelpers.Services.DataExchange.Ubl;

/// <summary>
/// Shared helpers for VAT number handling in UBL invoice/credit note builders.
/// </summary>
internal static class VatHelper
{
    /// <summary>
    /// Derives a PEPPOL endpoint ID from a VAT number where the mapping is well-defined.
    /// <list type="bullet">
    /// <item><b>Belgium</b> (BE): Enterprise number, 10 digits. Scheme 0208.</item>
    /// <item><b>Netherlands</b> (NL): KvK-style, strip B01/B02 suffix. Scheme 0106.</item>
    /// <item><b>Germany</b> (DE): 9 digits after prefix. Scheme 9930.</item>
    /// <item><b>France</b> (FR): SIREN (last 9 of 11-char local part). Scheme 0009.</item>
    /// <item><b>Italy</b> (IT): Codice Fiscale / Partita IVA, 11 digits. Scheme 0211.</item>
    /// <item><b>Spain</b> (ES): CIF/NIF, 9 chars (letter + 7 digits + check). Scheme 9920.</item>
    /// <item><b>Austria</b> (AT): 9 chars starting with "U". Scheme 9915.</item>
    /// <item><b>Portugal</b> (PT): 9 digits. Scheme 9946.</item>
    /// <item><b>Sweden</b> (SE): 12 digits (org number + "01"). Scheme 0007.</item>
    /// <item><b>Denmark</b> (DK): CVR number, 8 digits. Scheme 0198.</item>
    /// <item><b>Finland</b> (FI): 8 digits. Scheme 0037.</item>
    /// <item><b>Norway</b> (NO): Org number, 9 digits (last 3 = "MVA" stripped). Scheme 0192.</item>
    /// <item><b>Ireland</b> (IE): 8-9 chars (digit/letter mix). Scheme 9928.</item>
    /// <item><b>Luxembourg</b> (LU): 8 digits. Scheme 9938.</item>
    /// <item><b>Poland</b> (PL): NIP, 10 digits. Scheme 9945.</item>
    /// </list>
    /// Returns null for unsupported countries — consumers must provide explicit endpoint IDs
    /// via the <c>From(UblParty)</c> / <c>To(UblParty)</c> overload.
    /// </summary>
    internal static string? DeriveEndpointFromVat(string? vatNumber)
    {
        if (string.IsNullOrWhiteSpace(vatNumber))
            return null;

        var trimmed = vatNumber.Trim();

        if (trimmed.Length < 4 || !char.IsLetter(trimmed[0]) || !char.IsLetter(trimmed[1]))
            return null;

        var countryCode = trimmed[..2].ToUpperInvariant();
        var localPart = trimmed[2..];

        return countryCode switch
        {
            // Belgium: "BE0123456789" → "0123456789" (scheme 0208)
            "BE" when localPart.Length == 10 && AllDigits(localPart) => localPart,

            // Netherlands: "NL123456789B01" → strip B01/B02 → "123456789" (scheme 0106)
            "NL" => StripNlSuffix(localPart),

            // Germany: "DE123456789" → "123456789" (scheme 9930)
            "DE" when localPart.Length == 9 && AllDigits(localPart) => localPart,

            // France: "FRXX345678901" → SIREN = last 9 digits (scheme 0009)
            "FR" when localPart.Length == 11 => localPart[2..],

            // Italy: "IT12345678901" → "12345678901" (scheme 0211)
            "IT" when localPart.Length == 11 && AllDigits(localPart) => localPart,

            // Spain: "ESX1234567X" → "X1234567X" (scheme 9920)
            "ES" when localPart.Length == 9 => localPart,

            // Austria: "ATU12345678" → "U12345678" (scheme 9915)
            "AT" when localPart.Length == 9 && localPart.StartsWith('U') => localPart,

            // Portugal: "PT123456789" → "123456789" (scheme 9946)
            "PT" when localPart.Length == 9 && AllDigits(localPart) => localPart,

            // Sweden: "SE123456789012" → org number "1234567890" (strip "01" suffix, scheme 0007)
            "SE" when localPart.Length == 12 && AllDigits(localPart) => localPart[..10],

            // Denmark: "DK12345678" → "12345678" (scheme 0198)
            "DK" when localPart.Length == 8 && AllDigits(localPart) => localPart,

            // Finland: "FI12345678" → "12345678" (scheme 0037)
            "FI" when localPart.Length == 8 && AllDigits(localPart) => localPart,

            // Norway: "NO123456789MVA" or "NO123456789" → "123456789" (scheme 0192)
            "NO" => StripNoSuffix(localPart),

            // Ireland: "IE1234567X" or "IE1X23456X" → local part as-is (scheme 9928)
            "IE" when localPart.Length is 8 or 9 => localPart,

            // Luxembourg: "LU12345678" → "12345678" (scheme 9938)
            "LU" when localPart.Length == 8 && AllDigits(localPart) => localPart,

            // Poland: "PL1234567890" → "1234567890" (scheme 9945)
            "PL" when localPart.Length == 10 && AllDigits(localPart) => localPart,

            _ => null
        };
    }

    /// <summary>
    /// Dutch VAT numbers end with B01, B02, etc. Strip to get the base number.
    /// "123456789B01" → "123456789"
    /// </summary>
    private static string? StripNlSuffix(string localPart)
    {
        var bIndex = localPart.IndexOf('B');
        if (bIndex < 9)
            return null;

        var digits = localPart[..bIndex];
        return AllDigits(digits) ? digits : null;
    }

    /// <summary>
    /// Norwegian VAT numbers may end with "MVA". Strip to get the org number.
    /// "123456789MVA" → "123456789", "123456789" → "123456789"
    /// </summary>
    private static string? StripNoSuffix(string localPart)
    {
        var digits = localPart.EndsWith("MVA", StringComparison.OrdinalIgnoreCase)
            ? localPart[..^3]
            : localPart;

        return digits.Length == 9 && AllDigits(digits) ? digits : null;
    }

    private static bool AllDigits(string value) => value.All(char.IsDigit);
}
