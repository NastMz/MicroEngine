using System.Buffers.Binary;
using System.IO.Compression;

namespace ContentBench;

/// <summary>
/// A prototype, to measure how much of a PNG's decoding time is the decoder rather than the format: the pixel data is
/// inflated by the runtime's zlib (<see cref="ZLibStream"/>) instead of a managed port, row by row, and each row's
/// filter is undone straight into the output. It reads only what an art tool usually writes for game art (8 bits per
/// channel, RGB or RGBA, not interlaced) and answers null for anything else.
/// </summary>
internal static class FastPng
{
    public static byte[]? Decode(byte[] png, out int width, out int height)
    {
        width = height = 0;
        ReadOnlySpan<byte> signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        if (png.Length < 57 || !png.AsSpan(0, 8).SequenceEqual(signature)) return null;
        var data = new List<(int Offset, int Length)>();
        int channels = 0;
        for (int position = 8; position + 12 <= png.Length;)
        {
            int length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(position));
            uint type = BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(position + 4));
            if (length < 0 || position + 12 + (long)length > png.Length) return null;
            switch (type)
            {
                case 0x49484452: // IHDR
                    if (length != 13) return null;
                    width = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(position + 8));
                    height = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(position + 12));
                    byte depth = png[position + 16], color = png[position + 17], interlace = png[position + 20];
                    if (width <= 0 || height <= 0 || depth != 8 || interlace != 0 || color is not (2 or 6)) return null;
                    channels = color == 6 ? 4 : 3;
                    break;
                case 0x49444154: data.Add((position + 8, length)); break; // IDAT
                case 0x74524E53: return null; // tRNS: a colour key
                case 0x49454E44: position = png.Length; continue; // IEND
            }
            position += 12 + length;
        }
        if (channels == 0 || data.Count == 0 || (long)width * height * 4 > int.MaxValue) return null;

        int stride = width * channels;
        var output = new byte[width * height * 4];
        var raw = new byte[stride + 1];
        byte[] current = channels == 4 ? [] : new byte[stride], previous = channels == 4 ? [] : new byte[stride];
        using var inflate = new ZLibStream(new Segments(png, data), CompressionMode.Decompress);
        for (int y = 0; y < height; y++)
        {
            inflate.ReadExactly(raw);
            Span<byte> row = channels == 4 ? output.AsSpan(y * stride, stride) : current;
            ReadOnlySpan<byte> above = channels == 4 ? (y > 0 ? output.AsSpan((y - 1) * stride, stride) : default) : (y > 0 ? previous : default);
            Unfilter(raw[0], raw.AsSpan(1), row, above, channels);
            if (channels == 3)
            {
                var target = output.AsSpan(y * width * 4, width * 4);
                for (int x = 0; x < width; x++)
                {
                    target[x * 4] = row[x * 3]; target[x * 4 + 1] = row[x * 3 + 1]; target[x * 4 + 2] = row[x * 3 + 2]; target[x * 4 + 3] = 255;
                }
                (current, previous) = (previous, current);
            }
        }
        return output;
    }

    private static void Unfilter(byte filter, ReadOnlySpan<byte> raw, Span<byte> row, ReadOnlySpan<byte> above, int bpp)
    {
        int n = row.Length;
        switch (filter)
        {
            case 0: raw.CopyTo(row); break;
            case 1:
                raw[..bpp].CopyTo(row);
                for (int i = bpp; i < n; i++) row[i] = (byte)(raw[i] + row[i - bpp]);
                break;
            case 2:
                if (above.IsEmpty) { raw.CopyTo(row); break; }
                for (int i = 0; i < n; i++) row[i] = (byte)(raw[i] + above[i]);
                break;
            case 3:
                for (int i = 0; i < n; i++)
                {
                    int left = i >= bpp ? row[i - bpp] : 0, up = above.IsEmpty ? 0 : above[i];
                    row[i] = (byte)(raw[i] + ((left + up) >> 1));
                }
                break;
            case 4:
                for (int i = 0; i < n; i++)
                {
                    int a = i >= bpp ? row[i - bpp] : 0, b = above.IsEmpty ? 0 : above[i], c = i >= bpp && !above.IsEmpty ? above[i - bpp] : 0;
                    int p = a + b - c, pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
                    row[i] = (byte)(raw[i] + (pa <= pb && pa <= pc ? a : pb <= pc ? b : c));
                }
                break;
            default: throw new InvalidDataException($"PNG filter {filter}");
        }
    }

    /// <summary>The IDAT chunks of a file as one stream, without copying them.</summary>
    private sealed class Segments(byte[] file, List<(int Offset, int Length)> parts) : Stream
    {
        private int _part, _within;

        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            while (_part < parts.Count && _within == parts[_part].Length) { _part++; _within = 0; }
            if (_part == parts.Count) return 0;
            int take = Math.Min(buffer.Length, parts[_part].Length - _within);
            file.AsSpan(parts[_part].Offset + _within, take).CopyTo(buffer);
            _within += take;
            return take;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
