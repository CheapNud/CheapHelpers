using CheapHelpers.Models.Contracts;
using System.Linq.Expressions;

public class SearchConfiguration
{
    public required string Key { get; set; }
    public required string Label { get; set; }
    public required Type EntityType { get; set; }
    public required Func<IEntityCode, string> DisplayProp { get; set; }
    public Expression<Func<IEntityCode, bool>>? Where { get; set; }
    public Expression<Func<IEntityCode, object>>? OrderBy { get; set; }
    public Expression<Func<IEntityCode, object>>? OrderByDescending { get; set; }
    public bool UseSelect { get; set; } = true;
    public bool IgnoreAutoIncludes { get; set; } = true;
}
