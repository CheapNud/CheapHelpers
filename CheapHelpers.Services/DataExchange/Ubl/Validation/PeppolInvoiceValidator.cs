using System.Diagnostics;
using System.Text.RegularExpressions;
using CheapHelpers.Models.Dtos.Ubl;

namespace CheapHelpers.Services.DataExchange.Ubl.Validation;

/// <summary>
/// Validates UblInvoice and UblCreditNote DTOs against PEPPOL BIS 3.0 requirements
/// before XML generation.
/// </summary>
public static partial class PeppolInvoiceValidator
{
    private const decimal TaxToleranceAmount = 0.02m;

    [GeneratedRegex(@"^\d{10}$")]
    private static partial Regex BelgianEnterpriseNumberPattern();

    [GeneratedRegex(@"^BE\d{10}$")]
    private static partial Regex BelgianVatNumberPattern();

    /// <summary>
    /// Validates a UblInvoice DTO for PEPPOL BIS 3.0 compliance
    /// </summary>
    public static PeppolValidationResult Validate(UblInvoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        List<string> validationErrors = [];

        ValidateDocumentHeader(invoice.Id, invoice.IssueDate, validationErrors);
        ValidateParty(invoice.Seller, "Seller", invoice.SellerEndpointScheme, validationErrors);
        ValidateParty(invoice.Buyer, "Buyer", invoice.BuyerEndpointScheme, validationErrors);
        ValidateInvoiceLines(invoice.Lines, validationErrors);
        ValidateTaxConsistency(invoice.Lines, invoice.TaxSubtotals, invoice.TaxAmount, validationErrors);

        if (validationErrors.Count > 0)
        {
            Debug.WriteLine($"PEPPOL invoice validation failed with {validationErrors.Count} error(s)");
            return PeppolValidationResult.Failed(validationErrors);
        }

        Debug.WriteLine("PEPPOL invoice validation passed");
        return PeppolValidationResult.Success();
    }

    /// <summary>
    /// Validates a UblCreditNote DTO for PEPPOL BIS 3.0 compliance
    /// </summary>
    public static PeppolValidationResult Validate(UblCreditNote creditNote)
    {
        ArgumentNullException.ThrowIfNull(creditNote);

        List<string> validationErrors = [];

        ValidateDocumentHeader(creditNote.Id, creditNote.IssueDate, validationErrors);
        ValidateParty(creditNote.Seller, "Seller", creditNote.SellerEndpointScheme, validationErrors);
        ValidateParty(creditNote.Buyer, "Buyer", creditNote.BuyerEndpointScheme, validationErrors);
        ValidateInvoiceLines(creditNote.Lines, validationErrors);
        ValidateTaxConsistency(creditNote.Lines, creditNote.TaxSubtotals, creditNote.TaxAmount, validationErrors);

        if (validationErrors.Count > 0)
        {
            Debug.WriteLine($"PEPPOL credit note validation failed with {validationErrors.Count} error(s)");
            return PeppolValidationResult.Failed(validationErrors);
        }

        Debug.WriteLine("PEPPOL credit note validation passed");
        return PeppolValidationResult.Success();
    }

    private static void ValidateDocumentHeader(string documentId, DateTime issueDate, List<string> validationErrors)
    {
        if (string.IsNullOrWhiteSpace(documentId))
            validationErrors.Add("Document ID is required");

        if (issueDate == default)
            validationErrors.Add("Issue date is required");
    }

    private static void ValidateParty(UblParty party, string partyRole, string? endpointScheme, List<string> validationErrors)
    {
        if (string.IsNullOrWhiteSpace(party.Name))
            validationErrors.Add($"{partyRole} name is required");

        if (string.IsNullOrWhiteSpace(party.EndpointId))
            validationErrors.Add($"{partyRole} endpoint ID is required");

        // Belgian enterprise number format validation (10 digits when scheme is 0208)
        if (endpointScheme == PeppolConstants.BelgianEnterpriseScheme
            && !string.IsNullOrWhiteSpace(party.EndpointId)
            && !BelgianEnterpriseNumberPattern().IsMatch(party.EndpointId))
        {
            validationErrors.Add($"{partyRole} Belgian enterprise number must be exactly 10 digits (scheme {PeppolConstants.BelgianEnterpriseScheme})");
        }

        // Belgian VAT format validation (BE + 10 digits)
        if (!string.IsNullOrWhiteSpace(party.TaxId)
            && party.TaxId.StartsWith("BE", StringComparison.OrdinalIgnoreCase)
            && !BelgianVatNumberPattern().IsMatch(party.TaxId))
        {
            validationErrors.Add($"{partyRole} Belgian VAT number must match format BE + 10 digits (e.g. BE0123456789)");
        }
    }

    private static void ValidateInvoiceLines(List<UblInvoiceLine> lines, List<string> validationErrors)
    {
        if (lines.Count == 0)
        {
            validationErrors.Add("At least one invoice line is required");
            return;
        }

        for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            var line = lines[lineIndex];
            var lineLabel = $"Line {lineIndex + 1} (ID: {line.Id})";

            if (string.IsNullOrWhiteSpace(line.Id))
                validationErrors.Add($"Line {lineIndex + 1}: Line ID is required");

            if (line.TaxCategory is null)
                validationErrors.Add($"{lineLabel}: Tax category is required");
        }
    }

    private static void ValidateTaxConsistency(
        List<UblInvoiceLine> lines,
        List<UblTaxSubtotal> taxSubtotals,
        decimal documentTaxAmount,
        List<string> validationErrors)
    {
        if (taxSubtotals.Count == 0)
            return;

        // Sum of tax subtotal amounts should approximate the document-level tax amount
        var subtotalTaxSum = taxSubtotals.Sum(ts => ts.TaxAmount);
        var taxDifference = Math.Abs(subtotalTaxSum - documentTaxAmount);

        if (taxDifference > TaxToleranceAmount)
        {
            validationErrors.Add(
                $"Tax total inconsistency: sum of tax subtotals ({subtotalTaxSum:F2}) " +
                $"differs from document tax amount ({documentTaxAmount:F2}) by {taxDifference:F2}, " +
                $"which exceeds the tolerance of {TaxToleranceAmount:F2}");
        }
    }
}
