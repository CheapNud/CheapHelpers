using System.Text.Json;

namespace CheapHelpers.Extensions;

/// <summary>
/// Safe property getters for <see cref="JsonElement"/> that return a default instead of throwing
/// when the element is not an object, the property is missing, or it holds a different kind of value.
/// </summary>
public static class JsonElementExtensions
{
    /// <summary>Returns the named property, or null when the element is not an object or has no such property.</summary>
    public static JsonElement? GetPropertyOrNull(this JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var property)
            ? property
            : null;

    /// <summary>Returns the named string property, or <paramref name="defaultValue"/>.</summary>
    public static string? GetStringOrDefault(this JsonElement element, string propertyName, string? defaultValue = null) =>
        element.GetPropertyOrNull(propertyName) is { ValueKind: JsonValueKind.String } property
            ? property.GetString()
            : defaultValue;

    /// <summary>Returns the named property as an <see cref="int"/>, or <paramref name="defaultValue"/> when it is missing, not a number or out of range.</summary>
    public static int GetInt32OrDefault(this JsonElement element, string propertyName, int defaultValue = 0) =>
        element.GetPropertyOrNull(propertyName) is { ValueKind: JsonValueKind.Number } property && property.TryGetInt32(out var number)
            ? number
            : defaultValue;

    /// <summary>Returns the named <c>true</c>/<c>false</c> property, or <paramref name="defaultValue"/>.</summary>
    public static bool GetBooleanOrDefault(this JsonElement element, string propertyName, bool defaultValue = false) =>
        element.GetPropertyOrNull(propertyName) is { ValueKind: JsonValueKind.True or JsonValueKind.False } property
            ? property.GetBoolean()
            : defaultValue;
}
