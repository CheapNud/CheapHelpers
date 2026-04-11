using CsvHelper;
using CsvHelper.Configuration;
using System.Diagnostics;
using System.Globalization;

namespace CheapHelpers.Services.DataExchange.Csv;

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
    /// Writes a collection of records to a stream as CSV. Caller owns the stream.
    /// </summary>
    public async Task ExportToStreamAsync<T>(Stream stream, IEnumerable<T> records, string? culture = null, string? delimiter = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(records);

        try
        {
            var writer = new StreamWriter(stream, leaveOpen: true);
            await using (writer.ConfigureAwait(false))
            {
                var config = CreateCsvConfiguration(culture, delimiter);
                var csvWriter = new CsvWriter(writer, config);
                await using (csvWriter.ConfigureAwait(false))
                {
                    await csvWriter.WriteRecordsAsync(records);
                }
            }

            Debug.WriteLine($"Successfully exported CSV to stream");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to export CSV to stream: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Exports a collection of records to a CSV byte array. Convenience wrapper
    /// around <see cref="ExportToStreamAsync{T}"/> using a MemoryStream.
    /// </summary>
    public async Task<byte[]> ExportToBytesAsync<T>(IEnumerable<T> records, string? culture = null, string? delimiter = null)
    {
        ArgumentNullException.ThrowIfNull(records);

        using var memory = new MemoryStream();
        await ExportToStreamAsync(memory, records, culture, delimiter);
        return memory.ToArray();
    }

    /// <summary>
    /// Creates the CSV configuration with default settings
    /// </summary>
    private static CsvConfiguration CreateCsvConfiguration(string? culture = null, string? delimiter = null)
    {
        var ci = CultureInfo.CreateSpecificCulture(culture ?? DefaultCulture);
        return new CsvConfiguration(ci)
        {
            Delimiter = delimiter ?? DefaultDelimiter
        };
    }
}