# Interface IFont

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Text.dll  
Guide: [Graphics](../manual/rendering.md)  

Anything that measures and draws text at one size: the built-in [BitmapFont](MicroEngine.Graphics.BitmapFont.md) (at scale 1) and a
[ScalableFont](MicroEngine.Graphics.ScalableFont.md) at a size ([At](MicroEngine.Graphics.ScalableFont.md#MicroEngine_Graphics_ScalableFont_At_System_Single_)). UI widgets and layout code take an
[IFont](MicroEngine.Graphics.IFont.md), so the same code serves pixel-art and HD text.

```csharp
public interface IFont
```

#### Extension Methods

[FontExtensions.DrawCentered\(IFont, IRenderer, string, Vector2, Color\)](MicroEngine.Graphics.FontExtensions.md\#MicroEngine\_Graphics\_FontExtensions\_DrawCentered\_MicroEngine\_Graphics\_IFont\_MicroEngine\_Graphics\_IRenderer\_System\_String\_System\_Numerics\_Vector2\_MicroEngine\_Color\_), 
[FontExtensions.DrawCentered\(IFont, IRenderer, ReadOnlySpan<char\>, Vector2, Color\)](MicroEngine.Graphics.FontExtensions.md\#MicroEngine\_Graphics\_FontExtensions\_DrawCentered\_MicroEngine\_Graphics\_IFont\_MicroEngine\_Graphics\_IRenderer\_System\_ReadOnlySpan\_System\_Char\_\_System\_Numerics\_Vector2\_MicroEngine\_Color\_), 
[FontExtensions.DrawCentered\(IFont, IRenderer, InterpolatedText, Vector2, Color\)](MicroEngine.Graphics.FontExtensions.md\#MicroEngine\_Graphics\_FontExtensions\_DrawCentered\_MicroEngine\_Graphics\_IFont\_MicroEngine\_Graphics\_IRenderer\_MicroEngine\_Graphics\_InterpolatedText\_System\_Numerics\_Vector2\_MicroEngine\_Color\_), 
[FontExtensions.DrawRight\(IFont, IRenderer, string, Vector2, Color\)](MicroEngine.Graphics.FontExtensions.md\#MicroEngine\_Graphics\_FontExtensions\_DrawRight\_MicroEngine\_Graphics\_IFont\_MicroEngine\_Graphics\_IRenderer\_System\_String\_System\_Numerics\_Vector2\_MicroEngine\_Color\_), 
[FontExtensions.DrawRight\(IFont, IRenderer, ReadOnlySpan<char\>, Vector2, Color\)](MicroEngine.Graphics.FontExtensions.md\#MicroEngine\_Graphics\_FontExtensions\_DrawRight\_MicroEngine\_Graphics\_IFont\_MicroEngine\_Graphics\_IRenderer\_System\_ReadOnlySpan\_System\_Char\_\_System\_Numerics\_Vector2\_MicroEngine\_Color\_), 
[FontExtensions.DrawRight\(IFont, IRenderer, InterpolatedText, Vector2, Color\)](MicroEngine.Graphics.FontExtensions.md\#MicroEngine\_Graphics\_FontExtensions\_DrawRight\_MicroEngine\_Graphics\_IFont\_MicroEngine\_Graphics\_IRenderer\_MicroEngine\_Graphics\_InterpolatedText\_System\_Numerics\_Vector2\_MicroEngine\_Color\_), 
[FontExtensions.DrawShadowed\(IFont, IRenderer, string, Vector2, Color, Color, Vector2?\)](MicroEngine.Graphics.FontExtensions.md\#MicroEngine\_Graphics\_FontExtensions\_DrawShadowed\_MicroEngine\_Graphics\_IFont\_MicroEngine\_Graphics\_IRenderer\_System\_String\_System\_Numerics\_Vector2\_MicroEngine\_Color\_MicroEngine\_Color\_System\_Nullable\_System\_Numerics\_Vector2\_\_), 
[FontExtensions.DrawShadowed\(IFont, IRenderer, ReadOnlySpan<char\>, Vector2, Color, Color, Vector2?\)](MicroEngine.Graphics.FontExtensions.md\#MicroEngine\_Graphics\_FontExtensions\_DrawShadowed\_MicroEngine\_Graphics\_IFont\_MicroEngine\_Graphics\_IRenderer\_System\_ReadOnlySpan\_System\_Char\_\_System\_Numerics\_Vector2\_MicroEngine\_Color\_MicroEngine\_Color\_System\_Nullable\_System\_Numerics\_Vector2\_\_), 
[FontExtensions.DrawShadowed\(IFont, IRenderer, InterpolatedText, Vector2, Color, Color, Vector2?\)](MicroEngine.Graphics.FontExtensions.md\#MicroEngine\_Graphics\_FontExtensions\_DrawShadowed\_MicroEngine\_Graphics\_IFont\_MicroEngine\_Graphics\_IRenderer\_MicroEngine\_Graphics\_InterpolatedText\_System\_Numerics\_Vector2\_MicroEngine\_Color\_MicroEngine\_Color\_System\_Nullable\_System\_Numerics\_Vector2\_\_), 
[FontExtensions.MeasureWidth\(IFont, string\)](MicroEngine.Graphics.FontExtensions.md\#MicroEngine\_Graphics\_FontExtensions\_MeasureWidth\_MicroEngine\_Graphics\_IFont\_System\_String\_), 
[FontExtensions.MeasureWidth\(IFont, ReadOnlySpan<char\>\)](MicroEngine.Graphics.FontExtensions.md\#MicroEngine\_Graphics\_FontExtensions\_MeasureWidth\_MicroEngine\_Graphics\_IFont\_System\_ReadOnlySpan\_System\_Char\_\_), 
[FontExtensions.MeasureWidth\(IFont, InterpolatedText\)](MicroEngine.Graphics.FontExtensions.md\#MicroEngine\_Graphics\_FontExtensions\_MeasureWidth\_MicroEngine\_Graphics\_IFont\_MicroEngine\_Graphics\_InterpolatedText\_), 
[FontExtensions.Wrap\(IFont, string, float\)](MicroEngine.Graphics.FontExtensions.md\#MicroEngine\_Graphics\_FontExtensions\_Wrap\_MicroEngine\_Graphics\_IFont\_System\_String\_System\_Single\_)

## Properties

### <a id="MicroEngine_Graphics_IFont_LineHeight"></a> LineHeight

Distance between the tops of two lines, in canvas pixels.

```csharp
float LineHeight { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Graphics_IFont_Draw_MicroEngine_Graphics_IRenderer_System_String_System_Numerics_Vector2_MicroEngine_Color_"></a> Draw\(IRenderer, string, Vector2, Color\)

Draws <code class="paramref">text</code> with its top-left at <code class="paramref">position</code>, in <code class="paramref">color</code>.

```csharp
void Draw(IRenderer renderer, string text, Vector2 position, Color color)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`color` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Graphics_IFont_Draw_MicroEngine_Graphics_IRenderer_System_ReadOnlySpan_System_Char__System_Numerics_Vector2_MicroEngine_Color_"></a> Draw\(IRenderer, ReadOnlySpan<char\>, Vector2, Color\)

Draws <code class="paramref">text</code>, as [Draw](MicroEngine.Graphics.IFont.md#MicroEngine_Graphics_IFont_Draw_MicroEngine_Graphics_IRenderer_System_String_System_Numerics_Vector2_MicroEngine_Color_), for text that is not a
string. The engine's fonts draw it without allocating; a font of another kind that does not write this member
draws a copy of the text as a string.

```csharp
void Draw(IRenderer renderer, ReadOnlySpan<char> text, Vector2 position, Color color)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`text` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[char](https://learn.microsoft.com/dotnet/api/system.char)\>

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`color` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Graphics_IFont_Draw_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_InterpolatedText_System_Numerics_Vector2_MicroEngine_Color_"></a> Draw\(IRenderer, InterpolatedText, Vector2, Color\)

Draws an interpolated string (<code>$"{hp}/{max}"</code>), which is written into a reused buffer instead of a string:
a number drawn every frame makes no garbage.

```csharp
void Draw(IRenderer renderer, InterpolatedText text, Vector2 position, Color color)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`text` [InterpolatedText](MicroEngine.Graphics.InterpolatedText.md)

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`color` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Graphics_IFont_Measure_System_String_"></a> Measure\(string\)

Size of <code class="paramref">text</code> in canvas pixels: the widest line by the height of its lines. '\n' starts a new line.

```csharp
Vector2 Measure(string text)
```

#### Parameters

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Graphics_IFont_Measure_System_ReadOnlySpan_System_Char__"></a> Measure\(ReadOnlySpan<char\>\)

Size of <code class="paramref">text</code>, as [Measure](MicroEngine.Graphics.IFont.md#MicroEngine_Graphics_IFont_Measure_System_String_), for text that is not a string (a buffer a
number was written into). The engine's fonts measure it without allocating; a font of another kind that does not
write this member measures a copy of the text as a string.

```csharp
Vector2 Measure(ReadOnlySpan<char> text)
```

#### Parameters

`text` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[char](https://learn.microsoft.com/dotnet/api/system.char)\>

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Graphics_IFont_Measure_MicroEngine_Graphics_InterpolatedText_"></a> Measure\(InterpolatedText\)

Size of an interpolated string (<code>$"{score}"</code>), which is written into a reused buffer instead of a string.

```csharp
Vector2 Measure(InterpolatedText text)
```

#### Parameters

`text` [InterpolatedText](MicroEngine.Graphics.InterpolatedText.md)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

</div>
