using System.Diagnostics;
using System.Text.Json;

namespace CheapHelpers.Blazor.Hybrid.WebView;

/// <summary>
/// Helper class for parsing JSON data from WebView with escaped JSON handling
/// WebView has its own quirks and formatting which can't easily be handled out-of-the-box
/// This class abstracts the ugly bits away
/// </summary>
public static class WebViewJsonParser
{
    /// <summary>
    /// Parse JSON from WebView with automatic escaping handling
    /// </summary>
    public static T? ParseJson<T>(string? jsonData) where T : class
    {
        if (string.IsNullOrEmpty(jsonData) || jsonData == "null")
            return null;

        try
        {
            var cleanData = UnescapeWebViewJson(jsonData);
            return JsonSerializer.Deserialize<T>(cleanData);
        }
        catch (JsonException ex)
        {
            Debug.WriteLine($"WebView JSON parsing error: {ex.Message}");
            Debug.WriteLine($"Raw data: {jsonData[..Math.Min(200, jsonData.Length)]}...");
            return null;
        }
    }

    /// <summary>
    /// Parse JSON element from WebView
    /// </summary>
    public static JsonElement? ParseJsonElement(string? jsonData)
    {
        if (string.IsNullOrEmpty(jsonData) || jsonData == "null")
            return null;

        try
        {
            var cleanData = UnescapeWebViewJson(jsonData);
            return JsonSerializer.Deserialize<JsonElement>(cleanData);
        }
        catch (JsonException ex)
        {
            Debug.WriteLine($"WebView JSON element parsing error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Unescape JSON from WebView's multiple levels of escaping
    /// Handles the disgusting WebView JSON escaping quirks
    /// </summary>
    public static string UnescapeWebViewJson(string jsonData)
    {
        if (string.IsNullOrEmpty(jsonData))
            return jsonData;

        var cleanData = jsonData;

        // Remove outer quotes if present (WebView often wraps in quotes)
        if (cleanData.StartsWith('\"') && cleanData.EndsWith('\"'))
        {
            cleanData = cleanData[1..^1];
        }

        // Unescape JSON quotes (multiple passes for nested escaping)
        cleanData = cleanData.Replace("\\\"", "\"");

        // Handle additional backslash escaping if present
        if (cleanData.Contains("\\\\"))
        {
            cleanData = cleanData.Replace("\\\\", "\\");
        }

        return cleanData.Trim();
    }

    /// <summary>
    /// Extract a string property from WebView JSON
    /// </summary>
    public static string? ExtractStringProperty(string? jsonData, string propertyName)
    {
        var element = ParseJsonElement(jsonData);
        if (element == null)
            return null;

        if (element.Value.TryGetProperty(propertyName, out var property) &&
            property.ValueKind != JsonValueKind.Null)
        {
            return property.GetString();
        }

        return null;
    }

    /// <summary>
    /// Extract multiple properties from WebView JSON into a dictionary
    /// </summary>
    public static Dictionary<string, string> ExtractProperties(string? jsonData, params string[] propertyNames)
    {
        var result = new Dictionary<string, string>();
        var element = ParseJsonElement(jsonData);

        if (element == null)
            return result;

        foreach (var propertyName in propertyNames)
        {
            if (element.Value.TryGetProperty(propertyName, out var property) &&
                property.ValueKind != JsonValueKind.Null)
            {
                var value = property.GetString();
                if (!string.IsNullOrEmpty(value))
                {
                    result[propertyName] = value;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Try to extract a DateTime property from WebView JSON
    /// </summary>
    public static DateTime? ExtractDateTimeProperty(string? jsonData, string propertyName)
    {
        var stringValue = ExtractStringProperty(jsonData, propertyName);
        if (string.IsNullOrEmpty(stringValue))
            return null;

        if (DateTime.TryParse(stringValue, out var dateTime))
            return dateTime;

        return null;
    }

    /// <summary>
    /// Try to extract a boolean property from WebView JSON
    /// </summary>
    public static bool? ExtractBooleanProperty(string? jsonData, string propertyName)
    {
        var element = ParseJsonElement(jsonData);
        if (element == null)
            return null;

        if (element.Value.TryGetProperty(propertyName, out var property))
        {
            if (property.ValueKind == JsonValueKind.True)
                return true;
            if (property.ValueKind == JsonValueKind.False)
                return false;
        }

        return null;
    }

    /// <summary>
    /// Flatten nested JSON escaping levels (WebView can have 3+ levels of escaping)
    /// </summary>
    public static string DeepUnescape(string jsonData, int maxLevels = 5)
    {
        var result = jsonData;
        var previousResult = string.Empty;
        var level = 0;

        // Keep unescaping until no more changes or max levels reached
        while (result != previousResult && level < maxLevels)
        {
            previousResult = result;
            result = UnescapeWebViewJson(result);
            level++;
        }

        Debug.WriteLine($"Deep unescape completed in {level} levels");
        return result;
    }
}
