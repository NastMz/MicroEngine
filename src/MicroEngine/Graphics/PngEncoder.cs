using System.Buffers.Binary;
using System.IO.Compression;

namespace MicroEngine.Graphics;

/// <summary>Writes 8-bit RGBA PNG files (no filtering), for backends that read frames back as raw pixels.</summary>
public static class PngEncoder
{
    private static readonly uint[] CrcTable = BuildCrcTable();

    /// <summary>Encodes <paramref name="rgba"/> (width × height × 4 bytes, rows top to bottom), each pixel repeated <paramref name="scale"/> times in both directions.</summary>
    public static byte[] Encode(ReadOnlySpan<byte> rgba, int width, int height, int scale = 1)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (rgba.Length != width * height * 4) throw new ArgumentException("Pixel data does not match the size.", nameof(rgba));
        scale = Math.Max(1, scale);
        int w = width * scale, h = height * scale;
        using var raw = new MemoryStream();
        using (var z = new ZLibStream(raw, CompressionLevel.Fastest, leaveOpen: true))
        {
            var line = new byte[1 + w * 4];
            for (int y = 0; y < h; y++)
            {
                var source = rgba.Slice(y / scale * width * 4, width * 4);
                for (int x = 0; x < w; x++) source.Slice(x / scale * 4, 4).CopyTo(line.AsSpan(1 + x * 4));
                z.Write(line);
            }
        }
        using var png = new MemoryStream();
        png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        Span<byte> header = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, w);
        BinaryPrimitives.WriteInt32BigEndian(header[4..], h);
        header[8] = 8; header[9] = 6; header[10] = header[11] = header[12] = 0;
        Chunk(png, "IHDR"u8, header);
        Chunk(png, "IDAT"u8, raw.ToArray());
        Chunk(png, "IEND"u8, []);
        return png.ToArray();
    }

    private static void Chunk(Stream s, ReadOnlySpan<byte> type, ReadOnlySpan<byte> body)
    {
        Span<byte> number = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(number, body.Length);
        s.Write(number);
        s.Write(type);
        s.Write(body);
        uint crc = Crc(Crc(0xFFFFFFFFu, type), body) ^ 0xFFFFFFFFu;
        BinaryPrimitives.WriteUInt32BigEndian(number, crc);
        s.Write(number);
    }

    private static uint Crc(uint crc, ReadOnlySpan<byte> data)
    {
        foreach (var b in data) crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return crc;
    }

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            uint c = n;
            for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            table[n] = c;
        }
        return table;
    }
}
