namespace CheapHelpers.Services.DataExchange.Xml
{
    public interface IXmlService
    {
        Task Export(string filePath, dynamic data);
    }
}
