namespace CheapHelpers.Models.Ubl;

/// <summary>
/// Simplified order model for creating UBL documents
/// </summary>
public record UblOrder
{
    public string Id { get; init; } = string.Empty;
    public DateTime IssueDate { get; init; } = DateTime.Now;
    public string Currency { get; init; } = "EUR";
    public string? Note { get; init; }
    public string? AccountingCostCode { get; init; }
    public string? QuotationId { get; init; }
    public DateTime? ValidityEndDate { get; init; }

    public UblParty Buyer { get; init; } = new();
    public UblParty Seller { get; init; } = new();
    public UblDelivery? Delivery { get; init; }
    public UblMonetaryTotals? Totals { get; init; }
    public List<UblOrderLine> Lines { get; init; } = [];
    public List<UblAllowanceCharge> AllowancesCharges { get; init; } = [];
}

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

/// <summary>
/// Simplified address information
/// </summary>
public record UblAddress
{
    public string? StreetName { get; init; }
    public string? BuildingNumber { get; init; }
    public string? PostBox { get; init; }
    public string? AdditionalStreetName { get; init; }
    public string? Department { get; init; }
    public string CityName { get; init; } = string.Empty;
    public string? PostalZone { get; init; }
    public string? CountrySubentity { get; init; }
    public string CountryCode { get; init; } = "BE";
    public string? AddressId { get; init; }
}

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

/// <summary>
/// Order line item
/// </summary>
public record UblOrderLine
{
    public string Id { get; init; } = string.Empty;
    public string? Note { get; init; }
    public UblItem Item { get; init; } = new();
    public decimal Quantity { get; init; }
    public string QuantityUnit { get; init; } = "PCE";
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
    public decimal? TaxAmount { get; init; }
    public string? AccountingCostCode { get; init; }
    public bool AllowPartialDelivery { get; init; } = true;
    public UblDelivery? SpecificDelivery { get; init; }
}

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

/// <summary>
/// Item property (color, size, etc.)
/// </summary>
public record UblItemProperty
{
    public string Name { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}

/// <summary>
/// Monetary totals for the order
/// </summary>
public record UblMonetaryTotals
{
    public decimal LineExtensionAmount { get; init; }
    public decimal? AllowanceTotalAmount { get; init; }
    public decimal? ChargeTotalAmount { get; init; }
    public decimal? TaxAmount { get; init; }
    public decimal PayableAmount { get; init; }
}

/// <summary>
/// Allowances (discounts) and charges (fees)
/// </summary>
public record UblAllowanceCharge
{
    public bool IsCharge { get; init; } // true = charge, false = allowance/discount
    public string Reason { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "EUR";
}

/// <summary>
/// UBL document configuration options
/// </summary>
public record UblDocumentOptions
{
    public string UblVersion { get; init; } = "2.1";
    public string CustomizationId { get; init; } = "urn:www.cenbii.eu:transaction:biicoretrdm001:ver1.0";
    public string ProfileId { get; init; } = "urn:www.cenbii.eu:profile:BII01:ver1.0";
    public string DefaultCurrency { get; init; } = "EUR";
    public string DefaultCountryCode { get; init; } = "BE";
    public bool IncludeNamespaces { get; init; } = true;
    public bool ValidateOnCreate { get; init; } = true;
}