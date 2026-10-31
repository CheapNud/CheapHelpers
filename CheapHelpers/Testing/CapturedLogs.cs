using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Testing;

/// <summary>
/// A logger provider that keeps every log line in memory so a test can assert on it.
/// Every level is enabled. A line reads <c>{category}: {message}</c>, then the state pairs in brackets, then the
/// pairs of each active scope in brackets. The log level and event id are left out.
/// </summary>
public sealed class CapturedLogs : ILoggerProvider, ISupportExternalScope
{
    /// <summary>Every captured line, oldest first.</summary>
    public ConcurrentQueue<string> Lines { get; } = new();

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new CapturedLogger(categoryName, this);

    /// <inheritdoc />
    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => Scopes = scopeProvider;

    /// <summary>Does nothing; the lines stay readable after the logger factory is disposed.</summary>
    public void Dispose()
    {
    }

    // Replaced by the logger factory's provider when one is attached; this default covers direct use without a factory
    internal IExternalScopeProvider Scopes { get; private set; } = new LoggerExternalScopeProvider();
}
