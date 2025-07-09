using CheapHelpers.Models.Ubl;
using System.Diagnostics;
using System.Xml;
using UblSharp;
using UblSharp.CommonAggregateComponents;
using UblSharp.UnqualifiedDataTypes;

namespace CheapHelpers.Services;

public class UblService
{
    // Namespace Constants
    private const string CacNamespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private const string CbcNamespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";

    // Scheme Constants
    private const string BiiSchemeAgency = "BII";
    private const string ProfileScheme = "Profile";
    private const string GlnSchemeAgency = "9";
    private const string GlnScheme = "GLN";
    private const string VatScheme = "VAT";
    private const string VatSchemeId = "UN/ECE 5153";
    private const string VatSchemeAgency = "6";
    private const string GtinSchemeAgency = "6";
    private const string GtinScheme = "GTIN";
    private const string OrgNrScheme = "SE:ORGNR";

    private readonly UblDocumentOptions _defaultOptions;

    public UblService(UblDocumentOptions? options = null)
    {
        _defaultOptions = options ?? new UblDocumentOptions();
    }

    /// <summary>
    /// Creates a UBL Order document from simplified order model and saves to file
    /// </summary>
    public async Task CreateOrderAsync(UblOrder order, string outputFilePath, UblDocumentOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFilePath);

        try
        {
            var doc = await Task.Run(() => BuildUblOrder(order, options ?? _defaultOptions));

            // Ensure output directory exists
            var directory = Path.GetDirectoryName(outputFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            doc.Save(outputFilePath);
            Debug.WriteLine($"Successfully created UBL order document: {outputFilePath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to create UBL order document: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Creates a UBL Order document from simplified order model and returns as XML string
    /// </summary>
    public async Task<string> CreateOrderXmlAsync(UblOrder order, UblDocumentOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(order);

        try
        {
            var doc = await Task.Run(() => BuildUblOrder(order, options ?? _defaultOptions));

            using var stream = new MemoryStream();
            var serializer = doc.GetSerializer();
            serializer.Serialize(stream, doc);

            stream.Position = 0;
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to create UBL order XML: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Creates a UBL Order document and returns the UblSharp OrderType for further manipulation
    /// </summary>
    public async Task<OrderType> CreateOrderDocumentAsync(UblOrder order, UblDocumentOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(order);

        try
        {
            return await Task.Run(() => BuildUblOrder(order, options ?? _defaultOptions));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to create UBL order document: {ex.Message}");
            throw;
        }
    }

    private OrderType BuildUblOrder(UblOrder order, UblDocumentOptions options)
    {
        var doc = new OrderType
        {
            UBLVersionID = options.UblVersion,
            CustomizationID = options.CustomizationId,
            ProfileID = CreateProfileId(options.ProfileId),
            ID = order.Id,
            IssueDate = order.IssueDate.ToString("yyyy-MM-dd"),
            IssueTime = order.IssueDate.ToString("HH:mm:ss"),
            DocumentCurrencyCode = order.Currency,
            AccountingCostCode = order.AccountingCostCode,
        };

        // Add note if provided
        if (!string.IsNullOrWhiteSpace(order.Note))
        {
            doc.Note = [new TextType { Value = order.Note }];
        }

        // Add validity period if provided
        if (order.ValidityEndDate.HasValue)
        {
            doc.ValidityPeriod = [new PeriodType { EndDate = order.ValidityEndDate.Value.ToString("yyyy-MM-dd") }];
        }

        // Add document references
        if (!string.IsNullOrWhiteSpace(order.QuotationId))
        {
            doc.QuotationDocumentReference = new DocumentReferenceType { ID = order.QuotationId };
        }

        // Convert parties
        doc.BuyerCustomerParty = ConvertToCustomerParty(order.Buyer);
        doc.SellerSupplierParty = ConvertToSupplierParty(order.Seller);

        // Add delivery information
        if (order.Delivery is not null)
        {
            doc.Delivery = [ConvertToDelivery(order.Delivery)];
        }

        // Add allowances and charges
        if (order.AllowancesCharges.Count > 0)
        {
            doc.AllowanceCharge = order.AllowancesCharges.Select(ConvertToAllowanceCharge).ToList();
        }

        // Add monetary totals
        if (order.Totals is not null)
        {
            doc.AnticipatedMonetaryTotal = ConvertToMonetaryTotal(order.Totals, order.Currency);
        }

        // Add order lines
        if (order.Lines.Count > 0)
        {
            doc.OrderLine = order.Lines.Select(ConvertToOrderLine).ToList();
        }

        // Set namespaces if requested
        if (options.IncludeNamespaces)
        {
            doc.Xmlns = new System.Xml.Serialization.XmlSerializerNamespaces([
                new XmlQualifiedName("cac", CacNamespace),
                new XmlQualifiedName("cbc", CbcNamespace),
            ]);
        }

        return doc;
    }

    private static IdentifierType CreateProfileId(string profileId)
    {
        return new IdentifierType
        {
            schemeAgencyID = BiiSchemeAgency,
            schemeID = ProfileScheme,
            Value = profileId
        };
    }

    private CustomerPartyType ConvertToCustomerParty(UblParty party)
    {
        return new CustomerPartyType
        {
            Party = ConvertToParty(party)
        };
    }

    private SupplierPartyType ConvertToSupplierParty(UblParty party)
    {
        return new SupplierPartyType
        {
            Party = ConvertToParty(party)
        };
    }

    private static PartyType ConvertToParty(UblParty party)
    {
        var partyType = new PartyType();

        // Add endpoint ID if provided
        if (!string.IsNullOrWhiteSpace(party.EndpointId))
        {
            partyType.EndpointID = new IdentifierType
            {
                schemeAgencyID = GlnSchemeAgency,
                schemeID = GlnScheme,
                Value = party.EndpointId
            };
        }

        // Add party identification
        if (!string.IsNullOrWhiteSpace(party.Id))
        {
            partyType.PartyIdentification = [new PartyIdentificationType { ID = party.Id }];
        }

        // Add party name
        if (!string.IsNullOrWhiteSpace(party.Name))
        {
            partyType.PartyName = [new PartyNameType { Name = party.Name }];
        }

        // Add address
        if (party.Address is not null)
        {
            partyType.PostalAddress = ConvertToAddress(party.Address);
        }

        // Add contact
        if (party.Contact is not null)
        {
            partyType.Contact = ConvertToContact(party.Contact);
        }

        // Add main person
        if (party.MainPerson is not null)
        {
            partyType.Person = [ConvertToPerson(party.MainPerson)];
        }

        // Add legal entity if company ID is provided
        if (!string.IsNullOrWhiteSpace(party.CompanyId))
        {
            partyType.PartyLegalEntity = [new PartyLegalEntityType
            {
                RegistrationName = party.Name,
                CompanyID = new IdentifierType
                {
                    schemeID = OrgNrScheme,
                    Value = party.CompanyId
                }
            }];
        }

        // Add tax scheme if tax ID is provided
        if (!string.IsNullOrWhiteSpace(party.TaxId))
        {
            partyType.PartyTaxScheme = [new PartyTaxSchemeType
            {
                RegistrationName = party.Name,
                CompanyID = party.TaxId,
                TaxScheme = new TaxSchemeType
                {
                    ID = new IdentifierType
                    {
                        schemeID = VatSchemeId,
                        schemeAgencyID = VatSchemeAgency,
                        Value = VatScheme
                    }
                }
            }];
        }

        return partyType;
    }

    private static AddressType ConvertToAddress(UblAddress address)
    {
        var addressType = new AddressType
        {
            CityName = address.CityName,
            Country = new CountryType { IdentificationCode = address.CountryCode }
        };

        if (!string.IsNullOrWhiteSpace(address.AddressId))
        {
            addressType.ID = new IdentifierType
            {
                schemeAgencyID = GlnSchemeAgency,
                schemeID = GlnScheme,
                Value = address.AddressId
            };
        }

        if (!string.IsNullOrWhiteSpace(address.PostBox)) addressType.Postbox = address.PostBox;
        if (!string.IsNullOrWhiteSpace(address.StreetName)) addressType.StreetName = address.StreetName;
        if (!string.IsNullOrWhiteSpace(address.AdditionalStreetName)) addressType.AdditionalStreetName = address.AdditionalStreetName;
        if (!string.IsNullOrWhiteSpace(address.BuildingNumber)) addressType.BuildingNumber = address.BuildingNumber;
        if (!string.IsNullOrWhiteSpace(address.Department)) addressType.Department = address.Department;
        if (!string.IsNullOrWhiteSpace(address.PostalZone)) addressType.PostalZone = address.PostalZone;
        if (!string.IsNullOrWhiteSpace(address.CountrySubentity)) addressType.CountrySubentity = address.CountrySubentity;

        return addressType;
    }

    private static ContactType ConvertToContact(UblContact contact)
    {
        var contactType = new ContactType();

        if (!string.IsNullOrWhiteSpace(contact.Name)) contactType.Name = contact.Name;
        if (!string.IsNullOrWhiteSpace(contact.Telephone)) contactType.Telephone = contact.Telephone;
        if (!string.IsNullOrWhiteSpace(contact.Fax)) contactType.Telefax = contact.Fax;
        if (!string.IsNullOrWhiteSpace(contact.Email)) contactType.ElectronicMail = contact.Email;

        return contactType;
    }

    private static PersonType ConvertToPerson(UblPerson person)
    {
        var personType = new PersonType
        {
            FirstName = person.FirstName,
            FamilyName = person.LastName
        };

        if (!string.IsNullOrWhiteSpace(person.MiddleName)) personType.MiddleName = person.MiddleName;
        if (!string.IsNullOrWhiteSpace(person.JobTitle)) personType.JobTitle = person.JobTitle;

        return personType;
    }

    private static DeliveryType ConvertToDelivery(UblDelivery delivery)
    {
        var deliveryType = new DeliveryType();

        if (delivery.DeliveryAddress is not null)
        {
            deliveryType.DeliveryLocation = new LocationType
            {
                Address = ConvertToAddress(delivery.DeliveryAddress)
            };
        }

        if (delivery.RequestedStartDate.HasValue || delivery.RequestedEndDate.HasValue)
        {
            deliveryType.RequestedDeliveryPeriod = new PeriodType();
            if (delivery.RequestedStartDate.HasValue)
                deliveryType.RequestedDeliveryPeriod.StartDate = delivery.RequestedStartDate.Value.ToString("yyyy-MM-dd");
            if (delivery.RequestedEndDate.HasValue)
                deliveryType.RequestedDeliveryPeriod.EndDate = delivery.RequestedEndDate.Value.ToString("yyyy-MM-dd");
        }

        if (delivery.DeliveryParty is not null)
        {
            deliveryType.DeliveryParty = ConvertToParty(delivery.DeliveryParty);
        }

        return deliveryType;
    }

    private static AllowanceChargeType ConvertToAllowanceCharge(UblAllowanceCharge allowanceCharge)
    {
        return new AllowanceChargeType
        {
            ChargeIndicator = allowanceCharge.IsCharge,
            AllowanceChargeReason = [new TextType { Value = allowanceCharge.Reason }],
            Amount = new AmountType
            {
                currencyID = allowanceCharge.Currency,
                Value = allowanceCharge.Amount
            }
        };
    }

    private static MonetaryTotalType ConvertToMonetaryTotal(UblMonetaryTotals totals, string currency)
    {
        var monetaryTotal = new MonetaryTotalType
        {
            LineExtensionAmount = new AmountType { currencyID = currency, Value = totals.LineExtensionAmount },
            PayableAmount = new AmountType { currencyID = currency, Value = totals.PayableAmount }
        };

        if (totals.AllowanceTotalAmount.HasValue)
        {
            monetaryTotal.AllowanceTotalAmount = new AmountType { currencyID = currency, Value = totals.AllowanceTotalAmount.Value };
        }

        if (totals.ChargeTotalAmount.HasValue)
        {
            monetaryTotal.ChargeTotalAmount = new AmountType { currencyID = currency, Value = totals.ChargeTotalAmount.Value };
        }

        return monetaryTotal;
    }

    private OrderLineType ConvertToOrderLine(UblOrderLine line)
    {
        var orderLine = new OrderLineType
        {
            LineItem = new LineItemType
            {
                ID = line.Id,
                Quantity = new QuantityType { unitCode = line.QuantityUnit, Value = line.Quantity },
                LineExtensionAmount = new AmountType { currencyID = _defaultOptions.DefaultCurrency, Value = line.LineTotal },
                PartialDeliveryIndicator = line.AllowPartialDelivery,
                Price = new PriceType
                {
                    PriceAmount = new AmountType { currencyID = _defaultOptions.DefaultCurrency, Value = line.UnitPrice },
                    BaseQuantity = new QuantityType { unitCode = line.QuantityUnit, Value = 1M }
                },
                Item = ConvertToItem(line.Item)
            }
        };

        if (!string.IsNullOrWhiteSpace(line.Note))
        {
            orderLine.Note = [new TextType { Value = line.Note }];
        }

        if (line.TaxAmount.HasValue)
        {
            orderLine.LineItem.TotalTaxAmount = new AmountType { currencyID = _defaultOptions.DefaultCurrency, Value = line.TaxAmount.Value };
        }

        if (!string.IsNullOrWhiteSpace(line.AccountingCostCode))
        {
            orderLine.LineItem.AccountingCostCode = line.AccountingCostCode;
        }

        if (line.SpecificDelivery is not null)
        {
            orderLine.LineItem.Delivery = [ConvertToDelivery(line.SpecificDelivery)];
        }

        return orderLine;
    }

    private static ItemType ConvertToItem(UblItem item)
    {
        var itemType = new ItemType
        {
            Name = item.Name
        };

        if (!string.IsNullOrWhiteSpace(item.Description))
        {
            itemType.Description = [new TextType { Value = item.Description }];
        }

        if (!string.IsNullOrWhiteSpace(item.SellerItemId))
        {
            itemType.SellersItemIdentification = new ItemIdentificationType { ID = item.SellerItemId };
        }

        if (!string.IsNullOrWhiteSpace(item.BuyerItemId))
        {
            itemType.BuyersItemIdentification = new ItemIdentificationType { ID = item.BuyerItemId };
        }

        if (!string.IsNullOrWhiteSpace(item.Gtin))
        {
            itemType.StandardItemIdentification = new ItemIdentificationType
            {
                ID = new IdentifierType
                {
                    schemeAgencyID = GtinSchemeAgency,
                    schemeID = GtinScheme,
                    Value = item.Gtin
                }
            };
        }

        if (item.Properties.Count > 0)
        {
            itemType.AdditionalItemProperty = item.Properties.Select(p => new ItemPropertyType
            {
                Name = p.Name,
                Value = p.Value
            }).ToList();
        }

        return itemType;
    }
}