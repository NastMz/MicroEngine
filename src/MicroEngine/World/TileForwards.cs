using System.Runtime.CompilerServices;

// The tile types moved to MicroEngine.Physics.Tiles.dll (carried by the MicroEngine package) with their namespace, so
// the tile backend of the physics contract is built on them; code compiled against an earlier MicroEngine.dll still
// finds them here. The tile mover's result was renamed TileMoveResult (it was MoveResult, the name of the physics
// contract's result), so it has no forward: code that used it recompiles with the new name.
[assembly: TypeForwardedTo(typeof(MicroEngine.World.TileFlags))]
[assembly: TypeForwardedTo(typeof(MicroEngine.World.TileLayer))]
[assembly: TypeForwardedTo(typeof(MicroEngine.World.ICollisionGrid))]
[assembly: TypeForwardedTo(typeof(MicroEngine.World.TileGrid))]
[assembly: TypeForwardedTo(typeof(MicroEngine.World.Autotile))]
[assembly: TypeForwardedTo(typeof(MicroEngine.World.TileMotion))]
