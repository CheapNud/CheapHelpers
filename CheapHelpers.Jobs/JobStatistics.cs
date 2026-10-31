namespace CheapHelpers.Jobs;

/// <summary>Number of jobs per status.</summary>
/// <param name="Pending">Jobs waiting to run.</param>
/// <param name="Running">Jobs being processed.</param>
/// <param name="Completed">Jobs that finished.</param>
/// <param name="Cancelled">Jobs that were cancelled.</param>
/// <param name="DeadLetter">Jobs that ran out of retries.</param>
public sealed record JobStatistics(int Pending, int Running, int Completed, int Cancelled, int DeadLetter);
