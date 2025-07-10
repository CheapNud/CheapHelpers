namespace CheapHelpers.Models.Ubl;

/// <summary>
/// UBL document configuration options
/// </summary>
public record UblDocumentOptions
{
    public string UblVersion { get; init; } = "2.1";
    public string CustomizationId { get; init; } = "urn:www.cenbii.eu:transaction:biicoretrdm001:ver1.0";
    public string ProfileId { get; init; } = "urn:www.cenbii.eu:profile:BII01:ver1.0";
    public string DefaultCurrency { get; init; } = "EUR";
    public string DefaultCountryCode { get; init; } = "BE";
    public bool IncludeNamespaces { get; init; } = true;
    public bool ValidateOnCreate { get; init; } = true;
}