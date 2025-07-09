using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace CheapHelpers;

public static class DynamicHelper
{
    /// <summary>
    /// Checks if a dynamic object has a specific property
    /// </summary>
    public static bool HasProperty(dynamic obj, string name)
    {
        ArgumentNullException.ThrowIfNull(obj);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (obj is ExpandoObject)
        {
            return ((IDictionary<string, object>)obj).ContainsKey(name);
        }

        if (obj is JObject jobj)
        {
            return jobj.ContainsKey(name);
        }

        // For other dynamic types, use reflection - cast to object to avoid dynamic dispatch
        var objType = ((object)obj).GetType();
        return objType.GetProperty(name) is not null;
    }

    /// <summary>
    /// Gets all property names from a dynamic object
    /// </summary>
    public static List<string> GetPropertyNames(dynamic obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        if (obj is ExpandoObject expando)
        {
            return [.. ((IDictionary<string, object>)expando).Keys];
        }

        if (obj is JObject jObject)
        {
            return [.. jObject.Properties().Select(p => p.Name)];
        }

        // For other dynamic types, use reflection - cast to object to avoid dynamic dispatch
        var objType = ((object)obj).GetType();
        return [.. objType.GetProperties().Select(p => p.Name)];
    }

    /// <summary>
    /// Gets the value of a specific property from a dynamic object
    /// </summary>
    public static object? GetPropertyValue(dynamic obj, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(obj);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        if (obj is ExpandoObject expando)
        {
            var dict = (IDictionary<string, object>)expando;
            return dict.TryGetValue(propertyName, out var value) ? value : null;
        }

        if (obj is JObject jObject)
        {
            return jObject.TryGetValue(propertyName, out var token) ? token?.ToObject<object>() : null;
        }

        // For other dynamic types, use reflection - cast to object to avoid dynamic dispatch
        var objType = ((object)obj).GetType();
        var property = objType.GetProperty(propertyName);
        return property?.GetValue(obj);
    }

    /// <summary>
    /// Gets all property names and values from a dynamic object as key-value pairs
    /// </summary>
    public static Dictionary<string, object?> GetPropertiesAsDictionary(dynamic obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        var properties = GetPropertyNames(obj);
        var result = new Dictionary<string, object?>();

        foreach (var propertyName in properties)
        {
            result[propertyName] = GetPropertyValue(obj, propertyName);
        }

        return result;
    }
}