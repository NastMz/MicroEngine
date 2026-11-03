using System.Numerics;
using MicroEngine.World;

namespace MicroEngine.Physics.Tiles;

/// <summary>Ray tests of the tile backend: against a box (slabs) and through a tile grid (cell by cell).</summary>
internal static class TileRay
{
    /// <summary>
    /// Where the ray <c>from + dir × t</c>, t in [0, 1], enters <paramref name="box"/>. A ray starting strictly inside it,
    /// or only grazing an edge or a corner, does not hit. <paramref name="topOnly"/>: a one-way platform, entered only
    /// through its top face going down.
    /// </summary>
    public static bool Box(RectF box, Vector2 from, Vector2 dir, bool topOnly, bool skip, out float t, out Vector2 normal)
    {
        t = 0;
        normal = default;
        if (skip) return false;
        if (from.X > box.Left && from.X < box.Right && from.Y > box.Top && from.Y < box.Bottom) return false;
        float enter = float.NegativeInfinity, exit = float.PositiveInfinity;
        if (!Slab(from.X, dir.X, box.Left, box.Right, new Vector2(-1, 0), ref enter, ref exit, ref normal)) return false;
        if (!Slab(from.Y, dir.Y, box.Top, box.Bottom, new Vector2(0, -1), ref enter, ref exit, ref normal)) return false;
        if (!(enter < exit) || enter < 0 || enter > 1) return false;
        if (topOnly && normal != new Vector2(0, -1)) return false;
        t = enter;
        return true;
    }

    private static bool Slab(float p, float d, float min, float max, Vector2 minNormal, ref float enter, ref float exit, ref Vector2 normal)
    {
        if (d == 0) return p > min && p < max;
        float t1 = (min - p) / d, t2 = (max - p) / d;
        var n = d > 0 ? minNormal : -minNormal;
        if (t1 > t2) (t1, t2) = (t2, t1);
        if (t1 > enter)
        {
            enter = t1;
            normal = n;
        }
        exit = MathF.Min(exit, t2);
        return true;
    }

    /// <summary>
    /// Where the ray first enters a blocking cell of <paramref name="grid"/> (cells at <paramref name="origin"/>): solid
    /// cells from any side, one-way cells only through their top going down (unless <paramref name="passOneWay"/>).
    /// Solid cells the ray starts inside are not hit: it leaves them first.
    /// </summary>
    public static bool Cast(ICollisionGrid grid, Vector2 origin, Vector2 from, Vector2 dir, bool passOneWay, out float t, out Vector2 normal)
    {
        t = 0;
        normal = default;
        if (dir == Vector2.Zero) return false;
        int ts = grid.TileSize;
        var p = from - origin;
        int cx = (int)MathF.Floor(p.X / ts), cy = (int)MathF.Floor(p.Y / ts);
        // On a cell boundary moving towards lower coordinates, the ray starts in the cell on that side.
        if (dir.X < 0 && p.X == cx * ts) cx--;
        if (dir.Y < 0 && p.Y == cy * ts) cy--;
        int stepX = Math.Sign(dir.X), stepY = Math.Sign(dir.Y);
        float tMaxX = stepX > 0 ? ((cx + 1) * ts - p.X) / dir.X : stepX < 0 ? (cx * ts - p.X) / dir.X : float.PositiveInfinity;
        float tMaxY = stepY > 0 ? ((cy + 1) * ts - p.Y) / dir.Y : stepY < 0 ? (cy * ts - p.Y) / dir.Y : float.PositiveInfinity;
        float tDeltaX = stepX != 0 ? ts / MathF.Abs(dir.X) : float.PositiveInfinity;
        float tDeltaY = stepY != 0 ? ts / MathF.Abs(dir.Y) : float.PositiveInfinity;

        // The first cell: entered at t = 0 when the ray starts on the face it goes through; otherwise it starts inside.
        var first = grid.FlagsAt(cx, cy);
        Vector2 faceNormal = default;
        if (stepX > 0 && p.X == cx * ts) faceNormal = new Vector2(-1, 0);
        else if (stepX < 0 && p.X == (cx + 1) * ts) faceNormal = new Vector2(1, 0);
        if (stepY > 0 && p.Y == cy * ts) faceNormal = new Vector2(0, -1);
        else if (stepY < 0 && p.Y == (cy + 1) * ts) faceNormal = new Vector2(0, 1);
        bool inside = false;
        if (Blocks(first, faceNormal, passOneWay) && faceNormal != default)
        {
            normal = faceNormal;
            return true;
        }
        if ((first & TileFlags.Solid) != 0) inside = true;

        while (true)
        {
            Vector2 n;
            if (tMaxX < tMaxY)
            {
                t = tMaxX;
                cx += stepX;
                tMaxX += tDeltaX;
                n = new Vector2(-stepX, 0);
            }
            else
            {
                t = tMaxY;
                cy += stepY;
                tMaxY += tDeltaY;
                n = new Vector2(0, -stepY);
            }
            if (!(t <= 1)) return false;
            var flags = grid.FlagsAt(cx, cy);
            if (inside)
            {
                if ((flags & TileFlags.Solid) == 0) inside = false;
                continue;
            }
            if (Blocks(flags, n, passOneWay))
            {
                normal = n;
                return true;
            }
        }
    }

    private static bool Blocks(TileFlags flags, Vector2 normal, bool passOneWay) =>
        (flags & TileFlags.Solid) != 0 || (!passOneWay && (flags & TileFlags.OneWay) != 0 && normal == new Vector2(0, -1));
}
