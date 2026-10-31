namespace CheapHelpers.Services.Imaging;

/// <summary>
/// Byte order of the channels in an 8-bit raw pixel buffer
/// </summary>
public enum PixelBandOrder
{
    /// <summary>Red, green, blue</summary>
    Rgb,

    /// <summary>Blue, green, red</summary>
    Bgr,

    /// <summary>Red, green, blue, alpha</summary>
    Rgba,

    /// <summary>Blue, green, red, alpha</summary>
    Bgra,
}
