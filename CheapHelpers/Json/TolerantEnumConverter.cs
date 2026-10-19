using System.Text.Json;
using System.Text.Json.Serialization;

namespace CheapHelpers.Json;

/// <summary>
/// Enum converter that reads an ordinal, a name in any casing, or a retired name from <paramref name="legacyNames"/>,
/// and turns anything else into <paramref name="fallback"/> instead of throwing. Writes the enum name.
/// </summary>
/// <remarks>
/// A renamed or removed enum member otherwise makes the whole object fail to deserialise,
/// which takes every other stored value in it down too.
/// </remarks>
/// <param name="fallback">Value returned for unknown input (default: the enum's default).</param>
/// <param name="legacyNames">Optional map of retired names to current members, matched case-insensitively.</param>
public class TolerantEnumConverter<TEnum>(TEnum fallback = default, IReadOnlyDictionary<string, TEnum>? legacyNames = null) : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    private readonly Dictionary<string, TEnum>? _legacyNames = legacyNames is null
        ? null
        : new Dictionary<string, TEnum>(legacyNames, StringComparer.OrdinalIgnoreCase);

    /// <summary>Creates a converter that falls back to the enum's default; needed for <see cref="JsonConverterAttribute"/>.</summary>
    public TolerantEnumConverter() : this(default)
    {
    }

    /// <inheritdoc />
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                return reader.TryGetInt64(out var ordinal) ? (TEnum)Enum.ToObject(typeof(TEnum), ordinal) : fallback;

            case JsonTokenType.String:
                var name = reader.GetString();
                if (Enum.TryParse<TEnum>(name, ignoreCase: true, out var parsed))
                    return parsed;
                if (name is not null && _legacyNames?.TryGetValue(name, out var legacy) == true)
                    return legacy;
                return fallback;

            default:
                reader.Skip();
                return fallback;
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TEnum enumValue, JsonSerializerOptions options) =>
        writer.WriteStringValue(enumValue.ToString());
}
