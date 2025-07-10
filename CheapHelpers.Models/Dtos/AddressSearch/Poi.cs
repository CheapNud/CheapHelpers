namespace CheapHelpers.Models.DTOs.AddressSearch;

public class Poi
{
    public string? Name { get; init; }
    public List<CategorySet>? CategorySet { get; init; }
    public string? Url { get; init; }
    public List<string>? Categories { get; init; }
    public List<Classification>? Classifications { get; init; }
}
