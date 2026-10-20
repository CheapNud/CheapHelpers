using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CheapHelpers.Json;

/// <summary>
/// Reads a <see cref="string"/> from a string, a number (invariant culture) or a boolean; null stays null.
/// </summary>
public sealed class LenientStringConverter : JsonConverter<string?>
{
    /// <inheritdoc />
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString();
            case JsonTokenType.Number:
                // The raw token text, so large or precise numbers are not rounded through double.
                return Encoding.UTF8.GetString(reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan);
            case JsonTokenType.True:
                return bool.TrueString;
            case JsonTokenType.False:
                return bool.FalseString;
            default:
                reader.Skip();
                return null;
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, string? text, JsonSerializerOptions options)
    {
        if (text is null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(text);
    }

    /// <inheritdoc />
    public override string? ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetString();

    /// <inheritdoc />
    public override void WriteAsPropertyName(Utf8JsonWriter writer, string? text, JsonSerializerOptions options) =>
        writer.WritePropertyName(options.DictionaryKeyPolicy?.ConvertName(text!) ?? text!);
}
