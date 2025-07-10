namespace CheapHelpers.Services.DataExchange.Excel
{
    public interface IXlsxService
    {
        Task Generate(string filepath, List<dynamic> records);
    }
}
