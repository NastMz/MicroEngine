using MicroEngine.World;

namespace MicroEngine.Physics.Box2D;

/// <summary>
/// Turns a snapshot of tile flags into the geometry Box2D takes: the outline of the solid cells (squares and the two
/// slope triangles) as closed loops of grid points, and the one-way cells as horizontal runs. One loop bounds one
/// region of open space, with collinear edges merged, so a flat row of tiles is one segment and a body that slides
/// along it meets no seam.
/// </summary>
/// <remarks>
/// Points are in tiles, x to the right and y downward (row 0 is the top row). Every loop keeps the solid on its left
/// when seen with y up, which is the winding Box2D's chains need for their segments to face the open space. Where two
/// solid cells touch only at a corner, the loops follow the open space on each side of the corner instead of going
/// round each cell, so the corner is the inner corner it is for a body in that space.
/// </remarks>
internal static class TileOutline
{
    /// <summary>The loops (their points one after another in <c>Points</c>, the length of each in <c>Loops</c>) and the one-way runs (row, first column, one past the last).</summary>
    internal sealed class Result
    {
        public readonly List<(int X, int Y)> Points = [];
        public readonly List<int> Loops = [];
        public readonly List<(int Row, int From, int To)> OneWay = [];
    }

    private readonly record struct Edge(int Ax, int Ay, int Bx, int By);

    /// <summary>The corners of the solid part of a cell with <paramref name="flags"/>, counter-clockwise seen with y up; none when it is not solid geometry.</summary>
    private static int Corners(TileFlags flags, int x, int y, Span<(int X, int Y)> corners)
    {
        if ((flags & TileFlags.Solid) != 0)
        {
            corners[0] = (x, y + 1);
            corners[1] = (x + 1, y + 1);
            corners[2] = (x + 1, y);
            corners[3] = (x, y);
            return 4;
        }
        if ((flags & TileFlags.SlopeRight) != 0)
        {
            corners[0] = (x, y + 1);
            corners[1] = (x + 1, y + 1);
            corners[2] = (x + 1, y);
            return 3;
        }
        if ((flags & TileFlags.SlopeLeft) != 0)
        {
            corners[0] = (x, y + 1);
            corners[1] = (x + 1, y + 1);
            corners[2] = (x, y);
            return 3;
        }
        return 0;
    }

    /// <summary>Traces <paramref name="flags"/>, <paramref name="columns"/> × <paramref name="rows"/> cells in row order; cell (0, 0) is at point (<paramref name="originX"/>, <paramref name="originY"/>).</summary>
    public static Result Trace(ReadOnlySpan<TileFlags> flags, int columns, int rows, int originX, int originY)
    {
        var result = new Result();
        // Every edge of every solid cell; an edge whose reverse is another cell's is inside the solid.
        var edges = new HashSet<Edge>();
        var ordered = new List<Edge>();
        Span<(int X, int Y)> corners = stackalloc (int, int)[4];
        for (int y = 0; y < rows; y++)
        {
            int run = -1;
            for (int x = 0; x <= columns; x++)
            {
                var cell = x < columns ? flags[y * columns + x] : TileFlags.None;
                bool oneWay = (cell & TileFlags.OneWay) != 0 && (cell & (TileFlags.Solid | TileFlags.SlopeRight | TileFlags.SlopeLeft)) == 0;
                if (oneWay && run < 0) run = x;
                else if (!oneWay && run >= 0)
                {
                    result.OneWay.Add((y + originY, run + originX, x + originX));
                    run = -1;
                }
                if (x == columns) break;
                int n = Corners(cell, x + originX, y + originY, corners);
                for (int i = 0; i < n; i++)
                {
                    var (a, b) = (corners[i], corners[(i + 1) % n]);
                    var edge = new Edge(a.X, a.Y, b.X, b.Y);
                    edges.Add(edge);
                    ordered.Add(edge);
                }
            }
        }
        var outgoing = new Dictionary<(int, int), List<Edge>>();
        var boundary = new List<Edge>();
        foreach (var e in ordered)
        {
            if (edges.Contains(new Edge(e.Bx, e.By, e.Ax, e.Ay))) continue;
            boundary.Add(e);
            if (!outgoing.TryGetValue((e.Ax, e.Ay), out var list)) outgoing[(e.Ax, e.Ay)] = list = [];
            list.Add(e);
        }
        var used = new HashSet<Edge>();
        var loop = new List<(int X, int Y)>();
        foreach (var start in boundary)
        {
            if (used.Contains(start)) continue;
            loop.Clear();
            var edge = start;
            while (used.Add(edge))
            {
                loop.Add((edge.Ax, edge.Ay));
                edge = Next(edge, outgoing[(edge.Bx, edge.By)], used);
            }
            Simplify(loop);
            if (loop.Count < 3) continue;
            result.Points.AddRange(loop);
            result.Loops.Add(loop.Count);
        }
        return result;
    }

    /// <summary>The edge that follows <paramref name="edge"/> round the open space: of the unused ones that leave its end, the one that turns most to the right.</summary>
    private static Edge Next(Edge edge, List<Edge> candidates, HashSet<Edge> used)
    {
        // With y up: the direction of travel, and the turn to each candidate as an angle (negative to the right).
        int dx = edge.Bx - edge.Ax, dy = -(edge.By - edge.Ay);
        Edge best = edge;
        double bestTurn = double.MaxValue;
        foreach (var c in candidates)
        {
            if (used.Contains(c)) continue;
            int ex = c.Bx - c.Ax, ey = -(c.By - c.Ay);
            double turn = Math.Atan2(dx * ey - dy * ex, dx * ex + dy * ey);
            if (turn >= bestTurn) continue;
            bestTurn = turn;
            best = c;
        }
        return best;
    }

    /// <summary>Removes the points of a closed loop that lie on a straight line between their neighbours.</summary>
    private static void Simplify(List<(int X, int Y)> loop)
    {
        for (int i = 0; i < loop.Count && loop.Count >= 3;)
        {
            var a = loop[(i + loop.Count - 1) % loop.Count];
            var b = loop[i];
            var c = loop[(i + 1) % loop.Count];
            long cross = (long)(b.X - a.X) * (c.Y - b.Y) - (long)(b.Y - a.Y) * (c.X - b.X);
            long dot = (long)(b.X - a.X) * (c.X - b.X) + (long)(b.Y - a.Y) * (c.Y - b.Y);
            if (cross == 0 && dot > 0) loop.RemoveAt(i);
            else i++;
        }
    }
}
