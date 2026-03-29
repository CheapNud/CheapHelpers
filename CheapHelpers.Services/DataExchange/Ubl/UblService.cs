using CheapHelpers.Models.Dtos.Ubl;
using System.Diagnostics;
using System.Xml;
using UblSharp;
using UblSharp.CommonAggregateComponents;
using UblSharp.UnqualifiedDataTypes;

namespace CheapHelpers.Services.DataExchange.Ubl;

public class UblService
{
    // Namespace Constants
    private const string CacNamespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private const string CbcNamespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";

    // Scheme Constants
    private const string BiiSchemeAgency = "BII";
    private const string ProfileScheme = "Profile";
    private const string GtinSchemeAgency = "6";
    private const string GtinScheme = "GTIN";

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

    private static CustomerPartyType ConvertToCustomerParty(UblParty party)
    {
        return new CustomerPartyType
        {
            Party = UblPartyMapper.ConvertToParty(party)
        };
    }

    private static SupplierPartyType ConvertToSupplierParty(UblParty party)
    {
        return new SupplierPartyType
        {
            Party = UblPartyMapper.ConvertToParty(party)
        };
    }

    private static DeliveryType ConvertToDelivery(UblDelivery delivery)
    {
        var deliveryType = new DeliveryType();

        if (delivery.DeliveryAddress is not null)
        {
            deliveryType.DeliveryLocation = new LocationType
            {
                Address = UblPartyMapper.ConvertToAddress(delivery.DeliveryAddress)
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
            deliveryType.DeliveryParty = UblPartyMapper.ConvertToParty(delivery.DeliveryParty);
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