using CheapHelpers.Models.Entities;

namespace CheapHelpers.Services.Billing;

/// <summary>
/// Generates invoices, manages billing cycles, and handles invoice lifecycle for API key usage.
/// </summary>
public interface IBillingService
{
    /// <summary>
    /// Generates an invoice for the specified API key and billing plan over the given period.
    /// </summary>
    Task<BillingInvoice> GenerateInvoiceAsync(int apiKeyId, int billingPlanId, DateTime periodStart, DateTime periodEnd, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a single invoice by its identifier.
    /// </summary>
    Task<BillingInvoice?> GetInvoiceAsync(int invoiceId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all invoices associated with the specified API key, most recent first.
    /// </summary>
    Task<List<BillingInvoice>> GetInvoicesForKeyAsync(int apiKeyId, CancellationToken ct = default);

    /// <summary>
    /// Marks an invoice as paid. Returns false if the invoice was not found.
    /// </summary>
    Task<bool> MarkAsPaidAsync(int invoiceId, CancellationToken ct = default);

    /// <summary>
    /// Cancels an invoice. Returns false if the invoice was not found.
    /// </summary>
    Task<bool> CancelInvoiceAsync(int invoiceId, CancellationToken ct = default);

    /// <summary>
    /// Runs the automated billing cycle: aggregates usage and generates invoices for all active API keys with billing plans.
    /// </summary>
    Task RunBillingCycleAsync(CancellationToken ct = default);
}
