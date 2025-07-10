namespace CheapHelpers.Models.Ubl;

/// <summary>
/// Simplified party (buyer/seller) information
/// </summary>
public record UblParty
{
    public string Name { get; init; } = string.Empty;
    public string? Id { get; init; }
    public string? EndpointId { get; init; }
    public string? TaxId { get; init; }
    public string? CompanyId { get; init; }
    public UblAddress? Address { get; init; }
    public UblContact? Contact { get; init; }
    public UblPerson? MainPerson { get; init; }
}
