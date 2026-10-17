using System.Diagnostics;
using ZXing;
using ZXing.Common;

namespace CheapHelpers.Barcodes;

/// <summary>
/// Service for generating and reading barcodes with ZXing.Net, without any native image library
/// </summary>
public class BarcodeService : IBarcodeService
{
    // Default barcode generation constants
    private const int DefaultHeight = 30;
    private const int DefaultWidth = 100;
    private const BarcodeFormat DefaultFormat = BarcodeFormat.CODE_39;

    // ZXing's pixel renderer writes 4 bytes per pixel (BGRA32)
    private const int RenderedBytesPerPixel = 4;

    private event Func<string, Task>? _barcodeScannedHandler;

    /// <summary>
    /// Event triggered when a barcode is successfully scanned
    /// </summary>
    public event Func<string, Task> BarcodeScanned
    {
        add => _barcodeScannedHandler += value;
        remove => _barcodeScannedHandler -= value;
    }

    /// <summary>
    /// Generates a barcode image as byte array from input text
    /// </summary>
    /// <param name="input">Text to encode in the barcode</param>
    /// <param name="height">Height of the generated barcode image</param>
    /// <param name="width">Width of the generated barcode image</param>
    /// <param name="format">Barcode format to use</param>
    /// <returns>PNG-encoded (8-bit grayscale) barcode image as byte array</returns>
    /// <exception cref="ArgumentException">Thrown when input is null or empty</exception>
    /// <exception cref="InvalidOperationException">Thrown when barcode generation fails</exception>
    public byte[] GetBarcode(
        string input,
        int height = DefaultHeight,
        int width = DefaultWidth,
        BarcodeFormat format = DefaultFormat)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input cannot be null or empty", nameof(input));

        if (height <= 0)
            throw new ArgumentException("Height must be positive", nameof(height));

        if (width <= 0)
            throw new ArgumentException("Width must be positive", nameof(width));

        try
        {
            var barcodeWriter = new BarcodeWriterPixelData
            {
                Format = format,
                Options = new EncodingOptions
                {
                    Height = height,
                    Width = width
                }
            };

            var rendered = barcodeWriter.Write(input);

            // The renderer only produces black and white, so one channel is the grayscale value
            var gray = new byte[rendered.Width * rendered.Height];
            for (var i = 0; i < gray.Length; i++)
                gray[i] = rendered.Pixels[i * RenderedBytesPerPixel];

            return PngEncoder.EncodeGray8(gray, rendered.Width, rendered.Height);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to generate barcode for input '{input}': {ex.Message}");
            throw new InvalidOperationException($"Failed to generate barcode: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Triggers the barcode scanned event with the provided barcode data
    /// </summary>
    /// <param name="barcode">The scanned barcode data</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    public async Task OnScanAsync(string barcode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            Debug.WriteLine("Attempted to process null or empty barcode");
            return;
        }

        try
        {
            if (_barcodeScannedHandler != null)
            {
                await _barcodeScannedHandler.Invoke(barcode);
                Debug.WriteLine($"Barcode scan processed successfully: {barcode}");
            }
            else
            {
                Debug.WriteLine($"No handlers registered for barcode scan: {barcode}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error processing barcode scan '{barcode}': {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Reads barcode data from raw pixels, for example a camera frame or a bitmap the platform already decoded
    /// </summary>
    /// <param name="pixels">Pixel buffer, row by row, without padding</param>
    /// <param name="width">Width of the image in pixels</param>
    /// <param name="height">Height of the image in pixels</param>
    /// <param name="format">Byte layout of each pixel in <paramref name="pixels"/></param>
    /// <returns>Tuple containing the decoded text and barcode format, or null if no barcode found</returns>
    /// <exception cref="ArgumentException">Thrown when pixels is empty or a dimension is not positive</exception>
    /// <exception cref="InvalidOperationException">Thrown when decoding fails</exception>
    public (string Text, string Format)? ReadBarcode(
        ReadOnlySpan<byte> pixels,
        int width,
        int height,
        RGBLuminanceSource.BitmapFormat format)
    {
        if (pixels.IsEmpty)
            throw new ArgumentException("Pixels cannot be empty", nameof(pixels));

        if (width <= 0)
            throw new ArgumentException("Width must be positive", nameof(width));

        if (height <= 0)
            throw new ArgumentException("Height must be positive", nameof(height));

        try
        {
            // ZXing's luminance source only takes a byte[], so copy the span once
            var source = new RGBLuminanceSource(pixels.ToArray(), width, height, format);
            var result = new BarcodeReaderGeneric().Decode(source);

            if (result != null)
            {
                Debug.WriteLine($"Barcode read successfully: {result.Text} (Format: {result.BarcodeFormat})");
                return (result.Text, result.BarcodeFormat.ToString());
            }

            Debug.WriteLine("No barcode detected in the provided image");
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading barcode from image: {ex.Message}");
            throw new InvalidOperationException($"Failed to read barcode from image: {ex.Message}", ex);
        }
    }

}
