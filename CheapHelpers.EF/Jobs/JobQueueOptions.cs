namespace CheapHelpers.EF.Jobs;

/// <summary>Settings for the job queue and its worker.</summary>
public sealed class JobQueueOptions
{
    /// <summary>Jobs processed at the same time by one worker.</summary>
    public int MaxConcurrentJobs { get; set; } = 1;

    /// <summary>How long the worker waits for a new-work signal before it checks the table anyway.</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Minimum time between two progress writes to the database.</summary>
    public TimeSpan ProgressWriteInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Minimum time between two progress callbacks.</summary>
    public TimeSpan ProgressEventInterval { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>How long a stopping worker waits for running jobs.</summary>
    public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Retries after the first attempt that new jobs get.</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Starts the queue paused, so nothing is claimed until <see cref="JobQueue{TPayload, TContext}.Resume"/>.</summary>
    public bool StartPaused { get; set; }

    /// <summary>Table name, for documentation only; pass the same name to <see cref="JobModelBuilderExtensions.ApplyJobQueue"/>.</summary>
    public string TableName { get; set; } = "Jobs";
}
