namespace CheapHelpers.Services.Export.Excel
{
    public interface IXlsxService
    {
        Task Generate(string filepath, List<dynamic> records);
    }
}
