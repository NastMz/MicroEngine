# Struct ShapedGlyph

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Text.Shaping.dll  
Guide: [Graphics](../manual/rendering.md)  

One glyph of a shaped line.

```csharp
public readonly struct ShapedGlyph : IEquatable<ShapedGlyph>
```

#### Implements

[IEquatable<ShapedGlyph\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_ShapedGlyph__ctor_System_Int32_System_Int32_System_Int32_System_Numerics_Vector2_System_Single_System_Single_"></a> ShapedGlyph\(int, int, int, Vector2, float, float\)

One glyph of a shaped line.

```csharp
public ShapedGlyph(int Face, int Glyph, int Cluster, Vector2 Position, float Pen, float Advance)
```

#### Parameters

`Face` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Which face of the font it comes from: 0 the main face, then the fallbacks in order.

`Glyph` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The glyph's index in that face (0 is the face's missing-glyph box).

`Cluster` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The index in the line's text of the first character the glyph shows; glyphs of one letter, or letters merged into one glyph, share it.

`Position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

The glyph's origin, relative to the line's top-left corner: its y is the baseline, moved by any offset the font gives (marks).

`Pen` [float](https://learn.microsoft.com/dotnet/api/system.single)

How far along the line, from its left edge, this glyph's advance starts.

`Advance` [float](https://learn.microsoft.com/dotnet/api/system.single)

How far the glyph moves the pen.

## Properties

### <a id="MicroEngine_Graphics_ShapedGlyph_Advance"></a> Advance

How far the glyph moves the pen.

```csharp
public float Advance { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_ShapedGlyph_Cluster"></a> Cluster

The index in the line's text of the first character the glyph shows; glyphs of one letter, or letters merged into one glyph, share it.

```csharp
public int Cluster { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_ShapedGlyph_Face"></a> Face

Which face of the font it comes from: 0 the main face, then the fallbacks in order.

```csharp
public int Face { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_ShapedGlyph_Glyph"></a> Glyph

The glyph's index in that face (0 is the face's missing-glyph box).

```csharp
public int Glyph { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_ShapedGlyph_Pen"></a> Pen

How far along the line, from its left edge, this glyph's advance starts.

```csharp
public float Pen { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_ShapedGlyph_Position"></a> Position

The glyph's origin, relative to the line's top-left corner: its y is the baseline, moved by any offset the font gives (marks).

```csharp
public Vector2 Position { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

## Methods

### <a id="MicroEngine_Graphics_ShapedGlyph_Deconstruct_System_Int32__System_Int32__System_Int32__System_Numerics_Vector2__System_Single__System_Single__"></a> Deconstruct\(out int, out int, out int, out Vector2, out float, out float\)

```csharp
public void Deconstruct(out int Face, out int Glyph, out int Cluster, out Vector2 Position, out float Pen, out float Advance)
```

#### Parameters

`Face` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Glyph` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Cluster` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`Pen` [float](https://learn.microsoft.com/dotnet/api/system.single)

`Advance` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_ShapedGlyph_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_ShapedGlyph_Equals_MicroEngine_Graphics_ShapedGlyph_"></a> Equals\(ShapedGlyph\)

```csharp
public bool Equals(ShapedGlyph other)
```

#### Parameters

`other` [ShapedGlyph](MicroEngine.Graphics.ShapedGlyph.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_ShapedGlyph_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_ShapedGlyph_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_ShapedGlyph_op_Equality_MicroEngine_Graphics_ShapedGlyph_MicroEngine_Graphics_ShapedGlyph_"></a> operator ==\(ShapedGlyph, ShapedGlyph\)

```csharp
public static bool operator ==(ShapedGlyph left, ShapedGlyph right)
```

#### Parameters

`left` [ShapedGlyph](MicroEngine.Graphics.ShapedGlyph.md)

`right` [ShapedGlyph](MicroEngine.Graphics.ShapedGlyph.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_ShapedGlyph_op_Inequality_MicroEngine_Graphics_ShapedGlyph_MicroEngine_Graphics_ShapedGlyph_"></a> operator \!=\(ShapedGlyph, ShapedGlyph\)

```csharp
public static bool operator !=(ShapedGlyph left, ShapedGlyph right)
```

#### Parameters

`left` [ShapedGlyph](MicroEngine.Graphics.ShapedGlyph.md)

`right` [ShapedGlyph](MicroEngine.Graphics.ShapedGlyph.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
