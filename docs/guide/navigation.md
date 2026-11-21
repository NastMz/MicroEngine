# Navigation

The optional `MicroEngine.Navigation` package moves things through a level with walls, doors, slow ground or
platforms: agents that walk to where they are sent, keep apart from each other and go around what is built in their
way, on top of searches that find the cheapest way from one place to another. It answers "which way" and "how to get
there", never "where to" or "why": what an agent wants and when it changes its mind are your game's rules.

```xml
<PackageReference Include="MicroEngine.Navigation" Version="0.6.0-alpha.1" />
```

| To do this | Use |
| --- | --- |
| Describe where things can walk, from a tile map or any level sampled on a grid | `NavGrid` |
| Send a unit, an enemy or a whole group to a point and have them walk there | `NavAgents`, `agent.MoveTo` |
| Close a door, drop a crate, build a tower while agents are walking | `SetWalkable`, `SetBlocked`; agents go around |
| Refuse a tower that would wall the creeps in | `agents.CanBlock`, `NavRegions.StaysConnected` |
| Know whether a place can be reached, and where to go when it cannot | `NavRegions` |
| Send many agents to the same goal (a horde after the player) | `DistanceField`, `agent.Follow` |
| Move things your own way and only ask for the route | `NavGrid.FindPath`, `PathFinder`, `Steering` |
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
// The tile map the game draws and collides with: walls are solid; the door is solid only while closed.
private readonly TileGrid _tiles = new(
    TileLayer.Parse(Rows, new Dictionary<char, int> { ['#'] = Wall, ['D'] = Door, ['~'] = Mud }),
    Tile,
    new Dictionary<int, TileFlags> { [Wall] = TileFlags.Solid });
private NavGrid _nav = null!;
private DistanceField _field = null!;

private void BuildGrid()
{
    // The grid the agents walk: one cell per tile, solid tiles blocked. Mud can be walked, slowly, so its cells
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

## Agents

`NavAgents` is everything between "I have a grid" and "the unit is there". It is a
[scene system](../manual/scenes.md#scene-systems): add it to the scene and it is stepped with it, before the scene's
`Update`. Each agent is a body with a radius and a speed that takes orders.

<!-- code: samples/Pathfinding/RoomScene.cs#agents -->
```csharp
private NavAgents _agents = null!;

public override void Enter()
{
    BuildGrid();
    // The agents of this scene: the system is stepped with the scene, before its Update.
    _agents = Systems.Add(new NavAgents(_nav));
    // A body's radius and speed; mud slows these down as much as it costs.
    var walker = new NavAgentSettings(Radius, speed: 80) { SlowedByCost = true };
    Player = _agents.Add(At('P'), walker);
    // The chaser has arrived when it is within reach of the point it was sent to.
    Chaser = _agents.Add(At('C'), walker with { Speed = 50, ArrivalTolerance = CatchDistance });
    Posts = [At('A'), At('B')];
    Patrol = _agents.Add(Posts[0], walker with { Speed = 40 });
    foreach (var (x, y) in TileLayer.Find(Rows, 'b'))
        _swarm.Add(_agents.Add(_nav.CenterOf(_nav.NodeOf(x, y)), new NavAgentSettings(radius: 3, speed: 45) { SlowedByCost = true }));
    var (doorX, doorY) = TileLayer.Find(Rows, 'D').First();
    DoorBounds = new RectF(doorX * Tile, doorY * Tile, Tile, Tile);
    DoorCell = _nav.NodeOf(doorX, doorY);
}
```

An order is one call, and the scene says only what each agent wants:

<!-- code: samples/Pathfinding/RoomScene.cs#orders -->
```csharp
// A click sends the player's unit: it finds its way, walks it and stops on the point.
var pointer = Input.Pointer;
if (pointer.Primary && !_pointerWasDown) Player.MoveTo(pointer.Position);
_pointerWasDown = pointer.Primary;

// The chaser wants to be where the player is. Ordering it every step is free: a route is searched only when
// the player enters another cell.
Chaser.MoveTo(Player.Position);

// The patrol walks from post to post: when it has arrived, it is sent to the other one.
if (Patrol.Status is NavAgentStatus.Idle or NavAgentStatus.Arrived)
{
    _post = 1 - _post;
    Patrol.MoveTo(Posts[_post]);
}
```

From there the system does the rest, every step:

- **It finds the route** and walks it, turning only where a wall makes it turn, and cutting a corner as soon as the
  way past it is clear.
- **It arrives.** Alone, an agent stops on the point it was sent to. `ArrivalTolerance` makes "near enough" count
  (the chaser above stops within reach of the player) and `SlowingDistance` makes it slow down before it stops.
- **It searches again when the level changes** and its way is closed, and only then. An agent that could not reach
  its point tries again when a way opens.
- **It keeps apart from the others.** Bodies steer around each other, never overlap for long and never enter a wall.
- **It tells how it is doing**: `agent.Status` (`Idle`, `Planning`, `Moving`, `Arrived`), `agent.Path` (`Found`,
  or `Unreachable` when it is going as near as it gets), `agent.Route` (the points left, to draw them) and
  `agent.IsBlocked` (it has an order and made no way for a second: somebody stands in the only passage).

Draw an agent where it is: `agent.Position` and `agent.Radius`. `agent.Stop()` ends its order, `agents.Remove(agent)`
takes it out when the thing it stands for is gone, and `agent.Tag` holds whatever your game wants to find again
from the agent (the unit, the enemy).

| Setting | Default | What it does |
| --- | --- | --- |
| `Radius` | (required) | The body: a circle for the other agents, a square for walls |
| `Speed` | (required) | Units per second |
| `Acceleration` | at once | How fast its velocity changes |
| `ArrivalTolerance` | 0 | How near its destination counts as there |
| `SlowingDistance` | 0 | Where it starts to slow down before its destination |
| `Separation`, `Avoidance` | 1 | How strongly it keeps apart, and turns aside from a body it would run into; 0 for none |
| `Weight` | 1 | How hard it is to push; a body that stands holds its ground eight times as hard |
| `Collides` | true | False for what passes through bodies: a flyer, a ghost |
| `SlowedByCost` | false | Costly ground slows it: a cell that costs 3 is walked at a third of the speed |
| `MovedByGame` | false | Your code moves the body; the system only steers (see below) |

Settings are a record: `agent.Settings = agent.Settings with { Speed = 20 }` changes one while the agent walks.

### A group

A group is its units, each with the same order:

<!-- code: samples/Pathfinding/SkirmishScene.cs#order -->
```csharp
/// <summary>Sends the whole group to a point (what a click does).</summary>
public void Order(Vector2 point)
{
    Rally = point;
    // One order each. Their searches share the step's budget, and the bodies sort themselves out around the point.
    foreach (var unit in _units) unit.MoveTo(point);
}
```

They share the passages, and at the point each takes a place of its own: the first stands on it, and each of the
others, when it reaches those that arrived, takes the free place nearest the point on a honeycomb of places one body
apart, and walks to it. The group gathers around the point instead of stacking on one cell or pushing for ever; an
agent that cannot get to its place stops where it is held. Among agents that go the same way the one with less of
its way left goes first and the others wait behind it, so a crowd queues at a door instead of wedging in it.
Formations and what a unit does when it is blocked are your game's rules.

Searches are spread over steps. One step looks at `agents.SearchBudget` nodes at most (4096 by default) for all the
agents together; when a hundred units are ordered at once on a large map, most have their route in the same step and
the rest wait their turn standing (`Planning`), instead of the step taking as long as a hundred searches. An agent
that already walks keeps its previous route until the new one is known. Raise the budget for faster answers, lower
it for a tighter step.

Units of different sizes share one grid and one system:

<!-- code: samples/Pathfinding/SkirmishScene.cs#sizes -->
```csharp
// Two sizes on one grid. A large unit is wider than a cell: it is led only through passages three cells wide.
var small = new NavAgentSettings(radius: 4, speed: 70);
var large = new NavAgentSettings(radius: 11, speed: 45) { Weight = 4 };     // harder to push aside
for (int y = 0; y < Rows.Length; y++)
    for (int x = 0; x < Rows[y].Length; x++)
        if (Rows[y][x] is 'u' or 'U') _units.Add(_agents.Add(_nav.CenterOf(_nav.NodeOf(x, y)), Rows[y][x] == 'U' ? large : small));
```

### Building in their way

Agents are not obstacles in the grid: block cells for things that stay (doors, crates, towers). To change the level
while agents walk, change the grid; nothing else is needed. `agents.CanBlock(area)` tells first whether a wall can go
there: it is false when a body stands in the area, or when an agent on its way would be left with no route.

<!-- code: samples/Pathfinding/DefenceScene.cs#build -->
```csharp
/// <summary>Builds a tower on a cell, or takes away the one that is there. Returns whether the field changed.</summary>
public bool Toggle(NavCell cell)
{
    if (!_nav.InBounds(cell.X, cell.Y)) return false;
    var area = CellBounds(cell);
    if (_towers.Remove(cell))
    {
        _nav.SetBlocked(area, false);
        return true;
    }
    // The rule of the game: on open ground, where no creep stands or would be walled in, and where the gate
    // keeps a way to the keep for the creeps still to come.
    bool allowed = _nav.IsWalkable(cell.X, cell.Y)
        && _agents.CanBlock(area)
        && _agents.Regions(CreepRadius).StaysConnected(area, _nav.NodeAt(Gate), _nav.NodeAt(Keep));
    if (!allowed)
    {
        Refused = cell;
        _refusedFor = 0.4f;
        return false;
    }
    // Blocking the cells is all it takes: the creeps whose way it closes search again by themselves.
    _nav.SetBlocked(area, true);
    _towers.Add(cell);
    return true;
}
```

`CanBlock` knows the agents, not your level's fixed places: the creeps that have not left the gate yet are covered
by the last condition, which asks the regions the system keeps ([What can be reached](#what-can-be-reached)).

`agents.Query(center, radius, results)` adds the agents whose body reaches a circle, in the order they were added
(a tower's targets, the units under a selection), and `agents.Occupied(area)` tells whether a body overlaps a
rectangle:

<!-- code: samples/Pathfinding/DefenceScene.cs#wave -->
```csharp
// A creep leaves the gate when the last one made room, and is sent to the keep once.
_spawnIn -= dt;
if (_spawnIn <= 0 && !_agents.Occupied(RectF.FromCenter(Gate, 2 * CreepRadius, 2 * CreepRadius)))
{
    _spawnIn = SpawnEvery;
    var creep = _agents.Add(Gate, new NavAgentSettings(CreepRadius, speed: 40));
    creep.MoveTo(Keep);
    _creeps.Add(creep);
}
// One that arrived is in the keep: it leaves the field.
for (int i = _creeps.Count - 1; i >= 0; i--)
{
    if (_creeps[i].Status != NavAgentStatus.Arrived) continue;
    _agents.Remove(_creeps[i]);
    _creeps.RemoveAt(i);
    Reached++;
}
```

### A body your game moves

When the body is a [character controller](../manual/physics.md), a physics body or anything else your code moves,
set `MovedByGame`. The system then only steers: each step, move your body by `agent.DesiredVelocity` and tell the
agent where it ended. Other agents go around it and never push it.

```csharp
var hero = TopDownController2D.Create(physics, entity, start, new Vector2(10, 10), new TopDownMotion { MoveSpeed = 80 });
var agent = agents.Add(hero.Position, new NavAgentSettings(radius: 5, speed: 80) { MovedByGame = true });
agent.MoveTo(target);
// each step, in the scene's Update
hero.Step(dt, agent.DesiredVelocity / hero.Motion.MoveSpeed);
agent.Position = hero.Position;
```

Setting `agent.Position` on any agent moves it there at once (a teleport); its order stands.

### Outside a scene, and in saves

`new NavAgents(grid)` works without a scene: call `agents.Step(dt)` once per step. In a scene the system is part of
the scene's state hash, and a run with the same orders in the same steps repeats exactly, on every machine.

An agent's route is not something to save. Save what your game knows (where each unit is and where it was sent) and
give the orders again after loading: `agents.Add(position, settings)` and `agent.MoveTo(destination)`.

### Steering by hand

The rules agents steer by are functions of `Steering`, for things that are not agents (fish, birds, a platformer's
enemies, objects of an entity store): `Seek` and `Flee` head for or away from a point, `Arrive` slows down near it,
`Separate` pushes away from a neighbour that is too close, `Avoid` turns aside from a body it would run into, and
`Limit` caps a velocity. Each takes positions and returns a velocity or a push to add to it; they keep nothing
between calls. `grid.Slide(position, delta, radius)` moves a body along a grid's walls without entering them.

## Finding a route

Agents are made of searches a game can use directly, to move things its own way. A `PathFinder` does the searching.
Create one for the whole game: it keeps the memory of its searches, so searching again allocates nothing.
`grid.FindPath(finder, from, to, route, radius)` fills `route` with the world points to walk through, in order, from
the agent's position to the target.

<!-- code: samples/Pathfinding/Courier.cs#plan -->
```csharp
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

Moving along the route is then your game's code:

<!-- code: samples/Pathfinding/Courier.cs#follow -->
```csharp
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
```

### The cells under a route

After a search, `finder.Path` holds the cells of the path as nodes (node `y * Width + x` is cell (x, y)), from the
start to the end, and `finder.Cost` its cost. The next search replaces them, so copy what you keep.
`finder.Find(grid, start, goal)` searches without making a route, for games that move cell by cell;
`grid.NodeAt(point)`, `grid.CellOf(node)` and `grid.CenterOf(node)` convert between world points, cells and nodes.

### When to search

Searching every frame is never needed. Search when the answer can change: the target entered another cell
(`grid.NodeAt(target)` differs from the cell searched for), or the level changed (`grid.Version` differs from the
one remembered, and `grid.IsOpen(cells)` says the path is closed). Agents do exactly this by themselves.

### A search over several steps

On a very large map one long search can take more than a step of the game has to give. `finder.Begin(grid, start,
goal)` starts a search and `finder.Continue(nodes)` advances it by at most that many nodes: it returns
`PathStatus.Searching` until the answer is known, and then `Found`, `Unreachable` or `Incomplete` with the path in
`finder.Path`. The path, its cost and the work done are the ones `Find` gives, however the search was divided, so a
run stays repeatable when the budget is a number of nodes and not a time. The grid must not change while a search is
in progress: begin again when it does. `grid.ForAgent(radius)` is searched the same way.

## Obstacles that change

When a door closes or a crate lands, change the grid: `SetWalkable(x, y, false)` for one cell,
`SetBlocked(area, true)` for every cell a rectangle overlaps, or `ReadTiles(tiles)` / `ReadPhysics(world, filter)`
to read the whole level again.

<!-- code: samples/Pathfinding/RoomScene.cs#door -->
```csharp
/// <summary>Closes or opens the door; it stays open while somebody stands in it.</summary>
public bool SetDoor(bool closed)
{
    // Nobody is in the doorway, and nobody on the way somewhere would be shut in.
    if (closed && !_agents.CanBlock(DoorBounds)) return false;
    DoorClosed = closed;
    // The tile map changes, and the grid reads it again. That is all: the agents whose way crossed the door
    // search again by themselves, and the others never notice.
    _tiles.SetFlags(Door, closed ? TileFlags.Solid : TileFlags.None);
    _nav.ReadTiles(_tiles);
    return true;
}
```

Agents see the change in their next step: those whose way crossed the door search again, the others do nothing.

For routes followed by hand, every change gives the grid a new `Version`. An agent that remembers the version it last saw knows when to look
again, and `grid.IsOpen(cells)` tells whether the cells of its path can still be walked, so only those whose path
closed search again:

```csharp
if (walker.GridVersion != grid.Version)
{
    walker.GridVersion = grid.Version;
    if (!grid.IsOpen(walker.Cells)) walker.Plan(walker.Target);
}
```

## Agents with a size

The `radius` you give `FindPath` is half the side of the agent's square. It is used twice: the search leads the agent
only through cells where its whole body fits, and the straightened legs keep that distance from walls. A unit three
cells across is not sent through a door one cell wide: it goes around, or, when no way is wide enough, to the nearest
place it reaches (`PathStatus.Unreachable`). The route ends at the point asked for, or beside it when the body would
touch a wall there.

Agents of different sizes share one grid. For games that move cell by cell, `grid.ForAgent(radius)` is the grid as
that agent walks it: search it with `finder.Find(grid.ForAgent(radius), start, goal)`. `grid.IsOpen(cells, from,
radius)` tells whether a path still has room for the agent, and `new DistanceField(grid, radius)` is a field for
agents of that size.

`grid.ClearanceAt(x, y)` is the room around a cell: 0 for a wall, 1 for a cell with a wall beside it, 2 when the
nearest wall is two cells away. `grid.Fits(x, y, radius)` tells whether an agent can stand centred in a cell: use it
to choose where a large unit may be placed or spawned.

## What can be reached

A search to a place that cannot be reached looks at every cell the agent can reach before it gives up. `NavRegions`
answers the question first, without a search: the cells joined by some route form a region, and two places are
joined exactly when they are in the same one.

- `regions.Connected(a, b)` tells whether an agent at node `a` can walk to node `b`.
- `regions.NearestReachable(from, to)` is `to` when it can be reached and otherwise the cell of the agent's region
  nearest to it in a straight line: where a unit goes when the click lands on an island, in a wall or in a locked
  room. A search to that cell always finds its goal.
- `regions.StaysConnected(area, a, b)` tells whether `a` and `b` would still be joined if `area` were blocked, and
  changes nothing: the rule that lets a player build a tower anywhere except where it would wall the creeps in. It
  also takes a list of nodes (the gate, the keep and the cell of every creep on the field), all of which must stay
  joined.
- `regions.RegionOf(node)`, `regions.Count` and `regions.SizeOf(region)` are the regions themselves: pick spawn
  points in the player's region, or find the rooms a generated level left sealed.

Regions follow the grid: after a change, the first question measures them again, one pass over the grid. Create one
`NavRegions` per size of agent (`new NavRegions(grid, radius)`): a gap joins two rooms only for those that fit
through it. Region numbers change when the grid does, so compare them and do not keep them.

## Many agents, one goal

When many agents want the same place, one `DistanceField` replaces all their searches. `field.Compute(goal)` gives
every cell the cost of its cheapest route to the goal, and `agent.Follow(field)` makes an agent read it instead of
searching: each step it heads for the cell the field gives, keeping apart from the others like any agent, and it
arrives in the goal's cell or beside those that arrived before it. Compute the field again when the goal moves to
another cell or the grid's version changes, and set the agents after it again.

<!-- code: samples/Pathfinding/RoomScene.cs#field -->
```csharp
// The swarm all want the player: one field holds the cost from every cell to the player's cell, and each bug
// follows it without a search of its own. It is computed again when the player changes cell or the map
// changes, and the swarm is set after it again.
int playerCell = _nav.NodeAt(Player.Position);
if (playerCell != _fieldGoal || _field.Version != _nav.Version)
{
    _fieldGoal = playerCell;
    _field.Compute(playerCell);
    foreach (var bug in _swarm) bug.Follow(_field);
}
```

`Compute` also takes several goals (every agent heads for the nearest: the exits of a room) and a distance limit
that keeps the work near the goal on large maps. `field.DistanceAt(cell)` is the cost itself: use it to pick the
agent closest to something through the walls, not in a straight line. Things that are not agents read the field
themselves: `field.Next(cell)` is the cell to step to.

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
  search look at every cell the agent can reach, several frames of work: ask `NavRegions` first, spread the search
  over several steps, or set `finder.MaxExpansions` to bound it (the route then leads to the nearest cell found),
  and give fields a distance limit.
- A finder keeps 24 bytes per cell of the largest map it searched, a grid 5 and a field 8.
- One finder or field serves one search at a time: use one per thread.
- A system of a thousand agents on a 128 x 128 grid takes about two milliseconds a step. With the default search
  budget about a dozen long routes are found per step there, so a thousand units ordered at once all walk within a
  second or two; for one goal, a `DistanceField` gives them all a route in one computation.
- Agents steer by simple rules, not by a solver: they keep apart, turn aside and take turns at a door. A dense
  crowd queues at a narrow passage and goes through it one body at a time, at about half the rate of a single file
  walking freely: four hundred bodies of radius 0.3 cells at speed 4 take some 160 seconds through a gap of one
  cell.
- Bodies do not walk into each other, and a step in which a body would move more than half its radius is taken in
  parts. In a crowd pressed against a gap the overlap of two bodies stays under a tenth of the sum of their radii.
  Where two bodies cannot pass side by side (two streams head-on in a passage narrower than two bodies) they pass
  by overlapping, by up to a sixth of that sum.
- An agent with an order that nothing lets through (a body that stands in the only passage) stays `Moving` with
  `IsBlocked` set: it never gives up by itself.
- A body is a circle for other agents and a square for walls. A group has no formation: its units gather around the
  point in the order they get there.
- Agents walk a `NavGrid`. On a `NavGraph` or a space of your own, follow the path yourself.
- An agent wider than a cell is given whole cells of room on every side: wider than one cell it needs a passage
  three cells wide, wider than three cells one of five. Use smaller cells when a large unit must pass a gap of
  exactly its width.
- There are no navigation meshes and no automatic links between platforms.

## Related

- The `Pathfinding` sample (`samples/Pathfinding`), three scenes (C goes to the next): a room with a unit that walks
  to the clicked point, a chaser, a swarm on one field and a patrol that goes around when a door closes; a group of
  units of two sizes sent to one point; and a tower defence where towers are built in the way of a wave.
- [Scene systems](../manual/scenes.md#scene-systems): how `NavAgents` lives in a scene.
- [Tile maps and collision](../manual/world.md): the tile grids and physics worlds a `NavGrid` reads.
- API: [`NavAgents`](../api/MicroEngine.Navigation.NavAgents.md), [`NavAgent`](../api/MicroEngine.Navigation.NavAgent.md),
  [`NavAgentSettings`](../api/MicroEngine.Navigation.NavAgentSettings.md), [`Steering`](../api/MicroEngine.Navigation.Steering.md),
  [`NavRegions`](../api/MicroEngine.Navigation.NavRegions.md),
  [`NavGrid`](../api/MicroEngine.Navigation.NavGrid.md), [`PathFinder`](../api/MicroEngine.Navigation.PathFinder.md),
  [`DistanceField`](../api/MicroEngine.Navigation.DistanceField.md), [`NavGraph`](../api/MicroEngine.Navigation.NavGraph.md),
  [`INavGraph`](../api/MicroEngine.Navigation.INavGraph.md).
