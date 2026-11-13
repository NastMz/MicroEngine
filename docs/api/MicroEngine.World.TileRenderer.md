# Class TileRenderer

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.World](MicroEngine.World.md)  
Assembly: MicroEngine.dll  
Guide: [Tile maps and collision](../manual/world.md)  

Draws the visible part of a layer, asking the game which sprite each tile uses.

```csharp
public static class TileRenderer
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TileRenderer](MicroEngine.World.TileRenderer.md)

## Methods

### <a id="MicroEngine_World_TileRenderer_Draw_MicroEngine_Graphics_IRenderer_MicroEngine_World_TileLayer_System_Int32_MicroEngine_Graphics_Camera2D_System_Func_System_Int32_System_Int32_System_Int32_MicroEngine_Graphics_Sprite__"></a> Draw\(IRenderer, TileLayer, int, Camera2D?, Func<int, int, int, Sprite?\>\)

Draws the non-zero tiles that can be seen, each at its tile position minus its sprite's origin.

```csharp
public static void Draw(IRenderer renderer, TileLayer layer, int tileSize, Camera2D? camera, Func<int, int, int, Sprite?> spriteFor)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

The renderer; push <code class="paramref">camera</code> on it first when there is one.

`layer` [TileLayer](MicroEngine.World.TileLayer.md)

The tile ids; 0 is never drawn.

`tileSize` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Tile size in pixels.

`camera` [Camera2D](MicroEngine.Graphics.Camera2D.md)?

The camera, used only to skip tiles outside its view; null draws the tiles over the screen area.

`spriteFor` [Func](https://learn.microsoft.com/dotnet/api/system.func\-4)<[int](https://learn.microsoft.com/dotnet/api/system.int32), [int](https://learn.microsoft.com/dotnet/api/system.int32), [int](https://learn.microsoft.com/dotnet/api/system.int32), [Sprite](MicroEngine.Graphics.Sprite.md)?\>

Returns the sprite for a tile id at tile (x, y), or null to draw nothing.

### <a id="MicroEngine_World_TileRenderer_Draw_MicroEngine_Graphics_IRenderer_MicroEngine_World_TileLayer_System_Int32_MicroEngine_Graphics_Camera2D_System_Func_System_Int32_System_Int32_System_Int32_MicroEngine_Graphics_Sprite__System_Func_System_Int32_System_Int32_MicroEngine_World_TileOrientation__System_Numerics_Vector2_System_Nullable_MicroEngine_Color__System_Single_"></a> Draw\(IRenderer, TileLayer, int, Camera2D?, Func<int, int, int, Sprite?\>, Func<int, int, TileOrientation\>?, Vector2, Color?, float\)

Draws the non-zero tiles that can be seen, each at its tile position minus its sprite's origin, mirrored or
turned as <code class="paramref">orientationAt</code> says.

```csharp
public static void Draw(IRenderer renderer, TileLayer layer, int tileSize, Camera2D? camera, Func<int, int, int, Sprite?> spriteFor, Func<int, int, TileOrientation>? orientationAt, Vector2 offset = default, Color? tint = null, float margin = 0)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

The renderer; push <code class="paramref">camera</code> on it first when there is one.

`layer` [TileLayer](MicroEngine.World.TileLayer.md)

The tile ids; 0 is never drawn.

`tileSize` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Tile size in pixels.

`camera` [Camera2D](MicroEngine.Graphics.Camera2D.md)?

The camera, used only to skip tiles outside what it shows (zoom and rotation included); null draws the tiles over the screen area.

`spriteFor` [Func](https://learn.microsoft.com/dotnet/api/system.func\-4)<[int](https://learn.microsoft.com/dotnet/api/system.int32), [int](https://learn.microsoft.com/dotnet/api/system.int32), [int](https://learn.microsoft.com/dotnet/api/system.int32), [Sprite](MicroEngine.Graphics.Sprite.md)?\>

Returns the sprite for a tile id at tile (x, y), or null to draw nothing.

`orientationAt` [Func](https://learn.microsoft.com/dotnet/api/system.func\-3)<[int](https://learn.microsoft.com/dotnet/api/system.int32), [int](https://learn.microsoft.com/dotnet/api/system.int32), [TileOrientation](MicroEngine.World.TileOrientation.md)\>?

The orientation of the tile at (x, y); null draws every tile as it is. A turned tile turns about its sprite's centre.

`offset` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Where tile (0, 0) is drawn, in world pixels (the layer's offset).

`tint` [Color](MicroEngine.Color.md)?

Colour every tile is multiplied by (its alpha is the layer's opacity); null for white.

`margin` [float](https://learn.microsoft.com/dotnet/api/system.single)

Extra pixels around the view whose tiles are drawn too, for sprites larger than a cell.

</div>
