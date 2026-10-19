using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CheapHelpers.Json;

/// <summary>
/// Reads a <see cref="double"/> from a number or an invariant-culture numeric string; null, empty or unparseable input gives 0.
/// Registered in options it also covers <see cref="Nullable{T}"/> properties, where a JSON null stays null.
/// </summary>
public sealed class LenientDoubleConverter : JsonConverter<double>
{
    /// <inheritdoc />
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                return reader.TryGetDouble(out var number) ? number : default;
            case JsonTokenType.String:
                return double.TryParse(reader.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : default;
            default:
                reader.Skip();
                return default;
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, double number, JsonSerializerOptions options) =>
        writer.WriteNumberValue(number);
}
