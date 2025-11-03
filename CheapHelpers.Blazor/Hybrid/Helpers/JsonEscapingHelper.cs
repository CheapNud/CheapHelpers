using System.Text.Json;

namespace CheapHelpers.Blazor.Hybrid.Helpers;

/// <summary>
/// Helper class for handling JSON escaping issues common in WebView scenarios
/// </summary>
public static class JsonEscapingHelper
{
    /// <summary>
    /// Unescape JSON string with multiple levels of escaping
    /// </summary>
    public static string Unescape(string json, int maxLevels = 5)
    {
        if (string.IsNullOrEmpty(json))
            return json;

        var result = json;
        var previousResult = string.Empty;
        var level = 0;

        while (result != previousResult && level < maxLevels)
        {
            previousResult = result;
            result = UnescapeOnce(result);
            level++;
        }

        return result;
    }

    /// <summary>
    /// Unescape one level of JSON escaping
    /// </summary>
    private static string UnescapeOnce(string json)
    {
        if (string.IsNullOrEmpty(json))
            return json;

        var result = json.Trim();

        // Remove outer quotes if present
        if (result.StartsWith('\"') && result.EndsWith('\"'))
        {
            result = result[1..^1];
        }

        // Unescape JSON quotes
        result = result.Replace("\\\"", "\"");

        // Handle backslash escaping
        if (result.Contains("\\\\"))
        {
            result = result.Replace("\\\\", "\\");
        }

        return result;
    }

    /// <summary>
    /// Try to parse JSON with automatic escaping handling
    /// </summary>
    public static T? TryParseJson<T>(string json) where T : class
    {
        if (string.IsNullOrEmpty(json) || json == "null")
            return null;

        try
        {
            var unescaped = Unescape(json);
            return JsonSerializer.Deserialize<T>(unescaped);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extract a value from JSON string by key
    /// </summary>
    public static string? ExtractValue(string json, string key)
    {
        try
        {
            var unescaped = Unescape(json);
            using var doc = JsonDocument.Parse(unescaped);

            if (doc.RootElement.TryGetProperty(key, out var property))
            {
                return property.GetString();
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return null;
    }
}
