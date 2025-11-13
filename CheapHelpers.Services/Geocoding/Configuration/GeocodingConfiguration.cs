namespace CheapHelpers.Services.Geocoding.Configuration;

/// <summary>
/// Main configuration for geocoding services
/// </summary>
public class GeocodingConfiguration
{
    /// <summary>
    /// Default geocoding provider to use
    /// </summary>
    public GeocodingProvider DefaultProvider { get; set; } = GeocodingProvider.Mapbox;

    /// <summary>
    /// Mapbox configuration
    /// </summary>
    public MapboxOptions Mapbox { get; set; } = new();

    /// <summary>
    /// Azure Maps configuration
    /// </summary>
    public AzureMapsOptions AzureMaps { get; set; } = new();

    /// <summary>
    /// Google Maps configuration
    /// </summary>
    public GoogleMapsOptions GoogleMaps { get; set; } = new();

    /// <summary>
    /// PTV Maps configuration
    /// </summary>
    public PtvMapsOptions PtvMaps { get; set; } = new();
}
