using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using ZXing.Common;

namespace CheapHelpers.Services.Communication.Barcode;

/// <summary>
/// Service for generating and reading barcodes using ImageSharp and ZXing libraries
/// </summary>
public class BarcodeService : IBarcodeService
{
    // Default barcode generation constants
    private const int DefaultHeight = 30;
    private const int DefaultWidth = 100;
    private const ZXing.BarcodeFormat DefaultFormat = ZXing.BarcodeFormat.CODE_39;

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
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>JPEG-encoded barcode image as byte array</returns>
    /// <exception cref="ArgumentException">Thrown when input is null or empty</exception>
    /// <exception cref="InvalidOperationException">Thrown when barcode generation fails</exception>
    public async Task<byte[]> GetBarcodeAsync(
        string input,
        int height = DefaultHeight,
        int width = DefaultWidth,
        ZXing.BarcodeFormat format = DefaultFormat,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input cannot be null or empty", nameof(input));

        if (height <= 0)
            throw new ArgumentException("Height must be positive", nameof(height));

        if (width <= 0)
            throw new ArgumentException("Width must be positive", nameof(width));

        try
        {
            var barcodeWriter = new ZXing.ImageSharp.BarcodeWriter<Rgba32>
            {
                Format = format,
                Options = new EncodingOptions
                {
                    Height = height,
                    Width = width
                }
            };

            using var image = barcodeWriter.Write(input);
            using var memoryStream = new MemoryStream();

            await image.SaveAsync(memoryStream, new JpegEncoder(), cancellationToken);
            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to generate barcode for input '{input}': {ex.Message}");
            throw new InvalidOperationException($"Failed to generate barcode: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Legacy synchronous method for backward compatibility
    /// </summary>
    [Obsolete("Use GetBarcodeAsync instead for better performance")]
    public byte[] GetBarcode(string input, int height = DefaultHeight, int width = DefaultWidth)
        => GetBarcodeAsync(input, height, width).GetAwaiter().GetResult();

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
    /// Legacy synchronous method for backward compatibility
    /// </summary>
    [Obsolete("Use OnScanAsync instead for better performance")]
    public void OnScan(string barcode)
        => OnScanAsync(barcode).GetAwaiter().GetResult();

    /// <summary>
    /// Reads barcode data from image bytes
    /// </summary>
    /// <param name="imageBytes">Image data as byte array</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Tuple containing the decoded text and barcode format, or null if no barcode found</returns>
    /// <exception cref="ArgumentException">Thrown when imageBytes is null or empty</exception>
    public async Task<(string Text, string Format)?> ReadBarcodeAsync(
        byte[] imageBytes,
        CancellationToken cancellationToken = default)
    {
        if (imageBytes == null || imageBytes.Length == 0)
            throw new ArgumentException("Image bytes cannot be null or empty", nameof(imageBytes));

        try
        {
            using var imageStream = new MemoryStream(imageBytes);
            using var image = await Image.LoadAsync<Rgba32>(imageStream, cancellationToken);
            var reader = new ZXing.ImageSharp.BarcodeReader<Rgba32>();

            var result = reader.Decode(image);

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

    /// <summary>
    /// Legacy method signature for backward compatibility
    /// Note: width and height parameters are ignored in the new implementation as ImageSharp handles this automatically
    /// </summary>
    [Obsolete("Use ReadBarcodeAsync(byte[]) instead. Width and height parameters are no longer needed.")]
    public async Task<(string Text, string Format)?> ReadBarcodeAsync(
        byte[] bytes,
        int width,
        int height,
        CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Legacy ReadBarcode called with width={width}, height={height}. These parameters are ignored.");
        return await ReadBarcodeAsync(bytes, cancellationToken);
    }
}