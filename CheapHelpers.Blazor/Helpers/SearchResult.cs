using CheapHelpers.Models.Contracts;

public class SearchResult
{
    public IEntityCode? Entity { get; set; }
    public Type? EntityType { get; set; }
    public SearchConfiguration? Configuration { get; set; }
}