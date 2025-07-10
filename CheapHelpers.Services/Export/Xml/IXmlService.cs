namespace CheapHelpers.Services.Export.Xml
{
    public interface IXmlService
    {
        Task Export(string filePath, dynamic data);
    }
}
