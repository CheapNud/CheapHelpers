using Newtonsoft.Json;

namespace CheapHelpers.Models;

public class Root
{
    public Summary? Summary { get; init; }
    public List<Result>? Results { get; init; }
}

public class Summary
{
    public string? Query { get; init; }
    public string? QueryType { get; init; }
    public int QueryTime { get; init; }
    public int NumResults { get; init; }
    public int Offset { get; init; }
    public int TotalResults { get; init; }
    public int FuzzyLevel { get; init; }
    public List<object>? QueryIntent { get; init; }
}

public class Result
{
    public string? Type { get; init; }
    public string? Id { get; init; }
    public double Score { get; init; }
    public string? Info { get; init; }
    public Poi? Poi { get; init; }
    public Address? Address { get; init; }
    public Position? Position { get; init; }
    public Viewport? Viewport { get; init; }
    public List<EntryPoint>? EntryPoints { get; init; }
    public DataSources? DataSources { get; init; }
    public string? EntityType { get; init; }
    public BoundingBox? BoundingBox { get; init; }
}

public class Address
{
    public string? StreetNumber { get; init; }
    public string? StreetName { get; init; }
    public string? Municipality { get; init; }
    public string? CountrySecondarySubdivision { get; init; }
    public string? CountrySubdivision { get; init; }
    public string? PostalCode { get; init; }
    public string? CountryCode { get; init; }
    public string? Country { get; init; }
    public string? CountryCodeISO3 { get; init; }
    public string? FreeformAddress { get; init; }
    public string? LocalName { get; init; }
    public string? MunicipalitySubdivision { get; init; }
}

public record Position(double Lat, double Lon);

public record TopLeftPoint(double Lat, double Lon);

public record BottomRightPoint(double Lat, double Lon);

public class BoundingBox
{
    public TopLeftPoint? TopLeftPoint { get; init; }
    public BottomRightPoint? BottomRightPoint { get; init; }
}

public class Viewport
{
    public TopLeftPoint? TopLeftPoint { get; init; }
    public BottomRightPoint? BottomRightPoint { get; init; }
}

public class Poi
{
    public string? Name { get; init; }
    public List<CategorySet>? CategorySet { get; init; }
    public string? Url { get; init; }
    public List<string>? Categories { get; init; }
    public List<Classification>? Classifications { get; init; }
}

public record CategorySet(int Id);

public class Classification
{
    public string? Code { get; init; }
    public List<Name>? Names { get; init; }
}

public class Name
{
    public string? NameLocale { get; init; }

    [JsonProperty("name")]
    public string? NameString { get; init; }
}

public class EntryPoint
{
    public string? Type { get; init; }
    public Position? Position { get; init; }
}

public class DataSources
{
    public Geometry? Geometry { get; init; }
}

public record Geometry(string? Id);