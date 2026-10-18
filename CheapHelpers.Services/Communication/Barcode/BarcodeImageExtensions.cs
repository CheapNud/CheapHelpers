using CheapHelpers.Barcodes;
using NetVips;
using System.Diagnostics;
using ZXing;

namespace CheapHelpers.Services.Communication.Barcode;

public static class BarcodeImageExtensions
{
    /// <summary>
    /// Reads barcode data from encoded image bytes (JPEG, PNG, GIF, WebP, TIFF, HEIF and AVIF)
    /// </summary>
    /// <remarks>
    /// BMP is not supported, because NetVips.Native ships no BMP loader. Decode a BMP yourself and pass the pixels
    /// to <see cref="IBarcodeService.ReadBarcode(ReadOnlySpan{byte}, int, int, RGBLuminanceSource.BitmapFormat)"/>.
    /// </remarks>
    /// <param name="barcodes">The barcode service that decodes the pixels</param>
    /// <param name="imageBytes">Image data as byte array</param>
    /// <returns>Tuple containing the decoded text and barcode format, or null if no barcode found</returns>
    /// <exception cref="ArgumentException">Thrown when imageBytes is null or empty</exception>
    /// <exception cref="InvalidOperationException">Thrown when the image cannot be loaded or decoded</exception>
    public static (string Text, string Format)? ReadBarcode(this IBarcodeService barcodes, byte[] imageBytes)
    {
        if (imageBytes == null || imageBytes.Length == 0)
            throw new ArgumentException("Image bytes cannot be null or empty", nameof(imageBytes));

        byte[] pixels;
        int width;
        int height;
        try
        {
            using var image = Image.NewFromBuffer(imageBytes);
            // Grey, CMYK and 16-bit files all become 8-bit sRGB here; alpha survives the conversion
            using var srgb = image.Colourspace(Enums.Interpretation.Srgb);
            // Flatten onto white, not the libvips default of black, so bars on a transparent background stay readable
            using var rgb = srgb.HasAlpha() ? srgb.Flatten(background: [255]) : srgb.Copy();
            width = rgb.Width;
            height = rgb.Height;
            pixels = rgb.WriteToMemory<byte>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading barcode from image: {ex.Message}");
            throw new InvalidOperationException($"Failed to read barcode from image: {ex.Message}", ex);
        }

        // The service wraps its own decode errors, so only the image load is wrapped here
        return barcodes.ReadBarcode(pixels, width, height, RGBLuminanceSource.BitmapFormat.RGB24);
    }
}
