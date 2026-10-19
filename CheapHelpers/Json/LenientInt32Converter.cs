using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CheapHelpers.Json;

/// <summary>
/// Reads an <see cref="int"/> from a number or a numeric string; null, empty or unparseable input gives 0.
/// Registered in options it also covers <see cref="Nullable{T}"/> properties, where a JSON null stays null.
/// </summary>
public sealed class LenientInt32Converter : JsonConverter<int>
{
    /// <inheritdoc />
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                return reader.TryGetInt32(out var number) ? number : default;
            case JsonTokenType.String:
                return int.TryParse(reader.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : default;
            default:
                reader.Skip();
                return default;
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, int number, JsonSerializerOptions options) =>
        writer.WriteNumberValue(number);
}
