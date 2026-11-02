namespace MicroEngine.Graphics;

/// <summary>
/// A CPU-side RGBA image. Games author procedural art with it and hand it to
/// <see cref="IGraphicsDevice.CreateTexture(PixelImage)"/>; it also backs the built-in font.
/// </summary>
/// <remarks>
/// Drawing methods change this image and return it, so calls chain; methods that build a new image (<see cref="Clone"/>,
/// <see cref="FlipX"/>, <see cref="Crop"/>...) leave it unchanged. Coordinates outside the image are ignored when
/// writing and read as transparent. Drawing methods alpha-blend unless they say otherwise.
/// </remarks>
public sealed class PixelImage
{
    private readonly Color[] _pixels;
    /// <summary>Width in pixels.</summary>
    public int Width { get; }
    /// <summary>Height in pixels.</summary>
    public int Height { get; }

    /// <summary>Creates a fully transparent image.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="width"/> or <paramref name="height"/> is not positive.</exception>
    public PixelImage(int width, int height)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Image size must be positive.");
        Width = width;
        Height = height;
        _pixels = new Color[width * height];
    }

    /// <summary>The pixels, row by row from the top-left.</summary>
    public ReadOnlySpan<Color> Pixels => _pixels;

    /// <summary>The pixel at (<paramref name="x"/>, <paramref name="y"/>). Reading outside the image returns <see cref="Color.Transparent"/>; writing outside does nothing. Writing replaces the pixel without blending.</summary>
    public Color this[int x, int y]
    {
        get => InBounds(x, y) ? _pixels[y * Width + x] : Color.Transparent;
        set { if (InBounds(x, y)) _pixels[y * Width + x] = value; }
    }

    /// <summary>Whether (<paramref name="x"/>, <paramref name="y"/>) is inside the image.</summary>
    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

    /// <summary>Alpha-blends <paramref name="color"/> over the existing pixel.</summary>
    public void Blend(int x, int y, Color color)
    {
        if (!InBounds(x, y) || color.A == 0) return;
        if (color.A == 255) { _pixels[y * Width + x] = color; return; }
        var dst = _pixels[y * Width + x];
        float a = color.A / 255f, inv = 1 - a, outA = a + dst.A / 255f * inv;
        if (outA <= 0) return;
        byte Mix(byte s, byte d) => (byte)MathF.Round((s * a + d * (dst.A / 255f) * inv) / outA);
        _pixels[y * Width + x] = new Color(Mix(color.R, dst.R), Mix(color.G, dst.G), Mix(color.B, dst.B), (byte)MathF.Round(outA * 255));
    }

    /// <summary>Sets every pixel to <paramref name="color"/>, without blending.</summary>
    public PixelImage Clear(Color color) { Array.Fill(_pixels, color); return this; }

    /// <summary>Blends <paramref name="color"/> over a <paramref name="w"/> × <paramref name="h"/> rectangle whose top-left is (<paramref name="x"/>, <paramref name="y"/>).</summary>
    public PixelImage FillRect(int x, int y, int w, int h, Color color)
    {
        for (int j = y; j < y + h; j++)
        for (int i = x; i < x + w; i++)
            Blend(i, j, color);
        return this;
    }

    /// <summary>Draws the one-pixel outline of a rectangle.</summary>
    public PixelImage DrawRect(int x, int y, int w, int h, Color color)
    {
        for (int i = x; i < x + w; i++) { Blend(i, y, color); Blend(i, y + h - 1, color); }
        for (int j = y + 1; j < y + h - 1; j++) { Blend(x, j, color); Blend(x + w - 1, j, color); }
        return this;
    }

    /// <summary>Draws a one-pixel line between two pixels, both included.</summary>
    public PixelImage Line(int x0, int y0, int x1, int y1, Color color)
    {
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1, dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1, err = dx + dy;
        while (true)
        {
            Blend(x0, y0, color);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
        return this;
    }

    /// <summary>Filled circle centred on a pixel-space coordinate (half-pixel centres allowed).</summary>
    public PixelImage FillCircle(float cx, float cy, float radius, Color color)
    {
        int x0 = (int)MathF.Floor(cx - radius), x1 = (int)MathF.Ceiling(cx + radius);
        int y0 = (int)MathF.Floor(cy - radius), y1 = (int)MathF.Ceiling(cy + radius);
        float r2 = radius * radius;
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
            if (dx * dx + dy * dy <= r2) Blend(x, y, color);
        }
        return this;
    }

    /// <summary>Fills an ellipse centred on (<paramref name="cx"/>, <paramref name="cy"/>) with radii <paramref name="rx"/> and <paramref name="ry"/>.</summary>
    public PixelImage FillEllipse(float cx, float cy, float rx, float ry, Color color)
    {
        for (int y = (int)MathF.Floor(cy - ry); y <= (int)MathF.Ceiling(cy + ry); y++)
        for (int x = (int)MathF.Floor(cx - rx); x <= (int)MathF.Ceiling(cx + rx); x++)
        {
            float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
            if (dx * dx + dy * dy <= 1f) Blend(x, y, color);
        }
        return this;
    }

    /// <summary>Copies another image (alpha-blended) with optional horizontal flip.</summary>
    public PixelImage Blit(PixelImage source, int dx, int dy, bool flipX = false)
    {
        ArgumentNullException.ThrowIfNull(source);
        for (int y = 0; y < source.Height; y++)
        for (int x = 0; x < source.Width; x++)
            Blend(dx + x, dy + y, source[flipX ? source.Width - 1 - x : x, y]);
        return this;
    }

    /// <summary>Blends the <paramref name="w"/> × <paramref name="h"/> region of <paramref name="source"/> at (<paramref name="sx"/>, <paramref name="sy"/>) onto this image at (<paramref name="dx"/>, <paramref name="dy"/>).</summary>
    public PixelImage Blit(PixelImage source, int sx, int sy, int w, int h, int dx, int dy)
    {
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            Blend(dx + x, dy + y, source[sx + x, sy + y]);
        return this;
    }

    /// <summary>
    /// Paints a character map. Each row is a string; each char is looked up in <paramref name="palette"/>.
    /// Characters missing from the palette (conventionally '.' or ' ') are left untouched.
    /// </summary>
    public PixelImage Paint(int dx, int dy, IReadOnlyDictionary<char, Color> palette, params string[] rows)
    {
        ArgumentNullException.ThrowIfNull(palette);
        for (int y = 0; y < rows.Length; y++)
        for (int x = 0; x < rows[y].Length; x++)
            if (palette.TryGetValue(rows[y][x], out var c)) Blend(dx + x, dy + y, c);
        return this;
    }

    /// <summary>Creates an image from a character map; the image size is the map size.</summary>
    public static PixelImage FromMap(IReadOnlyDictionary<char, Color> palette, params string[] rows)
    {
        int w = rows.Max(r => r.Length);
        return new PixelImage(w, rows.Length).Paint(0, 0, palette, rows);
    }

    /// <summary>Adds a 1px outline of <paramref name="color"/> around every opaque region (4-neighbourhood).</summary>
    public PixelImage Outline(Color color, bool diagonals = false)
    {
        var copy = (Color[])_pixels.Clone();
        bool Solid(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height && copy[y * Width + x].A > 0;
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
        {
            if (Solid(x, y)) continue;
            bool near = Solid(x - 1, y) || Solid(x + 1, y) || Solid(x, y - 1) || Solid(x, y + 1)
                || diagonals && (Solid(x - 1, y - 1) || Solid(x + 1, y - 1) || Solid(x - 1, y + 1) || Solid(x + 1, y + 1));
            if (near) _pixels[y * Width + x] = color;
        }
        return this;
    }

    /// <summary>Replaces every exact <paramref name="from"/> pixel with <paramref name="to"/>.</summary>
    public PixelImage Recolor(Color from, Color to)
    {
        for (int i = 0; i < _pixels.Length; i++) if (_pixels[i] == from) _pixels[i] = to;
        return this;
    }

    /// <summary>Replaces each pixel with <c>transform(x, y, pixel)</c>.</summary>
    public PixelImage Map(Func<int, int, Color, Color> transform)
    {
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
            _pixels[y * Width + x] = transform(x, y, _pixels[y * Width + x]);
        return this;
    }

    /// <summary>Returns an independent copy.</summary>
    public PixelImage Clone()
    {
        var copy = new PixelImage(Width, Height);
        _pixels.CopyTo(copy._pixels, 0);
        return copy;
    }

    /// <summary>Returns a copy mirrored horizontally.</summary>
    public PixelImage FlipX()
    {
        var copy = new PixelImage(Width, Height);
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
            copy._pixels[y * Width + x] = _pixels[y * Width + Width - 1 - x];
        return copy;
    }

    /// <summary>Returns a copy mirrored vertically.</summary>
    public PixelImage FlipY()
    {
        var copy = new PixelImage(Width, Height);
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
            copy._pixels[y * Width + x] = _pixels[(Height - 1 - y) * Width + x];
        return copy;
    }

    /// <summary>Returns a copy rotated 90 degrees clockwise (width and height swap).</summary>
    public PixelImage Rotate90()
    {
        var copy = new PixelImage(Height, Width);
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
            copy._pixels[x * Height + (Height - 1 - y)] = _pixels[y * Width + x];
        return copy;
    }

    /// <summary>
    /// Doubles the image with the Scale2x (EPX) rule: flat areas stay flat while one-pixel stair steps
    /// become slopes, so small sprites can be shown at twice the size without looking blocky.
    /// Pixels beyond the edge count as copies of the edge pixel.
    /// </summary>
    public PixelImage Scale2x()
    {
        var big = new PixelImage(Width * 2, Height * 2);
        Color At(int x, int y) => _pixels[Math.Clamp(y, 0, Height - 1) * Width + Math.Clamp(x, 0, Width - 1)];
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
        {
            Color p = At(x, y), a = At(x, y - 1), b = At(x + 1, y), c = At(x - 1, y), d = At(x, y + 1);
            big[x * 2, y * 2] = c == a && c != d && a != b ? a : p;
            big[x * 2 + 1, y * 2] = a == b && a != c && b != d ? b : p;
            big[x * 2, y * 2 + 1] = d == c && d != b && c != a ? c : p;
            big[x * 2 + 1, y * 2 + 1] = b == d && b != a && d != c ? d : p;
        }
        return big;
    }

    /// <summary>
    /// A copy at <paramref name="width"/> by <paramref name="height"/> pixels. Whole-number enlargements repeat pixels,
    /// so pixel art stays sharp; other sizes average the source area under each pixel (weighted by opacity, so
    /// transparent pixels do not darken edges), so a 1024-pixel icon reduced for a launcher or a start screen keeps its
    /// detail instead of dropping rows.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">A size is not positive.</exception>
    public PixelImage Resized(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        var result = new PixelImage(width, height);
        if (width % Width == 0 && height % Height == 0 && width / Width == height / Height)
        {
            int k = width / Width;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++) result[x, y] = _pixels[y / k * Width + x / k];
            return result;
        }
        double stepX = (double)Width / width, stepY = (double)Height / height;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int x0 = (int)(x * stepX), y0 = (int)(y * stepY);
                int x1 = Math.Min(Width, Math.Max(x0 + 1, (int)((x + 1) * stepX))), y1 = Math.Min(Height, Math.Max(y0 + 1, (int)((y + 1) * stepY)));
                double r = 0, g = 0, b = 0, a = 0;
                int n = 0;
                for (int sy = y0; sy < y1; sy++)
                    for (int sx = x0; sx < x1; sx++)
                    {
                        var c = _pixels[sy * Width + sx];
                        r += c.R * c.A; g += c.G * c.A; b += c.B * c.A; a += c.A; n++;
                    }
                result[x, y] = a == 0 ? default : new Color((byte)(r / a), (byte)(g / a), (byte)(b / a), (byte)(a / n));
            }
        return result;
    }

    /// <summary>The largest size of this image that fits inside <paramref name="maxWidth"/> by <paramref name="maxHeight"/>, keeping its shape (at least 1 by 1).</summary>
    public (int Width, int Height) FitInside(int maxWidth, int maxHeight)
    {
        float k = Math.Min((float)maxWidth / Width, (float)maxHeight / Height);
        return (Math.Max(1, (int)(Width * k)), Math.Max(1, (int)(Height * k)));
    }

    /// <summary>Returns a copy where every visible pixel becomes <paramref name="color"/> (hit flashes, shadows).</summary>
    public PixelImage Silhouette(Color color)
    {
        var copy = new PixelImage(Width, Height);
        for (int i = 0; i < _pixels.Length; i++)
            if (_pixels[i].A > 0) copy._pixels[i] = color with { A = (byte)(color.A * _pixels[i].A / 255) };
        return copy;
    }

    /// <summary>Decodes a PNG (8-bit grey/RGB/RGBA or paletted, non-interlaced).</summary>
    /// <exception cref="InvalidDataException">The data is not a valid PNG.</exception>
    /// <exception cref="NotSupportedException">The PNG uses a format the decoder does not support.</exception>
    public static PixelImage FromPng(ReadOnlySpan<byte> data) => PngDecoder.Decode(data);

    /// <summary>Loads a PNG file; the message names the path when decoding fails.</summary>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="InvalidDataException">The file is not a PNG the decoder supports.</exception>
    public static PixelImage Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        try { return PngDecoder.Decode(File.ReadAllBytes(path)); }
        catch (Exception e) when (e is InvalidDataException or NotSupportedException)
        {
            throw new InvalidDataException($"{path}: {e.Message}", e);
        }
    }

    /// <summary>Copies a rectangle into a new image; areas outside the source become transparent.</summary>
    public PixelImage Crop(int x, int y, int width, int height)
    {
        var copy = new PixelImage(width, height);
        for (int j = 0; j < height; j++)
        for (int i = 0; i < width; i++)
            copy._pixels[j * width + i] = this[x + i, y + j];
        return copy;
    }

    /// <summary>Makes a rectangle fully transparent (unlike <see cref="FillRect"/>, which blends).</summary>
    public PixelImage ClearRect(int x, int y, int w, int h)
    {
        for (int j = y; j < y + h; j++)
        for (int i = x; i < x + w; i++)
            if (InBounds(i, j)) _pixels[j * Width + i] = Color.Transparent;
        return this;
    }

    /// <summary>Smallest rectangle containing every pixel with alpha above zero; null when fully transparent.</summary>
    public (int X, int Y, int Width, int Height)? OpaqueBounds()
    {
        int x0 = Width, y0 = Height, x1 = -1, y1 = -1;
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
        {
            if (_pixels[y * Width + x].A == 0) continue;
            x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y);
        }
        return x1 < 0 ? null : (x0, y0, x1 - x0 + 1, y1 - y0 + 1);
    }

    /// <summary>
    /// Returns a same-size copy rotated by <paramref name="radians"/> (clockwise on screen) around a pivot,
    /// sampling nearest neighbours so the palette stays intact. Content leaving the canvas is clipped.
    /// Useful for deriving extra poses (e.g. aiming arms) from authored frames.
    /// </summary>
    public PixelImage Rotated(float radians, float pivotX, float pivotY)
    {
        var copy = new PixelImage(Width, Height);
        float cos = MathF.Cos(radians), sin = MathF.Sin(radians);
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
        {
            // Inverse-map each destination pixel centre back into the source.
            float dx = x + 0.5f - pivotX, dy = y + 0.5f - pivotY;
            float sx = dx * cos + dy * sin + pivotX, sy = -dx * sin + dy * cos + pivotY;
            copy._pixels[y * Width + x] = this[(int)MathF.Floor(sx), (int)MathF.Floor(sy)];
        }
        return copy;
    }

    /// <summary>Raw RGBA8 bytes, row-major, for backends.</summary>
    public byte[] ToRgba()
    {
        var bytes = new byte[_pixels.Length * 4];
        for (int i = 0; i < _pixels.Length; i++)
        {
            var c = _pixels[i];
            bytes[i * 4] = c.R; bytes[i * 4 + 1] = c.G; bytes[i * 4 + 2] = c.B; bytes[i * 4 + 3] = c.A;
        }
        return bytes;
    }
}
