using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CheapHelpers.Json;

/// <summary>
/// Reads a <see cref="bool"/> from <c>true</c>/<c>false</c>, a number (non-zero is true) or a string holding either;
/// null, empty or unparseable input gives false.
/// Registered in options it also covers <see cref="Nullable{T}"/> properties, where a JSON null stays null.
/// </summary>
public sealed class LenientBooleanConverter : JsonConverter<bool>
{
    /// <inheritdoc />
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.True:
                return true;
            case JsonTokenType.False:
                return false;
            case JsonTokenType.Number:
                return reader.TryGetDouble(out var number) && number != 0;
            case JsonTokenType.String:
                var text = reader.GetString()?.Trim();
                if (bool.TryParse(text, out var parsed))
                    return parsed;
                return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var numeric) && numeric != 0;
            default:
                reader.Skip();
                return default;
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, bool flag, JsonSerializerOptions options) =>
        writer.WriteBooleanValue(flag);
}
