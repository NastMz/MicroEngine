# Class TileOrientations

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.World](MicroEngine.World.md)  
Assembly: MicroEngine.dll  
Guide: [Tile maps and collision](../manual/world.md)  

Turns a [TileOrientation](MicroEngine.World.TileOrientation.md) into drawing options.

```csharp
public static class TileOrientations
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TileOrientations](MicroEngine.World.TileOrientations.md)

## Methods

### <a id="MicroEngine_World_TileOrientations_ToDrawOptions_MicroEngine_World_TileOrientation_System_Numerics_Vector2_"></a> ToDrawOptions\(TileOrientation, Vector2\)

The flips and rotation that draw a tile of <code class="paramref">size</code> pixels in <code class="paramref">orientation</code>,
turning about the tile's centre so it stays in its cell. Pass the result to [Draw](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_Draw_MicroEngine_Graphics_Texture_MicroEngine_RectF_System_Numerics_Vector2_MicroEngine_Color_MicroEngine_Graphics_DrawOptions__) with
the tile's top-left as the position.

```csharp
public static DrawOptions ToDrawOptions(this TileOrientation orientation, Vector2 size)
```

#### Parameters

`orientation` [TileOrientation](MicroEngine.World.TileOrientation.md)

`size` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [DrawOptions](MicroEngine.Graphics.DrawOptions.md)

</div>
