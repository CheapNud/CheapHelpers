using CsvHelper;
using CsvHelper.Configuration;
using System.Diagnostics;
using System.Globalization;

namespace CheapHelpers.Services;

public class CsvService : ICsvService
{
    private const string DefaultCulture = "nl-BE";
    private const string DefaultDelimiter = ";";
    private const bool DefaultOverwriteFile = false;

    /// <summary>
    /// Exports a collection of strings to a CSV file
    /// </summary>
    public async Task Export(string filePath, IEnumerable<string> list)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(list);

        try
        {
            using var writer = new StreamWriter(filePath, !DefaultOverwriteFile);
            var config = CreateCsvConfiguration();
            using var csvWriter = new CsvWriter(writer, config);

            await csvWriter.WriteRecordsAsync(list);

            Debug.WriteLine($"Successfully exported CSV file: {filePath} with {list.Count()} string records");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to export CSV file '{filePath}': {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Exports a collection of dynamic objects to a CSV file
    /// </summary>
    public async Task Export(string filePath, IEnumerable<dynamic> list)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(list);

        try
        {
            using var writer = new StreamWriter(filePath, !DefaultOverwriteFile);
            var config = CreateCsvConfiguration();
            using var csvWriter = new CsvWriter(writer, config);

            await csvWriter.WriteRecordsAsync(list);

            Debug.WriteLine($"Successfully exported CSV file: {filePath} with {list.Count()} dynamic records");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to export CSV file '{filePath}': {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Creates the CSV configuration with default settings
    /// </summary>
    private static CsvConfiguration CreateCsvConfiguration()
    {
        var culture = CultureInfo.CreateSpecificCulture(DefaultCulture);
        return new CsvConfiguration(culture)
        {
            Delimiter = DefaultDelimiter
        };
    }
}