# MicroEngine.Navigation

Pathfinding for [MicroEngine](https://github.com/NastMz/MicroEngine) games: the cheapest path over a grid of cells or a
graph of waypoints, grids built from a tile map or from a physics world, obstacles that change while the game runs,
routes straightened for agents with a size, and distance fields for many agents with one goal. Managed code only, with
no dependency besides the engine.

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

The package finds paths; what an agent wants and how it moves along a route is the game's. A game's own space
(hexagons, rooms, a 3D lattice) is searched the same way by implementing `INavGraph`.

MicroEngine is in **Alpha** (0.x): public APIs may change incompatibly between versions; each change is listed in the
[migration notes](https://github.com/NastMz/MicroEngine/blob/main/docs/migrations.md). MIT licensed.
