using System.Buffers.Binary;
using System.IO.Compression;

namespace ContentBench;

/// <summary>
/// The content the measurements run on, drawn by code so the repository ships no art for them and every run reads the
/// same pixels: three kinds of high-resolution 2D art that compress differently as PNG. Pixels are straight-alpha RGBA,
/// rows from the top.
/// </summary>
internal static class Pictures
{
    /// <summary>
    /// "sprites": a page of flat-shaded characters with outlines on a transparent background (an atlas page of
    /// illustrated art); "painted": an opaque painted backdrop with soft gradients and grain; "tiles": an opaque
    /// tileset of repeated textured tiles, a quarter of them foliage with soft transparent edges.
    /// </summary>
    public static readonly string[] Kinds = ["sprites", "painted", "tiles"];

    public static byte[] Draw(string kind, int size)
    {
        var rgba = new byte[size * size * 4];
        Parallel.For(0, size, y =>
        {
            var row = rgba.AsSpan(y * size * 4, size * 4);
            for (int x = 0; x < size; x++)
            {
                var (r, g, b, a) = kind switch
                {
                    "sprites" => Sprites(x, y),
                    "painted" => Painted(x, y, size),
                    "tiles" => Tiles(x, y),
                    "emblem" => Emblem(x * 256f / size, y * 256f / size),
                    _ => throw new ArgumentException($"unknown kind '{kind}' (sprites, painted, tiles)", nameof(kind)),
                };
                row[x * 4] = Byte(r); row[x * 4 + 1] = Byte(g); row[x * 4 + 2] = Byte(b); row[x * 4 + 3] = Byte(a * 255);
            }
        });
        return rgba;
    }

    private static byte Byte(float v) => (byte)Math.Clamp(MathF.Round(v), 0, 255);

    // One picture with everything a texture format has to keep: a soft translucent shadow, a ring and a star with
    // hard antialiased edges, and a smooth gradient with faint clouds. It is the picture the Examples sample ships
    // (`ContentBench emblem FILE`), drawn in a 256-unit square whatever the size.
    private static (float, float, float, float) Emblem(float x, float y)
    {
        float dx = x + 0.5f - 128, dy = y + 0.5f - 124, radius = MathF.Sqrt(dx * dx + dy * dy);
        float shadow = 0.45f * Math.Clamp((118 - MathF.Sqrt(Sq(dx - 5) + Sq(dy - 8))) / 14 + 0.5f, 0, 1);
        float r = 10, g = 10, b = 30, a = shadow;
        void Over(float cr, float cg, float cb, float cover)
        {
            cover = Math.Clamp(cover, 0, 1);
            float na = cover + a * (1 - cover);
            if (na <= 0) return;
            r = (cr * cover + r * a * (1 - cover)) / na; g = (cg * cover + g * a * (1 - cover)) / na; b = (cb * cover + b * a * (1 - cover)) / na;
            a = na;
        }
        float t = (dy + 96) / 192, cloud = Fbm(x / 40, y / 40, 7, 4);
        Over(30 + 40 * t + 60 * cloud, 60 + 90 * t + 50 * cloud, 150 + 70 * t, 96.5f - radius);                   // the disc
        float ring = MathF.Abs(radius - 100) - 9;
        Over(60, 40, 10, 2.5f - ring);                                                                             // the ring's outline
        Over(250 - 60 * t, 200 - 70 * t, 70, 0.5f - ring);                                                         // the ring
        float angle = MathF.Atan2(dy, dx) + MathF.PI / 2, sector = MathF.Tau / 5;
        float local = MathF.Abs(((angle % sector) + sector) % sector - sector / 2);
        float star = radius * MathF.Cos(local) + radius * MathF.Sin(local) * 1.376f - 58;                          // inside a five-pointed star
        Over(70, 50, 10, 3f - star);
        Over(dx + dy > 0 ? 235 : 255, dx + dy > 0 ? 190 : 225, 60, 0.5f - star);
        return (r, g, b, a);
    }

    private static float Hash(int x, int y, int seed)
    {
        uint h = unchecked((uint)(x * 374761393 + y * 668265263 + seed * 1274126177));
        h = (h ^ (h >> 13)) * 1274126177u;
        return ((h ^ (h >> 16)) & 0xFFFFFF) / 16777215f;
    }

    private static float Value(float x, float y, int seed)
    {
        int xi = (int)MathF.Floor(x), yi = (int)MathF.Floor(y);
        float fx = x - xi, fy = y - yi;
        fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
        float top = Hash(xi, yi, seed) + (Hash(xi + 1, yi, seed) - Hash(xi, yi, seed)) * fx;
        float bottom = Hash(xi, yi + 1, seed) + (Hash(xi + 1, yi + 1, seed) - Hash(xi, yi + 1, seed)) * fx;
        return top + (bottom - top) * fy;
    }

    private static float Fbm(float x, float y, int seed, int octaves)
    {
        float sum = 0, amplitude = 0.5f, total = 0;
        for (int i = 0; i < octaves; i++)
        {
            sum += Value(x, y, seed + i) * amplitude;
            total += amplitude;
            amplitude *= 0.5f; x *= 2; y *= 2;
        }
        return sum / total;
    }

    // A backdrop painted at the page's size: a sky gradient over hills, both modulated by large soft noise, with the
    // grain of a canvas (the same value on the three channels, a level or two).
    private static (float, float, float, float) Painted(int x, int y, int size)
    {
        float u = x / (float)size, v = y / (float)size;
        float n = Fbm(u * 6, v * 6, 11, 6), detail = Fbm(u * 48 + n * 3, v * 48, 23, 4);
        float grain = (Hash(x, y, 31) - 0.5f) * 4;
        float horizon = 0.55f + 0.18f * (Fbm(u * 3, 0.5f, 41, 4) - 0.5f);
        float r, g, b;
        if (v < horizon)
        {
            float t = v / horizon, cloud = MathF.Max(0, n - 0.5f) * 2;
            r = 40 + 190 * t + 80 * cloud; g = 70 + 110 * t + 70 * cloud; b = 130 + 20 * t + 50 * cloud;
        }
        else
        {
            float t = (v - horizon) / (1 - horizon), shade = 0.7f + 0.6f * detail;
            r = (60 + 50 * t) * shade; g = (110 - 30 * t) * shade; b = (50 + 10 * t) * shade;
        }
        return (r + grain, g + grain, b + grain, 1);
    }

    // A packed page of characters, one per 136×232 cell: a body, a head, two limbs and an accent, each a flat colour
    // with a darker cel-shaded side and a dark outline, antialiased at the edge, over transparency.
    private static (float, float, float, float) Sprites(int x, int y)
    {
        const int cellWidth = 136, cellHeight = 232;
        int cx = x / cellWidth, cy = y / cellHeight;
        float px = x % cellWidth + 60.5f, py = y % cellHeight + 30.5f;
        float r = 0, g = 0, b = 0, a = 0;
        for (int part = 0; part < 5; part++)
        {
            float jitter = Hash(cx * 7 + part, cy * 13, 5) * 24 - 12;
            float d = part switch
            {
                0 => RoundBox(px - 128, py - 150 - jitter * 0.3f, 44 + jitter, 52, 18),
                1 => MathF.Sqrt(Sq(px - 128 - jitter * 0.4f) + Sq(py - 72)) - (34 + jitter * 0.3f),
                2 => Capsule(px, py, 96, 196, 84 + jitter, 240, 13),
                3 => Capsule(px, py, 160, 196, 172 - jitter, 240, 13),
                _ => MathF.Sqrt(Sq(px - 128) + Sq(py - 140)) - 12,
            };
            float cover = Math.Clamp(0.5f - d, 0, 1);
            if (cover <= 0) continue;
            float hue = Hash(cx, cy, 17 + (part == 4 ? 1 : 0)) * 6;
            var (fr, fg, fb) = Hue(hue + part * 0.35f);
            bool outline = d > -5, shaded = px + py * 0.6f > 128 + 150 * 0.6f + 20;
            float k = outline ? 0.25f : shaded ? 0.7f : 1f;
            r = fr * k * cover + r * (1 - cover); g = fg * k * cover + g * (1 - cover); b = fb * k * cover + b * (1 - cover);
            a = cover + a * (1 - cover);
        }
        return a > 0 ? (r / a, g / a, b / a, a) : (0, 0, 0, 0);
    }

    private static float Sq(float v) => v * v;

    private static float RoundBox(float x, float y, float halfWidth, float halfHeight, float radius)
    {
        float qx = MathF.Abs(x) - halfWidth + radius, qy = MathF.Abs(y) - halfHeight + radius;
        return MathF.Sqrt(Sq(MathF.Max(qx, 0)) + Sq(MathF.Max(qy, 0))) + MathF.Min(MathF.Max(qx, qy), 0) - radius;
    }

    private static float Capsule(float x, float y, float ax, float ay, float bx, float by, float radius)
    {
        float pax = x - ax, pay = y - ay, bax = bx - ax, bay = by - ay;
        float h = Math.Clamp((pax * bax + pay * bay) / (bax * bax + bay * bay), 0, 1);
        return MathF.Sqrt(Sq(pax - bax * h) + Sq(pay - bay * h)) - radius;
    }

    private static (float, float, float) Hue(float h)
    {
        float Channel(float offset) => 255 * (0.35f + 0.65f * Math.Clamp(MathF.Abs((h + offset) % 6 - 3) - 1, 0, 1));
        return (Channel(0), Channel(4), Channel(2));
    }

    // A tileset of 128-pixel tiles: twelve kinds of textured ground, each tile of a kind identical (as in a tileset
    // with repeats), bevelled at the edges; three kinds are foliage whose noise cuts soft transparent holes.
    private static (float, float, float, float) Tiles(int x, int y)
    {
        const int tile = 128;
        int kind = (int)(Hash(x / tile, y / tile, 3) * 12) % 12;
        int lx = x % tile, ly = y % tile;
        float n = Fbm((lx + kind * 131) / 14f, ly / 14f, 50 + kind, 5), fine = Hash(lx, ly, 70 + kind);
        var (r, g, b) = Hue(kind * 0.5f);
        float shade = 0.45f + 0.7f * n + (fine - 0.5f) * 0.08f;
        if (lx < 4 || ly < 4) shade += 0.12f;
        if (lx >= tile - 4 || ly >= tile - 4) shade -= 0.15f;
        float alpha = kind >= 9 ? Math.Clamp((n - 0.42f) * 9, 0, 1) : 1;
        return (r * 0.55f * shade + 40 * shade, g * 0.55f * shade + 40 * shade, b * 0.55f * shade + 30 * shade, alpha);
    }

    /// <summary>
    /// A PNG as an art tool writes one: 8-bit RGBA, each row with the filter that leaves the smallest sum of absolute
    /// values (the heuristic of libpng), deflated at the default level. The engine's own encoder is made for captures
    /// (no filter, fastest level) and would give files larger than a game ships.
    /// </summary>
    public static byte[] EncodePng(ReadOnlySpan<byte> rgba, int width, int height)
    {
        int stride = width * 4;
        var filtered = new byte[(stride + 1) * height];
        var source = rgba.ToArray();
        Parallel.For(0, height, y =>
        {
            var row = source.AsSpan(y * stride, stride);
            var above = y > 0 ? source.AsSpan((y - 1) * stride, stride) : default;
            var best = new byte[stride];
            var work = new byte[stride];
            long bestSum = long.MaxValue;
            int bestFilter = 0;
            for (int filter = 0; filter < 5; filter++)
            {
                long sum = 0;
                for (int i = 0; i < stride; i++)
                {
                    int left = i >= 4 ? row[i - 4] : 0, up = above.IsEmpty ? 0 : above[i], upLeft = i >= 4 && !above.IsEmpty ? above[i - 4] : 0;
                    int predicted = filter switch { 0 => 0, 1 => left, 2 => up, 3 => (left + up) / 2, _ => Paeth(left, up, upLeft) };
                    byte value = (byte)(row[i] - predicted);
                    work[i] = value;
                    sum += value < 128 ? value : 256 - value;
                }
                if (sum < bestSum) { bestSum = sum; bestFilter = filter; (best, work) = (work, best); }
            }
            filtered[y * (stride + 1)] = (byte)bestFilter;
            best.CopyTo(filtered.AsSpan(y * (stride + 1) + 1, stride));
        });

        using var data = new MemoryStream();
        using (var z = new ZLibStream(data, CompressionLevel.Optimal, leaveOpen: true)) z.Write(filtered);
        using var png = new MemoryStream();
        png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        Span<byte> header = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header[4..], height);
        header[8] = 8; header[9] = 6;
        Chunk(png, "IHDR", header);
        Chunk(png, "IDAT", data.GetBuffer().AsSpan(0, (int)data.Length));
        Chunk(png, "IEND", default);
        return png.ToArray();
    }

    private static int Paeth(int a, int b, int c)
    {
        int p = a + b - c, pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }

    private static readonly uint[] CrcTable = BuildCrcTable();

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

    private static void Chunk(Stream png, string type, ReadOnlySpan<byte> data)
    {
        Span<byte> word = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(word, data.Length);
        png.Write(word);
        Span<byte> name = [(byte)type[0], (byte)type[1], (byte)type[2], (byte)type[3]];
        png.Write(name);
        png.Write(data);
        uint crc = 0xFFFFFFFFu;
        foreach (byte v in name) crc = CrcTable[(crc ^ v) & 0xFF] ^ (crc >> 8);
        foreach (byte v in data) crc = CrcTable[(crc ^ v) & 0xFF] ^ (crc >> 8);
        BinaryPrimitives.WriteUInt32BigEndian(word, crc ^ 0xFFFFFFFFu);
        png.Write(word);
    }
}
