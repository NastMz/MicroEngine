using System.Numerics;
using MicroEngine.Physics.Tiles;

namespace MicroEngine.World;

/// <summary>
/// What happened during a <see cref="TileMotion.Move"/>. A body of the physics contract reports its move in
/// <see cref="MicroEngine.Physics.MoveResult"/> instead.
/// </summary>
/// <param name="Bounds">The box after the move.</param>
/// <param name="HitLeft">Movement to the left was stopped.</param>
/// <param name="HitRight">Movement to the right was stopped.</param>
/// <param name="HitCeiling">Upward movement was stopped.</param>
/// <param name="HitFloor">Downward movement was stopped (the body landed).</param>
/// <param name="Touched">Flags of every tile the box overlaps at the end (hazards, water...).</param>
public readonly record struct TileMoveResult(RectF Bounds, bool HitLeft, bool HitRight, bool HitCeiling, bool HitFloor, TileFlags Touched)
{
    /// <summary>Whether horizontal movement was stopped on either side.</summary>
    public bool HitWall => HitLeft || HitRight;
    /// <summary>The top-left of <see cref="Bounds"/>.</summary>
    public Vector2 Position => Bounds.Position;
}

/// <summary>
/// Moves an axis-aligned box through a tile grid (plus optional extra solid rectangles such as moving platforms or push
/// blocks). X is resolved before Y, each as a swept test in sub-steps no larger than half a tile: the box stops flush
/// against the first solid tile or rectangle in its way, however thin, so it never tunnels. One-way tiles only stop
/// bodies falling onto their top edge.
/// </summary>
/// <remarks>
/// A stateless convenience over one grid, for a game that moves its own boxes. A world of bodies, sensors, queries and
/// moving solids that push and carry bodies is the tile backend of the physics contract,
/// <see cref="MicroEngine.Physics.Tiles.TilePhysicsWorld2D"/>, built on the same swept test.
/// </remarks>
public static class TileMotion
{
    /// <summary>Moves <paramref name="box"/> by <paramref name="delta"/>, stopping flush against solid tiles, one-way tiles (from above) and <paramref name="solids"/>.</summary>
    /// <param name="box">The body's hitbox in world pixels.</param>
    /// <param name="delta">Movement in pixels for this step (velocity × dt).</param>
    /// <param name="grid">The tiles to collide with.</param>
    /// <param name="solids">
    /// Extra solid rectangles. One the box already overlaps before moving does not block it, so a body is never trapped
    /// inside it, except a rectangle that rose into a box moving down: when its top edge is inside the box's lower half,
    /// the box is first lifted onto it (if nothing solid is there), as a platform carries what stands on it.
    /// </param>
    /// <param name="ignoreOneWay">Passes through one-way tiles (dropping down through a platform).</param>
    /// <returns>The new box and what it hit. On a hit, movement along that axis stops for the rest of this step.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="delta"/> is not finite.</exception>
    public static TileMoveResult Move(RectF box, Vector2 delta, ICollisionGrid grid, IReadOnlyList<RectF>? solids = null, bool ignoreOneWay = false)
    {
        ArgumentNullException.ThrowIfNull(grid);
        if (!float.IsFinite(delta.X) || !float.IsFinite(delta.Y)) throw new ArgumentOutOfRangeException(nameof(delta));
        var gridObstacle = new GridObstacle(grid, Vector2.Zero, 0);
        ReadOnlySpan<GridObstacle> grids = new(in gridObstacle);
        int count = solids?.Count ?? 0;
        BoxObstacle[]? rented = count > 32 ? System.Buffers.ArrayPool<BoxObstacle>.Shared.Rent(count) : null;
        Span<BoxObstacle> boxes = rented is null ? stackalloc BoxObstacle[count] : rented.AsSpan(0, count);
        try
        {
            for (int i = 0; i < count; i++) boxes[i] = new BoxObstacle(solids![i], OneWay: false, i + 1);
            if (delta.Y > 0) box = LiftOntoRisenSolid(box, grids, boxes);

            bool left = false, right = false, ceiling = false, floor = false;
            float maxStep = grid.TileSize / 2f;

            int stepsX = Math.Max(1, (int)MathF.Ceiling(MathF.Abs(delta.X) / maxStep));
            for (int i = 0; i < stepsX; i++)
            {
                float dx = delta.X / stepsX;
                box = TileSweep.Sweep(box, dx, horizontal: true, grids, boxes, ignoreOneWay, compatible: true, out bool blocked, out _);
                if (!blocked) continue;
                if (dx > 0) right = true;
                else if (dx < 0) left = true;
                break;
            }

            int stepsY = Math.Max(1, (int)MathF.Ceiling(MathF.Abs(delta.Y) / maxStep));
            for (int i = 0; i < stepsY; i++)
            {
                float dy = delta.Y / stepsY;
                box = TileSweep.Sweep(box, dy, horizontal: false, grids, boxes, ignoreOneWay, compatible: true, out bool blocked, out _);
                if (!blocked) continue;
                if (dy > 0) floor = true;
                else if (dy < 0) ceiling = true;
                break;
            }
            return new TileMoveResult(box, left, right, ceiling, floor, TouchedFlags(box, grid));
        }
        finally
        {
            if (rented is not null) System.Buffers.ArrayPool<BoxObstacle>.Shared.Return(rented);
        }
    }

    /// <summary>True when a 1px probe directly below the box would collide (standing on ground or a platform).</summary>
    public static bool IsOnGround(RectF box, ICollisionGrid grid, IReadOnlyList<RectF>? solids = null, bool ignoreOneWay = false)
    {
        ArgumentNullException.ThrowIfNull(grid);
        var gridObstacle = new GridObstacle(grid, Vector2.Zero, 0);
        int count = solids?.Count ?? 0;
        BoxObstacle[]? rented = count > 32 ? System.Buffers.ArrayPool<BoxObstacle>.Shared.Rent(count) : null;
        Span<BoxObstacle> boxes = rented is null ? stackalloc BoxObstacle[count] : rented.AsSpan(0, count);
        try
        {
            for (int i = 0; i < count; i++) boxes[i] = new BoxObstacle(solids![i], OneWay: false, i + 1);
            return TileSweep.Overlapping(box.Offset(0, 1), box, horizontal: false, new ReadOnlySpan<GridObstacle>(in gridObstacle), boxes, ignoreOneWay, ignoreStartOverlaps: false, out _, out _);
        }
        finally
        {
            if (rented is not null) System.Buffers.ArrayPool<BoxObstacle>.Shared.Return(rented);
        }
    }

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

    /// <summary>Whether the tile under <paramref name="point"/> (world pixels) is solid. One-way tiles do not count.</summary>
    public static bool IsSolidAt(Vector2 point, ICollisionGrid grid)
        => (grid.FlagsAt((int)MathF.Floor(point.X / grid.TileSize), (int)MathF.Floor(point.Y / grid.TileSize)) & TileFlags.Solid) != 0;

    /// <summary>
    /// A solid that rose into a box (its top edge inside the box's lower half) lifts the box onto its top, when the lifted
    /// box overlaps no solid tile and no other solid; the highest such solid wins.
    /// </summary>
    private static RectF LiftOntoRisenSolid(RectF box, ReadOnlySpan<GridObstacle> grids, ReadOnlySpan<BoxObstacle> boxes)
    {
        float half = box.Top + box.Height / 2;
        int best = -1;
        for (int i = 0; i < boxes.Length; i++)
        {
            var s = boxes[i].Rect;
            if (!box.Intersects(s) || s.Top < half || s.Top <= box.Top) continue;
            if (best < 0 || s.Top < boxes[best].Rect.Top) best = i;
        }
        if (best < 0) return box;
        float top = boxes[best].Rect.Top;
        var lifted = box with { Y = top - box.Height };
        while (lifted.Bottom > top) lifted = lifted with { Y = MathF.BitDecrement(lifted.Y) };
        foreach (var other in boxes)
            if (lifted.Intersects(other.Rect)) return box;
        if (TileSweep.Overlapping(lifted, lifted, horizontal: true, grids, [], ignoreOneWay: true, ignoreStartOverlaps: false, out _, out _)) return box;
        return lifted;
    }
}
