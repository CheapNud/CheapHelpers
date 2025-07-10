namespace CheapHelpers.Models.Ubl;

/// <summary>
/// Item/product information
/// </summary>
public record UblItem
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? SellerItemId { get; init; }
    public string? BuyerItemId { get; init; }
    public string? StandardItemId { get; init; }
    public string? Gtin { get; init; }
    public List<UblItemProperty> Properties { get; init; } = [];
}
