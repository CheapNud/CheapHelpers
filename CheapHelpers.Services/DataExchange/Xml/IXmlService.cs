namespace CheapHelpers.Services.DataExchange.Xml;

public interface IXmlService
{
    // Dynamic object serialization (original functionality)
    Task ExportDynamic(string filePath, dynamic data);

    // Strongly-typed serialization using XmlSerializer
    Task<T?> DeserializeAsync<T>(string filePath) where T : class;
    Task SerializeAsync<T>(string filePath, T data) where T : class;
    T? DeserializeFromString<T>(string xml) where T : class;
    string SerializeToString<T>(T data) where T : class;
}
