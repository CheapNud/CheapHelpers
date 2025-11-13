using System.Text.Json.Serialization;

namespace CheapHelpers.Services.Geocoding.Providers.Mapbox;

internal class MapboxResponse
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("features")]
    public List<MapboxFeature>? Features { get; set; }

    [JsonPropertyName("attribution")]
    public string? Attribution { get; set; }
}

internal class MapboxFeature
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("geometry")]
    public MapboxGeometry? Geometry { get; set; }

    [JsonPropertyName("properties")]
    public MapboxProperties? Properties { get; set; }
}

internal class MapboxGeometry
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("coordinates")]
    public List<double>? Coordinates { get; set; }
}

internal class MapboxProperties
{
    [JsonPropertyName("mapbox_id")]
    public string? MapboxId { get; set; }

    [JsonPropertyName("feature_type")]
    public string? FeatureType { get; set; }

    [JsonPropertyName("full_address")]
    public string? FullAddress { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("name_preferred")]
    public string? NamePreferred { get; set; }

    [JsonPropertyName("place_formatted")]
    public string? PlaceFormatted { get; set; }

    [JsonPropertyName("context")]
    public MapboxContext? Context { get; set; }

    [JsonPropertyName("coordinates")]
    public MapboxCoordinates? Coordinates { get; set; }

    [JsonPropertyName("bbox")]
    public List<double>? BoundingBox { get; set; }

    [JsonPropertyName("match_code")]
    public MapboxMatchCode? MatchCode { get; set; }
}

internal class MapboxContext
{
    [JsonPropertyName("country")]
    public MapboxContextItem? Country { get; set; }

    [JsonPropertyName("region")]
    public MapboxContextItem? Region { get; set; }

    [JsonPropertyName("postcode")]
    public MapboxContextItem? Postcode { get; set; }

    [JsonPropertyName("district")]
    public MapboxContextItem? District { get; set; }

    [JsonPropertyName("place")]
    public MapboxContextItem? Place { get; set; }

    [JsonPropertyName("locality")]
    public MapboxContextItem? Locality { get; set; }

    [JsonPropertyName("neighborhood")]
    public MapboxContextItem? Neighborhood { get; set; }

    [JsonPropertyName("street")]
    public MapboxContextItem? Street { get; set; }

    [JsonPropertyName("address")]
    public MapboxAddressContext? Address { get; set; }
}

internal class MapboxContextItem
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("country_code")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("country_code_alpha_3")]
    public string? CountryCodeAlpha3 { get; set; }
}

internal class MapboxAddressContext
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("address_number")]
    public string? AddressNumber { get; set; }

    [JsonPropertyName("street_name")]
    public string? StreetName { get; set; }
}

internal class MapboxCoordinates
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("accuracy")]
    public string? Accuracy { get; set; }

    [JsonPropertyName("routable_points")]
    public List<MapboxRoutablePoint>? RoutablePoints { get; set; }
}

internal class MapboxRoutablePoint
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }
}

internal class MapboxMatchCode
{
    [JsonPropertyName("confidence")]
    public string? Confidence { get; set; }

    [JsonPropertyName("exact_match")]
    public bool? ExactMatch { get; set; }
}
