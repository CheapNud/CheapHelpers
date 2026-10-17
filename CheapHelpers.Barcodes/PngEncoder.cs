using System.Buffers.Binary;
using System.IO.Compression;
using System.IO.Hashing;

namespace CheapHelpers.Barcodes;

/// <summary>
/// Minimal PNG writer for 8-bit grayscale images, so the package needs no image library
/// </summary>
internal static class PngEncoder
{
    private static ReadOnlySpan<byte> Signature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static byte[] EncodeGray8(ReadOnlySpan<byte> gray, int width, int height)
    {
        using var png = new MemoryStream();
        png.Write(Signature);

        Span<byte> header = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header[4..], height);
        header[8] = 8; // bit depth
        // colour type 0 (grayscale), compression, filter and interlace all stay 0
        WriteChunk(png, "IHDR"u8, header);

        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
        {
            for (var row = 0; row < height; row++)
            {
                zlib.WriteByte(0); // filter type None
                zlib.Write(gray.Slice(row * width, width));
            }
        }
        WriteChunk(png, "IDAT"u8, compressed.GetBuffer().AsSpan(0, (int)compressed.Length));

        WriteChunk(png, "IEND"u8, []);
        return png.ToArray();
    }

    private static void WriteChunk(Stream png, ReadOnlySpan<byte> chunkType, ReadOnlySpan<byte> chunkBody)
    {
        Span<byte> number = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(number, chunkBody.Length);
        png.Write(number);
        png.Write(chunkType);
        png.Write(chunkBody);

        var crc = new Crc32();
        crc.Append(chunkType);
        crc.Append(chunkBody);
        BinaryPrimitives.WriteUInt32BigEndian(number, crc.GetCurrentHashAsUInt32());
        png.Write(number);
    }
}
