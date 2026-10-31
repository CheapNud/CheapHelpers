namespace CheapHelpers.EF.Jobs;

/// <summary>A progress report raised through <see cref="JobQueue{TPayload, TContext}.ProgressChanged"/>.</summary>
/// <param name="Id">The job id.</param>
/// <param name="Kind">The job's label.</param>
/// <param name="Percent">Progress, 0 to 100.</param>
/// <param name="Message">Optional progress message.</param>
/// <param name="EstimatedRemaining">Estimated time left, or null before enough progress is known.</param>
public sealed record JobProgress(Guid Id, string Kind, double Percent, string? Message, TimeSpan? EstimatedRemaining);
