using Microsoft.Extensions.Logging;

namespace CheapHelpers.Testing;

/// <summary>
/// Adds <see cref="CapturedLogs"/> to a logging builder.
/// </summary>
public static class CapturedLogsExtensions
{
    /// <summary>Adds <paramref name="logs"/> as a provider that receives every level, Trace included.</summary>
    public static ILoggingBuilder AddCapturedLogs(this ILoggingBuilder logging, CapturedLogs logs) =>
        // Without the filter the factory's default minimum level (Information) would drop Trace and Debug before they reach the provider
        logging.AddProvider(logs).AddFilter<CapturedLogs>(null, LogLevel.Trace);
}
