using System.Diagnostics;
using System.Dynamic;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace CheapHelpers.Services.DataExchange.Xml;

public class XmlService : IXmlService
{
    private const string XmlVersion = "1.0";
    private const bool XmlStandalone = true;
    private const string RootElementName = "Root";
    private const string ItemsContainerName = "Items";
    private const string ItemElementName = "Item";
    private const string DataElementName = "Data";
    private const string FallbackElementName = "Element";
    private const string ElementNameSeparator = "_";

    #region Dynamic Object Serialization

    public async Task ExportDynamic(string filePath, dynamic data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(data);

        try
        {
            var xml = await Task.Run(() => ConvertToXml(data));
            await File.WriteAllTextAsync(filePath, xml.ToString());

            Debug.WriteLine($"Successfully exported XML file: {filePath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to export XML file '{filePath}': {ex.Message}");
            throw;
        }
    }

    private static XDocument ConvertToXml(dynamic data)
    {
        var rootElement = new XElement(RootElementName);

        if (data is IEnumerable<dynamic> collection && data is not string)
        {
            // Handle collections
            var items = new XElement(ItemsContainerName);
            foreach (var item in collection)
            {
                items.Add(CreateXmlElement(ItemElementName, item));
            }
            rootElement.Add(items);
        }
        else
        {
            // Handle single object
            rootElement.Add(CreateXmlElement(DataElementName, data));
        }

        return new XDocument(new XDeclaration(XmlVersion, Encoding.UTF8.WebName, XmlStandalone ? "yes" : "no"), rootElement);
    }

    private static XElement CreateXmlElement(string elementName, dynamic obj)
    {
        var element = new XElement(elementName);

        if (obj is null)
        {
            return element;
        }

        if (obj is ExpandoObject expando)
        {
            var dict = (IDictionary<string, object>)expando;
            foreach (var kvp in dict)
            {
                element.Add(CreateXmlElement(SanitizeElementName(kvp.Key), kvp.Value));
            }
        }
        else if (obj is IEnumerable<dynamic> collection && obj is not string)
        {
            foreach (var item in collection)
            {
                element.Add(CreateXmlElement(ItemElementName, item));
            }
        }
        else if (IsPrimitiveType(obj))
        {
            element.Value = obj.ToString() ?? string.Empty;
        }
        else
        {
            // Handle regular objects using reflection
            var properties = obj.GetType().GetProperties();
            foreach (var property in properties)
            {
                try
                {
                    var value = property.GetValue(obj);
                    element.Add(CreateXmlElement(SanitizeElementName(property.Name), value));
                }
                catch
                {
                    // Skip properties that can't be read
                }
            }
        }

        return element;
    }

    private static string SanitizeElementName(string name)
    {
        // Ensure valid XML element name
        if (string.IsNullOrWhiteSpace(name))
            return FallbackElementName;

        // Replace invalid characters with underscore
        var sanitized = name.Replace(" ", ElementNameSeparator).Replace("-", ElementNameSeparator);

        // Ensure it starts with a letter or underscore
        if (!char.IsLetter(sanitized[0]) && sanitized[0] != '_')
        {
            sanitized = ElementNameSeparator + sanitized;
        }

        return sanitized;
    }

    private static bool IsPrimitiveType(object obj)
    {
        var type = obj.GetType();
        return type.IsPrimitive ||
               type == typeof(string) ||
               type == typeof(DateTime) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(TimeSpan) ||
               type == typeof(Guid) ||
               type == typeof(decimal);
    }

    #endregion

    #region Strongly-Typed Serialization

    /// <summary>
    /// Deserialize XML from a file using XmlSerializer
    /// </summary>
    public async Task<T?> DeserializeAsync<T>(string filePath) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        try
        {
            return await Task.Run(() =>
            {
                var serializer = new XmlSerializer(typeof(T));
                using var reader = new StreamReader(filePath);
                return serializer.Deserialize(reader) as T;
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to deserialize XML file '{filePath}': {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Serialize an object to XML file using XmlSerializer
    /// </summary>
    public async Task SerializeAsync<T>(string filePath, T data) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(data);

        try
        {
            await Task.Run(() =>
            {
                var serializer = new XmlSerializer(typeof(T));

                // Ensure output directory exists
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using var writer = new StreamWriter(filePath);
                serializer.Serialize(writer, data);
            });

            Debug.WriteLine($"Successfully serialized to XML file: {filePath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to serialize to XML file '{filePath}': {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Deserialize XML from a string using XmlSerializer
    /// </summary>
    public T? DeserializeFromString<T>(string xml) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xml);

        try
        {
            var serializer = new XmlSerializer(typeof(T));
            using var reader = new StringReader(xml);
            return serializer.Deserialize(reader) as T;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to deserialize XML string: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Serialize an object to XML string using XmlSerializer
    /// </summary>
    public string SerializeToString<T>(T data) where T : class
    {
        ArgumentNullException.ThrowIfNull(data);

        try
        {
            var serializer = new XmlSerializer(typeof(T));
            using var writer = new StringWriter();
            serializer.Serialize(writer, data);
            return writer.ToString();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to serialize to XML string: {ex.Message}");
            throw;
        }
    }

    #endregion
}