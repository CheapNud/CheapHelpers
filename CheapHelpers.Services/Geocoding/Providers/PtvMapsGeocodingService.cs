using CheapHelpers.Services.Geocoding.Configuration;
using CheapHelpers.Services.Geocoding.Models;
using CheapHelpers.Services.Geocoding.Providers.PtvMaps;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CheapHelpers.Services.Geocoding.Providers;

/// <summary>
/// PTV Maps Geocoding API implementation
/// Documentation: https://developer.myptv.com/en/documentation/geocoding-api/
/// </summary>
public class PtvMapsGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly PtvMapsOptions _options;
    private readonly ILogger<PtvMapsGeocodingService> _logger;
    private const string BaseUrl = "https://api.myptv.com/geocoding/v1";

    public PtvMapsGeocodingService(
        HttpClient httpClient,
        PtvMapsOptions options,
        ILogger<PtvMapsGeocodingService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new ArgumentException("PTV Maps API key is required", nameof(options));

        // Configure HttpClient
        if (!_httpClient.DefaultRequestHeaders.Contains("apiKey"))
        {
            _httpClient.DefaultRequestHeaders.Add("apiKey", _options.ApiKey);
        }
    }

    public async Task<GeocodingResult?> GeocodeAsync(
        string address,
        GeocodingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);

        try
        {
            var url = BuildGeocodeUrl(address, options);

            _logger.LogDebug("PTV Maps geocoding request for address: {Address}", address);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var ptvResponse = JsonSerializer.Deserialize<PtvGeocodeResponse>(json);

            if (ptvResponse?.Locations == null || ptvResponse.Locations.Count == 0)
            {
                _logger.LogInformation("No results found for address: {Address}", address);
                return null;
            }

            var result = MapToGeocodingResult(ptvResponse.Locations[0]);
            _logger.LogDebug("Successfully geocoded address: {Address}", address);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during PTV Maps geocoding for address: {Address}", address);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during PTV Maps geocoding for address: {Address}", address);
            throw;
        }
    }

    public async Task<GeocodingResult?> ReverseGeocodeAsync(
        double latitude,
        double longitude,
        GeocodingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var coordinate = new GeoCoordinate(latitude, longitude);
        if (!coordinate.IsValid())
            throw new ArgumentException("Invalid coordinates provided");

        try
        {
            var url = BuildReverseGeocodeUrl(latitude, longitude, options);

            _logger.LogDebug("PTV Maps reverse geocoding request for coordinates: {Lat},{Lng}", latitude, longitude);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var ptvResponse = JsonSerializer.Deserialize<PtvGeocodeResponse>(json);

            if (ptvResponse?.Locations == null || ptvResponse.Locations.Count == 0)
            {
                _logger.LogInformation("No results found for coordinates: {Lat},{Lng}", latitude, longitude);
                return null;
            }

            var result = MapToGeocodingResult(ptvResponse.Locations[0]);
            _logger.LogDebug("Successfully reverse geocoded coordinates: {Lat},{Lng}", latitude, longitude);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during PTV Maps reverse geocoding for coordinates: {Lat},{Lng}", latitude, longitude);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during PTV Maps reverse geocoding for coordinates: {Lat},{Lng}", latitude, longitude);
            throw;
        }
    }

    public async Task<List<GeocodingResult>> SearchAsync(
        string query,
        GeocodingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        try
        {
            // For search/autocomplete, we use the same geocoding endpoint but can return multiple results
            var url = BuildGeocodeUrl(query, options, allowMultiple: true);

            _logger.LogDebug("PTV Maps search request for query: {Query}", query);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var ptvResponse = JsonSerializer.Deserialize<PtvGeocodeResponse>(json);

            if (ptvResponse?.Locations == null || ptvResponse.Locations.Count == 0)
            {
                _logger.LogInformation("No search results found for query: {Query}", query);
                return new List<GeocodingResult>();
            }

            var results = ptvResponse.Locations
                .Select(MapToGeocodingResult)
                .ToList();

            _logger.LogDebug("Found {Count} search results for query: {Query}", results.Count, query);

            return results;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during PTV Maps search for query: {Query}", query);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during PTV Maps search for query: {Query}", query);
            throw;
        }
    }

    private string BuildGeocodeUrl(string address, GeocodingOptions? options, bool allowMultiple = false)
    {
        var encodedAddress = Uri.EscapeDataString(address);
        var url = $"{BaseUrl}/locations/by-text?searchText={encodedAddress}";

        if (options?.Language != null)
            url += $"&language={options.Language}";

        if (options?.Countries != null && options.Countries.Length > 0)
            url += $"&countries={string.Join(",", options.Countries)}";

        // PTV supports maxResults parameter
        if (allowMultiple && options?.Limit.HasValue == true)
            url += $"&maxResults={options.Limit.Value}";

        return url;
    }

    private string BuildReverseGeocodeUrl(double latitude, double longitude, GeocodingOptions? options)
    {
        var url = $"{BaseUrl}/locations/by-position/{latitude}/{longitude}";

        if (options?.Language != null)
            url += $"?language={options.Language}";

        return url;
    }

    private GeocodingResult MapToGeocodingResult(PtvLocation ptvLocation)
    {
        var address = ptvLocation.Address;
        var position = ptvLocation.ReferencePosition;

        // Build formatted address if not provided
        var formattedAddress = ptvLocation.FormattedAddress;
        if (string.IsNullOrEmpty(formattedAddress) && address != null)
        {
            var parts = new List<string>();

            if (!string.IsNullOrEmpty(address.Street))
            {
                var street = address.Street;
                if (!string.IsNullOrEmpty(address.HouseNumber))
                    street += " " + address.HouseNumber;
                parts.Add(street);
            }

            if (!string.IsNullOrEmpty(address.City))
                parts.Add(address.City);

            if (!string.IsNullOrEmpty(address.PostalCode))
                parts.Add(address.PostalCode);

            if (!string.IsNullOrEmpty(address.CountryName))
                parts.Add(address.CountryName);

            formattedAddress = string.Join(", ", parts.Where(p => !string.IsNullOrEmpty(p)));
        }
        else if (!string.IsNullOrEmpty(formattedAddress) && !string.IsNullOrEmpty(address?.CountryName))
        {
            // PTV's FormattedAddress doesn't always include country, so append it
            if (!formattedAddress.Contains(address.CountryName))
            {
                formattedAddress += ", " + address.CountryName;
            }
        }

        var components = new AddressComponents
        {
            StreetNumber = address?.HouseNumber,
            Street = address?.Street,
            City = address?.City,
            District = address?.District,
            State = address?.State ?? address?.Province,
            PostalCode = address?.PostalCode,
            Country = address?.CountryName,
            CountryCode = address?.CountryCode
        };

        // PTV provides qualityScore (0-1 range)
        double? confidence = ptvLocation.QualityScore;

        return new GeocodingResult
        {
            FormattedAddress = formattedAddress ?? string.Empty,
            Coordinate = new GeoCoordinate(position?.Latitude ?? 0, position?.Longitude ?? 0),
            Components = components,
            Confidence = confidence,
            PlaceId = ptvLocation.LocationId,
            BoundingBox = null, // PTV doesn't provide bounding box in standard response
            Provider = "PtvMaps"
        };
    }
}
