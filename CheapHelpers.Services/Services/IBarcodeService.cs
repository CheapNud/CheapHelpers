namespace CheapHelpers.Services
{
    public interface IBarcodeService
    {
        event Func<string, Task> BarcodeScanned;
        void OnScan(string barcode);
        Task<(string, string)> ReadBarcode(byte[] bytes, int width, int height);
        byte[] GetBarcode(string input, int height = 30, int width = 100);
    }
}
