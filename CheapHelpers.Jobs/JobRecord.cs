namespace CheapHelpers.Jobs;

/// <summary>One row of the job table; the EF entity mapped by <see cref="JobModelBuilderExtensions.ApplyJobQueue"/>.</summary>
public sealed class JobRecord
{
    /// <summary>Maximum stored length of <see cref="Kind"/>.</summary>
    public const int MaxKindLength = 100;

    /// <summary>Maximum stored length of <see cref="LastError"/>; longer messages are truncated.</summary>
    public const int MaxErrorLength = 2000;

    /// <summary>Maximum stored length of <see cref="MachineName"/>.</summary>
    public const int MaxMachineNameLength = 256;

    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>The app's own label for the job; the engine does not interpret it.</summary>
    public string Kind { get; set; } = "";

    /// <summary>The payload serialised with System.Text.Json.</summary>
    public string PayloadJson { get; set; } = "";

    /// <summary>Current lifecycle state.</summary>
    public JobStatus Status { get; set; }

    /// <summary>Last stored progress, 0 to 100.</summary>
    public double ProgressPercent { get; set; }

    /// <summary>Last stored progress message.</summary>
    public string? ProgressMessage { get; set; }

    /// <summary>Estimated time left at the last stored progress report.</summary>
    public TimeSpan? EstimatedRemaining { get; set; }

    /// <summary>Failed or interrupted attempts so far.</summary>
    public int RetryCount { get; set; }

    /// <summary>Retries allowed after the first attempt before the job goes to <see cref="JobStatus.DeadLetter"/>.</summary>
    public int MaxRetries { get; set; }

    /// <summary>When the job was enqueued.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Earliest time the job may be claimed; pushed back after a failed attempt.</summary>
    public DateTimeOffset AvailableAt { get; set; }

    /// <summary>When the current or last attempt started.</summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>When the job reached Completed, Cancelled or DeadLetter.</summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>When the row last changed.</summary>
    public DateTimeOffset LastUpdatedAt { get; set; }

    /// <summary>Message of the last failure, truncated to <see cref="MaxErrorLength"/> characters.</summary>
    public string? LastError { get; set; }

    /// <summary>Machine that claimed the job.</summary>
    public string? MachineName { get; set; }

    /// <summary>Process that claimed the job.</summary>
    public int? ProcessId { get; set; }
}
