using CheapHelpers.Services.Geocoding.Configuration;
using CheapHelpers.Services.Geocoding.Models;
using CheapHelpers.Services.Geocoding.Providers.Mapbox;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CheapHelpers.Services.Geocoding.Providers;

/// <summary>
/// Mapbox Geocoding API implementation
/// Documentation: https://docs.mapbox.com/api/search/geocoding/
/// </summary>
public class MapboxGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly MapboxOptions _options;
    private readonly ILogger<MapboxGeocodingService> _logger;
    private const string BaseUrl = "https://api.mapbox.com/search/geocode/v6";

    public MapboxGeocodingService(
        HttpClient httpClient,
        MapboxOptions options,
        ILogger<MapboxGeocodingService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(_options.AccessToken))
            throw new ArgumentException("Mapbox access token is required", nameof(options));
    }

    public async Task<GeocodingResult?> GeocodeAsync(
        string address,
        GeocodingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);

        try
        {
            var encodedAddress = Uri.EscapeDataString(address);
            var url = BuildForwardUrl(encodedAddress, options);

            _logger.LogDebug("Mapbox geocoding request for address: {Address}", address);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var mapboxResponse = JsonSerializer.Deserialize<MapboxResponse>(json);

            if (mapboxResponse?.Features == null || mapboxResponse.Features.Count == 0)
            {
                _logger.LogInformation("No results found for address: {Address}", address);
                return null;
            }

            var result = MapToGeocodingResult(mapboxResponse.Features[0]);
            _logger.LogDebug("Successfully geocoded address: {Address}", address);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during Mapbox geocoding for address: {Address}", address);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Mapbox geocoding for address: {Address}", address);
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
            var url = BuildReverseUrl(longitude, latitude, options);

            _logger.LogDebug("Mapbox reverse geocoding request for coordinates: {Lat},{Lng}", latitude, longitude);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var mapboxResponse = JsonSerializer.Deserialize<MapboxResponse>(json);

            if (mapboxResponse?.Features == null || mapboxResponse.Features.Count == 0)
            {
                _logger.LogInformation("No results found for coordinates: {Lat},{Lng}", latitude, longitude);
                return null;
            }

            var result = MapToGeocodingResult(mapboxResponse.Features[0]);
            _logger.LogDebug("Successfully reverse geocoded coordinates: {Lat},{Lng}", latitude, longitude);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during Mapbox reverse geocoding for coordinates: {Lat},{Lng}", latitude, longitude);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Mapbox reverse geocoding for coordinates: {Lat},{Lng}", latitude, longitude);
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
            var encodedQuery = Uri.EscapeDataString(query);
            var url = BuildSearchUrl(encodedQuery, options);

            _logger.LogDebug("Mapbox search request for query: {Query}", query);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var mapboxResponse = JsonSerializer.Deserialize<MapboxResponse>(json);

            if (mapboxResponse?.Features == null || mapboxResponse.Features.Count == 0)
            {
                _logger.LogInformation("No search results found for query: {Query}", query);
                return new List<GeocodingResult>();
            }

            var results = mapboxResponse.Features
                .Select(MapToGeocodingResult)
                .ToList();

            _logger.LogDebug("Found {Count} search results for query: {Query}", results.Count, query);

            return results;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during Mapbox search for query: {Query}", query);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Mapbox search for query: {Query}", query);
            throw;
        }
    }

    private string BuildForwardUrl(string encodedAddress, GeocodingOptions? options)
    {
        var queryParams = new List<string>
        {
            $"access_token={_options.AccessToken}"
        };

        AddCommonParameters(queryParams, options);

        return $"{BaseUrl}/forward?q={encodedAddress}&{string.Join("&", queryParams)}";
    }

    private string BuildReverseUrl(double longitude, double latitude, GeocodingOptions? options)
    {
        var queryParams = new List<string>
        {
            $"access_token={_options.AccessToken}"
        };

        AddCommonParameters(queryParams, options);

        return $"{BaseUrl}/reverse/{longitude},{latitude}?{string.Join("&", queryParams)}";
    }

    private string BuildSearchUrl(string encodedQuery, GeocodingOptions? options)
    {
        var queryParams = new List<string>
        {
            $"access_token={_options.AccessToken}",
            "autocomplete=true"
        };

        AddCommonParameters(queryParams, options);

        return $"{BaseUrl}/forward?q={encodedQuery}&{string.Join("&", queryParams)}";
    }

    private void AddCommonParameters(List<string> queryParams, GeocodingOptions? options)
    {
        if (options?.Language != null)
            queryParams.Add($"language={options.Language}");

        if (options?.Countries != null && options.Countries.Length > 0)
            queryParams.Add($"country={string.Join(",", options.Countries)}");

        if (options?.Limit.HasValue == true)
            queryParams.Add($"limit={options.Limit.Value}");

        if (options?.ProximityBias != null)
            queryParams.Add($"proximity={options.ProximityBias.Longitude},{options.ProximityBias.Latitude}");

        if (options?.BoundingBox != null)
        {
            var bbox = options.BoundingBox;
            queryParams.Add($"bbox={bbox.MinLongitude},{bbox.MinLatitude},{bbox.MaxLongitude},{bbox.MaxLatitude}");
        }
    }

    private GeocodingResult MapToGeocodingResult(MapboxFeature feature)
    {
        var props = feature.Properties;
        var context = props?.Context;
        var coords = feature.Geometry?.Coordinates;

        // Mapbox returns coordinates as [longitude, latitude]
        var longitude = coords?[0] ?? 0;
        var latitude = coords?[1] ?? 0;

        var formattedAddress = props?.FullAddress
            ?? props?.PlaceFormatted
            ?? props?.Name
            ?? string.Empty;

        var components = new AddressComponents
        {
            StreetNumber = context?.Address?.AddressNumber,
            Street = context?.Street?.Name ?? context?.Address?.StreetName,
            City = context?.Place?.Name ?? context?.Locality?.Name,
            District = context?.District?.Name ?? context?.Neighborhood?.Name,
            State = context?.Region?.Name,
            PostalCode = context?.Postcode?.Name,
            Country = context?.Country?.Name,
            CountryCode = context?.Country?.CountryCode
        };

        BoundingBox? boundingBox = null;
        if (props?.BoundingBox != null && props.BoundingBox.Count == 4)
        {
            boundingBox = new BoundingBox
            {
                MinLongitude = props.BoundingBox[0],
                MinLatitude = props.BoundingBox[1],
                MaxLongitude = props.BoundingBox[2],
                MaxLatitude = props.BoundingBox[3]
            };
        }

        double? confidence = props?.MatchCode?.Confidence switch
        {
            "exact" => 1.0,
            "high" => 0.8,
            "medium" => 0.5,
            "low" => 0.3,
            _ => null
        };

        return new GeocodingResult
        {
            FormattedAddress = formattedAddress,
            Coordinate = new GeoCoordinate(latitude, longitude),
            Components = components,
            Confidence = confidence,
            PlaceId = props?.MapboxId,
            BoundingBox = boundingBox,
            Provider = "Mapbox"
        };
    }
}
