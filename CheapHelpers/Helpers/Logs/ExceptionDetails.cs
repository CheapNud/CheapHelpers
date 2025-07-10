using System;

namespace CheapHelpers.Helpers.Logs;

/// <summary>
/// Structured exception information for logging and templating
/// </summary>
public record ExceptionDetails
{
    public string AssemblyName { get; init; } = "";
    public string Source { get; init; }
    public DateTime Timestamp { get; init; }
    public string ExceptionType { get; init; } = "";
    public string Message { get; init; } = "";
    public string StackTrace { get; init; }
    public bool IsInnerException { get; init; }
}
