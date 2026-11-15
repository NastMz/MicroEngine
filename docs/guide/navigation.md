# Navigation

The optional `MicroEngine.Navigation` package finds paths: the cheapest way from one place to another through a
level with walls, doors, slow ground or platforms. It answers "which way", never "where to" or "why": what an agent
wants, when it changes its mind and how it moves are your game's rules.

```xml
<PackageReference Include="MicroEngine.Navigation" Version="0.6.0-alpha.1" />
```

| To do this | Use |
| --- | --- |
| Find a route through a tile map or any level sampled on a grid | `NavGrid` and `PathFinder` |
| Send an agent to a point in the world and get the points to walk through | `NavGrid.FindPath` |
| Close a door, drop a crate, break a bridge while agents are walking | `SetWalkable`, `SetBlocked`, `Version`, `IsOpen` |
| Send many agents to the same goal (a horde after the player) | `DistanceField` |
| Join places by hand: roads, rooms, platforms with jumps and drops | `NavGraph` |
| Search a space of your own: hexagons, a 3D lattice, a world map | `INavGraph` |

The package is managed code with no dependency besides the engine, so it runs on every platform the engine runs on.

## A grid from the level

A `NavGrid` is the walkable space as square cells: each one walkable or blocked, each with a cost to enter it. For a
tile map it has one cell per tile. `NavGrid.FromTiles` reads a `TileGrid` (the one `TileMotion` and the tile physics
world collide with, or the one a [map from Tiled](../manual/world.md#maps-from-tiled) gives): tiles with the
`Solid` flag are blocked. Pass other flags to block more: `NavGrid.FromTiles(tiles, TileFlags.Solid | TileFlags.Hazard)`
keeps agents out of spikes.

<!-- code: samples/Pathfinding/RoomScene.cs#grid -->
```csharp
// The tile map the game collides with: walls are solid; the door is solid only while closed.
private readonly TileGrid _tiles = new(
    TileLayer.Parse(Rows, new Dictionary<char, int> { ['#'] = Wall, ['D'] = Door, ['~'] = Mud }),
    Tile,
    new Dictionary<int, TileFlags> { [Wall] = TileFlags.Solid });
private NavGrid _nav = null!;
private readonly PathFinder _finder = new();      // one for the whole game: its memory is reused by every search
private DistanceField _field = null!;

private void BuildGrid()
{
    // The grid the agents search: one cell per tile, solid tiles blocked. Mud can be walked, slowly, so its cells
    // cost more and a route crosses it only when going around takes longer.
    _nav = NavGrid.FromTiles(_tiles);
    for (int y = 0; y < _nav.Height; y++)
        for (int x = 0; x < _nav.Width; x++)
            if (_tiles.Layer[x, y] == Mud) _nav.SetCost(x, y, MudCost);
    _field = new DistanceField(_nav);
}
```

A level made of physics shapes instead of tiles is sampled the same way:
`NavGrid.FromPhysics(world, bounds, cellSize, filter)` asks the physics world which cells a shape overlaps and blocks
them. Give it the `QueryFilter` your agents move with, so walls block and pickups, sensors and other agents do not. A grid can also be filled by hand: `new NavGrid(width, height, cellSize)` and `SetWalkable(x, y, false)`.

The grid is a copy of the level, in your game's hands. It never follows the tile map or the physics world by itself;
[you tell it](#obstacles-that-change) when the level changes.

### Costs

`SetCost(x, y, cost)` makes a cell more expensive to enter than open ground (1): 3 for mud walked at a third of the
speed, 10 for a corridor a guard watches. Routes cross costly cells only when going around costs more. A step costs
its length (1, or 1.41 diagonally) times the cost of the cell entered.

### Diagonals

`grid.Diagonals` chooses how agents step between cells:

| Value | Steps | For |
| --- | --- | --- |
| `NoCornerCutting` (default) | Eight directions, but never diagonally past the corner of a blocked cell | Agents with a body: they do not clip wall corners |
| `Never` | Up, down, left, right | Games that move cell by cell |
| `Always` | Eight directions, corners included | Agents that are points |

## Finding a route

A `PathFinder` does the searching. Create one for the whole game: it keeps the memory of its searches, so searching
again allocates nothing. `grid.FindPath(finder, from, to, route, radius)` fills `route` with the world points to
walk through, in order, from the agent's position to the target.

<!-- code: samples/Pathfinding/RoomScene.cs#plan -->
```csharp
/// <summary>Gives <paramref name="walker"/> the route to <paramref name="target"/>, or to the nearest point it can reach.</summary>
public PathStatus Plan(Walker walker, Vector2 target)
{
    // The route: the points to walk through, straightened so a walker of this size clears every corner.
    var status = _nav.FindPath(_finder, walker.Center, target, walker.Route, Radius);
    // The cells under it, kept to know later whether a change of the map closed this path.
    walker.Cells.Clear();
    walker.Cells.AddRange(_finder.Path);
    walker.GridVersion = _nav.Version;
    walker.Searches++;
    return status;
}
```

The route is straightened: instead of one point per cell, it turns only where a wall makes it turn, and each leg is
a straight line the agent can walk without touching a wall. `radius` is half the side of the agent's box: legs keep
that distance from blocked cells. The last point is the exact target, not the centre of its cell.

`FindPath` returns how the search ended:

| Status | Meaning | The route leads to |
| --- | --- | --- |
| `Found` | The target can be reached, and no cheaper route exists | The target |
| `Unreachable` | The target is in a wall, cut off, or outside the grid | The nearest cell that can be reached |
| `Incomplete` | The search stopped at `finder.MaxExpansions` | The nearest cell found so far |

So a click on a wall still moves the unit as close as it can get. An agent standing in a blocked cell (a door closed
on it) can still walk out.

Moving along the route is your game's code, through the collision it already uses:

<!-- code: samples/Pathfinding/RoomScene.cs#follow -->
```csharp
// Walking is the game's: towards the next point of the route, and on to the one after it once there.
private void Follow(Walker walker, float dt)
{
    if (walker.Route.Count > 0 && Step(walker, walker.Route[0], dt)) walker.Route.RemoveAt(0);
}

// One step towards a point at the walker's speed (a third of it in mud), through the same tile collision as
// everything else that moves. Returns whether the point was reached.
private bool Step(Walker walker, Vector2 target, float dt)
{
    var cell = _nav.CellAt(walker.Center);
    float reach = walker.Speed * dt / _nav.CostAt(cell.X, cell.Y);
    var to = target - walker.Center;
    bool arrives = to.Length() <= reach;
    walker.Bounds = TileMotion.Move(walker.Bounds, arrives ? to : Vector2.Normalize(to) * reach, _tiles).Bounds;
    return arrives;
}
```

### The cells under a route

After a search, `finder.Path` holds the cells of the path as nodes (node `y * Width + x` is cell (x, y)), from the
start to the end, and `finder.Cost` its cost. The next search replaces them, so copy what you keep.
`finder.Find(grid, start, goal)` searches without making a route, for games that move cell by cell;
`grid.NodeAt(point)`, `grid.CellOf(node)` and `grid.CenterOf(node)` convert between world points, cells and nodes.

### When to search

Searching every frame is never needed. Search when the answer can change: the target entered another cell, or the
level changed.

<!-- code: samples/Pathfinding/RoomScene.cs#chase -->
```csharp
// The chaser wants to be where the player is. It searches when the player enters another cell or the map
// changes, never every frame.
if (playerCell != _chasedCell || Chaser.GridVersion != _nav.Version)
{
    _chasedCell = playerCell;
    Plan(Chaser, Player.Center);
}
if (Vector2.Distance(Chaser.Center, Player.Center) > CatchDistance) Follow(Chaser, dt);
```

## Obstacles that change

When a door closes or a crate lands, change the grid: `SetWalkable(x, y, false)` for one cell,
`SetBlocked(area, true)` for every cell a rectangle overlaps, or `ReadTiles(tiles)` / `ReadPhysics(world, filter)`
to read the whole level again.

<!-- code: samples/Pathfinding/RoomScene.cs#door -->
```csharp
/// <summary>Closes or opens the door; it stays open while somebody stands in it.</summary>
public bool SetDoor(bool closed)
{
    if (closed && (Player.Bounds.Intersects(DoorBounds) || Chaser.Bounds.Intersects(DoorBounds) || Patrol.Bounds.Intersects(DoorBounds)
        || _swarm.Exists(bug => bug.Bounds.Intersects(DoorBounds)))) return false;
    DoorClosed = closed;
    // The tile map changes for collision, and the grid reads it again: its version changes, which every walker sees.
    _tiles.SetFlags(Door, closed ? TileFlags.Solid : TileFlags.None);
    _nav.ReadTiles(_tiles);
    return true;
}
```

Every change gives the grid a new `Version`. An agent that remembers the version it last saw knows when to look
again, and `grid.IsOpen(cells)` tells whether the cells of its path can still be walked, so only the agents whose
path closed search again:

<!-- code: samples/Pathfinding/RoomScene.cs#replan -->
```csharp
// The patrol walks from post to post. When the map changes it looks at the cells of its path and searches
// again only if one of them closed: an open door elsewhere costs it nothing.
if (Patrol.Route.Count == 0)
{
    _post = 1 - _post;
    Plan(Patrol, Posts[_post]);
}
else if (Patrol.GridVersion != _nav.Version)
{
    Patrol.GridVersion = _nav.Version;
    if (!_nav.IsOpen(Patrol.Cells)) Plan(Patrol, Posts[_post]);
}
Follow(Patrol, dt);
```

Moving agents are not obstacles in the grid: block cells for things that stay (doors, crates, buildings), and keep
agents apart with your own rules.

## Many agents, one goal

When many agents want the same place, one `DistanceField` replaces all their searches. `field.Compute(goal)` gives
every cell the cost of its cheapest route to the goal; each agent then asks `field.Next(cell)` for the cell to step
to. Compute it again when the goal moves to another cell or the grid's version changes.

<!-- code: samples/Pathfinding/RoomScene.cs#field -->
```csharp
// The swarm all want the player: one field holds the cost from every cell to the player's cell, and each bug
// reads the cell to step to. It is computed again when the player changes cell or the map changes.
if (playerCell != _fieldGoal || _field.Version != _nav.Version)
{
    _fieldGoal = playerCell;
    _field.Compute(playerCell);
}
foreach (var bug in _swarm)
{
    int next = _field.Next(_nav.NodeAt(bug.Center));
    if (next >= 0 && Vector2.Distance(bug.Center, Player.Center) > CatchDistance) Step(bug, _nav.CenterOf(next), dt);
}
```

`Compute` also takes several goals (every agent heads for the nearest: the exits of a room) and a distance limit
that keeps the work near the goal on large maps. `field.DistanceAt(cell)` is the cost itself: use it to pick the
agent closest to something through the walls, not in a straight line.

## Waypoints, platforms and links

A grid suits levels where agents walk freely. Where movement follows fixed connections, build a `NavGraph`: nodes at
positions, joined by hand.

<!-- code: samples/Pathfinding/Platforms.cs#links -->
```csharp
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
```

A connection has a cost (the distance between its ends unless you give one), a direction and a tag, a number that
means whatever your game needs. That is how a platformer describes its level to the search: nodes on platforms, walks
both ways, jumps and drops one way, each at the cost of its effort. Whether a jump is possible is your game's
physics, so the links are yours to place.

A path over the graph is a list of nodes; `TryGetEdge` gives back the connection of each step, with its tag:

<!-- code: samples/Pathfinding/Platforms.cs#steps -->
```csharp
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
```

`graph.Nearest(point)` gives the node closest to a position;
`SetEnabled(node, false)` closes a place (a locked room, a platform that fell) and `Disconnect` removes a
connection; both change `graph.Version`.

## A space of your own

`PathFinder` searches anything that implements `INavGraph`: how many nodes there are, the steps that leave a node,
and an estimate of the cost between two nodes that never exceeds the true cost (0 is always valid). A hexagonal
board, the rooms of a dungeon, a galaxy map or a 3D lattice of voxels are searched by the same finder.

<!-- code: samples/Pathfinding/Platforms.cs#custom -->
```csharp
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
```

## Results you can rely on

- **The cheapest path.** `Found` means no cheaper path exists.
- **The same path every time.** The same question on the same grid or graph gives the same path, on every platform
  and in every run: among paths of equal cost the choice is fixed.
- **No garbage.** Once a finder or a field has seen a map, searching and computing again allocate nothing.

## Costs and limits

- On a 128 × 128 grid a search across the map takes a fraction of a millisecond, and a whole distance field between
  one and two. On a 1000 × 1000 grid a long path takes about a frame, and a target that cannot be reached makes the
  search look at every cell the agent can reach, several frames of work: set `finder.MaxExpansions` to bound it
  (the route then leads to the nearest cell found), and give fields a distance limit.
- A finder keeps 24 bytes per cell of the largest map it searched, a grid 5 and a field 8.
- One finder or field serves one search at a time: use one per thread.
- During the search an agent is at most one cell wide; `radius` then keeps the straightened route clear of walls.
  For agents wider than a cell, search a second grid where the cells next to walls are blocked too.
- There are no navigation meshes, no steering or crowd avoidance and no automatic links between platforms.

## Related

- The `Pathfinding` sample (`samples/Pathfinding`): a unit that walks to the clicked point, a chaser, a swarm on one
  field and a patrol that goes around when a door closes.
- [Tile maps and collision](../manual/world.md): the tile grids and physics worlds a `NavGrid` reads.
- API: [`NavGrid`](../api/MicroEngine.Navigation.NavGrid.md), [`PathFinder`](../api/MicroEngine.Navigation.PathFinder.md),
  [`DistanceField`](../api/MicroEngine.Navigation.DistanceField.md), [`NavGraph`](../api/MicroEngine.Navigation.NavGraph.md),
  [`INavGraph`](../api/MicroEngine.Navigation.INavGraph.md).
