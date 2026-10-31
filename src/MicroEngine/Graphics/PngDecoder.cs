using System.Buffers.Binary;
using System.IO.Compression;

namespace MicroEngine.Graphics;

/// <summary>
/// Managed PNG reader for authored art files: 8-bit greyscale, greyscale+alpha, RGB, RGBA and
/// 1/2/4/8-bit palette images, non-interlaced, with tRNS transparency. That covers what pixel-art
/// tools export; anything else fails with a clear message instead of decoding garbage.
/// </summary>
internal static class PngDecoder
{
    private static ReadOnlySpan<byte> Signature => [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];

    public static PixelImage Decode(ReadOnlySpan<byte> data)
    {
        if (data.Length < 8 || !data[..8].SequenceEqual(Signature)) throw new InvalidDataException("Not a PNG file.");
        int width = 0, height = 0, bitDepth = 0, colorType = -1;
        byte[]? palette = null, transparency = null;
        using var compressed = new MemoryStream();
        int p = 8;
        while (p + 8 <= data.Length)
        {
            int length = BinaryPrimitives.ReadInt32BigEndian(data[p..]);
            var type = System.Text.Encoding.ASCII.GetString(data.Slice(p + 4, 4));
            if (length < 0 || p + 12 + length > data.Length) throw new InvalidDataException("Truncated PNG chunk.");
            var chunk = data.Slice(p + 8, length);
            p += 12 + length;
            switch (type)
            {
                case "IHDR":
                    width = BinaryPrimitives.ReadInt32BigEndian(chunk);
                    height = BinaryPrimitives.ReadInt32BigEndian(chunk[4..]);
                    bitDepth = chunk[8];
                    colorType = chunk[9];
                    if (chunk[12] != 0) throw new NotSupportedException("Interlaced PNGs are not supported; re-export without interlacing.");
                    break;
                case "PLTE": palette = chunk.ToArray(); break;
                case "tRNS": transparency = chunk.ToArray(); break;
                case "IDAT": compressed.Write(chunk); break;
                case "IEND": p = data.Length; break;
            }
        }
        if (width <= 0 || height <= 0) throw new InvalidDataException("PNG has no image header.");
        int channels = colorType switch { 0 => 1, 2 => 3, 3 => 1, 4 => 2, 6 => 4, _ => throw new NotSupportedException($"PNG colour type {colorType} is not supported.") };
        bool paletted = colorType == 3;
        if (paletted ? bitDepth is not (1 or 2 or 4 or 8) : bitDepth != 8)
            throw new NotSupportedException($"PNG bit depth {bitDepth} is not supported for colour type {colorType}.");
        if (paletted && palette is null) throw new InvalidDataException("Paletted PNG has no PLTE chunk.");

        int bitsPerPixel = channels * bitDepth;
        int stride = (width * bitsPerPixel + 7) / 8;
        int bpp = Math.Max(1, bitsPerPixel / 8);
        var raw = new byte[(stride + 1) * height];
        compressed.Position = 0;
        using (var z = new ZLibStream(compressed, CompressionMode.Decompress))
        {
            int read = 0;
            while (read < raw.Length)
            {
                int n = z.Read(raw, read, raw.Length - read);
                if (n == 0) throw new InvalidDataException("PNG image data is truncated.");
                read += n;
            }
        }

        var image = new PixelImage(width, height);
        var previous = new byte[stride];
        var line = new byte[stride];
        for (int y = 0; y < height; y++)
        {
            int filter = raw[y * (stride + 1)];
            Array.Copy(raw, y * (stride + 1) + 1, line, 0, stride);
            Unfilter(filter, line, previous, bpp);
            for (int x = 0; x < width; x++) image[x, y] = Pixel(line, x, colorType, bitDepth, palette, transparency);
            (previous, line) = (line, previous);
        }
        return image;
    }

    private static void Unfilter(int filter, byte[] line, byte[] previous, int bpp)
    {
        for (int i = 0; i < line.Length; i++)
        {
            int a = i >= bpp ? line[i - bpp] : 0, b = previous[i], c = i >= bpp ? previous[i - bpp] : 0;
            line[i] = (byte)(line[i] + filter switch
            {
                0 => 0,
                1 => a,
                2 => b,
                3 => (a + b) / 2,
                4 => Paeth(a, b, c),
                _ => throw new InvalidDataException($"Unknown PNG filter {filter}."),
            });
        }
    }

    private static int Paeth(int a, int b, int c)
    {
        int p = a + b - c, pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }

    private static Color Pixel(byte[] line, int x, int colorType, int bitDepth, byte[]? palette, byte[]? trns)
    {
        switch (colorType)
        {
            case 0:
            {
                byte g = line[x];
                bool clear = trns is { Length: >= 2 } && BinaryPrimitives.ReadUInt16BigEndian(trns) == g;
                return new Color(g, g, g, clear ? (byte)0 : (byte)255);
            }
            case 2:
            {
                byte r = line[x * 3], g = line[x * 3 + 1], b = line[x * 3 + 2];
                bool clear = trns is { Length: >= 6 } && trns[1] == r && trns[3] == g && trns[5] == b;
                return new Color(r, g, b, clear ? (byte)0 : (byte)255);
            }
            case 3:
            {
                int perByte = 8 / bitDepth;
                int index = (line[x / perByte] >> (8 - bitDepth * (x % perByte + 1))) & ((1 << bitDepth) - 1);
                if (index * 3 + 2 >= palette!.Length) return Color.Transparent;
                byte a = trns is not null && index < trns.Length ? trns[index] : (byte)255;
                return new Color(palette[index * 3], palette[index * 3 + 1], palette[index * 3 + 2], a);
            }
            case 4:
                return new Color(line[x * 2], line[x * 2], line[x * 2], line[x * 2 + 1]);
            default:
                return new Color(line[x * 4], line[x * 4 + 1], line[x * 4 + 2], line[x * 4 + 3]);
        }
    }
}
