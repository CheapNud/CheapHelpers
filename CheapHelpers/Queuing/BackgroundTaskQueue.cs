using System.Threading.Channels;

namespace CheapHelpers.Queuing;

/// <summary>
/// Channel-based background task queue for high-performance async producer/consumer patterns.
/// Uses a bounded channel with configurable capacity, or an unbounded one when the capacity is null.
/// </summary>
/// <param name="capacity">Maximum number of queued work items before enqueueing waits. Default is 100; null means unbounded.</param>
public class BackgroundTaskQueue(int? capacity = 100) : IBackgroundTaskQueue
{
    private readonly Channel<Func<CancellationToken, ValueTask>> _queue = capacity is { } boundedCapacity
        ? Channel.CreateBounded<Func<CancellationToken, ValueTask>>(new BoundedChannelOptions(boundedCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        })
        : Channel.CreateUnbounded<Func<CancellationToken, ValueTask>>();

    /// <inheritdoc />
    public async ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        await _queue.Writer.WriteAsync(workItem, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
