using System.Text.Json.Serialization;

namespace CheapHelpers.Services.Geocoding.Providers.PtvMaps;

internal class PtvGeocodeResponse
{
    [JsonPropertyName("locations")]
    public List<PtvLocation>? Locations { get; set; }
}

internal class PtvLocation
{
    [JsonPropertyName("referencePosition")]
    public PtvCoordinate? ReferencePosition { get; set; }

    [JsonPropertyName("formattedAddress")]
    public string? FormattedAddress { get; set; }

    [JsonPropertyName("address")]
    public PtvAddress? Address { get; set; }

    [JsonPropertyName("locationId")]
    public string? LocationId { get; set; }

    [JsonPropertyName("qualityScore")]
    public double? QualityScore { get; set; }
}

internal class PtvCoordinate
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }
}

internal class PtvAddress
{
    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("countryName")]
    public string? CountryName { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("province")]
    public string? Province { get; set; }

    [JsonPropertyName("postalCode")]
    public string? PostalCode { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("district")]
    public string? District { get; set; }

    [JsonPropertyName("street")]
    public string? Street { get; set; }

    [JsonPropertyName("houseNumber")]
    public string? HouseNumber { get; set; }
}
