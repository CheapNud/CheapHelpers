using CheapHelpers.EF;
using CheapHelpers.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Services.Billing;

/// <summary>
/// Tracks and aggregates API usage for billing purposes.
/// Uses <c>dbContext.Set&lt;T&gt;()</c> so it compiles before DbSet properties are added to CheapContext.
/// </summary>
public class UsageMeterService<TUser>(
    CheapBusinessContext<TUser> dbContext,
    ILogger<UsageMeterService<TUser>> logger) : IUsageMeterService
    where TUser : IdentityUser
{
    /// <inheritdoc />
    public async Task RecordUsageAsync(
        int apiKeyId,
        string endpoint,
        string httpMethod,
        int responseCode,
        long? durationMs = null,
        CancellationToken ct = default)
    {
        try
        {
            var usageRecord = new UsageRecord
            {
                ApiKeyId = apiKeyId,
                Endpoint = endpoint,
                HttpMethod = httpMethod,
                ResponseCode = responseCode,
                DurationMs = durationMs,
                Timestamp = DateTime.UtcNow
            };

            dbContext.Set<UsageRecord>().Add(usageRecord);
            await dbContext.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Fire-and-forget safe: log the failure but do not propagate
            logger.LogError(ex, "Failed to record usage for ApiKeyId {ApiKeyId} on {Endpoint}", apiKeyId, endpoint);
        }
    }

    /// <inheritdoc />
    public async Task<List<UsageRecord>> GetUsageAsync(
        int apiKeyId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
    {
        return await dbContext.Set<UsageRecord>()
            .Where(r => r.ApiKeyId == apiKeyId && r.Timestamp >= from && r.Timestamp < to)
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<UsageAggregate?> GetUsageAggregateAsync(
        int apiKeyId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
    {
        // Try to find a pre-computed aggregate first
        var existingAggregate = await dbContext.Set<UsageAggregate>()
            .FirstOrDefaultAsync(a => a.ApiKeyId == apiKeyId && a.PeriodStart == from && a.PeriodEnd == to, ct);

        if (existingAggregate is not null)
            return existingAggregate;

        // Compute from raw records if no aggregate exists
        var rawRecords = await dbContext.Set<UsageRecord>()
            .Where(r => r.ApiKeyId == apiKeyId && r.Timestamp >= from && r.Timestamp < to)
            .ToListAsync(ct);

        if (rawRecords.Count == 0)
            return null;

        return new UsageAggregate
        {
            ApiKeyId = apiKeyId,
            PeriodStart = from,
            PeriodEnd = to,
            TotalRequests = rawRecords.Count,
            SuccessfulRequests = rawRecords.Count(r => r.ResponseCode >= 200 && r.ResponseCode < 300),
            FailedRequests = rawRecords.Count(r => r.ResponseCode >= 400),
            AvgDurationMs = rawRecords.Where(r => r.DurationMs.HasValue).Select(r => r.DurationMs!.Value).DefaultIfEmpty(0).Average(),
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <inheritdoc />
    public async Task<int> AggregateUsageAsync(
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken ct = default)
    {
        var groupedUsage = await dbContext.Set<UsageRecord>()
            .Where(r => r.Timestamp >= periodStart && r.Timestamp < periodEnd)
            .GroupBy(r => r.ApiKeyId)
            .Select(grp => new
            {
                ApiKeyId = grp.Key,
                TotalRequests = grp.Count(),
                SuccessCount = grp.Count(r => r.ResponseCode >= 200 && r.ResponseCode < 300),
                ErrorCount = grp.Count(r => r.ResponseCode >= 400),
                AverageDurationMs = grp.Where(r => r.DurationMs.HasValue).Select(r => r.DurationMs!.Value).DefaultIfEmpty(0).Average()
            })
            .ToListAsync(ct);

        var aggregateCount = 0;
        var aggregateSet = dbContext.Set<UsageAggregate>();

        foreach (var group in groupedUsage)
        {
            // Skip if an aggregate already exists for this key and period
            var alreadyExists = await aggregateSet
                .AnyAsync(a => a.ApiKeyId == group.ApiKeyId && a.PeriodStart == periodStart && a.PeriodEnd == periodEnd, ct);

            if (alreadyExists)
                continue;

            aggregateSet.Add(new UsageAggregate
            {
                ApiKeyId = group.ApiKeyId,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                TotalRequests = group.TotalRequests,
                SuccessfulRequests = group.SuccessCount,
                FailedRequests = group.ErrorCount,
                AvgDurationMs = group.AverageDurationMs,
                CreatedAt = DateTime.UtcNow
            });

            aggregateCount++;
        }

        if (aggregateCount > 0)
        {
            await dbContext.SaveChangesAsync(ct);
            logger.LogInformation("Created {AggregateCount} usage aggregates for period {PeriodStart} to {PeriodEnd}", aggregateCount, periodStart, periodEnd);
        }

        return aggregateCount;
    }
}
