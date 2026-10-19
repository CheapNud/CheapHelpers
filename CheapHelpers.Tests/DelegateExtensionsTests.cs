using CheapHelpers.Extensions;

namespace CheapHelpers.Tests;

/// <summary>Pins that one failing handler does not stop the others and that cancellation stops the fan-out.</summary>
public sealed class DelegateExtensionsTests
{
    [Fact]
    public async Task Second_handler_runs_after_the_first_throws()
    {
        var calls = new List<string>();
        Func<string, Task>? handlers = _ => throw new InvalidOperationException("first fails");
        handlers += argument =>
        {
            calls.Add(argument);
            return Task.CompletedTask;
        };

        await handlers.InvokeSafelyAsync("hello", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("hello", Assert.Single(calls));
    }

    [Fact]
    public async Task Cancellation_stops_before_the_next_handler()
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var secondRan = false;
        Func<int, Task>? handlers = async _ => await cancellation.CancelAsync();
        handlers += _ =>
        {
            secondRan = true;
            return Task.CompletedTask;
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handlers.InvokeSafelyAsync(1, cancellationToken: cancellation.Token));
        Assert.False(secondRan);
    }

    [Fact]
    public async Task Null_delegate_does_nothing()
    {
        Func<int, Task>? handlers = null;
        await handlers.InvokeSafelyAsync(1, cancellationToken: TestContext.Current.CancellationToken);
    }
}
