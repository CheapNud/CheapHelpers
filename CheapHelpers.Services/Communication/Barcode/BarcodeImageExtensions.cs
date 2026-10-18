using CheapHelpers.Barcodes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using ZXing;

namespace CheapHelpers.Services.Communication.Barcode;

public static class BarcodeImageExtensions
{
    // ImageSharp's Rgba32 is 4 bytes per pixel
    private const int Rgba32BytesPerPixel = 4;

    /// <summary>
    /// Reads barcode data from encoded image bytes (JPEG, PNG and the other formats ImageSharp decodes)
    /// </summary>
    /// <param name="barcodes">The barcode service that decodes the pixels</param>
    /// <param name="imageBytes">Image data as byte array</param>
    /// <returns>Tuple containing the decoded text and barcode format, or null if no barcode found</returns>
    /// <exception cref="ArgumentException">Thrown when imageBytes is null or empty</exception>
    /// <exception cref="InvalidOperationException">Thrown when the image cannot be loaded or decoded</exception>
    public static (string Text, string Format)? ReadBarcode(this IBarcodeService barcodes, byte[] imageBytes)
    {
        if (imageBytes == null || imageBytes.Length == 0)
            throw new ArgumentException("Image bytes cannot be null or empty", nameof(imageBytes));

        // This is the only place Services decodes image files for barcodes; #17 swaps the decoder
        byte[] pixels;
        int width;
        int height;
        try
        {
            using var image = Image.Load<Rgba32>(imageBytes);
            width = image.Width;
            height = image.Height;
            pixels = new byte[width * height * Rgba32BytesPerPixel];
            image.CopyPixelDataTo(pixels);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading barcode from image: {ex.Message}");
            throw new InvalidOperationException($"Failed to read barcode from image: {ex.Message}", ex);
        }

        // The service wraps its own decode errors, so only the image load is wrapped here
        return barcodes.ReadBarcode(pixels, width, height, RGBLuminanceSource.BitmapFormat.RGBA32);
    }
}
