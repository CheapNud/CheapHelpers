using System.ComponentModel.DataAnnotations;
using CheapHelpers.Models.Contracts;

namespace CheapHelpers.Models.Entities;

/// <summary>
/// Defines a billing tier with included units and overage pricing.
/// </summary>
public class BillingPlan : IEntityId, IAuditable
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Number of free API requests included per billing cycle.
    /// </summary>
    public long IncludedUnits { get; set; }

    /// <summary>
    /// Price per request above the included units.
    /// </summary>
    public decimal RatePerUnit { get; set; }

    /// <summary>
    /// Alternative overage rate (e.g., tiered pricing above a threshold).
    /// </summary>
    public decimal OverageRate { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "EUR";

    /// <summary>
    /// Billing cycle length in days (default 30).
    /// </summary>
    public int BillingCycleDays { get; set; } = 30;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
