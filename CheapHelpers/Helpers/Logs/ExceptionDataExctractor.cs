using System;
using System.Collections.Generic;
using System.Reflection;

namespace CheapHelpers.Helpers.Logs;

public static class ExceptionDataExtractor
{
    /// <summary>
    /// Extracts structured data from exception and all inner exceptions
    /// Future-ready for email templating system
    /// </summary>
    public static ExceptionReport ExtractExceptionData(this Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);

        var report = new ExceptionReport
        {
            MainException = CreateExceptionDetails(ex, false)
        };

        // Extract all inner exceptions using the same pattern as original
        var currentEx = ex;
        var innerExceptions = new List<ExceptionDetails>();

        while ((currentEx = currentEx.InnerException) != null)
        {
            innerExceptions.Add(CreateExceptionDetails(currentEx, true));
        }

        return report with { InnerExceptions = innerExceptions };
    }

    private static ExceptionDetails CreateExceptionDetails(Exception ex, bool isInner)
    {
        return new ExceptionDetails
        {
            AssemblyName = Assembly.GetExecutingAssembly().FullName ?? "Unknown",
            Source = ex.Source,
            Timestamp = DateTime.Now,
            ExceptionType = ex.GetType().Name,
            Message = ex.Message,
            StackTrace = ex.StackTrace,
            IsInnerException = isInner
        };
    }
}