using CheapHelpers.EF;
using CheapHelpers.Models.Entities;
using CheapHelpers.Models.Enums;
using CheapHelpers.Services.Billing.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Services.Billing;

/// <summary>
/// Generates invoices, manages billing cycles, and handles invoice lifecycle for API key usage.
/// Uses <c>dbContext.Set&lt;T&gt;()</c> so it compiles before DbSet properties are added to CheapContext.
/// </summary>
public class BillingService<TUser>(
    CheapBusinessContext<TUser> dbContext,
    IUsageMeterService usageMeter,
    BillingOptions billingOptions,
    ILogger<BillingService<TUser>> logger) : IBillingService
    where TUser : IdentityUser
{
    /// <inheritdoc />
    public async Task<BillingInvoice> GenerateInvoiceAsync(
        int apiKeyId,
        int billingPlanId,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken ct = default)
    {
        // 1. Get or compute usage aggregate for the period
        var aggregate = await usageMeter.GetUsageAggregateAsync(apiKeyId, periodStart, periodEnd, ct);
        var totalRequests = aggregate?.TotalRequests ?? 0;

        // 2. Load the billing plan
        var billingPlan = await dbContext.Set<BillingPlan>().FindAsync([billingPlanId], ct)
            ?? throw new InvalidOperationException($"BillingPlan with Id {billingPlanId} not found.");

        // 3. Calculate overage
        var overageUnits = Math.Max(0, totalRequests - billingPlan.IncludedUnits);
        var subTotal = overageUnits * billingPlan.RatePerUnit;

        // 4. Calculate tax
        var taxAmount = subTotal * (billingOptions.DefaultTaxRate / 100m);

        // 5. Generate sequential invoice number
        var existingInvoiceCount = await dbContext.Set<BillingInvoice>().CountAsync(ct);
        var nextSequential = existingInvoiceCount + 1;
        var invoiceNumber = $"{billingOptions.InvoiceNumberPrefix}-{DateTime.UtcNow:yyyy}-{nextSequential:D6}";

        // 6. Create and persist the invoice
        var billingInvoice = new BillingInvoice
        {
            ApiKeyId = apiKeyId,
            BillingPlanId = billingPlanId,
            InvoiceNumber = invoiceNumber,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TotalUnits = totalRequests,
            IncludedUnits = billingPlan.IncludedUnits,
            OverageUnits = overageUnits,
            SubTotal = subTotal,
            TaxAmount = taxAmount,
            TotalAmount = subTotal + taxAmount,
            Currency = billingOptions.DefaultCurrency,
            Status = InvoiceStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Set<BillingInvoice>().Add(billingInvoice);
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation(
            "Generated invoice {InvoiceNumber} for ApiKeyId {ApiKeyId}: {TotalRequests} requests, {OverageUnits} overage, total {TotalAmount} {Currency}",
            invoiceNumber, apiKeyId, totalRequests, overageUnits, billingInvoice.TotalAmount, billingInvoice.Currency);

        return billingInvoice;
    }

    /// <inheritdoc />
    public async Task<BillingInvoice?> GetInvoiceAsync(int invoiceId, CancellationToken ct = default)
    {
        return await dbContext.Set<BillingInvoice>().FindAsync([invoiceId], ct);
    }

    /// <inheritdoc />
    public async Task<List<BillingInvoice>> GetInvoicesForKeyAsync(int apiKeyId, CancellationToken ct = default)
    {
        return await dbContext.Set<BillingInvoice>()
            .Where(inv => inv.ApiKeyId == apiKeyId)
            .OrderByDescending(inv => inv.CreatedAt)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<bool> MarkAsPaidAsync(int invoiceId, CancellationToken ct = default)
    {
        var invoice = await dbContext.Set<BillingInvoice>().FindAsync([invoiceId], ct);

        if (invoice is null)
            return false;

        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Invoice {InvoiceNumber} (Id {InvoiceId}) marked as paid", invoice.InvoiceNumber, invoiceId);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> CancelInvoiceAsync(int invoiceId, CancellationToken ct = default)
    {
        var invoice = await dbContext.Set<BillingInvoice>().FindAsync([invoiceId], ct);

        if (invoice is null)
            return false;

        invoice.Status = InvoiceStatus.Cancelled;

        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Invoice {InvoiceNumber} (Id {InvoiceId}) cancelled", invoice.InvoiceNumber, invoiceId);
        return true;
    }

    /// <inheritdoc />
    public async Task RunBillingCycleAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Starting billing cycle run at {UtcNow}", DateTime.UtcNow);

        // Determine the billing period (previous month)
        var periodEnd = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodStart = periodEnd.AddMonths(-1);

        // Aggregate usage for the period
        var aggregateCount = await usageMeter.AggregateUsageAsync(periodStart, periodEnd, ct);
        logger.LogInformation("Aggregated usage for {AggregateCount} API keys", aggregateCount);

        // Find all active API keys that have a billing plan association
        var activeKeys = await dbContext.Set<ApiKey>()
            .Where(k => k.IsActive && k.BillingPlanId.HasValue)
            .ToListAsync(ct);

        logger.LogInformation("Found {ActiveKeyCount} active API keys with billing plans", activeKeys.Count);

        foreach (var apiKey in activeKeys)
        {
            try
            {
                // Check if an invoice already exists for this key and period
                var invoiceExists = await dbContext.Set<BillingInvoice>()
                    .AnyAsync(inv => inv.ApiKeyId == apiKey.Id && inv.PeriodStart == periodStart && inv.PeriodEnd == periodEnd, ct);

                if (invoiceExists)
                {
                    logger.LogDebug("Invoice already exists for ApiKeyId {ApiKeyId} period {PeriodStart}-{PeriodEnd}, skipping", apiKey.Id, periodStart, periodEnd);
                    continue;
                }

                await GenerateInvoiceAsync(apiKey.Id, apiKey.BillingPlanId!.Value, periodStart, periodEnd, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate invoice for ApiKeyId {ApiKeyId}", apiKey.Id);
            }
        }

        logger.LogInformation("Billing cycle run completed");
    }
}
