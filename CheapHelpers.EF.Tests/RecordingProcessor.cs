using System.Collections.Concurrent;
using CheapHelpers.EF.Jobs;

namespace CheapHelpers.EF.Tests;

public sealed class RecordingProcessor : IJobProcessor<TestPayload>
{
    public ConcurrentQueue<TestPayload> Received { get; } = new();

    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Func<JobContext<TestPayload>, CancellationToken, Task> Behaviour { get; set; } = (_, _) => Task.CompletedTask;

    public async Task ProcessAsync(JobContext<TestPayload> job, CancellationToken cancellationToken)
    {
        Received.Enqueue(job.Payload);
        Started.TrySetResult();
        await Behaviour(job, cancellationToken);
    }
}
