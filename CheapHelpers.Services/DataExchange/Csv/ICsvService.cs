namespace CheapHelpers.Services.DataExchange.Csv
{
    public interface ICsvService
    {
        Task Export(string filePath, IEnumerable<string> list);
        Task Export(string filePath, IEnumerable<dynamic> list);

        Task ExportToStreamAsync<T>(Stream stream, IEnumerable<T> records, string? culture = null, string? delimiter = null);
        Task<byte[]> ExportToBytesAsync<T>(IEnumerable<T> records, string? culture = null, string? delimiter = null);
    }
}