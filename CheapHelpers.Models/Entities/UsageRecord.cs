using System.ComponentModel.DataAnnotations;
using CheapHelpers.Models.Contracts;

namespace CheapHelpers.Models.Entities;

/// <summary>
/// Individual API usage event. Lean write-once record for high-volume metering.
/// Periodically aggregated into <see cref="UsageAggregate"/> by the billing service.
/// </summary>
public class UsageRecord : IEntityId
{
    public int Id { get; set; }

    public int ApiKeyId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string Endpoint { get; set; } = string.Empty;

    [MaxLength(10)]
    public string HttpMethod { get; set; } = string.Empty;

    public int ResponseCode { get; set; }

    public long? DurationMs { get; set; }

    public long? RequestSizeBytes { get; set; }

    public long? ResponseSizeBytes { get; set; }
}
