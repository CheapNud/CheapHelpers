using System;
using System.Collections.Generic;

namespace CheapHelpers.Helpers.Logs;

public record ExceptionReport
{
    public ExceptionDetails MainException { get; init; } = new();
    public List<ExceptionDetails> InnerExceptions { get; init; } = [];
    public string MachineName { get; init; } = Environment.MachineName;
    public DateTime ReportTimestamp { get; init; } = DateTime.Now;
}