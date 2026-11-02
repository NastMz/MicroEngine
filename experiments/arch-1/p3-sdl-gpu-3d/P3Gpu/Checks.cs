namespace P3Gpu;

public readonly record struct Rgba(byte R, byte G, byte B, byte A)
{
    public static Rgba From(System.Numerics.Vector4 c) => new(Q(c.X), Q(c.Y), Q(c.Z), Q(c.W));
    public static byte Q(float v) => (byte)Math.Clamp(MathF.Round(v * 255f), 0, 255);
    public override string ToString() => $"({R},{G},{B},{A})";
}

/// <summary>A captured RGBA8 frame, rows top to bottom.</summary>
public sealed class Image(int width, int height, byte[] rgba)
{
    public int Width { get; } = width;
    public int Height { get; } = height;
    public byte[] Rgba { get; } = rgba;

    public Rgba this[int x, int y]
    {
        get
        {
            var i = (y * Width + x) * 4;
            return new Rgba(Rgba[i], Rgba[i + 1], Rgba[i + 2], Rgba[i + 3]);
        }
    }
}

/// <summary>
/// Records checks. Every check names the property it evaluates; a tolerance names the source of variation the
/// contract allows (ARCH-1 tolerance policy).
/// </summary>
public sealed class Checks(string scope)
{
    private readonly List<(string Line, bool Pass)> _results = [];

    public int Failed => _results.Count(r => !r.Pass);
    public int Count => _results.Count;

    public void True(string property, string name, bool condition, string detail)
        => Add(condition, $"[{property}] {name}: {detail}");

    public void Exact(string property, string name, Rgba actual, Rgba expected)
        => Add(actual == expected, $"[{property}] {name}: expected {expected}, got {actual} (exact)");

    public void Near(string property, string name, Rgba actual, Rgba expected, int tolerance, string variation)
    {
        var pass = Math.Abs(actual.R - expected.R) <= tolerance && Math.Abs(actual.G - expected.G) <= tolerance
            && Math.Abs(actual.B - expected.B) <= tolerance && Math.Abs(actual.A - expected.A) <= tolerance;
        Add(pass, $"[{property}] {name}: expected {expected} ±{tolerance} ({variation}), got {actual}");
    }

    public void Approx(string property, string name, double actual, double expected, double tolerance, string variation)
        => Add(Math.Abs(actual - expected) <= tolerance, $"[{property}] {name}: expected {expected:R} ±{tolerance:G3} ({variation}), got {actual:R}");

    private void Add(bool pass, string line)
    {
        _results.Add((line, pass));
        Console.WriteLine($"  {(pass ? "PASS" : "FAIL")} {scope} {line}");
    }
}
