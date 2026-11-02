# Class SizedFont

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

A [ScalableFont](MicroEngine.Graphics.ScalableFont.md) at one size: an [IFont](MicroEngine.Graphics.IFont.md) for widgets, layout and drawing.

```csharp
public sealed class SizedFont : IFont
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SizedFont](MicroEngine.Graphics.SizedFont.md)

#### Implements

[IFont](MicroEngine.Graphics.IFont.md)

#### Extension Methods

[FontExtensions.DrawCentered\(IFont, IRenderer, string, Vector2, Color\)](MicroEngine.Graphics.FontExtensions.md\#MicroEngine\_Graphics\_FontExtensions\_DrawCentered\_MicroEngine\_Graphics\_IFont\_MicroEngine\_Graphics\_IRenderer\_System\_String\_System\_Numerics\_Vector2\_MicroEngine\_Color\_), 
[FontExtensions.DrawShadowed\(IFont, IRenderer, string, Vector2, Color, Color, Vector2?\)](MicroEngine.Graphics.FontExtensions.md\#MicroEngine\_Graphics\_FontExtensions\_DrawShadowed\_MicroEngine\_Graphics\_IFont\_MicroEngine\_Graphics\_IRenderer\_System\_String\_System\_Numerics\_Vector2\_MicroEngine\_Color\_MicroEngine\_Color\_System\_Nullable\_System\_Numerics\_Vector2\_\_), 
[FontExtensions.MeasureWidth\(IFont, string\)](MicroEngine.Graphics.FontExtensions.md\#MicroEngine\_Graphics\_FontExtensions\_MeasureWidth\_MicroEngine\_Graphics\_IFont\_System\_String\_), 
[FontExtensions.Wrap\(IFont, string, float\)](MicroEngine.Graphics.FontExtensions.md\#MicroEngine\_Graphics\_FontExtensions\_Wrap\_MicroEngine\_Graphics\_IFont\_System\_String\_System\_Single\_)

## Properties

### <a id="MicroEngine_Graphics_SizedFont_Family"></a> Family

The font this size belongs to.

```csharp
public ScalableFont Family { get; }
```

#### Property Value

 [ScalableFont](MicroEngine.Graphics.ScalableFont.md)

### <a id="MicroEngine_Graphics_SizedFont_LineHeight"></a> LineHeight

Distance between the tops of two lines, in canvas pixels.

```csharp
public float LineHeight { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_SizedFont_Size"></a> Size

The size in pixels per em.

```csharp
public float Size { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Graphics_SizedFont_Draw_MicroEngine_Graphics_IRenderer_System_String_System_Numerics_Vector2_MicroEngine_Color_"></a> Draw\(IRenderer, string, Vector2, Color\)

Draws <code class="paramref">text</code> with its top-left at <code class="paramref">position</code>, in <code class="paramref">color</code>.

```csharp
public void Draw(IRenderer renderer, string text, Vector2 position, Color color)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`color` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Graphics_SizedFont_Measure_System_String_"></a> Measure\(string\)

Size of <code class="paramref">text</code> in canvas pixels: the widest line by the height of its lines. '\n' starts a new line.

```csharp
public Vector2 Measure(string text)
```

#### Parameters

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Graphics_SizedFont_Prepare_System_String_"></a> Prepare\(string\)

Rasterizes <code class="paramref">characters</code> at this size now (see [Prepare](MicroEngine.Graphics.ScalableFont.md#MicroEngine_Graphics_ScalableFont_Prepare_System_String_System_Single___)).

```csharp
public void Prepare(string characters)
```

#### Parameters

`characters` [string](https://learn.microsoft.com/dotnet/api/system.string)

</div>
