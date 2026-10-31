using Microsoft.Extensions.Logging;

namespace CheapHelpers.Services.Tests;

/// <summary>A logger that keeps the structured values of every entry, so tests can read what was logged.</summary>
internal sealed class RecordingLogger<T> : ILogger<T>
{
    /// <summary>The structured values of every entry, in order.</summary>
    public List<IReadOnlyDictionary<string, object?>> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (state is IEnumerable<KeyValuePair<string, object?>> values)
        {
            lock (Entries)
            {
                Entries.Add(values.ToDictionary(pair => pair.Key, pair => pair.Value));
            }
        }
    }
}
