using CheapHelpers.Models.Contracts;

namespace CheapHelpers.Models.Entities;

/// <summary>
/// Aggregated API usage for a billing period. Created by the billing service from raw <see cref="UsageRecord"/>s.
/// </summary>
public class UsageAggregate : IEntityId, IAuditable
{
    public int Id { get; set; }

    public int ApiKeyId { get; set; }

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public long TotalRequests { get; set; }

    public long SuccessfulRequests { get; set; }

    public long FailedRequests { get; set; }

    public long TotalRequestSizeBytes { get; set; }

    public long TotalResponseSizeBytes { get; set; }

    public double? AvgDurationMs { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
