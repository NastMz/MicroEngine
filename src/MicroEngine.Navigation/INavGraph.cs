namespace MicroEngine.Navigation;

/// <summary>A step of a graph: the node it leads to, what it costs, and a number the game gives its own meaning.</summary>
/// <param name="To">The node the step arrives at.</param>
/// <param name="Cost">What the step costs; zero or more.</param>
/// <param name="Tag">Free for the game: how the step is crossed (a walk, a jump, a ladder, a door). The search never reads it.</param>
public readonly record struct NavEdge(int To, float Cost, int Tag = 0);

/// <summary>
/// Space a <see cref="PathFinder"/> can search: nodes numbered from 0, the steps that leave each one, and an estimate
/// of the cost between two of them. <see cref="NavGrid"/> (cells) and <see cref="NavGraph"/> (waypoints joined by hand)
/// implement it; a game implements it for a space of its own (hexagons, rooms, a 3D lattice).
/// </summary>
public interface INavGraph
{
    /// <summary>How many nodes the graph has; nodes are the numbers from 0 to this, excluded.</summary>
    int NodeCount { get; }

    /// <summary>
    /// Adds to <paramref name="edges"/> the steps that leave <paramref name="node"/>, always in the same order for the
    /// same graph (the order decides between paths of equal cost).
    /// </summary>
    void GetEdges(int node, List<NavEdge> edges);

    /// <summary>
    /// An estimate of the cost from <paramref name="from"/> to <paramref name="to"/> that never exceeds the true cost
    /// (0 is always valid and makes the search look in every direction). The closer to the true cost, the fewer nodes
    /// a search visits.
    /// </summary>
    float Estimate(int from, int to);
}

/// <summary>How a search ended.</summary>
public enum PathStatus
{
    /// <summary>The path reaches the goal and no cheaper one exists.</summary>
    Found,
    /// <summary>No path reaches the goal. The path leads to the reachable node the estimate puts nearest to it.</summary>
    Unreachable,
    /// <summary>The search stopped at <see cref="PathFinder.MaxExpansions"/> before knowing. The path leads to the nearest node found so far.</summary>
    Incomplete,
    /// <summary>The search has not ended: <see cref="PathFinder.Continue"/> has more nodes to look at, or an agent's route is still being searched. There is no path yet.</summary>
    Searching,
}
