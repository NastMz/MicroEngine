# Class PackedAtlas

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

Sprites packed into one picture by [Pack](MicroEngine.Graphics.SpriteAtlasBuilder.md#MicroEngine_Graphics_SpriteAtlasBuilder_Pack_System_Boolean_System_Boolean_), without a graphics device: the picture,
where each sprite lies in it, and the clips. [ToJson](MicroEngine.Graphics.PackedAtlas.md#MicroEngine_Graphics_PackedAtlas_ToJson_System_String_) writes the description packing tools write, so the
pair (a PNG and a JSON file) is an ordinary atlas for <code>Assets.Atlas</code>. The frames' points and boxes are written
with each frame and read back.

```csharp
public sealed record PackedAtlas : IEquatable<PackedAtlas>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PackedAtlas](MicroEngine.Graphics.PackedAtlas.md)

#### Implements

[IEquatable<PackedAtlas\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_PackedAtlas__ctor_MicroEngine_Graphics_PixelImage_System_Collections_Generic_IReadOnlyList_MicroEngine_Graphics_AtlasRegion__System_Collections_Generic_IReadOnlyList_MicroEngine_Graphics_SheetClip__"></a> PackedAtlas\(PixelImage, IReadOnlyList<AtlasRegion\>, IReadOnlyList<SheetClip\>\)

Sprites packed into one picture by [Pack](MicroEngine.Graphics.SpriteAtlasBuilder.md#MicroEngine_Graphics_SpriteAtlasBuilder_Pack_System_Boolean_System_Boolean_), without a graphics device: the picture,
where each sprite lies in it, and the clips. [ToJson](MicroEngine.Graphics.PackedAtlas.md#MicroEngine_Graphics_PackedAtlas_ToJson_System_String_) writes the description packing tools write, so the
pair (a PNG and a JSON file) is an ordinary atlas for <code>Assets.Atlas</code>. The frames' points and boxes are written
with each frame and read back.

```csharp
public PackedAtlas(PixelImage Image, IReadOnlyList<AtlasRegion> Regions, IReadOnlyList<SheetClip> Clips)
```

#### Parameters

`Image` [PixelImage](MicroEngine.Graphics.PixelImage.md)

The packed picture.

`Regions` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[AtlasRegion](MicroEngine.Graphics.AtlasRegion.md)\>

Each sprite's name, rectangle and anchor, in the order they were added.

`Clips` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[SheetClip](MicroEngine.Graphics.SheetClip.md)\>

The animations.

## Properties

### <a id="MicroEngine_Graphics_PackedAtlas_Clips"></a> Clips

The animations.

```csharp
public IReadOnlyList<SheetClip> Clips { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[SheetClip](MicroEngine.Graphics.SheetClip.md)\>

### <a id="MicroEngine_Graphics_PackedAtlas_Image"></a> Image

The packed picture.

```csharp
public PixelImage Image { get; init; }
```

#### Property Value

 [PixelImage](MicroEngine.Graphics.PixelImage.md)

### <a id="MicroEngine_Graphics_PackedAtlas_Regions"></a> Regions

Each sprite's name, rectangle and anchor, in the order they were added.

```csharp
public IReadOnlyList<AtlasRegion> Regions { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[AtlasRegion](MicroEngine.Graphics.AtlasRegion.md)\>

### <a id="MicroEngine_Graphics_PackedAtlas_Untrimmed"></a> Untrimmed

For an atlas packed with trimming, what was cut from each sprite, in the order of [Regions](MicroEngine.Graphics.PackedAtlas.md#MicroEngine_Graphics_PackedAtlas_Regions): where its
kept pixels lay in the whole image and that image's size. Null when nothing was trimmed.

```csharp
public IReadOnlyList<AtlasTrim>? Untrimmed { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[AtlasTrim](MicroEngine.Graphics.AtlasTrim.md)\>?

## Methods

### <a id="MicroEngine_Graphics_PackedAtlas__Clone__"></a> <Clone\>$\(\)

```csharp
public PackedAtlas <Clone>$()
```

#### Returns

 [PackedAtlas](MicroEngine.Graphics.PackedAtlas.md)

### <a id="MicroEngine_Graphics_PackedAtlas_Deconstruct_MicroEngine_Graphics_PixelImage__System_Collections_Generic_IReadOnlyList_MicroEngine_Graphics_AtlasRegion___System_Collections_Generic_IReadOnlyList_MicroEngine_Graphics_SheetClip___"></a> Deconstruct\(out PixelImage, out IReadOnlyList<AtlasRegion\>, out IReadOnlyList<SheetClip\>\)

```csharp
public void Deconstruct(out PixelImage Image, out IReadOnlyList<AtlasRegion> Regions, out IReadOnlyList<SheetClip> Clips)
```

#### Parameters

`Image` [PixelImage](MicroEngine.Graphics.PixelImage.md)

`Regions` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[AtlasRegion](MicroEngine.Graphics.AtlasRegion.md)\>

`Clips` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[SheetClip](MicroEngine.Graphics.SheetClip.md)\>

### <a id="MicroEngine_Graphics_PackedAtlas_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_PackedAtlas_Equals_MicroEngine_Graphics_PackedAtlas_"></a> Equals\(PackedAtlas?\)

```csharp
public bool Equals(PackedAtlas? other)
```

#### Parameters

`other` [PackedAtlas](MicroEngine.Graphics.PackedAtlas.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_PackedAtlas_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_PackedAtlas_ToJson_System_String_"></a> ToJson\(string\)

The atlas as the JSON of packing tools (an array of frames with Aseprite's frame durations and tags), which
[Parse](MicroEngine.Graphics.AtlasData.md#MicroEngine_Graphics_AtlasData_Parse_System_String_MicroEngine_Graphics_AtlasOptions_) and <code>Assets.Atlas</code> read. The same atlas always gives the same text.

```csharp
public string ToJson(string imageName)
```

#### Parameters

`imageName` [string](https://learn.microsoft.com/dotnet/api/system.string)

The picture's file name, next to the JSON file ("items.png").

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Remarks

A clip is written as a frame tag over consecutive frames. One whose frames are not consecutive sprites, or that
times a frame differently from another clip, gets entries of its own named "clip#0", "clip#1"..., on the same
pixels. A fixed rate is written as each frame's duration in milliseconds.

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

A clip names a sprite the atlas does not have.

### <a id="MicroEngine_Graphics_PackedAtlas_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_PackedAtlas_op_Equality_MicroEngine_Graphics_PackedAtlas_MicroEngine_Graphics_PackedAtlas_"></a> operator ==\(PackedAtlas?, PackedAtlas?\)

```csharp
public static bool operator ==(PackedAtlas? left, PackedAtlas? right)
```

#### Parameters

`left` [PackedAtlas](MicroEngine.Graphics.PackedAtlas.md)?

`right` [PackedAtlas](MicroEngine.Graphics.PackedAtlas.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_PackedAtlas_op_Inequality_MicroEngine_Graphics_PackedAtlas_MicroEngine_Graphics_PackedAtlas_"></a> operator \!=\(PackedAtlas?, PackedAtlas?\)

```csharp
public static bool operator !=(PackedAtlas? left, PackedAtlas? right)
```

#### Parameters

`left` [PackedAtlas](MicroEngine.Graphics.PackedAtlas.md)?

`right` [PackedAtlas](MicroEngine.Graphics.PackedAtlas.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
