using MicroEngine;
using MicroEngine.Graphics;

namespace TrailDuel.Visuals;

/// <summary>A shaded material: a dark-to-light ramp plus the colour used for its outline.</summary>
internal sealed record Material(Color[] Ramp, Color Line)
{
    public static Material Of(params string[] hexRamp)
    {
        var ramp = hexRamp.Select(Color.Hex).ToArray();
        return new Material(ramp, Color.Lerp(ramp[0], Pal.Ink, 0.6f));
    }
}

/// <summary>
/// A tiny "sculpting" rasterizer for drawn props: trees, rocks, bushes, shrubs, plants, the shrine's
/// altar and statue, and the bag's satchel. Props are composed from rounded primitives (ellipses,
/// tapered capsules, triangles) that carry a pseudo surface normal, so every sprite gets consistent
/// top-left lighting, dithered ramps, selective coloured outlines and inner contour lines where parts
/// overlap. Details are painted afterwards as decals.
/// </summary>
internal sealed class PropCanvas
{
    private interface IShape
    {
        /// <summary>Returns true when the pixel centre is inside; outputs a light value 0..1.</summary>
        bool Sample(float x, float y, out float light);
    }

    private sealed record Part(IShape Shape, Material Material, bool Contour);

    private static readonly (float X, float Y, float Z) LightDir = Normalize(-0.55f, -0.65f, 0.75f);

    private readonly List<Part> _parts = [];
    private readonly List<Action<PixelImage>> _decals = [];
    public int Size { get; }
    /// <summary>Uniform scale and offset applied to every coordinate.</summary>
    public float Scale { get; set; } = 1f;
    public float OffsetX { get; set; }
    public float OffsetY { get; set; }

    public PropCanvas(int size) => Size = size;

    private float X(float x) => x * Scale + OffsetX;
    private float Y(float y) => y * Scale + OffsetY;
    private float L(float v) => v * Scale;

    public PropCanvas Ellipse(float cx, float cy, float rx, float ry, Material m, bool contour = true, float bulge = 1f)
    {
        _parts.Add(new Part(new EllipseShape(X(cx), Y(cy), L(rx), L(ry), bulge), m, contour));
        return this;
    }

    public PropCanvas Circle(float cx, float cy, float r, Material m, bool contour = true) => Ellipse(cx, cy, r, r, m, contour);

    /// <summary>A tapered tube from (x0,y0) radius r0 to (x1,y1) radius r1: limbs, tails, necks.</summary>
    public PropCanvas Capsule(float x0, float y0, float x1, float y1, float r0, float r1, Material m, bool contour = true)
    {
        _parts.Add(new Part(new CapsuleShape(X(x0), Y(y0), X(x1), Y(y1), L(r0), L(r1)), m, contour));
        return this;
    }

    public PropCanvas Triangle(float ax, float ay, float bx, float by, float cx, float cy, Material m, bool contour = true)
    {
        _parts.Add(new Part(new TriangleShape(X(ax), Y(ay), X(bx), Y(by), X(cx), Y(cy)), m, contour));
        return this;
    }

    /// <summary>A polygon given as x,y pairs: claws, spikes, crests, faceted rock and other sharp forms.</summary>
    public PropCanvas Poly(Material m, params float[] xy) => Poly(m, true, xy);

    public PropCanvas Poly(Material m, bool contour, params float[] xy)
    {
        _parts.Add(new Part(new PolygonShape(Points(xy)), m, contour));
        return this;
    }

    /// <summary>A flat facet: the whole polygon faces (nx, ny) and takes one light value (rock planes, horns, plates).</summary>
    public PropCanvas Facet(Material m, float nx, float ny, params float[] xy)
    {
        float nz = MathF.Sqrt(MathF.Max(0.05f, 1 - nx * nx - ny * ny));
        var (a, b, c) = Normalize(nx, ny, nz);
        _parts.Add(new Part(new PolygonShape(Points(xy), Shade(a, b, c)), m, false));
        return this;
    }

    private (float X, float Y)[] Points(float[] xy)
    {
        var pts = new (float X, float Y)[xy.Length / 2];
        for (int i = 0; i < pts.Length; i++) pts[i] = (X(xy[i * 2]), Y(xy[i * 2 + 1]));
        return pts;
    }

    // ---- decals (painted after shading, in canvas coordinates) ----

    public PropCanvas Pixel(float x, float y, Color c)
    {
        int px = (int)MathF.Floor(X(x)), py = (int)MathF.Floor(Y(y));
        _decals.Add(img => img.Blend(px, py, c));
        return this;
    }

    public PropCanvas Dot(float cx, float cy, float r, Color c)
    {
        float x = X(cx), y = Y(cy), rr = L(r);
        _decals.Add(img => img.FillCircle(x, y, rr, c));
        return this;
    }

    public PropCanvas Stroke(float x0, float y0, float x1, float y1, Color c)
    {
        int a = (int)MathF.Floor(X(x0)), b = (int)MathF.Floor(Y(y0)), d = (int)MathF.Floor(X(x1)), e = (int)MathF.Floor(Y(y1));
        _decals.Add(img => img.Line(a, b, d, e, c));
        return this;
    }

    public PixelImage Render()
    {
        int n = Size * Size;
        var owner = new int[n];
        var light = new float[n];
        Array.Fill(owner, -1);
        for (int p = 0; p < _parts.Count; p++)
        {
            var shape = _parts[p].Shape;
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
                if (shape.Sample(x + 0.5f, y + 0.5f, out float l)) { owner[y * Size + x] = p; light[y * Size + x] = l; }
        }

        var img = new PixelImage(Size, Size);
        int Owner(int x, int y) => x < 0 || y < 0 || x >= Size || y >= Size ? -1 : owner[y * Size + x];
        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            int o = owner[y * Size + x];
            if (o < 0) continue;
            var part = _parts[o];
            var ramp = part.Material.Ramp;
            // Inner contour: this part's edge where it overlaps a part drawn behind it.
            bool edge = false;
            if (part.Contour)
                foreach (var (dx, dy) in Neighbours)
                {
                    int q = Owner(x + dx, y + dy);
                    if (q >= 0 && q < o && _parts[q].Material != part.Material) { edge = true; break; }
                }
            float l = light[y * Size + x];
            // Occlusion: parts in front cast a short shadow down-right onto the parts behind them,
            // which separates limbs, heads and tails from the body they overlap.
            if (Owner(x - 1, y - 1) > o || Owner(x, y - 1) > o || Owner(x - 1, y) > o) l -= 0.3f;
            else if (Owner(x - 2, y - 2) > o || Owner(x - 1, y - 2) > o || Owner(x - 2, y - 1) > o) l -= 0.14f;
            var c = edge ? part.Material.Line : Band(ramp, l, x, y);
            if (!edge && l > 0.97f) c = Color.Lerp(ramp[^1], Pal.White, 0.45f);
            img[x, y] = c;
        }

        // Selective outer outline: each silhouette pixel borders in its own material's colours, softer
        // (the darkest ramp step) on the lit top-left and the full line colour on the shadow side.
        var outline = new Color?[n];
        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            if (owner[y * Size + x] >= 0) continue;
            int best = -1;
            foreach (var (dx, dy) in Neighbours)
            {
                int q = Owner(x + dx, y + dy);
                if (q > best) best = q;
            }
            if (best < 0) continue;
            var m = _parts[best].Material;
            bool litSide = Owner(x + 1, y) >= 0 && Owner(x, y + 1) >= 0 && Owner(x - 1, y) < 0 && Owner(x, y - 1) < 0;
            outline[y * Size + x] = litSide ? Color.Lerp(m.Ramp[0], m.Line, 0.35f) : m.Line;
        }
        for (int i = 0; i < n; i++) if (outline[i] is { } c) img[i % Size, i / Size] = c;

        foreach (var decal in _decals) decal(img);
        return img;
    }

    /// <summary>Ramp lookup with crisp bands: dithering only in a thin seam between steps.</summary>
    private static Color Band(Color[] ramp, float value, int x, int y)
    {
        float scaled = Math.Clamp(value, 0, 0.9999f) * (ramp.Length - 1);
        int i = (int)scaled;
        float f = Math.Clamp((scaled - i - 0.5f) * 2.6f + 0.5f, 0, 1);
        return i + 1 < ramp.Length && Art.Dither(x, y, f) ? ramp[i + 1] : ramp[i];
    }

    private static readonly (int, int)[] Neighbours = [(-1, 0), (1, 0), (0, -1), (0, 1)];

    private static (float X, float Y, float Z) Normalize(float x, float y, float z)
    {
        float len = MathF.Sqrt(x * x + y * y + z * z);
        return (x / len, y / len, z / len);
    }

    private static float Shade(float nx, float ny, float nz)
    {
        var (lx, ly, lz) = LightDir;
        float diffuse = MathF.Max(0, nx * lx + ny * ly + nz * lz);
        // Lift shadows a little so the ramp's darkest step reads as form, not as a hole.
        return Math.Clamp(0.12f + diffuse * 0.95f, 0, 1);
    }

    private sealed class EllipseShape(float cx, float cy, float rx, float ry, float bulge) : IShape
    {
        public bool Sample(float x, float y, out float light)
        {
            float nx = (x - cx) / rx, ny = (y - cy) / ry;
            float d = nx * nx + ny * ny;
            light = 0;
            if (d > 1) return false;
            float nz = MathF.Sqrt(1 - d) * bulge;
            var (a, b, c) = Normalize(nx, ny, nz);
            light = Shade(a, b, c);
            return true;
        }
    }

    private sealed class CapsuleShape(float x0, float y0, float x1, float y1, float r0, float r1) : IShape
    {
        public bool Sample(float x, float y, out float light)
        {
            float vx = x1 - x0, vy = y1 - y0;
            float len2 = vx * vx + vy * vy;
            float t = len2 <= 0 ? 0 : Math.Clamp(((x - x0) * vx + (y - y0) * vy) / len2, 0, 1);
            float px = x0 + vx * t, py = y0 + vy * t;
            float r = r0 + (r1 - r0) * t;
            float dx = (x - px) / r, dy = (y - py) / r;
            float d = dx * dx + dy * dy;
            light = 0;
            if (d > 1) return false;
            var (a, b, c) = Normalize(dx, dy, MathF.Sqrt(1 - d));
            light = Shade(a, b, c);
            return true;
        }
    }

    private sealed class PolygonShape : IShape
    {
        private readonly (float X, float Y)[] _p;
        private readonly float _gx, _gy, _r;
        private readonly float? _flat;

        public PolygonShape((float X, float Y)[] points, float? flatLight = null)
        {
            _p = points;
            _flat = flatLight;
            _gx = points.Average(p => p.X);
            _gy = points.Average(p => p.Y);
            _r = MathF.Max(1, points.Max(p => MathF.Sqrt((p.X - _gx) * (p.X - _gx) + (p.Y - _gy) * (p.Y - _gy))));
        }

        public bool Sample(float x, float y, out float light)
        {
            light = 0;
            bool inside = false;
            for (int i = 0, j = _p.Length - 1; i < _p.Length; j = i++)
            {
                var (xi, yi) = _p[i];
                var (xj, yj) = _p[j];
                if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi) inside = !inside;
            }
            if (!inside) return false;
            if (_flat is { } flat) { light = flat; return true; }
            float nx = (x - _gx) / _r * 0.85f, ny = (y - _gy) / _r * 0.85f;
            var (a, b, c) = Normalize(nx, ny, MathF.Sqrt(MathF.Max(0.1f, 1 - nx * nx - ny * ny)));
            light = Shade(a, b, c);
            return true;
        }
    }

    private sealed class TriangleShape(float ax, float ay, float bx, float by, float cx, float cy) : IShape
    {
        private readonly float _gx = (ax + bx + cx) / 3f, _gy = (ay + by + cy) / 3f;
        private readonly float _r = MathF.Max(1, MathF.Max(MathF.Max(Dist(ax, ay, (ax + bx + cx) / 3f, (ay + by + cy) / 3f), Dist(bx, by, (ax + bx + cx) / 3f, (ay + by + cy) / 3f)), Dist(cx, cy, (ax + bx + cx) / 3f, (ay + by + cy) / 3f)));

        private static float Dist(float x0, float y0, float x1, float y1) => MathF.Sqrt((x0 - x1) * (x0 - x1) + (y0 - y1) * (y0 - y1));

        public bool Sample(float x, float y, out float light)
        {
            light = 0;
            float d1 = (x - bx) * (ay - by) - (ax - bx) * (y - by);
            float d2 = (x - cx) * (by - cy) - (bx - cx) * (y - cy);
            float d3 = (x - ax) * (cy - ay) - (cx - ax) * (y - ay);
            bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
            if (neg && pos) return false;
            float nx = (x - _gx) / _r * 0.8f, ny = (y - _gy) / _r * 0.8f;
            var (a, b, c) = Normalize(nx, ny, MathF.Sqrt(MathF.Max(0.1f, 1 - nx * nx - ny * ny)));
            light = Shade(a, b, c);
            return true;
        }
    }
}
