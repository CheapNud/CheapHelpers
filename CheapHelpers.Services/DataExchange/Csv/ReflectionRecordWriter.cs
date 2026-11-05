using System.Reflection;
using CsvHelper;

namespace CheapHelpers.Services.DataExchange.Csv;

/// <summary>
/// Writes the properties with a public getter of a type that has no generated CsvHelper record writer.
/// </summary>
internal sealed class ReflectionRecordWriter<T>(Type recordType) : CsvRecordWriter<T>
{
    private readonly PropertyInfo[] _properties = recordType
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(property => property.GetMethod?.IsPublic == true && property.GetIndexParameters().Length == 0)
        .ToArray();

    /// <inheritdoc/>
    public override int FieldCount => _properties.Length;

    /// <inheritdoc/>
    public override void WriteHeader(CsvWriter writer)
    {
        foreach (var property in _properties)
        {
            WriteColumnName(writer, property.Name);
        }
    }

    /// <inheritdoc/>
    public override void Write(CsvWriter writer, T record)
    {
        foreach (var property in _properties)
        {
            // A nested object is written as one field (its ToString), not flattened into columns as CsvHelper 33 did
            WriteValue(writer, property.GetValue(record));
        }
    }

    /// <summary>
    /// Writes one typed field; dates keep the culture's own text as CsvHelper 33 wrote them, where the library writes ISO 8601.
    /// </summary>
    internal static void WriteValue(CsvWriter writer, object? fieldValue)
    {
        if (fieldValue is DateTime or DateTimeOffset or DateOnly or TimeOnly)
        {
            writer.WriteField(((IFormattable)fieldValue).ToString(null, writer.Options.FormatProvider));
        }
        else
        {
            writer.WriteField(fieldValue);
        }
    }
}
