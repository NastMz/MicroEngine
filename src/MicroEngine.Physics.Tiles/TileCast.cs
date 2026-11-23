using System.Numerics;
using MicroEngine.World;

namespace MicroEngine.Physics.Tiles;

/// <summary>
/// The shape queries of the tile backend. A query shape is a rounded box: a core rectangle (half extents) with a
/// radius around it, which is a box (no radius), a circle (no core) or a standing capsule (a core with no width).
/// Moving it against a rectangle is moving its centre against that rectangle grown by the core and rounded by the
/// radius, so one exact test serves the three.
/// </summary>
internal static class TileCast
{
    /// <summary>
    /// Where the centre of the rounded box, going from <paramref name="from"/> along <paramref name="dir"/> (t in
    /// [0, 1]), first touches <paramref name="rect"/>. A shape that overlaps the rectangle where it starts does not
    /// hit it; one that touches it there hits at 0 when it moves into it.
    /// </summary>
    public static bool Rounded(RectF rect, Vector2 half, float radius, Vector2 from, Vector2 dir, out float t, out Vector2 normal)
    {
        var grown = new RectF(rect.X - half.X, rect.Y - half.Y, rect.Width + 2 * half.X, rect.Height + 2 * half.Y);
        if (radius <= 0) return TileRay.Box(grown, from, dir, topOnly: false, skip: false, out t, out normal);
        t = 0;
        normal = default;
        if (DistanceSquared(grown, from) < radius * radius) return false;
        bool found = false;
        // The rounded rectangle is two rectangles (grown along x, grown along y) and a circle at each corner.
        Consider(TileRay.Box(new RectF(grown.X - radius, grown.Y, grown.Width + 2 * radius, grown.Height), from, dir, false, false, out float ta, out var na), ta, na, ref found, ref t, ref normal);
        Consider(TileRay.Box(new RectF(grown.X, grown.Y - radius, grown.Width, grown.Height + 2 * radius), from, dir, false, false, out ta, out na), ta, na, ref found, ref t, ref normal);
        Consider(Circle(new Vector2(grown.Left, grown.Top), radius, from, dir, out ta, out na), ta, na, ref found, ref t, ref normal);
        Consider(Circle(new Vector2(grown.Right, grown.Top), radius, from, dir, out ta, out na), ta, na, ref found, ref t, ref normal);
        Consider(Circle(new Vector2(grown.Left, grown.Bottom), radius, from, dir, out ta, out na), ta, na, ref found, ref t, ref normal);
        Consider(Circle(new Vector2(grown.Right, grown.Bottom), radius, from, dir, out ta, out na), ta, na, ref found, ref t, ref normal);
        return found;
    }

    private static void Consider(bool hit, float t, Vector2 normal, ref bool found, ref float best, ref Vector2 bestNormal)
    {
        if (!hit || (found && t >= best)) return;
        found = true;
        best = t;
        bestNormal = normal;
    }

    /// <summary>Where the ray enters the circle; a ray that starts inside it does not hit.</summary>
    private static bool Circle(Vector2 centre, float radius, Vector2 from, Vector2 dir, out float t, out Vector2 normal)
    {
        t = 0;
        normal = default;
        var o = from - centre;
        float a = Vector2.Dot(dir, dir), b = Vector2.Dot(o, dir), c = Vector2.Dot(o, o) - radius * radius;
        if (a == 0 || b >= 0 || c < 0) return false;
        float disc = b * b - a * c;
        if (disc < 0) return false;
        t = MathF.Max(0, (-b - MathF.Sqrt(disc)) / a);
        if (t > 1) return false;
        normal = Vector2.Normalize(o + dir * t);
        return true;
    }

    /// <summary>The squared distance from <paramref name="p"/> to <paramref name="rect"/>; 0 inside it or on its edge.</summary>
    public static float DistanceSquared(RectF rect, Vector2 p)
    {
        float dx = MathF.Max(MathF.Max(rect.Left - p.X, p.X - rect.Right), 0);
        float dy = MathF.Max(MathF.Max(rect.Top - p.Y, p.Y - rect.Bottom), 0);
        return dx * dx + dy * dy;
    }

    /// <summary>The point of <paramref name="rect"/> nearest <paramref name="p"/> (<paramref name="p"/> itself inside it).</summary>
    public static Vector2 Nearest(RectF rect, Vector2 p) => new(Math.Clamp(p.X, rect.Left, rect.Right), Math.Clamp(p.Y, rect.Top, rect.Bottom));

    /// <summary>Whether the rounded box centred at <paramref name="centre"/> truly overlaps <paramref name="rect"/> (touching is not overlapping).</summary>
    public static bool Overlaps(RectF rect, Vector2 half, float radius, Vector2 centre)
    {
        if (radius <= 0)
            return centre.X + half.X > rect.Left && centre.X - half.X < rect.Right && centre.Y + half.Y > rect.Top && centre.Y - half.Y < rect.Bottom;
        var grown = new RectF(rect.X - half.X, rect.Y - half.Y, rect.Width + 2 * half.X, rect.Height + 2 * half.Y);
        return DistanceSquared(grown, centre) < radius * radius;
    }

    /// <summary>
    /// A one-way surface (a platform's rectangle or a one-way cell) stops a cast only when it comes down onto its top
    /// from above: the hit is on the top face and the shape's bottom started at or above it.
    /// </summary>
    public static bool OneWayStops(RectF rect, Vector2 half, float radius, Vector2 from, Vector2 normal) =>
        normal == new Vector2(0, -1) && from.Y + half.Y + radius <= rect.Top + TileSweep.OneWaySlack;

    /// <summary>The first blocking cell of <paramref name="grid"/> the rounded box touches: the cell's rectangle, when and with which normal.</summary>
    public static bool Grid(ICollisionGrid grid, Vector2 origin, Vector2 half, float radius, Vector2 from, Vector2 dir, bool passOneWay, out float t, out Vector2 normal, out RectF cell)
    {
        t = 0;
        normal = default;
        cell = default;
        int ts = grid.TileSize;
        var reach = half + new Vector2(radius);
        var to = from + dir;
        int x0 = (int)MathF.Floor((MathF.Min(from.X, to.X) - reach.X - origin.X) / ts), x1 = (int)MathF.Floor((MathF.Max(from.X, to.X) + reach.X - origin.X) / ts);
        int y0 = (int)MathF.Floor((MathF.Min(from.Y, to.Y) - reach.Y - origin.Y) / ts), y1 = (int)MathF.Floor((MathF.Max(from.Y, to.Y) + reach.Y - origin.Y) / ts);
        bool found = false;
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            var flags = grid.FlagsAt(x, y);
            bool solid = (flags & TileFlags.Solid) != 0;
            if (!solid && (passOneWay || (flags & TileFlags.OneWay) == 0)) continue;
            var tile = new RectF(x * ts + origin.X, y * ts + origin.Y, ts, ts);
            if (!Rounded(tile, half, radius, from, dir, out float tt, out var n)) continue;
            if (!solid && !OneWayStops(tile, half, radius, from, n)) continue;
            if (found && tt >= t) continue;
            found = true;
            t = tt;
            normal = n;
            cell = tile;
        }
        return found;
    }

    /// <summary>Whether the rounded box centred at <paramref name="centre"/> overlaps a blocking cell of <paramref name="grid"/>.</summary>
    public static bool GridOverlaps(ICollisionGrid grid, Vector2 origin, Vector2 half, float radius, Vector2 centre, bool passOneWay)
    {
        int ts = grid.TileSize;
        var reach = half + new Vector2(radius);
        int x0 = (int)MathF.Floor((centre.X - reach.X - origin.X) / ts), x1 = (int)MathF.Floor((centre.X + reach.X - origin.X) / ts);
        int y0 = (int)MathF.Floor((centre.Y - reach.Y - origin.Y) / ts), y1 = (int)MathF.Floor((centre.Y + reach.Y - origin.Y) / ts);
        var mask = passOneWay ? TileFlags.Solid : TileFlags.Solid | TileFlags.OneWay;
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
            if ((grid.FlagsAt(x, y) & mask) != 0 && Overlaps(new RectF(x * ts + origin.X, y * ts + origin.Y, ts, ts), half, radius, centre))
                return true;
        return false;
    }

    /// <summary>The point of the blocking cells of <paramref name="grid"/> nearest <paramref name="p"/>, within <paramref name="reach"/>.</summary>
    public static bool GridNearest(ICollisionGrid grid, Vector2 origin, Vector2 p, float reach, bool passOneWay, out Vector2 nearest, out float distanceSquared)
    {
        int ts = grid.TileSize;
        int x0 = (int)MathF.Floor((p.X - reach - origin.X) / ts), x1 = (int)MathF.Floor((p.X + reach - origin.X) / ts);
        int y0 = (int)MathF.Floor((p.Y - reach - origin.Y) / ts), y1 = (int)MathF.Floor((p.Y + reach - origin.Y) / ts);
        var mask = passOneWay ? TileFlags.Solid : TileFlags.Solid | TileFlags.OneWay;
        bool found = false;
        nearest = default;
        distanceSquared = 0;
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            if ((grid.FlagsAt(x, y) & mask) == 0) continue;
            var tile = new RectF(x * ts + origin.X, y * ts + origin.Y, ts, ts);
            float d = DistanceSquared(tile, p);
            if (found && d >= distanceSquared) continue;
            found = true;
            distanceSquared = d;
            nearest = Nearest(tile, p);
        }
        return found;
    }
}
