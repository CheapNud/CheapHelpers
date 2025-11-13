namespace CheapHelpers.Services.Geocoding;

/// <summary>
/// Factory for creating geocoding service instances
/// </summary>
public interface IGeocodingServiceFactory
{
    /// <summary>
    /// Get the default geocoding service (configured via DefaultProvider)
    /// </summary>
    IGeocodingService GetDefaultService();

    /// <summary>
    /// Get a specific geocoding provider
    /// </summary>
    IGeocodingService GetService(GeocodingProvider provider);
}

/// <summary>
/// Available geocoding providers
/// </summary>
public enum GeocodingProvider
{
    /// <summary>
    /// Mapbox Geocoding API
    /// </summary>
    Mapbox = 0,

    /// <summary>
    /// Azure Maps Search API
    /// </summary>
    AzureMaps = 1,

    /// <summary>
    /// Google Maps Geocoding API
    /// </summary>
    GoogleMaps = 2,

    /// <summary>
    /// PTV Maps Geocoding API
    /// </summary>
    PtvMaps = 3
}
