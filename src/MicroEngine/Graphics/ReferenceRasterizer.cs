using System.Numerics;

namespace MicroEngine.Graphics;

/// <summary>
/// The CPU reference rasterizer: an 8-bit RGBA canvas drawn exactly as the written rendering rules say (see the remarks
/// of <see cref="IRenderer"/>). It is the oracle the renderer conformance tests compare backends with and what the
/// headless renderer draws with when it rasterizes. It favours clarity over speed.
/// </summary>
/// <remarks>
/// Rules implemented: pixel centres at +0.5; rectangles and unrotated sprites cover the centres inside them with the
/// top-left rule (half-open intervals); rotated sprites, triangles and lines use the same rule on their edges; circles
/// cover the centres strictly inside the disc; nearest sampling takes the texel under the centre, linear sampling mixes
/// the four texels around it with the texture's edges clamped; straight alpha with the blend equations of
/// <see cref="IRenderer.SetBlendMode"/>, results rounded to nearest; <see cref="Clear"/> replaces the whole canvas; the
/// clip is truncated to whole pixels.
/// </remarks>
internal sealed class ReferenceRasterizer
{
    private readonly Color[] _canvas;
    private (int X0, int Y0, int X1, int Y1) _clip;

    public ReferenceRasterizer(int width, int height)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width), "The canvas size must be positive.");
        (Width, Height) = (width, height);
        _canvas = new Color[width * height];
        _clip = (0, 0, width, height);
    }

    public int Width { get; }
    public int Height { get; }
    public BlendMode Blend { get; set; }

    /// <summary>The canvas as stored (alpha included); captures make it opaque.</summary>
    public Color this[int x, int y] => _canvas[y * Width + x];

    /// <summary>A copy of the canvas, opaque as every capture is.</summary>
    public PixelImage Capture()
    {
        var image = new PixelImage(Width, Height);
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                var c = _canvas[y * Width + x];
                image[x, y] = c with { A = 255 };
            }
        return image;
    }

    /// <summary>Replaces every pixel, alpha included, whatever the clip and blend mode.</summary>
    public void Clear(Color color) => Array.Fill(_canvas, color);

    /// <summary>Restricts drawing to a rectangle whose X, Y, Width and Height are truncated to whole pixels; null removes it.</summary>
    public void SetClip(RectF? rect)
    {
        if (rect is not { } r) { _clip = (0, 0, Width, Height); return; }
        int x = (int)r.X, y = (int)r.Y, w = (int)r.Width, h = (int)r.Height;
        _clip = (Math.Clamp(x, 0, Width), Math.Clamp(y, 0, Height), Math.Clamp(x + Math.Max(w, 0), 0, Width), Math.Clamp(y + Math.Max(h, 0), 0, Height));
    }

    /// <summary>Covers the pixels whose centre c satisfies left ≤ c.x &lt; right and top ≤ c.y &lt; bottom.</summary>
    public void FillRect(RectF rect, Color color)
    {
        if (color.A == 0) return;
        var (x0, x1) = Span(rect.X, rect.Right, _clip.X0, _clip.X1);
        var (y0, y1) = Span(rect.Y, rect.Bottom, _clip.Y0, _clip.Y1);
        var src = Vector4.Create(color.R, color.G, color.B, color.A) / 255f;
        for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++) Put(x, y, src);
    }

    /// <summary>The pixels whose centre lies in [from, to): from ⌈from − 0.5⌉ to ⌈to − 0.5⌉ (exclusive), within the clip.</summary>
    private static (int From, int To) Span(float from, float to, int min, int max)
    {
        int a = (int)MathF.Ceiling(from - 0.5f), b = (int)MathF.Ceiling(to - 0.5f);
        return (Math.Max(a, min), Math.Min(b, max));
    }

    /// <summary>
    /// Draws the <paramref name="source"/> part of <paramref name="texture"/>: its unrotated, scaled top-left at
    /// <paramref name="position"/>, rotated clockwise by <paramref name="rotation"/> around <paramref name="origin"/>
    /// (source pixels), mirrored by the flips, multiplied by <paramref name="tint"/>.
    /// </summary>
    public void DrawSprite(PixelImage texture, TextureFilter filter, RectF source, Vector2 position, Vector2 scale, float rotation, Vector2 origin,
        bool flipX, bool flipY, Color tint)
    {
        float w = source.Width * scale.X, h = source.Height * scale.Y;
        if (w <= 0 || h <= 0 || tint.A == 0) return;
        var pivot = position + origin * scale;
        float cos = MathF.Cos(rotation), sin = MathF.Sin(rotation);
        // Corners in canvas space, for the bounding box only.
        Vector2 Corner(float lx, float ly)
        {
            var d = new Vector2(lx, ly) - origin * scale;
            return pivot + new Vector2(d.X * cos - d.Y * sin, d.X * sin + d.Y * cos);
        }
        Vector2[] corners = [Corner(0, 0), Corner(w, 0), Corner(0, h), Corner(w, h)];
        var (x0, x1) = Span(corners.Min(c => c.X), corners.Max(c => c.X) + 1, _clip.X0, _clip.X1);
        var (y0, y1) = Span(corners.Min(c => c.Y), corners.Max(c => c.Y) + 1, _clip.Y0, _clip.Y1);
        var tintF = Vector4.Create(tint.R, tint.G, tint.B, tint.A) / 255f;
        for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
            {
                // The pixel centre in the sprite's own unrotated, scaled space.
                var d = new Vector2(x + 0.5f, y + 0.5f) - pivot;
                float lx = d.X * cos + d.Y * sin + origin.X * scale.X, ly = -d.X * sin + d.Y * cos + origin.Y * scale.Y;
                if (rotation == 0)
                {
                    // Exact half-open test without the rotation's rounding.
                    lx = x + 0.5f - position.X;
                    ly = y + 0.5f - position.Y;
                }
                if (lx < 0 || lx >= w || ly < 0 || ly >= h) continue;
                float u = flipX ? (w - lx) / w : lx / w, v = flipY ? (h - ly) / h : ly / h;
                float s = source.X + u * source.Width, t = source.Y + v * source.Height;
                var texel = filter == TextureFilter.Linear ? Linear(texture, s, t) : Nearest(texture, source, s, t);
                Put(x, y, texel * tintF);
            }
    }

    /// <summary>The texel under (s, t), kept inside the source rectangle and the texture.</summary>
    private static Vector4 Nearest(PixelImage texture, RectF source, float s, float t)
    {
        int x = Math.Clamp((int)MathF.Floor(s), (int)MathF.Floor(source.X), (int)MathF.Ceiling(source.Right) - 1);
        int y = Math.Clamp((int)MathF.Floor(t), (int)MathF.Floor(source.Y), (int)MathF.Ceiling(source.Bottom) - 1);
        return Texel(texture, x, y);
    }

    /// <summary>Bilinear mix of the four texels around (s, t) (texel centres at +0.5), the texture's edges clamped; straight alpha.</summary>
    private static Vector4 Linear(PixelImage texture, float s, float t)
    {
        float fs = s - 0.5f, ft = t - 0.5f;
        int x = (int)MathF.Floor(fs), y = (int)MathF.Floor(ft);
        float ax = fs - x, ay = ft - y;
        var top = Vector4.Lerp(Texel(texture, x, y), Texel(texture, x + 1, y), ax);
        var bottom = Vector4.Lerp(Texel(texture, x, y + 1), Texel(texture, x + 1, y + 1), ax);
        return Vector4.Lerp(top, bottom, ay);
    }

    private static Vector4 Texel(PixelImage texture, int x, int y)
    {
        var c = texture[Math.Clamp(x, 0, texture.Width - 1), Math.Clamp(y, 0, texture.Height - 1)];
        return Vector4.Create(c.R, c.G, c.B, c.A) / 255f;
    }

    /// <summary>Covers the centres strictly inside the disc.</summary>
    public void FillCircle(Vector2 center, float radius, Color color)
    {
        if (radius <= 0) return;
        var (x0, x1) = Span(center.X - radius, center.X + radius + 1, _clip.X0, _clip.X1);
        var (y0, y1) = Span(center.Y - radius, center.Y + radius + 1, _clip.Y0, _clip.Y1);
        var src = Vector4.Create(color.R, color.G, color.B, color.A) / 255f;
        for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
                if (Vector2.DistanceSquared(new Vector2(x + 0.5f, y + 0.5f), center) < radius * radius) Put(x, y, src);
    }

    /// <summary>Covers the centres inside the triangle, either winding, with the top-left rule on its edges.</summary>
    public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        if (Edge(a, b, c) < 0) (b, c) = (c, b);
        if (Edge(a, b, c) == 0) return;
        var (x0, x1) = Span(MathF.Min(a.X, MathF.Min(b.X, c.X)), MathF.Max(a.X, MathF.Max(b.X, c.X)) + 1, _clip.X0, _clip.X1);
        var (y0, y1) = Span(MathF.Min(a.Y, MathF.Min(b.Y, c.Y)), MathF.Max(a.Y, MathF.Max(b.Y, c.Y)) + 1, _clip.Y0, _clip.Y1);
        var src = Vector4.Create(color.R, color.G, color.B, color.A) / 255f;
        for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                if (Inside(a, b, p) && Inside(b, c, p) && Inside(c, a, p)) Put(x, y, src);
            }
    }

    /// <summary>Twice the signed area of (a, b, p); positive when p is on the interior side of a → b for the winding used.</summary>
    private static float Edge(Vector2 a, Vector2 b, Vector2 p) => (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);

    /// <summary>Inside the edge, or on it when it is a left edge or a top edge (the top-left rule).</summary>
    private static bool Inside(Vector2 a, Vector2 b, Vector2 p)
    {
        float e = Edge(a, b, p);
        if (e != 0) return e > 0;
        float dx = b.X - a.X, dy = b.Y - a.Y;
        return dy < 0 || (dy == 0 && dx > 0);
    }

    /// <summary>The quad <paramref name="thickness"/> wide around the segment, as two triangles. A zero-length line draws nothing.</summary>
    public void DrawLine(Vector2 from, Vector2 to, float thickness, Color color)
    {
        var d = to - from;
        if (d.LengthSquared() < 1e-12f) return;
        var n = Vector2.Normalize(new Vector2(-d.Y, d.X)) * (thickness / 2);
        Vector2 a = from + n, b = from - n, c = to - n, e = to + n;
        // One pass over both triangles so the shared diagonal is covered once.
        if (Edge(a, b, c) < 0) (a, b, c, e) = (b, a, e, c);
        var (x0, x1) = Span(MathF.Min(MathF.Min(a.X, b.X), MathF.Min(c.X, e.X)), MathF.Max(MathF.Max(a.X, b.X), MathF.Max(c.X, e.X)) + 1, _clip.X0, _clip.X1);
        var (y0, y1) = Span(MathF.Min(MathF.Min(a.Y, b.Y), MathF.Min(c.Y, e.Y)), MathF.Max(MathF.Max(a.Y, b.Y), MathF.Max(c.Y, e.Y)) + 1, _clip.Y0, _clip.Y1);
        var src = Vector4.Create(color.R, color.G, color.B, color.A) / 255f;
        for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                if (Inside(a, b, p) && Inside(b, c, p) && Inside(c, e, p) && Inside(e, a, p)) Put(x, y, src);
            }
    }

    /// <summary>Blends a straight-alpha source colour (0..1 per channel) into one pixel with the current mode.</summary>
    private void Put(int x, int y, Vector4 s)
    {
        ref var pixel = ref _canvas[y * Width + x];
        var d = Vector4.Create(pixel.R, pixel.G, pixel.B, pixel.A) / 255f;
        float a = s.W;
        Vector4 o = Blend switch
        {
            BlendMode.Additive => new Vector4(s.X * a + d.X, s.Y * a + d.Y, s.Z * a + d.Z, d.W),
            BlendMode.Multiply => new Vector4(s.X * d.X + d.X * (1 - a), s.Y * d.Y + d.Y * (1 - a), s.Z * d.Z + d.Z * (1 - a), d.W),
            _ => new Vector4(s.X * a + d.X * (1 - a), s.Y * a + d.Y * (1 - a), s.Z * a + d.Z * (1 - a), a + d.W * (1 - a)),
        };
        pixel = new Color(Q(o.X), Q(o.Y), Q(o.Z), Q(o.W));
    }

    private static byte Q(float v) => (byte)MathF.Round(Math.Clamp(v, 0f, 1f) * 255f, MidpointRounding.AwayFromZero);
}
