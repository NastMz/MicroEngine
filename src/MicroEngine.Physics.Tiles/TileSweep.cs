using System.Numerics;
using MicroEngine.World;

namespace MicroEngine.Physics.Tiles;

/// <summary>A tile grid as obstacles: cell (x, y) covers <c>Origin + (x, y) × TileSize</c>.</summary>
internal readonly record struct GridObstacle(ICollisionGrid Grid, Vector2 Origin, int Source, bool OneWayAllowed = true);

/// <summary>A rectangle as an obstacle; one-way ones only stop boxes coming down onto their top.</summary>
internal readonly record struct BoxObstacle(RectF Rect, bool OneWay, int Source);

/// <summary>
/// The swept test of the tile mover, one axis at a time: a box moves along X or Y by a distance and stops flush against
/// the first solid cell or rectangle in its way, including thin ones lying wholly between where it starts and where it
/// would end (the tunnelling of the old sub-stepped test, ARCH-1 D6). One-way cells and rectangles stop only a box
/// moving down whose bottom started at or above their top.
/// </summary>
internal static class TileSweep
{
    /// <summary>The slack of the one-way rule: a box whose bottom is this far below a platform's top still lands on it.</summary>
    public const float OneWaySlack = 0.01f;

    /// <summary>
    /// The exact rule's rounding allowance: a box flush against a surface may end a few ulps inside it (the flush position
    /// is computed as the surface's edge minus the box's size), so overlaps up to this depth count as touching, not
    /// overlapping.
    /// </summary>
    public const float Tolerance = 1e-3f;

    /// <summary>
    /// Moves <paramref name="box"/> by <paramref name="d"/> along one axis.
    /// </summary>
    /// <param name="box">Where the box starts.</param>
    /// <param name="d">The distance, signed.</param>
    /// <param name="horizontal">X (true) or Y.</param>
    /// <param name="grids">The tile grids in the way.</param>
    /// <param name="boxes">The rectangles in the way.</param>
    /// <param name="ignoreOneWay">Pass through one-way cells and rectangles.</param>
    /// <param name="compatible">
    /// The rule of <see cref="TileMotion"/> for an unobstructed sweep: cells the box overlaps at its destination block
    /// it even when it already overlapped them (it is pushed out to their edge), the cell or rectangle whose centre is
    /// nearest wins, and rectangles it overlapped at the start are ignored; the exact rule otherwise applies only when
    /// something lies wholly between the start and the destination. False: the exact rule always (the world's mover):
    /// the obstacle with the nearest leading edge among those the box did not overlap at the start.
    /// </param>
    /// <param name="blocked">Whether something stopped the box.</param>
    /// <param name="source">The source id of what stopped it.</param>
    /// <returns>The box after the sweep: flush against what stopped it, or moved by <paramref name="d"/>.</returns>
    public static RectF Sweep(RectF box, float d, bool horizontal, ReadOnlySpan<GridObstacle> grids, ReadOnlySpan<BoxObstacle> boxes,
        bool ignoreOneWay, bool compatible, out bool blocked, out int source)
    {
        var moved = horizontal ? box.Offset(d, 0) : box.Offset(0, d);
        float eps = compatible ? 0 : Tolerance;
        RectF block;
        bool found;
        if (d != 0 && Swept(box, moved, d, horizontal, grids, boxes, ignoreOneWay, requireGap: compatible, eps, out block, out source))
            found = true;
        else if (compatible || d == 0)
            found = Overlapping(moved, box, horizontal, grids, boxes, ignoreOneWay, ignoreStartOverlaps: !compatible, out block, out source, eps);
        else
        {
            found = false;
            block = default;
            source = -1;
        }
        blocked = found;
        if (!found) return moved;
        if (horizontal)
        {
            if (d > 0) return box with { X = block.Left - box.Width };
            if (d < 0) return box with { X = block.Right };
        }
        else
        {
            if (d > 0) return box with { Y = block.Top - box.Height };
            if (d < 0) return box with { Y = block.Bottom };
        }
        return box;
    }

    /// <summary>
    /// Whether <paramref name="box"/> overlaps an obstacle (the old <c>TileMotion</c> test): cells it overlaps that block
    /// (one-way ones only when it came down onto them from <paramref name="previous"/>), and rectangles it overlaps that
    /// <paramref name="previous"/> did not; the one whose centre is nearest <paramref name="previous"/>'s wins.
    /// </summary>
    public static bool Overlapping(RectF box, RectF previous, bool horizontal, ReadOnlySpan<GridObstacle> grids, ReadOnlySpan<BoxObstacle> boxes,
        bool ignoreOneWay, bool ignoreStartOverlaps, out RectF block, out int source, float eps = 0)
    {
        bool found = false;
        block = default;
        source = -1;
        foreach (var g in grids)
        {
            int ts = g.Grid.TileSize;
            float left = box.Left - g.Origin.X, right = box.Right - g.Origin.X, top = box.Top - g.Origin.Y, bottom = box.Bottom - g.Origin.Y;
            int x0 = (int)MathF.Floor(left / ts), x1 = (int)MathF.Floor((right - 0.001f) / ts);
            int y0 = (int)MathF.Floor(top / ts), y1 = (int)MathF.Floor((bottom - 0.001f) / ts);
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                var flags = g.Grid.FlagsAt(x, y);
                var tile = new RectF(x * ts + g.Origin.X, y * ts + g.Origin.Y, ts, ts);
                bool solid = (flags & TileFlags.Solid) != 0;
                bool oneWay = !solid && g.OneWayAllowed && !ignoreOneWay && !horizontal && (flags & TileFlags.OneWay) != 0
                    && previous.Bottom <= tile.Top + OneWaySlack && box.Bottom > tile.Top;
                if (!solid && !oneWay) continue;
                if (ignoreStartOverlaps && Hits(previous, tile, eps)) continue;
                if (!found || Closer(tile, block, previous, horizontal)) { block = tile; source = g.Source; }
                found = true;
            }
        }
        foreach (var b in boxes)
        {
            var s = b.Rect;
            if (!Hits(box, s, eps) || Hits(previous, s, eps)) continue;
            if (b.OneWay && (ignoreOneWay || horizontal || !(previous.Bottom <= s.Top + OneWaySlack && box.Bottom > s.Top))) continue;
            if (!found || Closer(s, block, previous, horizontal)) { block = s; source = b.Source; }
            found = true;
        }
        return found;
    }

    /// <summary>
    /// The obstacle with the nearest leading edge among those the swept area from <paramref name="box"/> to
    /// <paramref name="moved"/> crosses and <paramref name="box"/> does not overlap; with <paramref name="requireGap"/>,
    /// only when one of them lies wholly between the two (neither overlaps it).
    /// </summary>
    private static bool Swept(RectF box, RectF moved, float d, bool horizontal, ReadOnlySpan<GridObstacle> grids, ReadOnlySpan<BoxObstacle> boxes,
        bool ignoreOneWay, bool requireGap, float eps, out RectF block, out int source)
    {
        var swept = Union(box, moved);
        bool found = false, gap = false;
        float best = 0;
        block = default;
        source = -1;
        foreach (var g in grids)
        {
            int ts = g.Grid.TileSize;
            int x0 = (int)MathF.Floor((swept.Left - g.Origin.X) / ts), x1 = (int)MathF.Floor((swept.Right - g.Origin.X - 0.001f) / ts);
            int y0 = (int)MathF.Floor((swept.Top - g.Origin.Y) / ts), y1 = (int)MathF.Floor((swept.Bottom - g.Origin.Y - 0.001f) / ts);
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                var tile = new RectF(x * ts + g.Origin.X, y * ts + g.Origin.Y, ts, ts);
                if (!Hits(swept, tile, eps) || Hits(box, tile, eps)) continue;
                var flags = g.Grid.FlagsAt(x, y);
                bool solid = (flags & TileFlags.Solid) != 0;
                bool oneWay = !solid && g.OneWayAllowed && !ignoreOneWay && !horizontal && (flags & TileFlags.OneWay) != 0
                    && box.Bottom <= tile.Top + OneWaySlack && moved.Bottom > tile.Top;
                if (!solid && !oneWay) continue;
                Consider(tile, g.Source, moved, d, horizontal, eps, ref found, ref gap, ref best, ref block, ref source);
            }
        }
        foreach (var b in boxes)
        {
            var s = b.Rect;
            if (!Hits(swept, s, eps) || Hits(box, s, eps)) continue;
            if (b.OneWay && (ignoreOneWay || horizontal || !(box.Bottom <= s.Top + OneWaySlack && moved.Bottom > s.Top))) continue;
            Consider(s, b.Source, moved, d, horizontal, eps, ref found, ref gap, ref best, ref block, ref source);
        }
        return found && (gap || !requireGap);
    }

    private static void Consider(RectF obstacle, int id, RectF moved, float d, bool horizontal, float eps, ref bool found, ref bool gap, ref float best, ref RectF block, ref int source)
    {
        // The leading edge the moving box meets first: the nearest one in the direction of motion.
        float edge = horizontal ? (d > 0 ? obstacle.Left : -obstacle.Right) : (d > 0 ? obstacle.Top : -obstacle.Bottom);
        if (!Hits(moved, obstacle, eps)) gap = true;
        if (found && edge >= best) return;
        found = true;
        best = edge;
        block = obstacle;
        source = id;
    }

    /// <summary>
    /// Adds to <paramref name="sources"/> (once each, in obstacle order) every obstacle a sweep of <paramref name="d"/>
    /// from <paramref name="box"/> would meet, by the exact rule: the surfaces the box touches on that side when
    /// <paramref name="d"/> is a small probe distance.
    /// </summary>
    public static void Touching(RectF box, float d, bool horizontal, ReadOnlySpan<GridObstacle> grids, ReadOnlySpan<BoxObstacle> boxes, bool ignoreOneWay, List<int> sources)
    {
        var moved = horizontal ? box.Offset(d, 0) : box.Offset(0, d);
        var swept = Union(box, moved);
        foreach (var g in grids)
        {
            int ts = g.Grid.TileSize;
            int x0 = (int)MathF.Floor((swept.Left - g.Origin.X) / ts), x1 = (int)MathF.Floor((swept.Right - g.Origin.X - 0.001f) / ts);
            int y0 = (int)MathF.Floor((swept.Top - g.Origin.Y) / ts), y1 = (int)MathF.Floor((swept.Bottom - g.Origin.Y - 0.001f) / ts);
            bool hit = false;
            for (int y = y0; y <= y1 && !hit; y++)
            for (int x = x0; x <= x1 && !hit; x++)
            {
                var tile = new RectF(x * ts + g.Origin.X, y * ts + g.Origin.Y, ts, ts);
                if (!Hits(swept, tile, Tolerance) && !Hits(moved, tile, 0) || Hits(box, tile, Tolerance)) continue;
                var flags = g.Grid.FlagsAt(x, y);
                bool solid = (flags & TileFlags.Solid) != 0;
                bool oneWay = !solid && g.OneWayAllowed && !ignoreOneWay && !horizontal && d > 0 && (flags & TileFlags.OneWay) != 0
                    && box.Bottom <= tile.Top + OneWaySlack && moved.Bottom > tile.Top;
                hit = solid || oneWay;
            }
            if (hit && !sources.Contains(g.Source)) sources.Add(g.Source);
        }
        foreach (var b in boxes)
        {
            var s = b.Rect;
            if (!Hits(swept, s, Tolerance) && !Hits(moved, s, 0) || Hits(box, s, Tolerance)) continue;
            if (b.OneWay && (ignoreOneWay || horizontal || d <= 0 || !(box.Bottom <= s.Top + OneWaySlack && moved.Bottom > s.Top))) continue;
            if (!sources.Contains(b.Source)) sources.Add(b.Source);
        }
    }

    /// <summary>Overlap deeper than <paramref name="eps"/> on both axes (with 0: <see cref="RectF.Intersects"/>, strict).</summary>
    public static bool Hits(RectF a, RectF b, float eps) =>
        a.X < b.Right - eps && a.Right > b.X + eps && a.Y < b.Bottom - eps && a.Bottom > b.Y + eps;

    /// <summary>The smallest rectangle holding both.</summary>
    public static RectF Union(RectF a, RectF b)
    {
        float left = MathF.Min(a.Left, b.Left), top = MathF.Min(a.Top, b.Top);
        return new RectF(left, top, MathF.Max(a.Right, b.Right) - left, MathF.Max(a.Bottom, b.Bottom) - top);
    }

    private static bool Closer(RectF candidate, RectF current, RectF from, bool horizontal)
        => horizontal
            ? MathF.Abs(candidate.Center.X - from.Center.X) < MathF.Abs(current.Center.X - from.Center.X)
            : MathF.Abs(candidate.Center.Y - from.Center.Y) < MathF.Abs(current.Center.Y - from.Center.Y);
}
