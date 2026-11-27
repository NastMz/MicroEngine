# Struct AtlasRegion

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

One named rectangle of an atlas image, as a packing tool described it.

```csharp
public readonly struct AtlasRegion : IEquatable<AtlasRegion>
```

#### Implements

[IEquatable<AtlasRegion\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_AtlasRegion__ctor_System_String_MicroEngine_RectF_System_Numerics_Vector2_System_Nullable_System_Single__System_Boolean_"></a> AtlasRegion\(string, RectF, Vector2, float?, bool\)

One named rectangle of an atlas image, as a packing tool described it.

```csharp
public AtlasRegion(string Name, RectF Source, Vector2 Origin, float? Duration, bool Rotated = false)
```

#### Parameters

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

The frame's name, without a file extension ("hero_run_0" for "hero_run_0.png", "slime 0" for Aseprite's "slime 0.aseprite").

`Source` [RectF](MicroEngine.RectF.md)

The rectangle in the atlas image, in pixels.

`Origin` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

The anchor in the rectangle's pixels (see [Origin](MicroEngine.Graphics.Sprite.md#MicroEngine_Graphics_Sprite_Origin)): the tool's pivot when it wrote one, otherwise the
[Pivot](MicroEngine.Graphics.AtlasOptions.md#MicroEngine_Graphics_AtlasOptions_Pivot) of the untrimmed frame, so trimmed frames line up as their untrimmed originals.

`Duration` [float](https://learn.microsoft.com/dotnet/api/system.single)?

How long the frame shows in an animation, in seconds, when the tool wrote it (Aseprite).

`Rotated` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether the tool stored the frame turned 90 degrees clockwise to pack it tighter: <code class="paramref">Source</code> is then the
turned rectangle as it lies in the image, and <code class="paramref">Origin</code> the anchor of the upright frame.
[ToSheet](MicroEngine.Graphics.AtlasData.md#MicroEngine_Graphics_AtlasData_ToSheet_MicroEngine_Graphics_PixelImage_) turns such frames upright.

## Properties

### <a id="MicroEngine_Graphics_AtlasRegion_Boxes"></a> Boxes

The frame's named boxes, in the rectangle's pixels (of the upright frame); null when the file gives none.

```csharp
public IReadOnlyList<SpriteBox>? Boxes { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[SpriteBox](MicroEngine.Graphics.SpriteBox.md)\>?

### <a id="MicroEngine_Graphics_AtlasRegion_Duration"></a> Duration

How long the frame shows in an animation, in seconds, when the tool wrote it (Aseprite).

```csharp
public float? Duration { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)?

### <a id="MicroEngine_Graphics_AtlasRegion_Name"></a> Name

The frame's name, without a file extension ("hero_run_0" for "hero_run_0.png", "slime 0" for Aseprite's "slime 0.aseprite").

```csharp
public string Name { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Graphics_AtlasRegion_Origin"></a> Origin

The anchor in the rectangle's pixels (see [Origin](MicroEngine.Graphics.Sprite.md#MicroEngine_Graphics_Sprite_Origin)): the tool's pivot when it wrote one, otherwise the
[Pivot](MicroEngine.Graphics.AtlasOptions.md#MicroEngine_Graphics_AtlasOptions_Pivot) of the untrimmed frame, so trimmed frames line up as their untrimmed originals.

```csharp
public Vector2 Origin { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Graphics_AtlasRegion_Points"></a> Points

The frame's named points, in the rectangle's pixels (of the upright frame); null when the file gives none.

```csharp
public IReadOnlyList<SpritePoint>? Points { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[SpritePoint](MicroEngine.Graphics.SpritePoint.md)\>?

### <a id="MicroEngine_Graphics_AtlasRegion_Rotated"></a> Rotated

Whether the tool stored the frame turned 90 degrees clockwise to pack it tighter: <code class="paramref">Source</code> is then the
turned rectangle as it lies in the image, and <code class="paramref">Origin</code> the anchor of the upright frame.
[ToSheet](MicroEngine.Graphics.AtlasData.md#MicroEngine_Graphics_AtlasData_ToSheet_MicroEngine_Graphics_PixelImage_) turns such frames upright.

```csharp
public bool Rotated { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_AtlasRegion_Source"></a> Source

The rectangle in the atlas image, in pixels.

```csharp
public RectF Source { get; init; }
```

#### Property Value

 [RectF](MicroEngine.RectF.md)

## Methods

### <a id="MicroEngine_Graphics_AtlasRegion_Deconstruct_System_String__MicroEngine_RectF__System_Numerics_Vector2__System_Nullable_System_Single___System_Boolean__"></a> Deconstruct\(out string, out RectF, out Vector2, out float?, out bool\)

```csharp
public void Deconstruct(out string Name, out RectF Source, out Vector2 Origin, out float? Duration, out bool Rotated)
```

#### Parameters

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Source` [RectF](MicroEngine.RectF.md)

`Origin` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`Duration` [float](https://learn.microsoft.com/dotnet/api/system.single)?

`Rotated` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_AtlasRegion_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_AtlasRegion_Equals_MicroEngine_Graphics_AtlasRegion_"></a> Equals\(AtlasRegion\)

```csharp
public bool Equals(AtlasRegion other)
```

#### Parameters

`other` [AtlasRegion](MicroEngine.Graphics.AtlasRegion.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_AtlasRegion_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_AtlasRegion_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_AtlasRegion_op_Equality_MicroEngine_Graphics_AtlasRegion_MicroEngine_Graphics_AtlasRegion_"></a> operator ==\(AtlasRegion, AtlasRegion\)

```csharp
public static bool operator ==(AtlasRegion left, AtlasRegion right)
```

#### Parameters

`left` [AtlasRegion](MicroEngine.Graphics.AtlasRegion.md)

`right` [AtlasRegion](MicroEngine.Graphics.AtlasRegion.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_AtlasRegion_op_Inequality_MicroEngine_Graphics_AtlasRegion_MicroEngine_Graphics_AtlasRegion_"></a> operator \!=\(AtlasRegion, AtlasRegion\)

```csharp
public static bool operator !=(AtlasRegion left, AtlasRegion right)
```

#### Parameters

`left` [AtlasRegion](MicroEngine.Graphics.AtlasRegion.md)

`right` [AtlasRegion](MicroEngine.Graphics.AtlasRegion.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
