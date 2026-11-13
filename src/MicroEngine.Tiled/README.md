# MicroEngine.Tiled

Maps made in the [Tiled](https://www.mapeditor.org/) editor for [MicroEngine](https://github.com/NastMz/MicroEngine):
`.tmx` and `.tmj` maps with their external tilesets and templates, read through the game's assets (folders, archives
and browser packages alike) into the engine's tile types. Tile layers become `TileLayer`s with each cell's flips,
tilesets become sprites, objects become records with typed properties, and a collision grid comes from a tile
property the game names.

```csharp
var map = Assets.TiledMap("maps/level1.tmx");
var sprites = new TiledSprites(map, Assets);
var solid = map.CollisionGrid(map.TileLayer("ground"), "collision"); // "solid", "oneway", ... on each tile
foreach (var spawn in map.ObjectLayer("spawns").Objects)
    if (spawn.Class == "enemy") SpawnEnemy(spawn.Position, spawn.Properties.Int("hp", 1));
// In Draw, with the camera pushed:
sprites.DrawAll(renderer, camera, elapsedSeconds);
```

The engine gives no class or property a meaning: what an object is, is the game's rule. Only orthogonal maps load.
Maps are parsed by [DotTiled](https://github.com/dcronqvist/DotTiled), which this package's public API never exposes.

MicroEngine is in **Alpha** (0.x): public APIs may change incompatibly between versions; each change is listed in the
[migration notes](https://github.com/NastMz/MicroEngine/blob/main/docs/migrations.md). MIT licensed; DotTiled is
under MIT.
