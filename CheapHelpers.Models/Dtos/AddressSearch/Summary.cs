namespace CheapHelpers.Models.Dtos.AddressSearch;

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
