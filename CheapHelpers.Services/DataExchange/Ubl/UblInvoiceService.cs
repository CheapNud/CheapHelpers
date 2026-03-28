using CheapHelpers.Models.Dtos.Ubl;
using CheapHelpers.Services.DataExchange.Ubl.Validation;
using System.Diagnostics;
using System.Xml;
using UblSharp;
using UblSharp.CommonAggregateComponents;
using UblSharp.UnqualifiedDataTypes;

namespace CheapHelpers.Services.DataExchange.Ubl;

/// <summary>
/// Service for creating PEPPOL BIS 3.0 compliant UBL Invoice and Credit Note documents
/// </summary>
public class UblInvoiceService
{
    // Namespace Constants
    private const string CacNamespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private const string CbcNamespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";

    private readonly UblDocumentOptions _defaultOptions;

    public UblInvoiceService(UblDocumentOptions? options = null)
    {
        _defaultOptions = options ?? new UblDocumentOptions();
    }

    /// <summary>
    /// Creates a PEPPOL BIS 3.0 Invoice document and saves to file
    /// </summary>
    public async Task CreateInvoiceAsync(UblInvoice invoice, string outputFilePath, UblDocumentOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFilePath);

        var activeOptions = options ?? _defaultOptions;

        if (activeOptions.ValidateOnCreate)
        {
            var validationOutcome = PeppolInvoiceValidator.Validate(invoice);
            if (!validationOutcome.IsValid)
                throw new InvalidOperationException($"PEPPOL invoice validation failed: {string.Join("; ", validationOutcome.Errors)}");
        }

        try
        {
            var doc = await Task.Run(() => BuildUblInvoice(invoice, activeOptions));

            var directory = Path.GetDirectoryName(outputFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            doc.Save(outputFilePath);
            Debug.WriteLine($"Successfully created PEPPOL invoice document: {outputFilePath}");
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to create PEPPOL invoice document: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Creates a PEPPOL BIS 3.0 Invoice document and returns as XML string
    /// </summary>
    public async Task<string> CreateInvoiceXmlAsync(UblInvoice invoice, UblDocumentOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        var activeOptions = options ?? _defaultOptions;

        if (activeOptions.ValidateOnCreate)
        {
            var validationOutcome = PeppolInvoiceValidator.Validate(invoice);
            if (!validationOutcome.IsValid)
                throw new InvalidOperationException($"PEPPOL invoice validation failed: {string.Join("; ", validationOutcome.Errors)}");
        }

        try
        {
            var doc = await Task.Run(() => BuildUblInvoice(invoice, activeOptions));

            using var stream = new MemoryStream();
            var serializer = doc.GetSerializer();
            serializer.Serialize(stream, doc);

            stream.Position = 0;
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to create PEPPOL invoice XML: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Creates a PEPPOL BIS 3.0 Credit Note document and saves to file
    /// </summary>
    public async Task CreateCreditNoteAsync(UblCreditNote creditNote, string outputFilePath, UblDocumentOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(creditNote);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFilePath);

        var activeOptions = options ?? _defaultOptions;

        if (activeOptions.ValidateOnCreate)
        {
            var validationOutcome = PeppolInvoiceValidator.Validate(creditNote);
            if (!validationOutcome.IsValid)
                throw new InvalidOperationException($"PEPPOL credit note validation failed: {string.Join("; ", validationOutcome.Errors)}");
        }

        try
        {
            var doc = await Task.Run(() => BuildUblCreditNote(creditNote, activeOptions));

            var directory = Path.GetDirectoryName(outputFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            doc.Save(outputFilePath);
            Debug.WriteLine($"Successfully created PEPPOL credit note document: {outputFilePath}");
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to create PEPPOL credit note document: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Creates a PEPPOL BIS 3.0 Credit Note document and returns as XML string
    /// </summary>
    public async Task<string> CreateCreditNoteXmlAsync(UblCreditNote creditNote, UblDocumentOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(creditNote);

        var activeOptions = options ?? _defaultOptions;

        if (activeOptions.ValidateOnCreate)
        {
            var validationOutcome = PeppolInvoiceValidator.Validate(creditNote);
            if (!validationOutcome.IsValid)
                throw new InvalidOperationException($"PEPPOL credit note validation failed: {string.Join("; ", validationOutcome.Errors)}");
        }

        try
        {
            var doc = await Task.Run(() => BuildUblCreditNote(creditNote, activeOptions));

            using var stream = new MemoryStream();
            var serializer = doc.GetSerializer();
            serializer.Serialize(stream, doc);

            stream.Position = 0;
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to create PEPPOL credit note XML: {ex.Message}");
            throw;
        }
    }

    private InvoiceType BuildUblInvoice(UblInvoice invoice, UblDocumentOptions options)
    {
        var doc = new InvoiceType
        {
            UBLVersionID = PeppolConstants.UblVersionId,
            CustomizationID = PeppolConstants.InvoiceCustomizationId,
            ProfileID = PeppolConstants.ProfileId,
            ID = invoice.Id,
            IssueDate = invoice.IssueDate.ToString("yyyy-MM-dd"),
            InvoiceTypeCode = invoice.InvoiceTypeCode,
            DocumentCurrencyCode = invoice.Currency,
        };

        // Due date
        if (invoice.DueDate.HasValue)
        {
            doc.DueDate = invoice.DueDate.Value.ToString("yyyy-MM-dd");
        }

        // Note
        if (!string.IsNullOrWhiteSpace(invoice.Note))
        {
            doc.Note = [new TextType { Value = invoice.Note }];
        }

        // Buyer reference
        if (!string.IsNullOrWhiteSpace(invoice.BuyerReference))
        {
            doc.BuyerReference = invoice.BuyerReference;
        }

        // Invoice period
        if (invoice.PeriodStartDate.HasValue || invoice.PeriodEndDate.HasValue)
        {
            var invoicePeriod = new PeriodType();
            if (invoice.PeriodStartDate.HasValue)
                invoicePeriod.StartDate = invoice.PeriodStartDate.Value.ToString("yyyy-MM-dd");
            if (invoice.PeriodEndDate.HasValue)
                invoicePeriod.EndDate = invoice.PeriodEndDate.Value.ToString("yyyy-MM-dd");
            doc.InvoicePeriod = [invoicePeriod];
        }

        // Billing reference (preceding document)
        if (!string.IsNullOrWhiteSpace(invoice.BillingReferenceId))
        {
            doc.BillingReference = [new BillingReferenceType
            {
                InvoiceDocumentReference = new DocumentReferenceType
                {
                    ID = invoice.BillingReferenceId
                }
            }];
        }

        // Parties
        doc.AccountingSupplierParty = new SupplierPartyType
        {
            Party = UblPartyMapper.ConvertToPartyWithEndpointScheme(invoice.Seller, invoice.SellerEndpointScheme)
        };
        doc.AccountingCustomerParty = new CustomerPartyType
        {
            Party = UblPartyMapper.ConvertToPartyWithEndpointScheme(invoice.Buyer, invoice.BuyerEndpointScheme)
        };

        // Payment means
        if (invoice.PaymentMeans is not null)
        {
            doc.PaymentMeans = [BuildPaymentMeans(invoice.PaymentMeans)];
        }

        // Document-level allowances and charges
        if (invoice.AllowancesCharges.Count > 0)
        {
            doc.AllowanceCharge = invoice.AllowancesCharges.Select(ac => new AllowanceChargeType
            {
                ChargeIndicator = ac.IsCharge,
                AllowanceChargeReason = [new TextType { Value = ac.Reason }],
                Amount = new AmountType { currencyID = invoice.Currency, Value = ac.Amount }
            }).ToList();
        }

        // Tax total
        doc.TaxTotal = [BuildTaxTotal(invoice.TaxAmount, invoice.TaxSubtotals, invoice.Currency)];

        // Legal monetary total
        doc.LegalMonetaryTotal = BuildLegalMonetaryTotal(invoice.Totals, invoice.Currency);

        // Invoice lines
        doc.InvoiceLine = invoice.Lines.Select(line => BuildInvoiceLine(line, invoice.Currency)).ToList();

        // Namespaces
        if (options.IncludeNamespaces)
        {
            doc.Xmlns = new System.Xml.Serialization.XmlSerializerNamespaces([
                new XmlQualifiedName("cac", CacNamespace),
                new XmlQualifiedName("cbc", CbcNamespace),
            ]);
        }

        return doc;
    }

    private CreditNoteType BuildUblCreditNote(UblCreditNote creditNote, UblDocumentOptions options)
    {
        var doc = new CreditNoteType
        {
            UBLVersionID = PeppolConstants.UblVersionId,
            CustomizationID = PeppolConstants.CreditNoteCustomizationId,
            ProfileID = PeppolConstants.ProfileId,
            ID = creditNote.Id,
            IssueDate = creditNote.IssueDate.ToString("yyyy-MM-dd"),
            CreditNoteTypeCode = creditNote.CreditNoteTypeCode,
            DocumentCurrencyCode = creditNote.Currency,
        };

        // Note
        if (!string.IsNullOrWhiteSpace(creditNote.Note))
        {
            doc.Note = [new TextType { Value = creditNote.Note }];
        }

        // Buyer reference
        if (!string.IsNullOrWhiteSpace(creditNote.BuyerReference))
        {
            doc.BuyerReference = creditNote.BuyerReference;
        }

        // Credit note period
        if (creditNote.PeriodStartDate.HasValue || creditNote.PeriodEndDate.HasValue)
        {
            var creditNotePeriod = new PeriodType();
            if (creditNote.PeriodStartDate.HasValue)
                creditNotePeriod.StartDate = creditNote.PeriodStartDate.Value.ToString("yyyy-MM-dd");
            if (creditNote.PeriodEndDate.HasValue)
                creditNotePeriod.EndDate = creditNote.PeriodEndDate.Value.ToString("yyyy-MM-dd");
            doc.InvoicePeriod = [creditNotePeriod];
        }

        // Billing reference (the original invoice being credited)
        if (!string.IsNullOrWhiteSpace(creditNote.BillingReferenceId))
        {
            doc.BillingReference = [new BillingReferenceType
            {
                InvoiceDocumentReference = new DocumentReferenceType
                {
                    ID = creditNote.BillingReferenceId
                }
            }];
        }

        // Parties
        doc.AccountingSupplierParty = new SupplierPartyType
        {
            Party = UblPartyMapper.ConvertToPartyWithEndpointScheme(creditNote.Seller, creditNote.SellerEndpointScheme)
        };
        doc.AccountingCustomerParty = new CustomerPartyType
        {
            Party = UblPartyMapper.ConvertToPartyWithEndpointScheme(creditNote.Buyer, creditNote.BuyerEndpointScheme)
        };

        // Payment means
        if (creditNote.PaymentMeans is not null)
        {
            doc.PaymentMeans = [BuildPaymentMeans(creditNote.PaymentMeans)];
        }

        // Document-level allowances and charges
        if (creditNote.AllowancesCharges.Count > 0)
        {
            doc.AllowanceCharge = creditNote.AllowancesCharges.Select(ac => new AllowanceChargeType
            {
                ChargeIndicator = ac.IsCharge,
                AllowanceChargeReason = [new TextType { Value = ac.Reason }],
                Amount = new AmountType { currencyID = creditNote.Currency, Value = ac.Amount }
            }).ToList();
        }

        // Tax total
        doc.TaxTotal = [BuildTaxTotal(creditNote.TaxAmount, creditNote.TaxSubtotals, creditNote.Currency)];

        // Legal monetary total
        doc.LegalMonetaryTotal = BuildLegalMonetaryTotal(creditNote.Totals, creditNote.Currency);

        // Credit note lines
        doc.CreditNoteLine = creditNote.Lines.Select(line => BuildCreditNoteLine(line, creditNote.Currency)).ToList();

        // Namespaces
        if (options.IncludeNamespaces)
        {
            doc.Xmlns = new System.Xml.Serialization.XmlSerializerNamespaces([
                new XmlQualifiedName("cac", CacNamespace),
                new XmlQualifiedName("cbc", CbcNamespace),
            ]);
        }

        return doc;
    }

    private static PaymentMeansType BuildPaymentMeans(UblPaymentMeans paymentMeans)
    {
        var paymentMeansType = new PaymentMeansType
        {
            PaymentMeansCode = paymentMeans.PaymentMeansCode
        };

        if (!string.IsNullOrWhiteSpace(paymentMeans.PaymentId))
        {
            paymentMeansType.PaymentID = [paymentMeans.PaymentId];
        }

        if (!string.IsNullOrWhiteSpace(paymentMeans.Iban))
        {
            var financialAccount = new FinancialAccountType
            {
                ID = paymentMeans.Iban
            };

            if (!string.IsNullOrWhiteSpace(paymentMeans.AccountName))
            {
                financialAccount.Name = paymentMeans.AccountName;
            }

            if (!string.IsNullOrWhiteSpace(paymentMeans.Bic))
            {
                financialAccount.FinancialInstitutionBranch = new BranchType
                {
                    ID = paymentMeans.Bic
                };
            }

            paymentMeansType.PayeeFinancialAccount = financialAccount;
        }

        return paymentMeansType;
    }

    private static TaxTotalType BuildTaxTotal(decimal taxAmount, List<UblTaxSubtotal> taxSubtotals, string currency)
    {
        var taxTotal = new TaxTotalType
        {
            TaxAmount = new AmountType { currencyID = currency, Value = taxAmount }
        };

        if (taxSubtotals.Count > 0)
        {
            taxTotal.TaxSubtotal = taxSubtotals.Select(ts => new TaxSubtotalType
            {
                TaxableAmount = new AmountType { currencyID = currency, Value = ts.TaxableAmount },
                TaxAmount = new AmountType { currencyID = currency, Value = ts.TaxAmount },
                TaxCategory = new TaxCategoryType
                {
                    ID = ts.TaxCategory.CategoryCode,
                    Percent = ts.TaxCategory.Percent,
                    TaxExemptionReason = !string.IsNullOrWhiteSpace(ts.TaxCategory.ExemptionReason)
                        ? [new TextType { Value = ts.TaxCategory.ExemptionReason }]
                        : null,
                    TaxScheme = new TaxSchemeType { ID = ts.TaxCategory.TaxSchemeId }
                }
            }).ToList();
        }

        return taxTotal;
    }

    private static MonetaryTotalType BuildLegalMonetaryTotal(UblMonetaryTotals totals, string currency)
    {
        var monetaryTotal = new MonetaryTotalType
        {
            LineExtensionAmount = new AmountType { currencyID = currency, Value = totals.LineExtensionAmount },
            PayableAmount = new AmountType { currencyID = currency, Value = totals.PayableAmount }
        };

        if (totals.TaxAmount.HasValue)
        {
            monetaryTotal.TaxExclusiveAmount = new AmountType { currencyID = currency, Value = totals.LineExtensionAmount };
            monetaryTotal.TaxInclusiveAmount = new AmountType { currencyID = currency, Value = totals.LineExtensionAmount + totals.TaxAmount.Value };
        }

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

    private static InvoiceLineType BuildInvoiceLine(UblInvoiceLine line, string currency)
    {
        var invoiceLine = new InvoiceLineType
        {
            ID = line.Id,
            InvoicedQuantity = new QuantityType { unitCode = line.QuantityUnit, Value = line.Quantity },
            LineExtensionAmount = new AmountType { currencyID = currency, Value = line.LineTotal },
            Price = new PriceType
            {
                PriceAmount = new AmountType { currencyID = currency, Value = line.UnitPrice },
                BaseQuantity = new QuantityType { unitCode = line.QuantityUnit, Value = 1M }
            },
            Item = BuildLineItem(line.Item, line.TaxCategory)
        };

        if (!string.IsNullOrWhiteSpace(line.Note))
        {
            invoiceLine.Note = [new TextType { Value = line.Note }];
        }

        if (line.AllowancesCharges.Count > 0)
        {
            invoiceLine.AllowanceCharge = line.AllowancesCharges.Select(ac => new AllowanceChargeType
            {
                ChargeIndicator = ac.IsCharge,
                AllowanceChargeReason = [new TextType { Value = ac.Reason }],
                Amount = new AmountType { currencyID = currency, Value = ac.Amount }
            }).ToList();
        }

        return invoiceLine;
    }

    private static CreditNoteLineType BuildCreditNoteLine(UblInvoiceLine line, string currency)
    {
        var creditNoteLine = new CreditNoteLineType
        {
            ID = line.Id,
            CreditedQuantity = new QuantityType { unitCode = line.QuantityUnit, Value = line.Quantity },
            LineExtensionAmount = new AmountType { currencyID = currency, Value = line.LineTotal },
            Price = new PriceType
            {
                PriceAmount = new AmountType { currencyID = currency, Value = line.UnitPrice },
                BaseQuantity = new QuantityType { unitCode = line.QuantityUnit, Value = 1M }
            },
            Item = BuildLineItem(line.Item, line.TaxCategory)
        };

        if (!string.IsNullOrWhiteSpace(line.Note))
        {
            creditNoteLine.Note = [new TextType { Value = line.Note }];
        }

        if (line.AllowancesCharges.Count > 0)
        {
            creditNoteLine.AllowanceCharge = line.AllowancesCharges.Select(ac => new AllowanceChargeType
            {
                ChargeIndicator = ac.IsCharge,
                AllowanceChargeReason = [new TextType { Value = ac.Reason }],
                Amount = new AmountType { currencyID = currency, Value = ac.Amount }
            }).ToList();
        }

        return creditNoteLine;
    }

    private static ItemType BuildLineItem(UblItem item, UblTaxCategory taxCategory)
    {
        var itemType = new ItemType
        {
            Name = item.Name,
            ClassifiedTaxCategory = [new TaxCategoryType
            {
                ID = taxCategory.CategoryCode,
                Percent = taxCategory.Percent,
                TaxScheme = new TaxSchemeType { ID = taxCategory.TaxSchemeId }
            }]
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
                    schemeID = "0160",
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
