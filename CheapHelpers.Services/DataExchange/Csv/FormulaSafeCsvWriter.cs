using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace CheapHelpers.Services.DataExchange.Csv;

/// <summary>
/// CSV writer that applies the configured injection escaping to every field except plain numbers.
/// </summary>
internal sealed class FormulaSafeCsvWriter(TextWriter writer, IWriterConfiguration configuration) : CsvWriter(writer, configuration)
{
    // A value that parses as a number is read as a number by spreadsheets, never as a formula,
    // so negative and signed numbers ("-5", "+3,5") are written without the escape prefix.
    // The field arrives already quoted when it contains the delimiter ("-3,5" with a comma delimiter).
    /// <inheritdoc/>
    protected override string SanitizeForInjection(string field) =>
        double.TryParse(field.Trim(Configuration.Quote), NumberStyles.Float | NumberStyles.AllowThousands, Configuration.CultureInfo, out _)
            ? field
            : base.SanitizeForInjection(field);
}
