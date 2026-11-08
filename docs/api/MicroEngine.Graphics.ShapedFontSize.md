# Class ShapedFontSize

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Text.Shaping.dll  
Guide: [Graphics](../manual/rendering.md)  

A [ShapedFont](MicroEngine.Graphics.ShapedFont.md) at one size: an [IFont](MicroEngine.Graphics.IFont.md) for widgets, layout and drawing.

```csharp
public sealed class ShapedFontSize : IFont
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ShapedFontSize](MicroEngine.Graphics.ShapedFontSize.md)

#### Implements

[IFont](MicroEngine.Graphics.IFont.md)

## Properties

### <a id="MicroEngine_Graphics_ShapedFontSize_Baseline"></a> Baseline

Distance from the top of a line to its baseline, in canvas pixels.

```csharp
public float Baseline { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_ShapedFontSize_Family"></a> Family

The font this size belongs to.

```csharp
public ShapedFont Family { get; }
```

#### Property Value

 [ShapedFont](MicroEngine.Graphics.ShapedFont.md)

### <a id="MicroEngine_Graphics_ShapedFontSize_LineHeight"></a> LineHeight

Distance between the tops of two lines, in canvas pixels.

```csharp
public float LineHeight { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_ShapedFontSize_Size"></a> Size

The size, as for [At](MicroEngine.Graphics.ShapedFont.md#MicroEngine_Graphics_ShapedFont_At_System_Single_).

```csharp
public float Size { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Graphics_ShapedFontSize_Draw_MicroEngine_Graphics_IRenderer_System_String_System_Numerics_Vector2_MicroEngine_Color_"></a> Draw\(IRenderer, string, Vector2, Color\)

Draws <code class="paramref">text</code> with its top-left at <code class="paramref">position</code>, in <code class="paramref">color</code>.

```csharp
public void Draw(IRenderer renderer, string text, Vector2 position, Color color)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`color` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Graphics_ShapedFontSize_IsRightToLeft_System_String_"></a> IsRightToLeft\(string\)

Whether <code class="paramref">line</code> reads right to left: its first letter with a direction is Hebrew, Arabic or
another right-to-left script. Align such lines to the right.

```csharp
public bool IsRightToLeft(string line)
```

#### Parameters

`line` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_ShapedFontSize_Measure_System_String_"></a> Measure\(string\)

Size of <code class="paramref">text</code> in canvas pixels: the widest line by the height of its lines. '\n' starts a new line.

```csharp
public Vector2 Measure(string text)
```

#### Parameters

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Graphics_ShapedFontSize_Prepare_System_String_"></a> Prepare\(string\)

Shapes and rasterizes <code class="paramref">text</code> now, so drawing it later costs no font work.

```csharp
public void Prepare(string text)
```

#### Parameters

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Graphics_ShapedFontSize_Shape_System_String_"></a> Shape\(string\)

The glyphs of one line of <code class="paramref">line</code> (no '\n') in the order they are drawn, left to right, with
where each sits: for effects of a game's own (a wave, a reveal that follows the reading order through
[Cluster](MicroEngine.Graphics.ShapedGlyph.md#MicroEngine_Graphics_ShapedGlyph_Cluster)).

```csharp
public IReadOnlyList<ShapedGlyph> Shape(string line)
```

#### Parameters

`line` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[ShapedGlyph](MicroEngine.Graphics.ShapedGlyph.md)\>

</div>
