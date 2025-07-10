namespace CheapHelpers.Models.Dtos.Ubl;

/// <summary>
/// Contact information
/// </summary>
public record UblContact
{
    public string? Name { get; init; }
    public string? Telephone { get; init; }
    public string? Fax { get; init; }
    public string? Email { get; init; }
}
