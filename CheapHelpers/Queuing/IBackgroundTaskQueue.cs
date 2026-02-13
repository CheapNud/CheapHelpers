namespace CheapHelpers.Queuing;

/// <summary>
/// Interface for a channel-based background task queue.
/// Implementations use <see cref="System.Threading.Channels.Channel"/> for high-performance async producer/consumer patterns.
/// </summary>
public interface IBackgroundTaskQueue
{
    /// <summary>
    /// Queues a background work item. Blocks if the queue is at capacity.
    /// </summary>
    /// <param name="workItem">The async work item to queue.</param>
    ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem);

    /// <summary>
    /// Dequeues a background work item. Blocks until an item is available or cancellation is requested.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the dequeue operation.</param>
    ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken);
}
