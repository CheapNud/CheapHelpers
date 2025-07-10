namespace CheapHelpers.Models.Ubl;

/// <summary>
/// Delivery information
/// </summary>
public record UblDelivery
{
    public UblAddress? DeliveryAddress { get; init; }
    public DateTime? RequestedStartDate { get; init; }
    public DateTime? RequestedEndDate { get; init; }
    public UblParty? DeliveryParty { get; init; }
    public string? DeliveryTerms { get; init; }
    public string? SpecialInstructions { get; init; }
}
