namespace CheapHelpers.Models.Ubl;

/// <summary>
/// Item property (color, size, etc.)
/// </summary>
public record UblItemProperty
{
    public string Name { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}
