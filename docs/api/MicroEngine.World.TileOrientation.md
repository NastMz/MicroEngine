# Enum TileOrientation

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.World](MicroEngine.World.md)  
Assembly: MicroEngine.dll  
Guide: [Tile maps and collision](../manual/world.md)  

How one cell's tile is mirrored or turned, as map editors store it (Tiled's flip bits). The diagonal flip (x and y
swapped) applies first, then the horizontal and the vertical flips, so the eight combinations cover every quarter
turn with or without a mirror: <code>Diagonal | FlipX</code> is a quarter turn clockwise, <code>Diagonal | FlipY</code> a
quarter turn anticlockwise, <code>FlipX | FlipY</code> a half turn.

```csharp
[Flags]
public enum TileOrientation : byte
```

#### Extension Methods

[TileOrientations.ToDrawOptions\(TileOrientation, Vector2\)](MicroEngine.World.TileOrientations.md\#MicroEngine\_World\_TileOrientations\_ToDrawOptions\_MicroEngine\_World\_TileOrientation\_System\_Numerics\_Vector2\_)

## Fields

`Diagonal = 4` 

Mirrored across the top-left to bottom-right diagonal (x and y swapped), before the other flips.



`FlipX = 1` 

Mirrored left to right.



`FlipY = 2` 

Mirrored top to bottom.



`None = 0` 

Drawn as it is in the tileset.

</div>
