using CheapHelpers.Models.Dtos.Ubl;

namespace CheapHelpers.Services.DataExchange.Ubl;

/// <summary>
/// Fluent builder for creating PEPPOL BIS 3.0 invoices without direct UBL model exposure.
/// Handles tax calculations, monetary totals, and line numbering automatically.
/// <para>
/// For advanced scenarios requiring full UBL control (custom tax schemes, allowances/charges,
/// billing references), construct <see cref="UblInvoice"/> directly.
/// </para>
/// </summary>
public class InvoiceBuilder
{
    private string _id = string.Empty;
    private DateTime _issueDate;
    private DateTime? _absoluteDueDate;
    private int? _relativeDueDays;
    private string _currency = "EUR";
    private string? _note;
    private string? _billingReference;
    private UblParty _seller = new();
    private UblParty _buyer = new();
    private UblPaymentMeans? _paymentMeans;
    private readonly List<LineItem> _lines = [];

    /// <summary>
    /// Creates a new invoice builder with the given invoice number.
    /// </summary>
    public static InvoiceBuilder Create(string invoiceNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceNumber);
        return new InvoiceBuilder { _id = invoiceNumber, _issueDate = DateTime.UtcNow };
    }

    /// <summary>
    /// Sets the invoice date (defaults to <see cref="DateTime.UtcNow"/>).
    /// </summary>
    public InvoiceBuilder IssuedOn(DateTime date)
    {
        _issueDate = date;
        return this;
    }

    /// <summary>
    /// Sets an absolute payment due date.
    /// </summary>
    public InvoiceBuilder DueOn(DateTime date)
    {
        _absoluteDueDate = date;
        _relativeDueDays = null;
        return this;
    }

    /// <summary>
    /// Sets the due date relative to the issue date. Computed at <see cref="Build"/> time,
    /// so call order with <see cref="IssuedOn"/> does not matter.
    /// </summary>
    public InvoiceBuilder DueIn(int days)
    {
        _relativeDueDays = days;
        _absoluteDueDate = null;
        return this;
    }

    /// <summary>
    /// Sets the currency (defaults to EUR).
    /// </summary>
    public InvoiceBuilder InCurrency(string currencyCode)
    {
        _currency = currencyCode;
        return this;
    }

    /// <summary>
    /// Adds a note to the invoice.
    /// </summary>
    public InvoiceBuilder WithNote(string note)
    {
        _note = note;
        return this;
    }

    /// <summary>
    /// References an original invoice (for credit notes or corrections).
    /// </summary>
    public InvoiceBuilder ReferencingInvoice(string originalInvoiceId)
    {
        _billingReference = originalInvoiceId;
        return this;
    }

    /// <summary>
    /// Sets the seller (your company). The electronic address for e-invoicing
    /// is derived automatically from the VAT number.
    /// </summary>
    /// <param name="companyName">Company or trade name.</param>
    /// <param name="vatNumber">VAT number including country prefix (e.g., "BE0123456789").</param>
    public InvoiceBuilder From(string companyName, string? vatNumber = null)
    {
        _seller = new UblParty
        {
            Name = companyName,
            TaxId = vatNumber,
            EndpointId = DeriveEndpointFromVat(vatNumber),
        };
        return this;
    }

    /// <summary>
    /// Sets the seller with full address details.
    /// </summary>
    public InvoiceBuilder From(string companyName, string vatNumber, string street, string city, string postalCode, string countryCode = "BE")
    {
        _seller = new UblParty
        {
            Name = companyName,
            TaxId = vatNumber,
            EndpointId = DeriveEndpointFromVat(vatNumber),
            Address = new UblAddress
            {
                StreetName = street,
                CityName = city,
                PostalZone = postalCode,
                CountryCode = countryCode
            }
        };
        return this;
    }

    /// <summary>
    /// Sets the seller from a pre-built <see cref="UblParty"/>.
    /// Use this for advanced PEPPOL scenarios where you need full control over
    /// the electronic address (endpoint ID), tax schemes, or legal entity fields.
    /// </summary>
    public InvoiceBuilder From(UblParty seller)
    {
        _seller = seller;
        return this;
    }

    /// <summary>
    /// Sets the buyer (customer). The electronic address for e-invoicing
    /// is derived automatically from the VAT number.
    /// </summary>
    /// <param name="customerName">Customer or trade name.</param>
    /// <param name="vatNumber">VAT number including country prefix (e.g., "BE9876543210").</param>
    public InvoiceBuilder To(string customerName, string? vatNumber = null)
    {
        _buyer = new UblParty
        {
            Name = customerName,
            TaxId = vatNumber,
            EndpointId = DeriveEndpointFromVat(vatNumber),
        };
        return this;
    }

    /// <summary>
    /// Sets the buyer with full address details.
    /// </summary>
    public InvoiceBuilder To(string customerName, string vatNumber, string street, string city, string postalCode, string countryCode = "BE")
    {
        _buyer = new UblParty
        {
            Name = customerName,
            TaxId = vatNumber,
            EndpointId = DeriveEndpointFromVat(vatNumber),
            Address = new UblAddress
            {
                StreetName = street,
                CityName = city,
                PostalZone = postalCode,
                CountryCode = countryCode
            }
        };
        return this;
    }

    /// <summary>
    /// Sets the buyer from a pre-built UblParty (for advanced scenarios).
    /// </summary>
    public InvoiceBuilder To(UblParty buyer)
    {
        _buyer = buyer;
        return this;
    }

    /// <summary>
    /// Adds a line item. Tax and line total are calculated automatically.
    /// </summary>
    /// <param name="description">What is being invoiced.</param>
    /// <param name="quantity">Number of units.</param>
    /// <param name="unitPrice">Price per unit (excl. tax).</param>
    /// <param name="vatPercent">VAT rate in percent (e.g., 21 for Belgian standard rate). Use 0 for zero-rated.</param>
    public InvoiceBuilder AddLine(string description, decimal quantity, decimal unitPrice, decimal vatPercent = 21)
    {
        _lines.Add(new LineItem(description, quantity, unitPrice, vatPercent));
        return this;
    }

    /// <summary>
    /// Sets the payment details (bank transfer).
    /// </summary>
    /// <param name="iban">IBAN account number.</param>
    /// <param name="bic">BIC/SWIFT code (optional).</param>
    /// <param name="paymentReference">Structured payment reference (e.g., Belgian +++OGM+++ format).</param>
    public InvoiceBuilder WithPayment(string iban, string? bic = null, string? paymentReference = null)
    {
        _paymentMeans = new UblPaymentMeans
        {
            PaymentMeansCode = bic is not null ? "58" : "30", // SEPA if BIC provided
            PaymentId = paymentReference,
            PayeeFinancialAccount = new UblFinancialAccount
            {
                Id = iban,
                FinancialInstitutionBranch = bic
            }
        };
        return this;
    }

    /// <summary>
    /// Builds the <see cref="UblInvoice"/> with all tax and monetary totals calculated automatically.
    /// </summary>
    public UblInvoice Build()
    {
        if (_lines.Count == 0)
            throw new InvalidOperationException("Invoice must have at least one line item.");

        // Build invoice lines with auto-numbering and tax calculation
        var invoiceLines = _lines.Select((line, index) => new UblInvoiceLine
        {
            Id = (index + 1).ToString(),
            Item = new UblItem { Name = line.Description },
            Quantity = line.Quantity,
            UnitPrice = line.UnitPrice,
            LineTotal = Math.Round(line.Quantity * line.UnitPrice, 2),
            TaxCategory = new UblTaxCategory
            {
                Id = line.VatPercent == 0 ? "Z" : "S",
                Percent = line.VatPercent
            }
        }).ToList();

        // Calculate totals
        var lineExtensionAmount = invoiceLines.Sum(l => l.LineTotal);

        // Group by VAT rate for tax subtotals
        var taxSubtotals = invoiceLines
            .GroupBy(l => l.TaxCategory.Percent)
            .Select(g => new UblTaxSubtotal
            {
                TaxableAmount = g.Sum(l => l.LineTotal),
                TaxAmount = Math.Round(g.Sum(l => l.LineTotal) * g.Key / 100, 2),
                TaxCategory = new UblTaxCategory
                {
                    Id = g.Key == 0 ? "Z" : "S",
                    Percent = g.Key
                }
            }).ToList();

        var totalTax = taxSubtotals.Sum(t => t.TaxAmount);
        var payableAmount = lineExtensionAmount + totalTax;

        // Resolve due date — relative days computed against final issue date
        var resolvedDueDate = _absoluteDueDate ?? (_relativeDueDays.HasValue ? _issueDate.AddDays(_relativeDueDays.Value) : (DateTime?)null);

        return new UblInvoice
        {
            Id = _id,
            IssueDate = _issueDate,
            DueDate = resolvedDueDate,
            Currency = _currency,
            Note = _note,
            BillingReference = _billingReference,
            Seller = _seller,
            Buyer = _buyer,
            PaymentMeans = _paymentMeans,
            Lines = invoiceLines,
            TaxTotal = new UblTaxTotal
            {
                TaxAmount = totalTax,
                TaxSubtotals = taxSubtotals
            },
            Totals = new UblMonetaryTotals
            {
                LineExtensionAmount = lineExtensionAmount,
                TaxAmount = totalTax,
                PayableAmount = payableAmount
            }
        };
    }

    private static string? DeriveEndpointFromVat(string? vatNumber) =>
        VatHelper.DeriveEndpointFromVat(vatNumber);

    private record LineItem(string Description, decimal Quantity, decimal UnitPrice, decimal VatPercent);
}
