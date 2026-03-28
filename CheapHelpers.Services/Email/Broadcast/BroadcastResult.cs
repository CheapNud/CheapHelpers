namespace CheapHelpers.Services.Email.Broadcast;

/// <summary>
/// Aggregate result of a broadcast email operation.
/// </summary>
public sealed record BroadcastResult
{
    public int TotalRecipients { get; init; }
    public int SentCount { get; init; }
    public int FailedCount { get; init; }
    public int SkippedCount { get; init; }
    public TimeSpan Duration { get; init; }
    public IReadOnlyList<BroadcastRecipientResult> Results { get; init; } = [];

    public IReadOnlyList<BroadcastRecipientResult> Failures =>
        Results.Where(r => !r.IsSuccess && !r.IsSkipped).ToList();
}
