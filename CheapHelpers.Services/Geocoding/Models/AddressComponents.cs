namespace CheapHelpers.Services.Geocoding.Models;

/// <summary>
/// Represents the structured components of an address
/// </summary>
public class AddressComponents
{
    /// <summary>
    /// Street number (e.g., "123")
    /// </summary>
    public string? StreetNumber { get; init; }

    /// <summary>
    /// Street name (e.g., "Main Street")
    /// </summary>
    public string? Street { get; init; }

    /// <summary>
    /// City or locality name
    /// </summary>
    public string? City { get; init; }

    /// <summary>
    /// District or neighborhood
    /// </summary>
    public string? District { get; init; }

    /// <summary>
    /// State or province name
    /// </summary>
    public string? State { get; init; }

    /// <summary>
    /// Postal or ZIP code
    /// </summary>
    public string? PostalCode { get; init; }

    /// <summary>
    /// Full country name
    /// </summary>
    public string? Country { get; init; }

    /// <summary>
    /// ISO country code (e.g., "US", "GB", "BE")
    /// </summary>
    public string? CountryCode { get; init; }
}
