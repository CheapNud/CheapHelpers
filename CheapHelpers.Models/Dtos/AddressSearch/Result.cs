namespace CheapHelpers.Models.Dtos.AddressSearch;

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
