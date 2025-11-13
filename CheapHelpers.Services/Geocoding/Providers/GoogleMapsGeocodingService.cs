using CheapHelpers.Services.Geocoding.Configuration;
using CheapHelpers.Services.Geocoding.Models;
using GoogleApi;
using GoogleApi.Entities.Common.Enums;
using GoogleApi.Entities.Maps.Geocoding.Address.Request;
using GoogleApi.Entities.Maps.Geocoding.Common;
using GoogleApi.Entities.Maps.Geocoding.Common.Enums;
using GoogleApi.Entities.Maps.Geocoding.Location.Request;
using Microsoft.Extensions.Logging;
using GeocodeResult = GoogleApi.Entities.Maps.Geocoding.Common.Result;

namespace CheapHelpers.Services.Geocoding.Providers;

/// <summary>
/// Google Maps Geocoding API implementation
/// Documentation: https://developers.google.com/maps/documentation/geocoding
/// </summary>
public class GoogleMapsGeocodingService : IGeocodingService
{
    private readonly GoogleMapsOptions _options;
    private readonly ILogger<GoogleMapsGeocodingService> _logger;

    public GoogleMapsGeocodingService(
        GoogleMapsOptions options,
        ILogger<GoogleMapsGeocodingService> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new ArgumentException("Google Maps API key is required", nameof(options));
    }

    public async Task<GeocodingResult?> GeocodeAsync(
        string address,
        GeocodingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);

        try
        {
            var request = new AddressGeocodeRequest
            {
                Address = address,
                Key = _options.ApiKey
            };

            if (options?.Language != null && Enum.TryParse<GoogleApi.Entities.Common.Enums.Language>(options.Language, true, out var lang))
                request.Language = lang;

            if (options?.BoundingBox != null)
            {
                var bbox = options.BoundingBox;
                request.Bounds = new GoogleApi.Entities.Common.ViewPort(
                    new GoogleApi.Entities.Common.Coordinate(bbox.MinLatitude, bbox.MinLongitude),
                    new GoogleApi.Entities.Common.Coordinate(bbox.MaxLatitude, bbox.MaxLongitude)
                );
            }

            _logger.LogDebug("Google Maps geocoding request for address: {Address}", address);

            var response = await GoogleMaps.Geocode.AddressGeocode.QueryAsync(request, cancellationToken);

            if (response.Status != Status.Ok || !response.Results.Any())
            {
                _logger.LogInformation("No results found for address: {Address}. Status: {Status}", address, response.Status);
                return null;
            }

            var result = MapToGeocodingResult(response.Results.First());
            _logger.LogDebug("Successfully geocoded address: {Address}", address);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google Maps geocoding for address: {Address}", address);
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
            var request = new LocationGeocodeRequest
            {
                Location = new GoogleApi.Entities.Common.Coordinate(latitude, longitude),
                Key = _options.ApiKey
            };

            if (options?.Language != null && Enum.TryParse<GoogleApi.Entities.Common.Enums.Language>(options.Language, true, out var lang))
                request.Language = lang;

            _logger.LogDebug("Google Maps reverse geocoding request for coordinates: {Lat},{Lng}", latitude, longitude);

            var response = await GoogleMaps.Geocode.LocationGeocode.QueryAsync(request, cancellationToken);

            if (response.Status != Status.Ok || !response.Results.Any())
            {
                _logger.LogInformation("No results found for coordinates: {Lat},{Lng}. Status: {Status}", latitude, longitude, response.Status);
                return null;
            }

            var result = MapToGeocodingResult(response.Results.First());
            _logger.LogDebug("Successfully reverse geocoded coordinates: {Lat},{Lng}", latitude, longitude);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google Maps reverse geocoding for coordinates: {Lat},{Lng}", latitude, longitude);
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
            var request = new AddressGeocodeRequest
            {
                Address = query,
                Key = _options.ApiKey
            };

            if (options?.Language != null && Enum.TryParse<GoogleApi.Entities.Common.Enums.Language>(options.Language, true, out var lang))
                request.Language = lang;

            if (options?.BoundingBox != null)
            {
                var bbox = options.BoundingBox;
                request.Bounds = new GoogleApi.Entities.Common.ViewPort(
                    new GoogleApi.Entities.Common.Coordinate(bbox.MinLatitude, bbox.MinLongitude),
                    new GoogleApi.Entities.Common.Coordinate(bbox.MaxLatitude, bbox.MaxLongitude)
                );
            }

            _logger.LogDebug("Google Maps search request for query: {Query}", query);

            var response = await GoogleMaps.Geocode.AddressGeocode.QueryAsync(request, cancellationToken);

            if (response.Status != Status.Ok || !response.Results.Any())
            {
                _logger.LogInformation("No search results found for query: {Query}. Status: {Status}", query, response.Status);
                return new List<GeocodingResult>();
            }

            // Apply limit if specified
            var results = response.Results;
            if (options?.Limit.HasValue == true && options.Limit.Value > 0)
            {
                results = results.Take(options.Limit.Value).ToList();
            }

            var geocodingResults = results
                .Select(MapToGeocodingResult)
                .ToList();

            _logger.LogDebug("Found {Count} search results for query: {Query}", geocodingResults.Count, query);

            return geocodingResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google Maps search for query: {Query}", query);
            throw;
        }
    }

    private GeocodingResult MapToGeocodingResult(GeocodeResult googleResult)
    {
        var formattedAddress = googleResult.FormattedAddress;
        var location = googleResult.Geometry.Location;

        var components = new AddressComponents
        {
            StreetNumber = GetAddressComponent(googleResult, AddressComponentType.Street_Number),
            Street = GetAddressComponent(googleResult, AddressComponentType.Route),
            City = GetAddressComponent(googleResult, AddressComponentType.Locality)
                ?? GetAddressComponent(googleResult, AddressComponentType.Postal_Town),
            District = GetAddressComponent(googleResult, AddressComponentType.Sublocality)
                ?? GetAddressComponent(googleResult, AddressComponentType.Administrative_Area_Level_2),
            State = GetAddressComponent(googleResult, AddressComponentType.Administrative_Area_Level_1),
            PostalCode = GetAddressComponent(googleResult, AddressComponentType.Postal_Code),
            Country = GetAddressComponentLong(googleResult, AddressComponentType.Country),
            CountryCode = GetAddressComponent(googleResult, AddressComponentType.Country)
        };

        BoundingBox? boundingBox = null;
        var viewport = googleResult.Geometry?.ViewPort;
        if (viewport != null)
        {
            boundingBox = new Models.BoundingBox
            {
                MinLatitude = viewport.SouthWest.Latitude,
                MinLongitude = viewport.SouthWest.Longitude,
                MaxLatitude = viewport.NorthEast.Latitude,
                MaxLongitude = viewport.NorthEast.Longitude
            };
        }

        // Google doesn't provide a direct confidence score in the response
        // We can infer confidence from partial match status
        double? confidence = googleResult.PartialMatch == true ? 0.7 : 0.9;

        return new GeocodingResult
        {
            FormattedAddress = formattedAddress,
            Coordinate = new GeoCoordinate(location.Latitude, location.Longitude),
            Components = components,
            Confidence = confidence,
            PlaceId = googleResult.PlaceId,
            BoundingBox = boundingBox,
            Provider = "GoogleMaps"
        };
    }

    private string? GetAddressComponent(GeocodeResult result, AddressComponentType type)
    {
        return result.AddressComponents
            .FirstOrDefault(ac => ac.Types.Contains(type))?
            .ShortName;
    }

    private string? GetAddressComponentLong(GeocodeResult result, AddressComponentType type)
    {
        return result.AddressComponents
            .FirstOrDefault(ac => ac.Types.Contains(type))?
            .LongName;
    }
}
