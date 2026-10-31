using System.Text;
using Microsoft.Extensions.Logging;

namespace CheapHelpers.Testing;

/// <summary>
/// Writes one line per log call into its <see cref="CapturedLogs"/>.
/// </summary>
internal sealed class CapturedLogger(string category, CapturedLogs logs) : ILogger
{
    // Microsoft.Extensions.Logging adds the message template under this key; it repeats the message, so it is skipped
    private const string OriginalFormatKey = "{OriginalFormat}";

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => logs.Scopes.Push(state);

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var line = new StringBuilder(category).Append(": ").Append(formatter(state, exception));
        AppendBracketed(line, state);
        logs.Scopes.ForEachScope((scope, builder) => AppendBracketed(builder, scope), line);
        logs.Lines.Enqueue(line.ToString());
    }

    private static void AppendBracketed(StringBuilder line, object? state)
    {
        if (state is IEnumerable<KeyValuePair<string, object?>> pairs)
        {
            var shown = pairs.Where(pair => pair.Key != OriginalFormatKey).Select(pair => $"{pair.Key}={pair.Value}").ToList();
            if (shown.Count > 0)
            {
                line.Append(" [").AppendJoin(", ", shown).Append(']');
            }
        }
        else if (state is not null)
        {
            line.Append(" [").Append(state).Append(']');
        }
    }
}
