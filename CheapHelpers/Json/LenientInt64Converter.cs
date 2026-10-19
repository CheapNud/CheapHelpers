using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CheapHelpers.Json;

/// <summary>
/// Reads a <see cref="long"/> from a number or a numeric string; null, empty or unparseable input gives 0.
/// Registered in options it also covers <see cref="Nullable{T}"/> properties, where a JSON null stays null.
/// </summary>
public sealed class LenientInt64Converter : JsonConverter<long>
{
    /// <inheritdoc />
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                return reader.TryGetInt64(out var number) ? number : default;
            case JsonTokenType.String:
                return long.TryParse(reader.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : default;
            default:
                reader.Skip();
                return default;
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, long number, JsonSerializerOptions options) =>
        writer.WriteNumberValue(number);
}
