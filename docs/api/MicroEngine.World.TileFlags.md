# Enum TileFlags

Namespace: [MicroEngine.World](MicroEngine.World.md)  
Assembly: MicroEngine.dll  
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

Blocks only bodies moving down onto its top edge (jump-through platform).



`Solid = 1` 

Blocks movement from every side.



`Water = 8` 

Liquid; does not block.
