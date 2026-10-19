using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Extensions;

/// <summary>
/// Extensions for invoking multicast async delegates such as <c>event Func&lt;T, Task&gt;</c>.
/// </summary>
public static class DelegateExtensions
{
    /// <summary>
    /// Awaits every handler of a multicast delegate in order. A handler that throws is logged and skipped,
    /// so the remaining handlers still run. Cancellation is checked before each handler.
    /// </summary>
    /// <param name="handlers">The delegate (usually an event field); null does nothing.</param>
    /// <param name="argument">The argument passed to each handler.</param>
    /// <param name="logger">Receives handler failures; without one they go to <see cref="Debug"/>.</param>
    /// <param name="cancellationToken">Stops the fan-out between handlers.</param>
    public static async Task InvokeSafelyAsync<T>(this Func<T, Task>? handlers, T argument, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        if (handlers is null)
            return;

        foreach (var handler in handlers.GetInvocationList().Cast<Func<T, Task>>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await handler(argument);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                if (logger is null)
                    Debug.WriteLine($"Handler {handler.Method.Name} failed: {ex}");
                else
                    logger.LogError(ex, "Handler {Handler} failed", handler.Method.Name);
            }
        }
    }
}
