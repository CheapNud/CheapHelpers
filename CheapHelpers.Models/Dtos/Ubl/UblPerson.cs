namespace CheapHelpers.Models.Dtos.Ubl;

/// <summary>
/// Person information
/// </summary>
public record UblPerson
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? MiddleName { get; init; }
    public string? JobTitle { get; init; }
}
