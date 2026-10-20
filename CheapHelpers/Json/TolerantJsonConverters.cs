using System.Text.Json;

namespace CheapHelpers.Json;

/// <summary>
/// Registers every tolerant and lenient converter in this namespace at once.
/// </summary>
public static class TolerantJsonConverters
{
    /// <summary>
    /// Adds the lenient number, boolean and string converters and a tolerant converter for every enum to <paramref name="options"/>.
    /// Call it before the options are first used, since used options are read-only.
    /// </summary>
    /// <returns>The same options, for chaining.</returns>
    public static JsonSerializerOptions AddAll(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Converters.Add(new LenientInt32Converter());
        options.Converters.Add(new LenientInt64Converter());
        options.Converters.Add(new LenientDoubleConverter());
        options.Converters.Add(new LenientBooleanConverter());
        options.Converters.Add(new LenientStringConverter());
        options.Converters.Add(new TolerantEnumConverterFactory());
        return options;
    }
}
