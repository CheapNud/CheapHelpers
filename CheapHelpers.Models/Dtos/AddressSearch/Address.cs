namespace CheapHelpers.Models.DTOs.AddressSearch;

public class Address
{
    public string? StreetNumber { get; init; }
    public string? StreetName { get; init; }
    public string? Municipality { get; init; }
    public string? CountrySecondarySubdivision { get; init; }
    public string? CountrySubdivision { get; init; }
    public string? PostalCode { get; init; }
    public string? CountryCode { get; init; }
    public string? Country { get; init; }
    public string? CountryCodeISO3 { get; init; }
    public string? FreeformAddress { get; init; }
    public string? LocalName { get; init; }
    public string? MunicipalitySubdivision { get; init; }
}
