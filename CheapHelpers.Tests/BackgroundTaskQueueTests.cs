using CheapHelpers.Queuing;

namespace CheapHelpers.Tests;

/// <summary>Pins cancellation of a blocked enqueue and the unbounded mode.</summary>
public sealed class BackgroundTaskQueueTests
{
    private static ValueTask NoOp(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    [Fact]
    public async Task Cancelling_an_enqueue_blocked_on_a_full_queue_throws()
    {
        var queue = new BackgroundTaskQueue(capacity: 1);
        await queue.QueueBackgroundWorkItemAsync(NoOp, TestContext.Current.CancellationToken);

        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var blocked = queue.QueueBackgroundWorkItemAsync(NoOp, cancellation.Token);
        Assert.False(blocked.IsCompleted);

        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await blocked);
    }

    [Fact]
    public void Unbounded_queue_accepts_a_thousand_items_without_waiting()
    {
        const int itemCount = 1000;
        var queue = new BackgroundTaskQueue(capacity: null);

        for (var index = 0; index < itemCount; index++)
            Assert.True(queue.QueueBackgroundWorkItemAsync(NoOp, TestContext.Current.CancellationToken).IsCompletedSuccessfully);
    }

    [Fact]
    public async Task Dequeue_returns_items_in_order()
    {
        var queue = new BackgroundTaskQueue();
        Func<CancellationToken, ValueTask> first = NoOp;
        Func<CancellationToken, ValueTask> second = _ => ValueTask.CompletedTask;

        await queue.QueueBackgroundWorkItemAsync(first, TestContext.Current.CancellationToken);
        await queue.QueueBackgroundWorkItemAsync(second, TestContext.Current.CancellationToken);

        Assert.Same(first, await queue.DequeueAsync(TestContext.Current.CancellationToken));
        Assert.Same(second, await queue.DequeueAsync(TestContext.Current.CancellationToken));
    }
}
