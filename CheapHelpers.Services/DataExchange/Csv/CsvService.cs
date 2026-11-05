using CsvHelper;
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
            await using var writer = new StreamWriter(filePath, !DefaultOverwriteFile);
            await using var csvWriter = CsvWriter.Create(writer, CreateOptions(), leaveOpen: true);

            foreach (var line in list)
            {
                csvWriter.WriteRecord(line);
            }

            await csvWriter.FlushAsync();

            Debug.WriteLine($"Successfully exported CSV file: {filePath} with string records");
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
            await using var writer = new StreamWriter(filePath, !DefaultOverwriteFile);
            await using var csvWriter = CsvWriter.Create(writer, CreateOptions(), leaveOpen: true);

            await WriteRecordsAsync<object>(csvWriter, list);

            Debug.WriteLine($"Successfully exported CSV file: {filePath} with dynamic records");
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
            var options = CreateOptions(culture, delimiter);
            var writer = new StreamWriter(stream, leaveOpen: true);
            await using (writer.ConfigureAwait(false))
            {
                var csvWriter = CsvWriter.Create(writer, options, leaveOpen: true);
                await using (csvWriter.ConfigureAwait(false))
                {
                    await WriteRecordsAsync(csvWriter, records);
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
    /// Writes the records through the generated record writer when one exists, through the dynamic path for
    /// dictionaries and expando objects, and through <see cref="WritePlainRecordsAsync{T}"/> for anything else.
    /// </summary>
    private static async Task WriteRecordsAsync<T>(CsvWriter csvWriter, IEnumerable<T> records)
    {
        // Typed dictionaries and expando objects take the library's dynamic path as well
        if (CsvRecordWriter.IsRegistered<T>() || typeof(IDictionary<string, object?>).IsAssignableFrom(typeof(T)))
        {
            await csvWriter.WriteRecordsAsync(records);
            return;
        }

        if (typeof(T) != typeof(object))
        {
            await WritePlainRecordsAsync(csvWriter, records, typeof(T));
            return;
        }

        // The writer is chosen from the first record; the same enumerator then writes every record,
        // so the source is read once and never buffered
        using var enumerator = records.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return;
        }

        // CsvHelper 34 rejects a null record in any position; a null first record fails the same way instead of writing nothing
        var first = enumerator.Current ?? throw new ArgumentException("The sequence contains a null record.", nameof(records));

        var allRecords = Resume(first, enumerator);
        if (first is IDictionary<string, object?>)
        {
            await csvWriter.WriteRecordsAsync(allRecords);
        }
        else
        {
            // The columns come from the first record's type, so every record must be of that type
            await WritePlainRecordsAsync(csvWriter, allRecords, first.GetType());
        }
    }

    /// <summary>
    /// Writes scalars one value per record and other types through <see cref="ReflectionRecordWriter{T}"/>.
    /// </summary>
    private static async Task WritePlainRecordsAsync<T>(CsvWriter csvWriter, IEnumerable<T> records, Type recordType)
    {
        if (Type.GetTypeCode(Nullable.GetUnderlyingType(recordType) ?? recordType) != TypeCode.Object)
        {
            // Strings, numbers, enums and dates go out one value per record without a header, as CsvHelper 33 wrote them
            foreach (var record in records)
            {
                ReflectionRecordWriter<T>.WriteValue(csvWriter, record);
                await csvWriter.EndRecordAsync();
            }

            return;
        }

        await csvWriter.WriteRecordsAsync(records, new ReflectionRecordWriter<T>(recordType));
    }

    /// <summary>
    /// Yields the record already read and then the rest of the enumerator.
    /// </summary>
    private static IEnumerable<T> Resume<T>(T first, IEnumerator<T> enumerator)
    {
        yield return first;
        while (enumerator.MoveNext())
        {
            yield return enumerator.Current;
        }
    }

    /// <summary>
    /// Creates the CSV writer options with default settings
    /// </summary>
    private static CsvWriterOptions CreateOptions(string? culture = null, string? delimiter = null)
    {
        // CsvHelper 34 takes the separator as a single char, so longer delimiters are rejected up front
        var separator = delimiter ?? DefaultDelimiter;
        if (separator.Length != 1)
        {
            throw new ArgumentException("The delimiter must be exactly one character.", nameof(delimiter));
        }

        return CsvWriterOptions.Default with
        {
            Separator = separator[0],
            FormatProvider = CultureInfo.CreateSpecificCulture(culture ?? DefaultCulture),
            // Cells that start with =, +, -, @, tab or CR get a ' prefix so spreadsheets do not run them as formulas;
            // typed numbers are never prefixed, but text that looks like a number ("-5") is, on purpose
            Injection = CsvInjection.Escape,
        };
    }
}