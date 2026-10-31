using System.Numerics;

namespace MicroEngine.World;

/// <summary>What happened during a <see cref="TileMotion.Move"/>.</summary>
public readonly record struct MoveResult(RectF Bounds, bool HitLeft, bool HitRight, bool HitCeiling, bool HitFloor, TileFlags Touched)
{
    public bool HitWall => HitLeft || HitRight;
    public Vector2 Position => Bounds.Position;
}

/// <summary>
/// Moves an axis-aligned box through a tile grid (plus optional extra solid rectangles such as
/// moving platforms or push blocks). X is resolved before Y, in sub-steps no larger than half a
/// tile, so fast bodies never tunnel. One-way tiles only stop bodies falling onto their top edge.
/// </summary>
public static class TileMotion
{
    public static MoveResult Move(RectF box, Vector2 delta, ICollisionGrid grid, IReadOnlyList<RectF>? solids = null, bool ignoreOneWay = false)
    {
        ArgumentNullException.ThrowIfNull(grid);
        if (!float.IsFinite(delta.X) || !float.IsFinite(delta.Y)) throw new ArgumentOutOfRangeException(nameof(delta));
        bool left = false, right = false, ceiling = false, floor = false;
        float maxStep = grid.TileSize / 2f;

        int stepsX = Math.Max(1, (int)MathF.Ceiling(MathF.Abs(delta.X) / maxStep));
        for (int i = 0; i < stepsX; i++)
        {
            float dx = delta.X / stepsX;
            var moved = box.Offset(dx, 0);
            if (Blocked(moved, grid, solids, out var block, horizontal: true, previous: box, ignoreOneWay))
            {
                if (dx > 0) { box = box with { X = block.Left - box.Width }; right = true; }
                else if (dx < 0) { box = box with { X = block.Right }; left = true; }
                break;
            }
            box = moved;
        }

        int stepsY = Math.Max(1, (int)MathF.Ceiling(MathF.Abs(delta.Y) / maxStep));
        for (int i = 0; i < stepsY; i++)
        {
            float dy = delta.Y / stepsY;
            var moved = box.Offset(0, dy);
            if (Blocked(moved, grid, solids, out var block, horizontal: false, previous: box, ignoreOneWay))
            {
                if (dy > 0) { box = box with { Y = block.Top - box.Height }; floor = true; }
                else if (dy < 0) { box = box with { Y = block.Bottom }; ceiling = true; }
                break;
            }
            box = moved;
        }
        return new MoveResult(box, left, right, ceiling, floor, TouchedFlags(box, grid));
    }

    /// <summary>True when a 1px probe directly below the box would collide (standing on ground or a platform).</summary>
    public static bool IsOnGround(RectF box, ICollisionGrid grid, IReadOnlyList<RectF>? solids = null, bool ignoreOneWay = false)
        => Blocked(box.Offset(0, 1), grid, solids, out _, horizontal: false, previous: box, ignoreOneWay);

    /// <summary>Union of flags of every tile overlapping the box.</summary>
    public static TileFlags TouchedFlags(RectF box, ICollisionGrid grid)
    {
        int ts = grid.TileSize;
        int x0 = (int)MathF.Floor(box.Left / ts), x1 = (int)MathF.Floor((box.Right - 0.001f) / ts);
        int y0 = (int)MathF.Floor(box.Top / ts), y1 = (int)MathF.Floor((box.Bottom - 0.001f) / ts);
        var flags = TileFlags.None;
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
            flags |= grid.FlagsAt(x, y);
        return flags;
    }

    public static bool IsSolidAt(Vector2 point, ICollisionGrid grid)
        => (grid.FlagsAt((int)MathF.Floor(point.X / grid.TileSize), (int)MathF.Floor(point.Y / grid.TileSize)) & TileFlags.Solid) != 0;

    private static bool Blocked(RectF box, ICollisionGrid grid, IReadOnlyList<RectF>? solids, out RectF block, bool horizontal, RectF previous, bool ignoreOneWay)
    {
        int ts = grid.TileSize;
        int x0 = (int)MathF.Floor(box.Left / ts), x1 = (int)MathF.Floor((box.Right - 0.001f) / ts);
        int y0 = (int)MathF.Floor(box.Top / ts), y1 = (int)MathF.Floor((box.Bottom - 0.001f) / ts);
        bool found = false;
        block = default;
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            var flags = grid.FlagsAt(x, y);
            var tile = new RectF(x * ts, y * ts, ts, ts);
            bool solid = (flags & TileFlags.Solid) != 0;
            bool oneWay = !solid && !ignoreOneWay && !horizontal && (flags & TileFlags.OneWay) != 0
                && previous.Bottom <= tile.Top + 0.01f && box.Bottom > tile.Top;
            if (!solid && !oneWay) continue;
            if (!found || Closer(tile, block, previous, horizontal)) block = tile;
            found = true;
        }
        if (solids is not null)
            foreach (var s in solids)
            {
                if (!box.Intersects(s) || previous.Intersects(s)) continue;
                if (!found || Closer(s, block, previous, horizontal)) block = s;
                found = true;
            }
        return found;
    }

    private static bool Closer(RectF candidate, RectF current, RectF from, bool horizontal)
        => horizontal
            ? MathF.Abs(candidate.Center.X - from.Center.X) < MathF.Abs(current.Center.X - from.Center.X)
            : MathF.Abs(candidate.Center.Y - from.Center.Y) < MathF.Abs(current.Center.Y - from.Center.Y);
}
