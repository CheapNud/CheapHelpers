namespace CheapHelpers.Jobs;

/// <summary>Lifecycle state of a job row.</summary>
public enum JobStatus
{
    /// <summary>Waiting to be claimed once <see cref="JobRecord.AvailableAt"/> has passed.</summary>
    Pending,

    /// <summary>Claimed by a worker and being processed.</summary>
    Running,

    /// <summary>The processor returned without throwing.</summary>
    Completed,

    /// <summary>Cancelled before or while running.</summary>
    Cancelled,

    /// <summary>Failed more often than <see cref="JobRecord.MaxRetries"/> allows; only a manual retry brings it back.</summary>
    DeadLetter,
}
