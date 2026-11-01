# Struct PanelStyle

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

Colours of a panel drawn with [Panel](MicroEngine.Graphics.RendererExtensions.md#MicroEngine_Graphics_RendererExtensions_Panel_MicroEngine_Graphics_IRenderer_MicroEngine_RectF_MicroEngine_Graphics_PanelStyle_).

```csharp
public readonly struct PanelStyle : IEquatable<PanelStyle>
```

#### Implements

[IEquatable<PanelStyle\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_PanelStyle__ctor_MicroEngine_Color_MicroEngine_Color_MicroEngine_Color_MicroEngine_Color_MicroEngine_Color_"></a> PanelStyle\(Color, Color, Color, Color, Color\)

Colours of a panel drawn with [Panel](MicroEngine.Graphics.RendererExtensions.md#MicroEngine_Graphics_RendererExtensions_Panel_MicroEngine_Graphics_IRenderer_MicroEngine_RectF_MicroEngine_Graphics_PanelStyle_).

```csharp
public PanelStyle(Color Fill, Color Border, Color Highlight = default, Color Shadow = default, Color InnerBorder = default)
```

#### Parameters

`Fill` [Color](MicroEngine.Color.md)

Inside colour.

`Border` [Color](MicroEngine.Color.md)

One-pixel outer border with cut corners.

`Highlight` [Color](MicroEngine.Color.md)

A one-pixel line along the top inside edge; transparent (the default) draws none.

`Shadow` [Color](MicroEngine.Color.md)

A drop shadow offset by 2 pixels; transparent draws none.

`InnerBorder` [Color](MicroEngine.Color.md)

A second outline 2 pixels inside the border; transparent draws none.

## Properties

### <a id="MicroEngine_Graphics_PanelStyle_Border"></a> Border

One-pixel outer border with cut corners.

```csharp
public Color Border { get; init; }
```

#### Property Value

 [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Graphics_PanelStyle_Fill"></a> Fill

Inside colour.

```csharp
public Color Fill { get; init; }
```

#### Property Value

 [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Graphics_PanelStyle_Highlight"></a> Highlight

A one-pixel line along the top inside edge; transparent (the default) draws none.

```csharp
public Color Highlight { get; init; }
```

#### Property Value

 [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Graphics_PanelStyle_InnerBorder"></a> InnerBorder

A second outline 2 pixels inside the border; transparent draws none.

```csharp
public Color InnerBorder { get; init; }
```

#### Property Value

 [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Graphics_PanelStyle_Shadow"></a> Shadow

A drop shadow offset by 2 pixels; transparent draws none.

```csharp
public Color Shadow { get; init; }
```

#### Property Value

 [Color](MicroEngine.Color.md)

## Methods

### <a id="MicroEngine_Graphics_PanelStyle_Deconstruct_MicroEngine_Color__MicroEngine_Color__MicroEngine_Color__MicroEngine_Color__MicroEngine_Color__"></a> Deconstruct\(out Color, out Color, out Color, out Color, out Color\)

```csharp
public void Deconstruct(out Color Fill, out Color Border, out Color Highlight, out Color Shadow, out Color InnerBorder)
```

#### Parameters

`Fill` [Color](MicroEngine.Color.md)

`Border` [Color](MicroEngine.Color.md)

`Highlight` [Color](MicroEngine.Color.md)

`Shadow` [Color](MicroEngine.Color.md)

`InnerBorder` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Graphics_PanelStyle_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_PanelStyle_Equals_MicroEngine_Graphics_PanelStyle_"></a> Equals\(PanelStyle\)

```csharp
public bool Equals(PanelStyle other)
```

#### Parameters

`other` [PanelStyle](MicroEngine.Graphics.PanelStyle.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_PanelStyle_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_PanelStyle_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_PanelStyle_op_Equality_MicroEngine_Graphics_PanelStyle_MicroEngine_Graphics_PanelStyle_"></a> operator ==\(PanelStyle, PanelStyle\)

```csharp
public static bool operator ==(PanelStyle left, PanelStyle right)
```

#### Parameters

`left` [PanelStyle](MicroEngine.Graphics.PanelStyle.md)

`right` [PanelStyle](MicroEngine.Graphics.PanelStyle.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_PanelStyle_op_Inequality_MicroEngine_Graphics_PanelStyle_MicroEngine_Graphics_PanelStyle_"></a> operator \!=\(PanelStyle, PanelStyle\)

```csharp
public static bool operator !=(PanelStyle left, PanelStyle right)
```

#### Parameters

`left` [PanelStyle](MicroEngine.Graphics.PanelStyle.md)

`right` [PanelStyle](MicroEngine.Graphics.PanelStyle.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
