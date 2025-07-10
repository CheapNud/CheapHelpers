using System;
using System.Diagnostics;

namespace CheapHelpers.Helpers.Logs;

public static class LogHelper
{
    /// <summary>
    /// Outputs exception report to debug window in readable format
    /// </summary>
    public static void LogExceptionToDebug(this ExceptionReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        Debug.WriteLine("=== EXCEPTION REPORT ===");
        Debug.WriteLine($"Machine: {report.MachineName}");
        Debug.WriteLine($"Report Time: {report.ReportTimestamp:MM/dd/yyyy HH:mm:ss}");
        Debug.WriteLine("");

        LogExceptionDetailsToDebug(report.MainException, "MAIN EXCEPTION");

        foreach (var inner in report.InnerExceptions)
        {
            Debug.WriteLine("");
            LogExceptionDetailsToDebug(inner, "INNER EXCEPTION");
        }

        Debug.WriteLine("=== END EXCEPTION REPORT ===");
    }

    private static void LogExceptionDetailsToDebug(ExceptionDetails details, string header)
    {
        Debug.WriteLine($"--- {header} ---");
        Debug.WriteLine($"Assembly: {details.AssemblyName}");
        Debug.WriteLine($"Source: {details.Source}");
        Debug.WriteLine($"Time: {details.Timestamp:MM/dd/yyyy HH:mm:ss}");
        Debug.WriteLine($"Type: {details.ExceptionType}");
        Debug.WriteLine($"Message: {details.Message}");
        Debug.WriteLine($"StackTrace: {details.StackTrace}");
    }
}