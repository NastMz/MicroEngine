# Class SpriteAtlasBuilder

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

Packs many procedurally authored [PixelImage](MicroEngine.Graphics.PixelImage.md)s into one texture and
exposes them by name. Keeps draw calls batched and art code independent of layout.

```csharp
public sealed class SpriteAtlasBuilder
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SpriteAtlasBuilder](MicroEngine.Graphics.SpriteAtlasBuilder.md)

## Examples

```csharp
var hero = new PixelImage(16, 16);
hero.FillRect(4, 2, 8, 12, Color.White);
var atlas = new SpriteAtlasBuilder()
    .Add("hero", hero, origin: new Vector2(8, 16))
    .Build(Graphics);

// In Draw:
renderer.Draw(atlas["hero"], new Vector2(160, 120));
```

## Remarks

Add images with [Add](MicroEngine.Graphics.SpriteAtlasBuilder.md#MicroEngine_Graphics_SpriteAtlasBuilder_Add_System_String_MicroEngine_Graphics_PixelImage_System_Nullable_System_Numerics_Vector2__), [AddFrames](MicroEngine.Graphics.SpriteAtlasBuilder.md#MicroEngine_Graphics_SpriteAtlasBuilder_AddFrames_System_String_System_Collections_Generic_IEnumerable_MicroEngine_Graphics_PixelImage__System_Nullable_System_Numerics_Vector2__) or [AddGrid](MicroEngine.Graphics.SpriteAtlasBuilder.md#MicroEngine_Graphics_SpriteAtlasBuilder_AddGrid_System_String_MicroEngine_Graphics_PixelImage_System_Int32_System_Int32_System_Nullable_System_Int32__System_Nullable_System_Numerics_Vector2__), then call [Build](MicroEngine.Graphics.SpriteAtlasBuilder.md#MicroEngine_Graphics_SpriteAtlasBuilder_Build_MicroEngine_Graphics_IGraphicsDevice_) once
from <code>Game.Load</code> (it needs the graphics device). Names are case-sensitive.

## Constructors

### <a id="MicroEngine_Graphics_SpriteAtlasBuilder__ctor"></a> SpriteAtlasBuilder\(\)

```csharp
public SpriteAtlasBuilder()
```

## Properties

### <a id="MicroEngine_Graphics_SpriteAtlasBuilder_Count"></a> Count

Number of sprites added so far (each frame counts as one).

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Graphics_SpriteAtlasBuilder_Add_System_String_MicroEngine_Graphics_PixelImage_System_Nullable_System_Numerics_Vector2__"></a> Add\(string, PixelImage, Vector2?\)

Adds an image under <code class="paramref">name</code>. The image is copied into the atlas when [Build](MicroEngine.Graphics.SpriteAtlasBuilder.md#MicroEngine_Graphics_SpriteAtlasBuilder_Build_MicroEngine_Graphics_IGraphicsDevice_) runs, so keep it unchanged until then.

```csharp
public SpriteAtlasBuilder Add(string name, PixelImage image, Vector2? origin = null)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

Unique, non-empty sprite name.

`image` [PixelImage](MicroEngine.Graphics.PixelImage.md)

The pixels.

`origin` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)?

The sprite's anchor in its own pixels (see [Origin](MicroEngine.Graphics.Sprite.md#MicroEngine_Graphics_Sprite_Origin)); null for the top-left corner.

#### Returns

 [SpriteAtlasBuilder](MicroEngine.Graphics.SpriteAtlasBuilder.md)

This builder, for chaining.

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">name</code> is empty or already added.

### <a id="MicroEngine_Graphics_SpriteAtlasBuilder_AddFrames_System_String_System_Collections_Generic_IEnumerable_MicroEngine_Graphics_PixelImage__System_Nullable_System_Numerics_Vector2__"></a> AddFrames\(string, IEnumerable<PixelImage\>, Vector2?\)

Adds each frame as "name0", "name1", ... sharing the same origin.

```csharp
public SpriteAtlasBuilder AddFrames(string name, IEnumerable<PixelImage> frames, Vector2? origin = null)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`frames` [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[PixelImage](MicroEngine.Graphics.PixelImage.md)\>

`origin` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)?

#### Returns

 [SpriteAtlasBuilder](MicroEngine.Graphics.SpriteAtlasBuilder.md)

### <a id="MicroEngine_Graphics_SpriteAtlasBuilder_AddGrid_System_String_MicroEngine_Graphics_PixelImage_System_Int32_System_Int32_System_Nullable_System_Int32__System_Nullable_System_Numerics_Vector2__"></a> AddGrid\(string, PixelImage, int, int, int?, Vector2?\)

Slices a sprite sheet laid out on a regular grid (row-major) into frames "name0", "name1", ...
<code class="paramref">count</code> limits the number of cells (default: every cell); fully transparent
cells are skipped only when they are trailing cells of the last row.

```csharp
public SpriteAtlasBuilder AddGrid(string name, PixelImage sheet, int cellWidth, int cellHeight, int? count = null, Vector2? origin = null)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`sheet` [PixelImage](MicroEngine.Graphics.PixelImage.md)

`cellWidth` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`cellHeight` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`count` [int](https://learn.microsoft.com/dotnet/api/system.int32)?

`origin` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)?

#### Returns

 [SpriteAtlasBuilder](MicroEngine.Graphics.SpriteAtlasBuilder.md)

### <a id="MicroEngine_Graphics_SpriteAtlasBuilder_Build_MicroEngine_Graphics_IGraphicsDevice_"></a> Build\(IGraphicsDevice\)

Shelf-packs every image (with 1px padding) into the smallest power-of-two width that fits.

```csharp
public SpriteAtlas Build(IGraphicsDevice device)
```

#### Parameters

`device` [IGraphicsDevice](MicroEngine.Graphics.IGraphicsDevice.md)

#### Returns

 [SpriteAtlas](MicroEngine.Graphics.SpriteAtlas.md)

</div>
