# Struct NineSlice

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

An image that stretches to any size without distorting its frame: the part of [Source](MicroEngine.Graphics.NineSlice.md#MicroEngine_Graphics_NineSlice_Source) within
[Left](MicroEngine.Graphics.NineSlice.md#MicroEngine_Graphics_NineSlice_Left), [Top](MicroEngine.Graphics.NineSlice.md#MicroEngine_Graphics_NineSlice_Top), [Right](MicroEngine.Graphics.NineSlice.md#MicroEngine_Graphics_NineSlice_Right) and [Bottom](MicroEngine.Graphics.NineSlice.md#MicroEngine_Graphics_NineSlice_Bottom) pixels of its sides is the
border, drawn at its size at the corners and stretched along the edges; the rest is the centre, stretched to fill.
Draw one with [DrawNineSlice](MicroEngine.Graphics.RendererExtensions.md#MicroEngine_Graphics_RendererExtensions_DrawNineSlice_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_NineSlice_MicroEngine_RectF_System_Nullable_MicroEngine_Color__), or set it as a [Image](MicroEngine.Graphics.PanelStyle.md#MicroEngine_Graphics_PanelStyle_Image).

```csharp
public readonly struct NineSlice : IEquatable<NineSlice>
```

#### Implements

[IEquatable<NineSlice\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_NineSlice__ctor_MicroEngine_Graphics_Texture_MicroEngine_RectF_System_Single_System_Single_System_Single_System_Single_"></a> NineSlice\(Texture, RectF, float, float, float, float\)

An image that stretches to any size without distorting its frame: the part of [Source](MicroEngine.Graphics.NineSlice.md#MicroEngine_Graphics_NineSlice_Source) within
[Left](MicroEngine.Graphics.NineSlice.md#MicroEngine_Graphics_NineSlice_Left), [Top](MicroEngine.Graphics.NineSlice.md#MicroEngine_Graphics_NineSlice_Top), [Right](MicroEngine.Graphics.NineSlice.md#MicroEngine_Graphics_NineSlice_Right) and [Bottom](MicroEngine.Graphics.NineSlice.md#MicroEngine_Graphics_NineSlice_Bottom) pixels of its sides is the
border, drawn at its size at the corners and stretched along the edges; the rest is the centre, stretched to fill.
Draw one with [DrawNineSlice](MicroEngine.Graphics.RendererExtensions.md#MicroEngine_Graphics_RendererExtensions_DrawNineSlice_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_NineSlice_MicroEngine_RectF_System_Nullable_MicroEngine_Color__), or set it as a [Image](MicroEngine.Graphics.PanelStyle.md#MicroEngine_Graphics_PanelStyle_Image).

```csharp
public NineSlice(Texture Texture, RectF Source, float Left, float Top, float Right, float Bottom)
```

#### Parameters

`Texture` [Texture](MicroEngine.Graphics.Texture.md)

The texture that holds the image.

`Source` [RectF](MicroEngine.RectF.md)

The image's rectangle in the texture, in pixels.

`Left` [float](https://learn.microsoft.com/dotnet/api/system.single)

Width of the left border, in source pixels.

`Top` [float](https://learn.microsoft.com/dotnet/api/system.single)

Height of the top border, in source pixels.

`Right` [float](https://learn.microsoft.com/dotnet/api/system.single)

Width of the right border, in source pixels.

`Bottom` [float](https://learn.microsoft.com/dotnet/api/system.single)

Height of the bottom border, in source pixels.

### <a id="MicroEngine_Graphics_NineSlice__ctor_MicroEngine_Graphics_Texture_MicroEngine_RectF_System_Single_"></a> NineSlice\(Texture, RectF, float\)

An image with a border of <code class="paramref">border</code> pixels on every side.

```csharp
public NineSlice(Texture texture, RectF source, float border)
```

#### Parameters

`texture` [Texture](MicroEngine.Graphics.Texture.md)

`source` [RectF](MicroEngine.RectF.md)

`border` [float](https://learn.microsoft.com/dotnet/api/system.single)

## Properties

### <a id="MicroEngine_Graphics_NineSlice_Bottom"></a> Bottom

Height of the bottom border, in source pixels.

```csharp
public float Bottom { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_NineSlice_Left"></a> Left

Width of the left border, in source pixels.

```csharp
public float Left { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_NineSlice_Right"></a> Right

Width of the right border, in source pixels.

```csharp
public float Right { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_NineSlice_Scale"></a> Scale

How many destination units one source pixel of the border takes (1 by default); 2 draws the frame twice as thick.

```csharp
public float Scale { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_NineSlice_Source"></a> Source

The image's rectangle in the texture, in pixels.

```csharp
public RectF Source { get; init; }
```

#### Property Value

 [RectF](MicroEngine.RectF.md)

### <a id="MicroEngine_Graphics_NineSlice_Texture"></a> Texture

The texture that holds the image.

```csharp
public Texture Texture { get; init; }
```

#### Property Value

 [Texture](MicroEngine.Graphics.Texture.md)

### <a id="MicroEngine_Graphics_NineSlice_Top"></a> Top

Height of the top border, in source pixels.

```csharp
public float Top { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Graphics_NineSlice_Deconstruct_MicroEngine_Graphics_Texture__MicroEngine_RectF__System_Single__System_Single__System_Single__System_Single__"></a> Deconstruct\(out Texture, out RectF, out float, out float, out float, out float\)

```csharp
public void Deconstruct(out Texture Texture, out RectF Source, out float Left, out float Top, out float Right, out float Bottom)
```

#### Parameters

`Texture` [Texture](MicroEngine.Graphics.Texture.md)

`Source` [RectF](MicroEngine.RectF.md)

`Left` [float](https://learn.microsoft.com/dotnet/api/system.single)

`Top` [float](https://learn.microsoft.com/dotnet/api/system.single)

`Right` [float](https://learn.microsoft.com/dotnet/api/system.single)

`Bottom` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_NineSlice_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_NineSlice_Equals_MicroEngine_Graphics_NineSlice_"></a> Equals\(NineSlice\)

```csharp
public bool Equals(NineSlice other)
```

#### Parameters

`other` [NineSlice](MicroEngine.Graphics.NineSlice.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_NineSlice_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_NineSlice_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_NineSlice_op_Equality_MicroEngine_Graphics_NineSlice_MicroEngine_Graphics_NineSlice_"></a> operator ==\(NineSlice, NineSlice\)

```csharp
public static bool operator ==(NineSlice left, NineSlice right)
```

#### Parameters

`left` [NineSlice](MicroEngine.Graphics.NineSlice.md)

`right` [NineSlice](MicroEngine.Graphics.NineSlice.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_NineSlice_op_Inequality_MicroEngine_Graphics_NineSlice_MicroEngine_Graphics_NineSlice_"></a> operator \!=\(NineSlice, NineSlice\)

```csharp
public static bool operator !=(NineSlice left, NineSlice right)
```

#### Parameters

`left` [NineSlice](MicroEngine.Graphics.NineSlice.md)

`right` [NineSlice](MicroEngine.Graphics.NineSlice.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
