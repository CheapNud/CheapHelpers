namespace CheapHelpers.Services.Geocoding.Models;

/// <summary>
/// Represents a geocoding result with address and location information
/// </summary>
public class GeocodingResult
{
    /// <summary>
    /// Complete formatted address as a single string
    /// </summary>
    public required string FormattedAddress { get; init; }

    /// <summary>
    /// Geographic coordinate (latitude/longitude)
    /// </summary>
    public required GeoCoordinate Coordinate { get; init; }

    /// <summary>
    /// Structured address components
    /// </summary>
    public required AddressComponents Components { get; init; }

    /// <summary>
    /// Confidence score of the result (0-1, where 1 is highest confidence)
    /// </summary>
    public double? Confidence { get; init; }

    /// <summary>
    /// Provider-specific place identifier
    /// </summary>
    public string? PlaceId { get; init; }

    /// <summary>
    /// Bounding box encompassing the location
    /// </summary>
    public BoundingBox? BoundingBox { get; init; }

    /// <summary>
    /// Name of the geocoding provider that generated this result
    /// </summary>
    public required string Provider { get; init; }
}
