using ZXing;

namespace CheapHelpers.Services.Communication.Barcode;

/// <summary>
/// Interface for barcode generation and reading services
/// </summary>
public interface IBarcodeService
{
    /// <summary>
    /// Event triggered when a barcode is successfully scanned
    /// </summary>
    event Func<string, Task> BarcodeScanned;

    /// <summary>
    /// Generates a barcode image as byte array from input text (async)
    /// </summary>
    /// <param name="input">Text to encode in the barcode</param>
    /// <param name="height">Height of the generated barcode image</param>
    /// <param name="width">Width of the generated barcode image</param>
    /// <param name="format">Barcode format to use</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>JPEG-encoded barcode image as byte array</returns>
    Task<byte[]> GetBarcodeAsync(
        string input,
        int height = 30,
        int width = 100,
        BarcodeFormat format = BarcodeFormat.CODE_39,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Legacy synchronous method for backward compatibility
    /// </summary>
    [Obsolete("Use GetBarcodeAsync instead for better performance")]
    byte[] GetBarcode(string input, int height = 30, int width = 100);

    /// <summary>
    /// Triggers the barcode scanned event with the provided barcode data (async)
    /// </summary>
    /// <param name="barcode">The scanned barcode data</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    Task OnScanAsync(string barcode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Legacy synchronous method for backward compatibility
    /// </summary>
    [Obsolete("Use OnScanAsync instead for better performance")]
    void OnScan(string barcode);

    /// <summary>
    /// Reads barcode data from image bytes
    /// </summary>
    /// <param name="imageBytes">Image data as byte array</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Tuple containing the decoded text and barcode format, or null if no barcode found</returns>
    Task<(string Text, string Format)?> ReadBarcodeAsync(
        byte[] imageBytes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Legacy method signature for backward compatibility
    /// </summary>
    [Obsolete("Use ReadBarcodeAsync(byte[]) instead. Width and height parameters are no longer needed.")]
    Task<(string Text, string Format)?> ReadBarcodeAsync(
        byte[] bytes,
        int width,
        int height,
        CancellationToken cancellationToken = default);
}