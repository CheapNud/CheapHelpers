using System.Threading.Channels;

namespace CheapHelpers.Queuing;

/// <summary>
/// Channel-based background task queue for high-performance async producer/consumer patterns.
/// Uses a bounded channel with configurable capacity.
/// </summary>
/// <param name="capacity">Maximum number of queued work items before blocking. Default is 100.</param>
public class BackgroundTaskQueue(int capacity = 100) : IBackgroundTaskQueue
{
    private readonly Channel<Func<CancellationToken, ValueTask>> _queue =
        Channel.CreateBounded<Func<CancellationToken, ValueTask>>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

    /// <inheritdoc />
    public async ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        await _queue.Writer.WriteAsync(workItem);
    }

    /// <inheritdoc />
    public async ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
