namespace CheapHelpers.Services
{
    public interface ICsvService
    {
        Task Export(string filePath, IEnumerable<string> list);
        Task Export(string filePath, IEnumerable<dynamic> list);

    }
}