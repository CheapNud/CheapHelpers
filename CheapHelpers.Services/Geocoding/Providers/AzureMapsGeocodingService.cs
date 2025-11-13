using CheapHelpers.Extensions;
using CheapHelpers.Models.Dtos.AddressSearch;
using CheapHelpers.Services.Geocoding.Configuration;
using CheapHelpers.Services.Geocoding.Models;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Services.Geocoding.Providers;

/// <summary>
/// Azure Maps Search API implementation
/// Documentation: https://docs.microsoft.com/en-us/rest/api/maps/search
/// </summary>
public class AzureMapsGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly AzureMapsOptions _options;
    private readonly ILogger<AzureMapsGeocodingService> _logger;
    private const string ApiVersion = "1.0";
    private const string ClientIdHeader = "x-ms-client-id";

    public AzureMapsGeocodingService(
        HttpClient httpClient,
        AzureMapsOptions options,
        ILogger<AzureMapsGeocodingService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(_options.SubscriptionKey))
            throw new ArgumentException("Azure Maps subscription key is required", nameof(options));

        if (string.IsNullOrWhiteSpace(_options.ClientId))
            throw new ArgumentException("Azure Maps client ID is required", nameof(options));

        if (string.IsNullOrWhiteSpace(_options.Endpoint))
            throw new ArgumentException("Azure Maps endpoint is required", nameof(options));
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

            _logger.LogDebug("Azure Maps geocoding request for address: {Address}", address);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add(ClientIdHeader, _options.ClientId);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var azureResponse = json.FromJson<Root>();

            if (azureResponse?.Results == null || azureResponse.Results.Count == 0)
            {
                _logger.LogInformation("No results found for address: {Address}", address);
                return null;
            }

            var result = MapToGeocodingResult(azureResponse.Results[0]);
            _logger.LogDebug("Successfully geocoded address: {Address}", address);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during Azure Maps geocoding for address: {Address}", address);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Azure Maps geocoding for address: {Address}", address);
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

            _logger.LogDebug("Azure Maps reverse geocoding request for coordinates: {Lat},{Lng}", latitude, longitude);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add(ClientIdHeader, _options.ClientId);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var azureResponse = json.FromJson<Root>();

            if (azureResponse?.Results == null || azureResponse.Results.Count == 0)
            {
                _logger.LogInformation("No results found for coordinates: {Lat},{Lng}", latitude, longitude);
                return null;
            }

            var result = MapToGeocodingResult(azureResponse.Results[0]);
            _logger.LogDebug("Successfully reverse geocoded coordinates: {Lat},{Lng}", latitude, longitude);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during Azure Maps reverse geocoding for coordinates: {Lat},{Lng}", latitude, longitude);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Azure Maps reverse geocoding for coordinates: {Lat},{Lng}", latitude, longitude);
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
            var url = BuildFuzzySearchUrl(query, options);

            _logger.LogDebug("Azure Maps fuzzy search request for query: {Query}", query);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add(ClientIdHeader, _options.ClientId);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var azureResponse = json.FromJson<Root>();

            if (azureResponse?.Results == null || azureResponse.Results.Count == 0)
            {
                _logger.LogInformation("No search results found for query: {Query}", query);
                return new List<GeocodingResult>();
            }

            var results = azureResponse.Results
                .Select(MapToGeocodingResult)
                .ToList();

            _logger.LogDebug("Found {Count} search results for query: {Query}", results.Count, query);

            return results;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during Azure Maps search for query: {Query}", query);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Azure Maps search for query: {Query}", query);
            throw;
        }
    }

    private string BuildGeocodeUrl(string address, GeocodingOptions? options)
    {
        var countryCodes = options?.Countries != null && options.Countries.Length > 0
            ? string.Join(",", options.Countries)
            : "BE,NL";

        var query = $"api-version={ApiVersion}" +
                    $"&query={Uri.EscapeDataString(address)}" +
                    $"&countryset={countryCodes}" +
                    $"&subscription-key={_options.SubscriptionKey}";

        if (options?.Limit.HasValue == true)
            query += $"&limit={options.Limit.Value}";

        if (options?.Language != null)
            query += $"&language={options.Language}";

        return $"{_options.Endpoint}/address/json?{query}";
    }

    private string BuildReverseGeocodeUrl(double latitude, double longitude, GeocodingOptions? options)
    {
        var query = $"api-version={ApiVersion}" +
                    $"&query={latitude},{longitude}" +
                    $"&subscription-key={_options.SubscriptionKey}";

        if (options?.Language != null)
            query += $"&language={options.Language}";

        return $"{_options.Endpoint}/address/reverse/json?{query}";
    }

    private string BuildFuzzySearchUrl(string searchQuery, GeocodingOptions? options)
    {
        var countryCodes = options?.Countries != null && options.Countries.Length > 0
            ? string.Join(",", options.Countries)
            : "BE,NL";

        var query = $"api-version={ApiVersion}" +
                    $"&query={Uri.EscapeDataString(searchQuery)}" +
                    $"&countryset={countryCodes}" +
                    $"&typeahead=true" +
                    $"&subscription-key={_options.SubscriptionKey}";

        if (options?.Limit.HasValue == true)
            query += $"&limit={options.Limit.Value}";

        if (options?.Language != null)
            query += $"&language={options.Language}";

        return $"{_options.Endpoint}/fuzzy/json?{query}";
    }

    private GeocodingResult MapToGeocodingResult(Result azureResult)
    {
        var address = azureResult.Address;
        var position = azureResult.Position;

        var formattedAddress = address?.FreeformAddress ?? string.Empty;

        var components = new AddressComponents
        {
            StreetNumber = address?.StreetNumber,
            Street = address?.StreetName,
            City = address?.Municipality ?? address?.LocalName,
            District = address?.MunicipalitySubdivision ?? address?.CountrySecondarySubdivision,
            State = address?.CountrySubdivision,
            PostalCode = address?.PostalCode,
            Country = address?.Country,
            CountryCode = address?.CountryCode
        };

        Models.BoundingBox? boundingBox = null;
        if (azureResult.BoundingBox != null)
        {
            var bbox = azureResult.BoundingBox;
            if (bbox.TopLeftPoint != null && bbox.BottomRightPoint != null)
            {
                boundingBox = new Models.BoundingBox
                {
                    MinLatitude = bbox.BottomRightPoint.Lat,
                    MinLongitude = bbox.TopLeftPoint.Lon,
                    MaxLatitude = bbox.TopLeftPoint.Lat,
                    MaxLongitude = bbox.BottomRightPoint.Lon
                };
            }
        }

        // Azure Maps score is typically 0-1, use it directly as confidence
        double? confidence = azureResult.Score > 0 ? azureResult.Score : null;

        return new GeocodingResult
        {
            FormattedAddress = formattedAddress,
            Coordinate = new GeoCoordinate(position?.Lat ?? 0, position?.Lon ?? 0),
            Components = components,
            Confidence = confidence,
            PlaceId = azureResult.Id,
            BoundingBox = boundingBox,
            Provider = "AzureMaps"
        };
    }
}
