using NetVips;

namespace CheapHelpers.Services.Imaging;

/// <summary>
/// Encodes raw pixel buffers and normalises uploaded photos with libvips
/// </summary>
public static class ImageHelpers
{
    private const int MinDimension = 1;

    /// <summary>
    /// Encodes an 8-bit pixel buffer as PNG, optionally shrunk so its longest side is at most <paramref name="maxLongestSide"/>
    /// </summary>
    /// <param name="pixels">Row-major pixels without row padding, <paramref name="width"/> * <paramref name="height"/> * bands bytes</param>
    /// <param name="width">Width in pixels</param>
    /// <param name="height">Height in pixels</param>
    /// <param name="bandOrder">Channel order of <paramref name="pixels"/></param>
    /// <param name="maxLongestSide">Longest side of the output in pixels; never upscales</param>
    /// <returns>The PNG bytes</returns>
    /// <exception cref="ArgumentOutOfRangeException">Width, height or <paramref name="maxLongestSide"/> is below 1</exception>
    /// <exception cref="ArgumentException"><paramref name="pixels"/> does not match the size and band count</exception>
    public static byte[] EncodePng(byte[] pixels, int width, int height, PixelBandOrder bandOrder, int? maxLongestSide = null)
    {
        ArgumentNullException.ThrowIfNull(pixels);
        ArgumentOutOfRangeException.ThrowIfLessThan(width, MinDimension);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, MinDimension);
        ThrowIfInvalidLongestSide(maxLongestSide);

        var bands = bandOrder is PixelBandOrder.Rgb or PixelBandOrder.Bgr ? 3 : 4;
        if (pixels.LongLength != (long)width * height * bands)
            throw new ArgumentException($"Expected {(long)width * height * bands} bytes for {width}x{height} {bandOrder}, got {pixels.LongLength}", nameof(pixels));

        // libvips borrows the array, so everything is encoded before this method returns
        using var raw = Image.NewFromMemory(pixels, width, height, bands, Enums.BandFormat.Uchar);
        using var ordered = bandOrder is PixelBandOrder.Bgr or PixelBandOrder.Bgra ? SwapRedAndBlue(raw) : raw.Copy();
        using var srgb = ordered.Copy(interpretation: Enums.Interpretation.Srgb);
        using var sized = Downsize(srgb, maxLongestSide);
        return sized.PngsaveBuffer();
    }

    /// <summary>
    /// Turns an encoded photo upright and re-encodes it as JPEG without EXIF or other metadata
    /// </summary>
    /// <param name="imageBytes">The encoded image</param>
    /// <param name="maxLongestSide">Longest side of the output in pixels; never upscales</param>
    /// <returns>The JPEG bytes, or null when libvips has no loader for the format (BMP, for one); store the original then</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLongestSide"/> is below 1</exception>
    public static byte[]? NormalizePhoto(byte[] imageBytes, int? maxLongestSide = null)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);
        ThrowIfInvalidLongestSide(maxLongestSide);

        if (Image.FindLoadBuffer(imageBytes) is null)
            return null;

        using var image = Image.NewFromBuffer(imageBytes);
        return SaveUprightJpeg(image, maxLongestSide);
    }

    /// <summary>
    /// Same as <see cref="NormalizePhoto(byte[], int?)"/> for a seekable stream, read from its start and left at an unspecified position
    /// </summary>
    /// <param name="imageStream">A seekable stream holding the encoded image</param>
    /// <param name="maxLongestSide">Longest side of the output in pixels; never upscales</param>
    /// <returns>The JPEG bytes, or null when libvips has no loader for the format (BMP, for one); store the original then</returns>
    /// <exception cref="ArgumentException"><paramref name="imageStream"/> is not seekable</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLongestSide"/> is below 1</exception>
    public static byte[]? NormalizePhoto(Stream imageStream, int? maxLongestSide = null)
    {
        ArgumentNullException.ThrowIfNull(imageStream);
        if (!imageStream.CanSeek)
            throw new ArgumentException("The stream must be seekable", nameof(imageStream));
        ThrowIfInvalidLongestSide(maxLongestSide);

        imageStream.Position = 0;
        var loader = Image.FindLoadStream(imageStream);
        // The probe reads from the stream, so it is rewound before the real load
        imageStream.Position = 0;
        if (loader is null)
            return null;

        using var image = Image.NewFromStream(imageStream);
        return SaveUprightJpeg(image, maxLongestSide);
    }

    private static byte[] SaveUprightJpeg(Image image, int? maxLongestSide)
    {
        using var upright = image.Autorot();
        using var sized = Downsize(upright, maxLongestSide);
        return sized.JpegsaveBuffer(keep: Enums.ForeignKeep.None);
    }

    private static Image SwapRedAndBlue(Image image)
    {
        using var first = image[0];
        using var second = image[1];
        using var third = image[2];
        if (image.Bands == 3)
            return third.Bandjoin(second, first);

        using var alpha = image[3];
        return third.Bandjoin(second, first, alpha);
    }

    private static Image Downsize(Image image, int? maxLongestSide) =>
        maxLongestSide is { } limit && Math.Max(image.Width, image.Height) > limit
            ? image.ThumbnailImage(limit, height: limit, size: Enums.Size.Down)
            : image.Copy();

    private static void ThrowIfInvalidLongestSide(int? maxLongestSide)
    {
        if (maxLongestSide is { } limit)
            ArgumentOutOfRangeException.ThrowIfLessThan(limit, MinDimension, nameof(maxLongestSide));
    }
}
