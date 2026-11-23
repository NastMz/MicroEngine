# Enum TileFlags

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.World](MicroEngine.World.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Tile maps and collision](../manual/world.md)  

Collision semantics of a tile. Games may use the Custom bits for their own meaning.

```csharp
[Flags]
public enum TileFlags
```

## Fields

`Custom1 = 16` 

Free for the game's own meaning (ladders, ice, triggers...).



`Custom2 = 32` 

Free for the game's own meaning.



`Custom3 = 64` 

Free for the game's own meaning.



`Custom4 = 128` 

Free for the game's own meaning.



`Hazard = 4` 

Hurts on contact; does not block.



`None = 0` 

No collision meaning: the tile is empty space.



`OneWay = 2` 

Blocks only bodies moving down onto its top edge (jump-through platform). For overlaps, point queries and the
nearest surface it is a platform at the top of the tile, <code>PhysicsContract.OneWayTileThickness</code> of a tile thick.



`SlopeLeft = 512` 

A slope that rises to the left: the half of the tile under the line from its upper left corner to its lower
right one is solid. Geometry only on a backend that declares
[TileSlopes](MicroEngine.Physics.PhysicsCapabilities.md).



`SlopeRight = 256` 

A slope that rises to the right: the half of the tile under the line from its lower left corner to its upper
right one is solid. Geometry only on a backend that declares
[TileSlopes](MicroEngine.Physics.PhysicsCapabilities.md). A character walks on slopes as a capsule; a
box is stopped at the foot of one.



`Solid = 1` 

Blocks movement from every side.



`Water = 8` 

Liquid; does not block.

</div>
