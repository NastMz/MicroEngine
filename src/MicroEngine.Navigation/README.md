# MicroEngine.Navigation

Navigation for [MicroEngine](https://github.com/NastMz/MicroEngine) games: agents that walk to where they are sent,
keep apart, go around what is built in their way and arrive; the cheapest path over a grid of cells or a graph of
waypoints, for agents of any size; grids built from a tile map or from a physics world; what can be reached and
whether a wall would cut a way; and distance fields for many agents with one goal. Managed code only, with no
dependency besides the engine.

```csharp
var nav = NavGrid.FromTiles(tiles);              // solid tiles block; one cell per tile
var agents = Systems.Add(new NavAgents(nav));    // a scene system: stepped with the scene
var unit = agents.Add(spawn, new NavAgentSettings(radius: 5, speed: 80));

unit.MoveTo(click);                              // it finds its route, walks it and stops there
// A tower is built only where nobody is walled in; those whose way it closes go around.
if (agents.CanBlock(tower)) nav.SetBlocked(tower, true);
```

The searches the agents are made of are there for games that move things their own way:

```csharp
var nav = NavGrid.FromTiles(tiles);              // solid tiles block; one cell per tile
var finder = new PathFinder();                   // one per game: searching again allocates nothing
var route = new List<Vector2>();

// Where the enemy walks to reach the player, as points to move through in order.
if (nav.FindPath(finder, enemy.Center, player.Center, route, radius: 6) == PathStatus.Found)
    enemy.Follow(route);

// A door closes: the grid changes, and agents whose paths crossed it search again.
nav.SetBlocked(door.Bounds, true);
```

What an agent wants is the game's. A game's own space (hexagons, rooms, a 3D lattice) is searched the same way by
implementing `INavGraph`.

MicroEngine is in **Alpha** (0.x): public APIs may change incompatibly between versions; each change is listed in the
[migration notes](https://github.com/NastMz/MicroEngine/blob/main/docs/migrations.md). MIT licensed.
