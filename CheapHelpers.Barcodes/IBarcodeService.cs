using ZXing;

namespace CheapHelpers.Barcodes;

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
    /// Generates a barcode image as byte array from input text
    /// </summary>
    /// <param name="input">Text to encode in the barcode</param>
    /// <param name="height">Height of the generated barcode image</param>
    /// <param name="width">Width of the generated barcode image</param>
    /// <param name="format">Barcode format to use</param>
    /// <returns>PNG-encoded (8-bit grayscale) barcode image as byte array</returns>
    byte[] GetBarcode(
        string input,
        int height = 30,
        int width = 100,
        BarcodeFormat format = BarcodeFormat.CODE_39);

    /// <summary>
    /// Triggers the barcode scanned event with the provided barcode data (async)
    /// </summary>
    /// <param name="barcode">The scanned barcode data</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    Task OnScanAsync(string barcode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads barcode data from raw pixels, for example a camera frame or a bitmap the platform already decoded
    /// </summary>
    /// <param name="pixels">Pixel buffer, row by row, without padding</param>
    /// <param name="width">Width of the image in pixels</param>
    /// <param name="height">Height of the image in pixels</param>
    /// <param name="format">Byte layout of each pixel in <paramref name="pixels"/></param>
    /// <returns>Tuple containing the decoded text and barcode format, or null if no barcode found</returns>
    (string Text, string Format)? ReadBarcode(
        ReadOnlySpan<byte> pixels,
        int width,
        int height,
        RGBLuminanceSource.BitmapFormat format);

}
