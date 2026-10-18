using MicroEngine.Loop;
namespace MicroEngine.TwoD;

public readonly record struct Motion(Rect Bounds, bool BlockedX, bool BlockedY);

/// <summary>Immutable static AABBs; displacement policy and forces belong to the caller.</summary>
public sealed class StaticWorld
{
    private readonly Rect[] _solids;
    public StaticWorld(IEnumerable<Rect> solids)
    {
        ArgumentNullException.ThrowIfNull(solids);
        _solids = solids.ToArray();
        foreach (var solid in _solids) Validate(solid);
    }
    public static bool Overlaps(Rect a, Rect b)
    {
        Validate(a); Validate(b);
        return Intersects(a, b);
    }
    private static bool Intersects(Rect a, Rect b) =>
        a.X < b.X + b.Width && a.X + a.Width > b.X && a.Y < b.Y + b.Height && a.Y + a.Height > b.Y;
    internal static void Validate(Rect box)
    {
        if (!float.IsFinite(box.X) || !float.IsFinite(box.Y) || !float.IsFinite(box.Width) || !float.IsFinite(box.Height)
            || box.Width <= 0 || box.Height <= 0 || !float.IsFinite(box.X + box.Width) || !float.IsFinite(box.Y + box.Height)
            || box.X + box.Width <= box.X || box.Y + box.Height <= box.Y)
            throw new ArgumentOutOfRangeException(nameof(box), "Bounds must be positive, finite and representable.");
    }
    public Motion Move(Rect body, float dx, float dy)
    {
        Validate(body);
        if (!float.IsFinite(dx) || !float.IsFinite(dy)) throw new ArgumentOutOfRangeException(nameof(dx));
        if (_solids.Any(solid => Intersects(body, solid))) throw new ArgumentException("Body initially overlaps a solid.", nameof(body));
        float x = body.X + dx;
        foreach (var solid in _solids)
        {
            if (body.Y >= solid.Y + solid.Height || body.Y + body.Height <= solid.Y) continue;
            if (dx > 0 && body.X + body.Width <= solid.X && x + body.Width >= solid.X) x = Math.Min(x, solid.X - body.Width);
            if (dx < 0 && body.X >= solid.X + solid.Width && x <= solid.X + solid.Width) x = Math.Max(x, solid.X + solid.Width);
        }
        var horizontal = new Rect(x, body.Y, body.Width, body.Height); Validate(horizontal);
        float y = body.Y + dy;
        foreach (var solid in _solids)
        {
            if (x >= solid.X + solid.Width || x + body.Width <= solid.X) continue;
            if (dy > 0 && body.Y + body.Height <= solid.Y && y + body.Height >= solid.Y) y = Math.Min(y, solid.Y - body.Height);
            if (dy < 0 && body.Y >= solid.Y + solid.Height && y <= solid.Y + solid.Height) y = Math.Max(y, solid.Y + solid.Height);
        }
        var bounds = new Rect(x, y, body.Width, body.Height); Validate(bounds);
        return new(bounds, x != body.X + dx, y != body.Y + dy);
    }
}
