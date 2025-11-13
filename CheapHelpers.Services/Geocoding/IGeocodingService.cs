using CheapHelpers.Services.Geocoding.Models;

namespace CheapHelpers.Services.Geocoding;

/// <summary>
/// Provides geocoding, reverse geocoding, and address search services
/// </summary>
public interface IGeocodingService
{
    /// <summary>
    /// Forward Geocoding: Convert complete address to coordinates
    /// </summary>
    /// <param name="address">Complete address to geocode</param>
    /// <param name="options">Optional geocoding options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Geocoding result with coordinates and address components</returns>
    Task<GeocodingResult?> GeocodeAsync(
        string address,
        GeocodingOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reverse Geocoding: Convert coordinates to address
    /// </summary>
    /// <param name="latitude">Latitude coordinate</param>
    /// <param name="longitude">Longitude coordinate</param>
    /// <param name="options">Optional geocoding options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Geocoding result with formatted address</returns>
    Task<GeocodingResult?> ReverseGeocodeAsync(
        double latitude,
        double longitude,
        GeocodingOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fuzzy Search/Autocomplete: Search for addresses with partial input
    /// Perfect for search boxes with typeahead functionality
    /// </summary>
    /// <param name="query">Partial address or search query</param>
    /// <param name="options">Optional geocoding options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching geocoding results</returns>
    Task<List<GeocodingResult>> SearchAsync(
        string query,
        GeocodingOptions? options = null,
        CancellationToken cancellationToken = default);
}
