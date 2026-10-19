using System.Text.Json;
using System.Text.Json.Serialization;

namespace CheapHelpers.Json;

/// <summary>
/// Creates a <see cref="TolerantEnumConverter{TEnum}"/> for every enum type, falling back to the enum's default.
/// </summary>
public sealed class TolerantEnumConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

    /// <inheritdoc />
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter?)Activator.CreateInstance(typeof(TolerantEnumConverter<>).MakeGenericType(typeToConvert));
}
