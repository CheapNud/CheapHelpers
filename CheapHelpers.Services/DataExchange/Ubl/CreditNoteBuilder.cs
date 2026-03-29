using CheapHelpers.Models.Dtos.Ubl;

namespace CheapHelpers.Services.DataExchange.Ubl;

/// <summary>
/// Fluent builder for creating PEPPOL BIS 3.0 credit notes.
/// Mirrors <see cref="InvoiceBuilder"/> but for crediting/correcting an original invoice.
/// </summary>
public class CreditNoteBuilder
{
    private string _id = string.Empty;
    private DateTime _issueDate;
    private string _currency = "EUR";
    private string? _note;
    private string? _creditReason;
    private readonly List<string> _billingReferences = [];
    private UblParty _seller = new();
    private UblParty _buyer = new();
    private UblPaymentMeans? _paymentMeans;
    private readonly List<LineItem> _lines = [];

    public static CreditNoteBuilder Create(string creditNoteNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(creditNoteNumber);
        return new CreditNoteBuilder { _id = creditNoteNumber, _issueDate = DateTime.UtcNow };
    }

    public CreditNoteBuilder IssuedOn(DateTime date) { _issueDate = date; return this; }
    public CreditNoteBuilder InCurrency(string currencyCode) { _currency = currencyCode; return this; }
    public CreditNoteBuilder WithNote(string note) { _note = note; return this; }
    public CreditNoteBuilder WithReason(string reason) { _creditReason = reason; return this; }

    /// <summary>
    /// References the original invoice being credited. Can be called multiple times.
    /// </summary>
    public CreditNoteBuilder CreditsInvoice(string originalInvoiceId)
    {
        _billingReferences.Add(originalInvoiceId);
        return this;
    }

    /// <inheritdoc cref="InvoiceBuilder.From(string, string?)"/>
    public CreditNoteBuilder From(string companyName, string? vatNumber = null)
    {
        _seller = new UblParty { Name = companyName, TaxId = vatNumber, EndpointId = DeriveEndpointFromVat(vatNumber) };
        return this;
    }

    /// <inheritdoc cref="InvoiceBuilder.From(UblParty)"/>
    public CreditNoteBuilder From(UblParty seller) { _seller = seller; return this; }

    /// <inheritdoc cref="InvoiceBuilder.To(string, string?)"/>
    public CreditNoteBuilder To(string customerName, string? vatNumber = null)
    {
        _buyer = new UblParty { Name = customerName, TaxId = vatNumber, EndpointId = DeriveEndpointFromVat(vatNumber) };
        return this;
    }

    /// <inheritdoc cref="InvoiceBuilder.To(UblParty)"/>
    public CreditNoteBuilder To(UblParty buyer) { _buyer = buyer; return this; }

    public CreditNoteBuilder AddLine(string description, decimal quantity, decimal unitPrice, decimal vatPercent = 21)
    {
        _lines.Add(new LineItem(description, quantity, unitPrice, vatPercent));
        return this;
    }

    public CreditNoteBuilder WithPayment(string iban, string? bic = null, string? paymentReference = null)
    {
        _paymentMeans = new UblPaymentMeans
        {
            PaymentMeansCode = bic is not null ? "58" : "30",
            PaymentId = paymentReference,
            PayeeFinancialAccount = new UblFinancialAccount { Id = iban, FinancialInstitutionBranch = bic }
        };
        return this;
    }

    public UblCreditNote Build()
    {
        if (_lines.Count == 0)
            throw new InvalidOperationException("Credit note must have at least one line item.");

        if (_billingReferences.Count == 0)
            throw new InvalidOperationException("Credit note must reference at least one original invoice.");

        var creditNoteLines = _lines.Select((line, index) => new UblInvoiceLine
        {
            Id = (index + 1).ToString(),
            Item = new UblItem { Name = line.Description },
            Quantity = line.Quantity,
            UnitPrice = line.UnitPrice,
            LineTotal = Math.Round(line.Quantity * line.UnitPrice, 2),
            TaxCategory = new UblTaxCategory { Id = line.VatPercent == 0 ? "Z" : "S", Percent = line.VatPercent }
        }).ToList();

        var lineExtensionAmount = creditNoteLines.Sum(l => l.LineTotal);
        var taxSubtotals = creditNoteLines
            .GroupBy(l => l.TaxCategory.Percent)
            .Select(g => new UblTaxSubtotal
            {
                TaxableAmount = g.Sum(l => l.LineTotal),
                TaxAmount = Math.Round(g.Sum(l => l.LineTotal) * g.Key / 100, 2),
                TaxCategory = new UblTaxCategory { Id = g.Key == 0 ? "Z" : "S", Percent = g.Key }
            }).ToList();

        var totalTax = Math.Round(taxSubtotals.Sum(t => t.TaxAmount), 2);

        return new UblCreditNote
        {
            Id = _id,
            IssueDate = _issueDate,
            Currency = _currency,
            Note = _note,
            CreditReason = _creditReason,
            BillingReferences = _billingReferences,
            Seller = _seller,
            Buyer = _buyer,
            PaymentMeans = _paymentMeans,
            Lines = creditNoteLines,
            TaxTotal = new UblTaxTotal { TaxAmount = totalTax, TaxSubtotals = taxSubtotals },
            Totals = new UblMonetaryTotals
            {
                LineExtensionAmount = lineExtensionAmount,
                TaxAmount = totalTax,
                PayableAmount = Math.Round(lineExtensionAmount + totalTax, 2)
            }
        };
    }

    private static string? DeriveEndpointFromVat(string? vatNumber) =>
        VatHelper.DeriveEndpointFromVat(vatNumber);

    private record LineItem(string Description, decimal Quantity, decimal UnitPrice, decimal VatPercent);
}
