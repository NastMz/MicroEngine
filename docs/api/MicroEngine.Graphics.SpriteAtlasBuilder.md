# Class SpriteAtlasBuilder

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
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

Add images with [Add](MicroEngine.Graphics.SpriteAtlasBuilder.md#MicroEngine_Graphics_SpriteAtlasBuilder_Add_System_String_MicroEngine_Graphics_PixelImage_System_Nullable_System_Numerics_Vector2__), [AddFrames](MicroEngine.Graphics.SpriteAtlasBuilder.md#MicroEngine_Graphics_SpriteAtlasBuilder_AddFrames_System_String_System_Collections_Generic_IEnumerable_MicroEngine_Graphics_PixelImage__System_Nullable_System_Numerics_Vector2__) or [AddGrid](MicroEngine.Graphics.SpriteAtlasBuilder.md#MicroEngine_Graphics_SpriteAtlasBuilder_AddGrid_System_String_MicroEngine_Graphics_PixelImage_System_Int32_System_Int32_System_Nullable_System_Int32__System_Nullable_System_Numerics_Vector2__), then call [Build](MicroEngine.Graphics.SpriteAtlasBuilder.md#MicroEngine_Graphics_SpriteAtlasBuilder_Build_MicroEngine_Graphics_IGraphicsDevice_System_Nullable_MicroEngine_Graphics_TextureFilter__) once
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

Adds an image under <code class="paramref">name</code>. The image is copied into the atlas when [Build](MicroEngine.Graphics.SpriteAtlasBuilder.md#MicroEngine_Graphics_SpriteAtlasBuilder_Build_MicroEngine_Graphics_IGraphicsDevice_System_Nullable_MicroEngine_Graphics_TextureFilter__) runs, so keep it unchanged until then.

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

### <a id="MicroEngine_Graphics_SpriteAtlasBuilder_Add_MicroEngine_Graphics_SheetFrame_"></a> Add\(SheetFrame\)

Adds a frame with everything a sheet says about it: its name, pixels, anchor, points and boxes.

```csharp
public SpriteAtlasBuilder Add(SheetFrame frame)
```

#### Parameters

`frame` [SheetFrame](MicroEngine.Graphics.SheetFrame.md)

#### Returns

 [SpriteAtlasBuilder](MicroEngine.Graphics.SpriteAtlasBuilder.md)

This builder, for chaining.

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

The frame's name is empty or already added.

### <a id="MicroEngine_Graphics_SpriteAtlasBuilder_AddClip_MicroEngine_Graphics_SheetClip_"></a> AddClip\(SheetClip\)

Names an animation of sprites added to this builder (before or after this call): the atlas gives it with
[Clip](MicroEngine.Graphics.SpriteAtlas.md#MicroEngine_Graphics_SpriteAtlas_Clip_System_String_System_Nullable_System_Boolean__) and lists it in [Animations](MicroEngine.Graphics.SpriteAtlas.md#MicroEngine_Graphics_SpriteAtlas_Animations).

```csharp
public SpriteAtlasBuilder AddClip(SheetClip clip)
```

#### Parameters

`clip` [SheetClip](MicroEngine.Graphics.SheetClip.md)

#### Returns

 [SpriteAtlasBuilder](MicroEngine.Graphics.SpriteAtlasBuilder.md)

This builder, for chaining.

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

A clip of that name was already added.

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

### <a id="MicroEngine_Graphics_SpriteAtlasBuilder_AddFrames_System_String_System_Collections_Generic_IEnumerable_MicroEngine_Graphics_PixelImage__System_Single_System_Nullable_System_Numerics_Vector2__System_Boolean_"></a> AddFrames\(string, IEnumerable<PixelImage\>, float, Vector2?, bool\)

Adds each frame as "name0", "name1", ... and the clip <code class="paramref">name</code> that plays them at
<code class="paramref">framesPerSecond</code>: a strip drawn in code is an animation without its frame names being listed
([Clip](MicroEngine.Graphics.SpriteAtlas.md#MicroEngine_Graphics_SpriteAtlas_Clip_System_String_System_Nullable_System_Boolean__)).

```csharp
public SpriteAtlasBuilder AddFrames(string name, IEnumerable<PixelImage> frames, float framesPerSecond, Vector2? origin = null, bool loop = true)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

The strip's and the clip's name.

`frames` [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[PixelImage](MicroEngine.Graphics.PixelImage.md)\>

The frames, in playing order.

`framesPerSecond` [float](https://learn.microsoft.com/dotnet/api/system.single)

The clip's rate; positive.

`origin` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)?

The anchor every frame shares; null for the top-left corner.

`loop` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether the clip starts over after its last frame.

#### Returns

 [SpriteAtlasBuilder](MicroEngine.Graphics.SpriteAtlasBuilder.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">framesPerSecond</code> is not a positive number.

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

### <a id="MicroEngine_Graphics_SpriteAtlasBuilder_AddSheet_MicroEngine_Graphics_SpriteSheet_"></a> AddSheet\(SpriteSheet\)

Adds every frame of <code class="paramref">sheet</code> under its name, and its clips: what a <code>.sheet</code> file or an artist's
tool described ([SpriteSheet](MicroEngine.Graphics.SpriteSheet.md)), next to whatever else the builder holds.

```csharp
public SpriteAtlasBuilder AddSheet(SpriteSheet sheet)
```

#### Parameters

`sheet` [SpriteSheet](MicroEngine.Graphics.SpriteSheet.md)

#### Returns

 [SpriteAtlasBuilder](MicroEngine.Graphics.SpriteAtlasBuilder.md)

This builder, for chaining.

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

A frame or a clip has a name already added.

### <a id="MicroEngine_Graphics_SpriteAtlasBuilder_Build_MicroEngine_Graphics_IGraphicsDevice_System_Nullable_MicroEngine_Graphics_TextureFilter__"></a> Build\(IGraphicsDevice, TextureFilter?\)

Shelf-packs every image into the smallest power-of-two width that fits, with a 1-pixel gap between images. Sampled
linearly, each image also gets a 1-pixel border repeating its edge pixels, so scaled sprites and tiles never
show their neighbours' colours or seams.

```csharp
public SpriteAtlas Build(IGraphicsDevice device, TextureFilter? filter = null)
```

#### Parameters

`device` [IGraphicsDevice](MicroEngine.Graphics.IGraphicsDevice.md)

Creates the texture.

`filter` [TextureFilter](MicroEngine.Graphics.TextureFilter.md)?

The texture's filter; null for the device's [DefaultFilter](MicroEngine.Graphics.IGraphicsDevice.md#MicroEngine_Graphics_IGraphicsDevice_DefaultFilter).

#### Returns

 [SpriteAtlas](MicroEngine.Graphics.SpriteAtlas.md)

### <a id="MicroEngine_Graphics_SpriteAtlasBuilder_Pack_System_Boolean_System_Boolean_"></a> Pack\(bool, bool\)

Packs every image into one, without a graphics device: the picture and where each sprite lies in it, for tools
that write an atlas to disk ([ToJson](MicroEngine.Graphics.PackedAtlas.md#MicroEngine_Graphics_PackedAtlas_ToJson_System_String_)) and for tests. [Build](MicroEngine.Graphics.SpriteAtlasBuilder.md#MicroEngine_Graphics_SpriteAtlasBuilder_Build_MicroEngine_Graphics_IGraphicsDevice_System_Nullable_MicroEngine_Graphics_TextureFilter__) packs the same way.

```csharp
public PackedAtlas Pack(bool extrude = false, bool trim = false)
```

#### Parameters

`extrude` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether each image gets a 1-pixel border repeating its edge pixels, so linear sampling and scaling never show a neighbour.

`trim` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether the fully transparent margins of each image are left out of the picture. A sprite then covers only its
opaque pixels, anchored so that it draws where the whole image would; [Untrimmed](MicroEngine.Graphics.PackedAtlas.md#MicroEngine_Graphics_PackedAtlas_Untrimmed) keeps
what was cut.

#### Returns

 [PackedAtlas](MicroEngine.Graphics.PackedAtlas.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Nothing was added, or a clip names a sprite that was not.

</div>
