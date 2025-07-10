namespace CheapHelpers.Services
{
    public interface IXlsxService
    {
        Task Generate(string filepath, List<dynamic> records);
    }
}
