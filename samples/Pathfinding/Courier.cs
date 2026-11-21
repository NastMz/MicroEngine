using System.Numerics;
using MicroEngine.Navigation;

namespace Pathfinding;

/// <summary>
/// A walker the game moves by its own rule, with nothing but the searches of the package: it keeps the route it was
/// given and steps along it. The scenes of this sample use agents instead; this is the same thing done by hand, for
/// a game that wants its own movement.
/// </summary>
public sealed class Courier(NavGrid grid, PathFinder finder, Vector2 position, float radius, float speed)
{
    public Vector2 Position { get; private set; } = position;
    /// <summary>The points still to walk through, in order.</summary>
    public List<Vector2> Route { get; } = [];
    /// <summary>The cells of the path the route was made from.</summary>
    public List<int> Cells { get; } = [];

    #region plan
    /// <summary>Gives the courier the route to <paramref name="target"/>, or to the nearest point it can reach.</summary>
    public PathStatus Plan(Vector2 target)
    {
        // The route: the points to walk through, straightened so a body of this size clears every corner.
        var status = grid.FindPath(finder, Position, target, Route, radius);
        // The cells under it, kept to know later whether a change of the map closed this path (grid.IsOpen).
        Cells.Clear();
        Cells.AddRange(finder.Path);
        return status;
    }
    #endregion

    #region follow
    /// <summary>Walks towards the next point of the route, and on to the one after it once there.</summary>
    public void Step(float dt)
    {
        if (Route.Count == 0) return;
        var to = Route[0] - Position;
        float reach = speed * dt;
        bool arrives = to.Length() <= reach;
        // Along the grid's walls, never into them.
        Position = grid.Slide(Position, arrives ? to : Vector2.Normalize(to) * reach, radius);
        if (arrives) Route.RemoveAt(0);
    }
    #endregion
}
