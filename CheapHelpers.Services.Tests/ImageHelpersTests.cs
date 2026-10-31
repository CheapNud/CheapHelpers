using CheapHelpers.Services.Imaging;
using NetVips;

namespace CheapHelpers.Services.Tests;

public class ImageHelpersTests
{
    private const int RotateClockwise = 6;
    private const string OrientationField = "orientation";

    [Fact]
    public void EncodePng_Bgra_SwapsToRgba()
    {
        // Pixel 0 pure blue, pixel 1 pure red, both opaque
        byte[] bgra = [255, 0, 0, 255, 0, 0, 255, 255];

        var png = ImageHelpers.EncodePng(bgra, 2, 1, PixelBandOrder.Bgra);

        Assert.Equal([0, 0, 255, 255, 255, 0, 0, 255], DecodePixels(png));
    }

    [Fact]
    public void EncodePng_Rgb_KeepsOrder()
    {
        byte[] rgb = [0, 0, 255, 255, 0, 0];

        var png = ImageHelpers.EncodePng(rgb, 2, 1, PixelBandOrder.Rgb);

        Assert.Equal(rgb, DecodePixels(png));
    }

    [Fact]
    public void EncodePng_WrongLength_ThrowsForPixels()
    {
        var error = Assert.Throws<ArgumentException>(() => ImageHelpers.EncodePng(new byte[7], 2, 1, PixelBandOrder.Bgra));

        Assert.Equal("pixels", error.ParamName);
    }

    [Fact]
    public void EncodePng_ZeroWidthOrCap_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ImageHelpers.EncodePng([], 0, 1, PixelBandOrder.Rgb));
        Assert.Throws<ArgumentOutOfRangeException>(() => ImageHelpers.EncodePng(new byte[3], 1, 1, PixelBandOrder.Rgb, maxLongestSide: 0));
    }

    [Theory]
    [InlineData(100, 100, 50)]
    [InlineData(null, 400, 200)]
    public void EncodePng_LongestSideCap_KeepsAspectRatio(int? maxLongestSide, int expectedWidth, int expectedHeight)
    {
        var png = ImageHelpers.EncodePng(new byte[400 * 200 * 3], 400, 200, PixelBandOrder.Rgb, maxLongestSide);

        using var decoded = Image.NewFromBuffer(png);
        Assert.Equal(expectedWidth, decoded.Width);
        Assert.Equal(expectedHeight, decoded.Height);
    }

    [Fact]
    public void NormalizePhoto_OrientationTag_RotatesAndDropsMetadata()
    {
        var jpeg = LandscapeJpegTaggedClockwise();
        using (var tagged = Image.NewFromBuffer(jpeg))
            Assert.Equal(RotateClockwise, (int)tagged.Get(OrientationField));

        var normalized = ImageHelpers.NormalizePhoto(jpeg);

        Assert.NotNull(normalized);
        using var decoded = Image.NewFromBuffer(normalized);
        Assert.Equal(20, decoded.Width);
        Assert.Equal(40, decoded.Height);
        Assert.False(decoded.Contains(OrientationField));
        // The white left columns of the stored image end up as the top rows once upright
        Assert.True(decoded.Getpoint(10, 5)[0] > 200);
        Assert.True(decoded.Getpoint(10, 35)[0] < 50);
    }

    [Fact]
    public void NormalizePhoto_IccProfile_ConvertsToSrgbBeforeDroppingIt()
    {
        using var red = Image.Black(8, 8, bands: 3) + new double[] { 200, 60, 40 };
        using var srgb = red.Cast(Enums.BandFormat.Uchar).Copy(interpretation: Enums.Interpretation.Srgb);
        using var cmyk = srgb.IccTransform("cmyk", inputProfile: "srgb");
        var jpeg = cmyk.JpegsaveBuffer();
        using var profiled = Image.NewFromBuffer(jpeg);
        using var expected = profiled.IccTransform("srgb", embedded: true);

        var normalized = ImageHelpers.NormalizePhoto(jpeg);

        Assert.NotNull(normalized);
        using var decoded = Image.NewFromBuffer(normalized);
        Assert.Equal(3, decoded.Bands);
        Assert.False(decoded.Contains("icc-profile-data"));
        var expectedPixel = expected.Getpoint(4, 4);
        var actualPixel = decoded.Getpoint(4, 4);
        for (var band = 0; band < 3; band++)
            Assert.InRange(actualPixel[band], expectedPixel[band] - 3, expectedPixel[band] + 3);
    }

    [Fact]
    public void NormalizePhoto_Png_ReturnsJpeg()
    {
        var png = ImageHelpers.EncodePng(new byte[8 * 8 * 3], 8, 8, PixelBandOrder.Rgb);

        var normalized = ImageHelpers.NormalizePhoto(png);

        Assert.NotNull(normalized);
        Assert.StartsWith("VipsForeignLoadJpeg", Image.FindLoadBuffer(normalized));
    }

    [Fact]
    public void NormalizePhoto_Bmp_ReturnsNull()
    {
        var bmp = MinimalBmp();

        Assert.Null(ImageHelpers.NormalizePhoto(bmp));
        using var stream = new MemoryStream(bmp);
        Assert.Null(ImageHelpers.NormalizePhoto(stream));
    }

    [Fact]
    public void NormalizePhoto_LongestSideCap_IsHonoured()
    {
        var png = ImageHelpers.EncodePng(new byte[400 * 200 * 3], 400, 200, PixelBandOrder.Rgb);

        var normalized = ImageHelpers.NormalizePhoto(png, maxLongestSide: 100);

        Assert.NotNull(normalized);
        using var decoded = Image.NewFromBuffer(normalized);
        Assert.Equal(100, decoded.Width);
        Assert.Equal(50, decoded.Height);
    }

    [Fact]
    public void NormalizePhoto_Stream_MatchesByteOverload()
    {
        var jpeg = LandscapeJpegTaggedClockwise();
        using var stream = new MemoryStream(jpeg);

        Assert.Equal(ImageHelpers.NormalizePhoto(jpeg), ImageHelpers.NormalizePhoto(stream));
    }

    [Fact]
    public void DrawingToPng_Tiff_GivesGreyscalePng()
    {
        var png = ImageHelpers.DrawingToPng(LineDrawingTiff(400, 200), maxWidth: 100);

        Assert.StartsWith("VipsForeignLoadPng", Image.FindLoadBuffer(png));
        using var decoded = Image.NewFromBuffer(png);
        Assert.Equal(1, decoded.Bands);
        Assert.Equal(Enums.BandFormat.Uchar, decoded.Format);
        Assert.Equal(100, decoded.Width);
        Assert.Equal(50, decoded.Height);
    }

    [Fact]
    public void DrawingToPng_TallDrawing_IsNotCappedByHeight()
    {
        var png = ImageHelpers.DrawingToPng(LineDrawingTiff(200, 800), maxWidth: 100);

        using var decoded = Image.NewFromBuffer(png);
        Assert.Equal(100, decoded.Width);
        Assert.Equal(400, decoded.Height);
    }

    [Fact]
    public void DrawingToPng_SmallDrawing_IsNotEnlarged()
    {
        var png = ImageHelpers.DrawingToPng(LineDrawingTiff(60, 30), maxWidth: 100);

        using var decoded = Image.NewFromBuffer(png);
        Assert.Equal(60, decoded.Width);
        Assert.Equal(30, decoded.Height);
    }

    [Fact]
    public void DrawingToPng_ThinLines_SurviveShrinking()
    {
        var png = ImageHelpers.DrawingToPng(LineDrawingTiff(400, 200), maxWidth: 100);

        using var decoded = Image.NewFromBuffer(png);
        // Each strip stays clear of the other line, so a bright pixel there can only come from the line under test
        using var column = decoded.ExtractArea(50, 0, 1, 20);
        using var row = decoded.ExtractArea(0, 25, 40, 1);
        Assert.True(column.Max() > 0);
        Assert.True(row.Max() > 0);
        Assert.Equal(0, decoded.Getpoint(10, 10)[0]);
    }

    [Fact]
    public void DrawingToPng_ColourPng_GivesOneBand()
    {
        using var colour = Image.Black(80, 40, bands: 3) + new double[] { 200, 60, 40 };
        using var srgb = colour.Cast(Enums.BandFormat.Uchar).Copy(interpretation: Enums.Interpretation.Srgb);

        var png = ImageHelpers.DrawingToPng(srgb.PngsaveBuffer(), maxWidth: 100);

        using var decoded = Image.NewFromBuffer(png);
        Assert.Equal(1, decoded.Bands);
        Assert.Equal(80, decoded.Width);
        Assert.Equal(40, decoded.Height);
    }

    [Fact]
    public void DrawingToPng_Garbage_ThrowsVipsException()
    {
        var garbage = new byte[64];
        new Random(1).NextBytes(garbage);

        Assert.Throws<VipsException>(() => ImageHelpers.DrawingToPng(garbage, maxWidth: 100));
        Assert.Throws<ArgumentOutOfRangeException>(() => ImageHelpers.DrawingToPng(LineDrawingTiff(60, 30), maxWidth: 0));
    }

    private static byte[] DecodePixels(byte[] png)
    {
        using var decoded = Image.NewFromBuffer(png);
        return decoded.WriteToMemory<byte>();
    }

    /// <summary>
    /// A 40x20 JPEG, white in the left 10 columns and black elsewhere, tagged to be shown rotated 90 degrees clockwise
    /// </summary>
    private static byte[] LandscapeJpegTaggedClockwise()
    {
        using var coordinates = Image.Xyz(40, 20);
        using var x = coordinates[0];
        using var white = x < 10;
        using var rgb = white.Bandjoin(white, white);
        using var srgb = rgb.Copy(interpretation: Enums.Interpretation.Srgb);
        using var tagged = srgb.Mutate(mutable => mutable.Set(GValue.GIntType, OrientationField, RotateClockwise));
        return tagged.JpegsaveBuffer();
    }

    /// <summary>
    /// A 1-bit CCITT G4 TIFF, black with a one-pixel white line down the middle column and one across the middle row
    /// </summary>
    private static byte[] LineDrawingTiff(int width, int height)
    {
        using var coordinates = Image.Xyz(width, height);
        using var x = coordinates[0];
        using var y = coordinates[1];
        using var vertical = x.Equal(width / 2);
        using var horizontal = y.Equal(height / 2);
        using var lines = vertical | horizontal;
        using var page = lines.Copy(interpretation: Enums.Interpretation.Bw);
        return page.TiffsaveBuffer(compression: Enums.ForeignTiffCompression.Ccittfax4, bitdepth: 1);
    }

    /// <summary>
    /// A 2x2 24-bit BMP: 54-byte header plus two rows padded to 8 bytes
    /// </summary>
    private static byte[] MinimalBmp()
    {
        const int headerSize = 54;
        const int pixelBytes = 16;
        using var buffer = new MemoryStream();
        using var writer = new BinaryWriter(buffer);
        writer.Write("BM"u8);
        writer.Write(headerSize + pixelBytes);
        writer.Write(0);
        writer.Write(headerSize);
        writer.Write(40);
        writer.Write(2);
        writer.Write(2);
        writer.Write((short)1);
        writer.Write((short)24);
        writer.Write(0);
        writer.Write(pixelBytes);
        writer.Write(2835);
        writer.Write(2835);
        writer.Write(0);
        writer.Write(0);
        writer.Write(new byte[pixelBytes]);
        writer.Flush();
        return buffer.ToArray();
    }
}
