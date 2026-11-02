# Class BitmapFont

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.dll  
Guide: [Text](../manual/text.md)  

A proportional pixel font. The built-in face ("Micro 5x7") is authored in code so every game
has crisp, license-free text without loading files. Glyph widths are derived by trimming
empty columns, so narrow letters like 'i' take less space.

```csharp
public sealed class BitmapFont : IFont, IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[BitmapFont](MicroEngine.Graphics.BitmapFont.md)

#### Implements

[IFont](MicroEngine.Graphics.IFont.md), 
[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

#### Extension Methods

[FontExtensions.DrawCentered\(IFont, IRenderer, string, Vector2, Color\)](MicroEngine.Graphics.FontExtensions.md\#MicroEngine\_Graphics\_FontExtensions\_DrawCentered\_MicroEngine\_Graphics\_IFont\_MicroEngine\_Graphics\_IRenderer\_System\_String\_System\_Numerics\_Vector2\_MicroEngine\_Color\_), 
[FontExtensions.DrawShadowed\(IFont, IRenderer, string, Vector2, Color, Color, Vector2?\)](MicroEngine.Graphics.FontExtensions.md\#MicroEngine\_Graphics\_FontExtensions\_DrawShadowed\_MicroEngine\_Graphics\_IFont\_MicroEngine\_Graphics\_IRenderer\_System\_String\_System\_Numerics\_Vector2\_MicroEngine\_Color\_MicroEngine\_Color\_System\_Nullable\_System\_Numerics\_Vector2\_\_), 
[FontExtensions.MeasureWidth\(IFont, string\)](MicroEngine.Graphics.FontExtensions.md\#MicroEngine\_Graphics\_FontExtensions\_MeasureWidth\_MicroEngine\_Graphics\_IFont\_System\_String\_), 
[FontExtensions.Wrap\(IFont, string, float\)](MicroEngine.Graphics.FontExtensions.md\#MicroEngine\_Graphics\_FontExtensions\_Wrap\_MicroEngine\_Graphics\_IFont\_System\_String\_System\_Single\_)

## Remarks

Every game has the built-in face in [Font](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Font). It is an [IFont](MicroEngine.Graphics.IFont.md) (at scale 1), like a
[ScalableFont](MicroEngine.Graphics.ScalableFont.md) at a size, so widgets and layout code take either. Text is drawn in white glyphs multiplied by the
colour given, so any colour works. Characters without a glyph are drawn as '?'; '\n' starts a new line.

## Properties

### <a id="MicroEngine_Graphics_BitmapFont_CapHeight"></a> CapHeight

Height of capital letters above the baseline.

```csharp
public int CapHeight { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_BitmapFont_LineHeight"></a> LineHeight

Distance between the tops of two lines, in pixels (10 for the built-in face).

```csharp
public int LineHeight { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_BitmapFont_Spacing"></a> Spacing

Pixels between two characters (1).

```csharp
public int Spacing { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Graphics_BitmapFont_Advance_System_Char_"></a> Advance\(char\)

Horizontal distance from <code class="paramref">c</code> to the next character, spacing included, in unscaled pixels.

```csharp
public int Advance(char c)
```

#### Parameters

`c` [char](https://learn.microsoft.com/dotnet/api/system.char)

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_BitmapFont_CreateDefault_MicroEngine_Graphics_IGraphicsDevice_"></a> CreateDefault\(IGraphicsDevice?\)

Creates the built-in face. Pass null for a measurement-only font (tests, layout).

```csharp
public static BitmapFont CreateDefault(IGraphicsDevice? device)
```

#### Parameters

`device` [IGraphicsDevice](MicroEngine.Graphics.IGraphicsDevice.md)?

#### Returns

 [BitmapFont](MicroEngine.Graphics.BitmapFont.md)

### <a id="MicroEngine_Graphics_BitmapFont_Dispose"></a> Dispose\(\)

Releases the font's texture. The engine disposes [Font](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Font) itself.

```csharp
public void Dispose()
```

### <a id="MicroEngine_Graphics_BitmapFont_Draw_MicroEngine_Graphics_IRenderer_System_String_System_Numerics_Vector2_MicroEngine_Color_System_Single_"></a> Draw\(IRenderer, string, Vector2, Color, float\)

Draws <code class="paramref">text</code> with its top-left at <code class="paramref">position</code>.

```csharp
public void Draw(IRenderer renderer, string text, Vector2 position, Color color, float scale = 1)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

The renderer.

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

The text; '\n' starts a new line.

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Top-left of the first line, in virtual (or camera world) pixels.

`color` [Color](MicroEngine.Color.md)

Text colour.

`scale` [float](https://learn.microsoft.com/dotnet/api/system.single)

Size multiplier; whole numbers keep the pixels crisp.

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The font was created without a graphics device.

### <a id="MicroEngine_Graphics_BitmapFont_DrawCentered_MicroEngine_Graphics_IRenderer_System_String_System_Numerics_Vector2_MicroEngine_Color_System_Single_System_Nullable_MicroEngine_Color__"></a> DrawCentered\(IRenderer, string, Vector2, Color, float, Color?\)

Draws <code class="paramref">text</code> centred on <code class="paramref">center</code>, rounded to whole pixels, with an optional 1px drop shadow.

```csharp
public void DrawCentered(IRenderer renderer, string text, Vector2 center, Color color, float scale = 1, Color? shadow = null)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

`center` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`color` [Color](MicroEngine.Color.md)

`scale` [float](https://learn.microsoft.com/dotnet/api/system.single)

`shadow` [Color](MicroEngine.Color.md)?

### <a id="MicroEngine_Graphics_BitmapFont_DrawOutlined_MicroEngine_Graphics_IRenderer_System_String_System_Numerics_Vector2_MicroEngine_Color_MicroEngine_Color_System_Single_"></a> DrawOutlined\(IRenderer, string, Vector2, Color, Color, float\)

Draws text with a full 1px outline (8 directions).

```csharp
public void DrawOutlined(IRenderer renderer, string text, Vector2 position, Color color, Color outline, float scale = 1)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`color` [Color](MicroEngine.Color.md)

`outline` [Color](MicroEngine.Color.md)

`scale` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_BitmapFont_DrawShadowed_MicroEngine_Graphics_IRenderer_System_String_System_Numerics_Vector2_MicroEngine_Color_MicroEngine_Color_System_Single_"></a> DrawShadowed\(IRenderer, string, Vector2, Color, Color, float\)

Draws text with a 1px drop shadow, the most legible style over busy pixel art.

```csharp
public void DrawShadowed(IRenderer renderer, string text, Vector2 position, Color color, Color shadow, float scale = 1)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`color` [Color](MicroEngine.Color.md)

`shadow` [Color](MicroEngine.Color.md)

`scale` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_BitmapFont_Measure_System_String_System_Single_"></a> Measure\(string, float\)

Size of <code class="paramref">text</code> in pixels at <code class="paramref">scale</code>: the widest line by the height of its lines (8 pixels for one line at scale 1).

```csharp
public Vector2 Measure(string text, float scale = 1)
```

#### Parameters

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

`scale` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Graphics_BitmapFont_MeasureWidth_System_String_"></a> MeasureWidth\(string\)

Width in pixels of the widest line (unscaled).

```csharp
public int MeasureWidth(string text)
```

#### Parameters

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_BitmapFont_Wrap_System_String_System_Int32_"></a> Wrap\(string, int\)

Greedy word wrap that respects explicit newlines. A word wider than <code class="paramref">maxWidth</code> on its own (a long
name, a URL) is broken between characters, so no line is wider than <code class="paramref">maxWidth</code>.

```csharp
public IReadOnlyList<string> Wrap(string text, int maxWidth)
```

#### Parameters

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

`maxWidth` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

</div>
