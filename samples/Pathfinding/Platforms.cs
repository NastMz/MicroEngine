using System.Numerics;
using MicroEngine.Navigation;

namespace Pathfinding;

/// <summary>
/// A side-view level described to the search by hand: nodes on the platforms, joined by walks, a jump and drops. The
/// room scene does not use it; it shows the other two kinds of space the finder searches.
/// </summary>
public sealed class Platforms
{
    #region links
    public const int Walk = 0, Jump = 1, Drop = 2;

    public NavGraph Graph { get; } = new();
    public int GroundLeft { get; }
    public int GroundMiddle { get; }
    public int GroundRight { get; }
    public int LedgeLeft { get; }
    public int LedgeRight { get; }

    public Platforms()
    {
        // The ground, and a ledge above its right half.
        GroundLeft = Graph.AddNode(new Vector2(0, 100));
        GroundMiddle = Graph.AddNode(new Vector2(50, 100));
        GroundRight = Graph.AddNode(new Vector2(100, 100));
        LedgeLeft = Graph.AddNode(new Vector2(50, 60));
        LedgeRight = Graph.AddNode(new Vector2(100, 60));
        Graph.Connect(GroundLeft, GroundMiddle, tag: Walk);                              // both ways, at the distance between them
        Graph.Connect(GroundMiddle, GroundRight, tag: Walk);
        Graph.Connect(LedgeLeft, LedgeRight, tag: Walk);
        Graph.Connect(GroundMiddle, LedgeLeft, cost: 60, tag: Jump, bothWays: false);    // up: only by jumping, under the ledge's left end
        Graph.Connect(LedgeLeft, GroundMiddle, cost: 10, tag: Drop, bothWays: false);    // down: step off either end
        Graph.Connect(LedgeRight, GroundRight, cost: 10, tag: Drop, bothWays: false);
    }
    #endregion

    #region steps
    /// <summary>The way from one node to another: each node to go to and how to cross to it; empty when there is no way.</summary>
    public List<(int Node, int How)> Steps(PathFinder finder, int from, int to)
    {
        var steps = new List<(int Node, int How)>();
        if (finder.Find(Graph, from, to) != PathStatus.Found) return steps;
        for (int i = 1; i < finder.Path.Count; i++)
        {
            // The connection's tag says how this step is crossed; crossing it is the game's movement code.
            Graph.TryGetEdge(finder.Path[i - 1], finder.Path[i], out var link);
            steps.Add((link.To, link.Tag));
        }
        return steps;
    }
    #endregion
}

#region custom
/// <summary>A board of hexagons (odd rows shifted half a hexagon to the right) searched by the same finder.</summary>
public sealed class HexBoard(int width, int height) : INavGraph
{
    private readonly bool[] _blocked = new bool[width * height];

    public int NodeCount => width * height;
    public int NodeOf(int column, int row) => row * width + column;
    public void Block(int column, int row) => _blocked[NodeOf(column, row)] = true;

    // The steps out of a hexagon: to each of its six neighbours on the board that is not blocked, one unit each.
    public void GetEdges(int node, List<NavEdge> edges)
    {
        int column = node % width, row = node / width, shift = row & 1;
        Span<(int Column, int Row)> around =
        [
            (column - 1, row), (column + 1, row),
            (column - 1 + shift, row - 1), (column + shift, row - 1),
            (column - 1 + shift, row + 1), (column + shift, row + 1),
        ];
        foreach (var (c, r) in around)
            if (c >= 0 && r >= 0 && c < width && r < height && !_blocked[NodeOf(c, r)]) edges.Add(new NavEdge(NodeOf(c, r), 1));
    }

    // The distance in hexagons with nothing in the way: never more than the true cost.
    public float Estimate(int from, int to)
    {
        var (a, b) = (Axial(from), Axial(to));
        return (Math.Abs(a.Q - b.Q) + Math.Abs(a.R - b.R) + Math.Abs(a.Q + a.R - b.Q - b.R)) / 2f;
    }

    private (int Q, int R) Axial(int node)
    {
        int row = node / width;
        return (node % width - (row - (row & 1)) / 2, row);
    }
}
#endregion
