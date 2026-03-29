using CheapHelpers.Models.Entities;

namespace CheapHelpers.Services.Billing;

/// <summary>
/// Tracks and aggregates API usage for billing purposes.
/// </summary>
public interface IUsageMeterService
{
    /// <summary>
    /// Records a single API usage event. Fire-and-forget safe — failures are logged, not thrown.
    /// </summary>
    Task RecordUsageAsync(int apiKeyId, string endpoint, string httpMethod, int responseCode, long? durationMs = null, CancellationToken ct = default);

    /// <summary>
    /// Retrieves raw usage records for an API key within the specified date range.
    /// </summary>
    Task<List<UsageRecord>> GetUsageAsync(int apiKeyId, DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>
    /// Returns the pre-computed usage aggregate for an API key and period, or computes one from raw records if none exists.
    /// </summary>
    Task<UsageAggregate?> GetUsageAggregateAsync(int apiKeyId, DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>
    /// Groups raw usage records by API key for the given period and persists a UsageAggregate for each.
    /// Returns the number of aggregates created.
    /// </summary>
    Task<int> AggregateUsageAsync(DateTime periodStart, DateTime periodEnd, CancellationToken ct = default);
}
