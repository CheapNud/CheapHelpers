namespace CheapHelpers.EF.Jobs;

/// <summary>Does the work for one job; resolved from a new DI scope per job.</summary>
public interface IJobProcessor<TPayload>
{
    /// <summary>Processes the job. Returning completes it; throwing fails the attempt.</summary>
    Task ProcessAsync(JobContext<TPayload> job, CancellationToken cancellationToken);
}
