namespace CheapHelpers.Services.Email.Broadcast;

/// <summary>
/// Progress snapshot for a broadcast email operation, reported via <see cref="IProgress{T}"/>.
/// </summary>
public sealed record BroadcastProgress(
    int TotalRecipients,
    int SentCount,
    int FailedCount,
    int SkippedCount,
    string? CurrentRecipient = null)
{
    public double PercentComplete => TotalRecipients > 0
        ? (double)(SentCount + FailedCount + SkippedCount) / TotalRecipients * 100
        : 0;
}
