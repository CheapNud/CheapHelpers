namespace CheapHelpers.Models.Ubl;

/// <summary>
/// Simplified address information
/// </summary>
public record UblAddress
{
    public string? StreetName { get; init; }
    public string? BuildingNumber { get; init; }
    public string? PostBox { get; init; }
    public string? AdditionalStreetName { get; init; }
    public string? Department { get; init; }
    public string CityName { get; init; } = string.Empty;
    public string? PostalZone { get; init; }
    public string? CountrySubentity { get; init; }
    public string CountryCode { get; init; } = "BE";
    public string? AddressId { get; init; }
}
