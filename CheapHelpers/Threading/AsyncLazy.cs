using System.Runtime.CompilerServices;

namespace CheapHelpers.Threading;

/// <summary>
/// Provides thread-safe lazy initialization with async factory support.
/// Wraps <see cref="Lazy{T}"/> around a <see cref="Task{T}"/> to ensure
/// the factory runs exactly once, even under concurrent access.
/// </summary>
/// <typeparam name="T">The type of the lazily initialized value.</typeparam>
public class AsyncLazy<T>(Func<Task<T>> factory)
{
    private readonly Lazy<Task<T>> _lazy = new(factory);

    /// <summary>
    /// Gets the lazily initialized value.
    /// </summary>
    public Task<T> Value => _lazy.Value;

    /// <summary>
    /// Gets whether the value has been created and completed successfully.
    /// </summary>
    public bool IsValueCreated => _lazy.IsValueCreated && _lazy.Value.IsCompletedSuccessfully;

    /// <summary>
    /// Allows <c>await</c> directly on the <see cref="AsyncLazy{T}"/> instance.
    /// </summary>
    public TaskAwaiter<T> GetAwaiter() => Value.GetAwaiter();
}
