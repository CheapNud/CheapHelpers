namespace CheapHelpers.Services.DataExchange.Ubl;

/// <summary>
/// Constants for PEPPOL BIS 3.0 Billing specification
/// </summary>
public static class PeppolConstants
{
    public const string InvoiceCustomizationId = "urn:cen.eu:en16931:2017#compliant#urn:fdc:peppol.eu:2017:poacc:billing:3.0";
    public const string CreditNoteCustomizationId = "urn:cen.eu:en16931:2017#compliant#urn:fdc:peppol.eu:2017:poacc:billing:3.0";
    public const string ProfileId = "urn:fdc:peppol.eu:2017:poacc:billing:01:1.0";
    public const string UblVersionId = "2.1";

    // Belgian endpoint schemes
    public const string BelgianEnterpriseScheme = "0208";
    public const string EanGlnScheme = "0088";
    public const string BelgianVatScheme = "9925";

    // Document type codes
    public const string InvoiceTypeCode = "380";
    public const string CreditNoteTypeCode = "381";

    // Payment means codes
    public const string BankTransfer = "30";
    public const string SepaCreditTransfer = "58";

    // Tax category codes
    public const string StandardRate = "S";
    public const string ZeroRate = "Z";
    public const string ReverseCharge = "AE";
    public const string Exempt = "E";
}
