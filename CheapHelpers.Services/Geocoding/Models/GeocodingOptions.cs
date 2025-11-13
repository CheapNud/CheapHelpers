namespace CheapHelpers.Services.Geocoding.Models;

/// <summary>
/// Options for configuring geocoding requests
/// </summary>
public class GeocodingOptions
{
    /// <summary>
    /// Language code for results (e.g., "en", "fr", "nl")
    /// </summary>
    public string? Language { get; init; } = "en";

    /// <summary>
    /// Restrict results to specific countries using ISO country codes
    /// </summary>
    public string[]? Countries { get; init; }

    /// <summary>
    /// Maximum number of results to return (for search operations)
    /// </summary>
    public int? Limit { get; init; } = 5;

    /// <summary>
    /// Bias results towards coordinates near this location
    /// </summary>
    public GeoCoordinate? ProximityBias { get; init; }

    /// <summary>
    /// Restrict results to within this bounding box
    /// </summary>
    public BoundingBox? BoundingBox { get; init; }
}
