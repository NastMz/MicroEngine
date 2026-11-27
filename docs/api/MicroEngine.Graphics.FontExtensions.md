# Class FontExtensions

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Text.dll  
Guide: [Graphics](../manual/rendering.md)  

Layout and drawing helpers that work with any [IFont](MicroEngine.Graphics.IFont.md).

```csharp
public static class FontExtensions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[FontExtensions](MicroEngine.Graphics.FontExtensions.md)

## Methods

### <a id="MicroEngine_Graphics_FontExtensions_DrawCentered_MicroEngine_Graphics_IFont_MicroEngine_Graphics_IRenderer_System_String_System_Numerics_Vector2_MicroEngine_Color_"></a> DrawCentered\(IFont, IRenderer, string, Vector2, Color\)

Draws <code class="paramref">text</code> centred on <code class="paramref">center</code>, rounded to whole pixels.

```csharp
public static void DrawCentered(this IFont font, IRenderer renderer, string text, Vector2 center, Color color)
```

#### Parameters

`font` [IFont](MicroEngine.Graphics.IFont.md)

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

`center` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`color` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Graphics_FontExtensions_DrawCentered_MicroEngine_Graphics_IFont_MicroEngine_Graphics_IRenderer_System_ReadOnlySpan_System_Char__System_Numerics_Vector2_MicroEngine_Color_"></a> DrawCentered\(IFont, IRenderer, ReadOnlySpan<char\>, Vector2, Color\)

Draws <code class="paramref">text</code> centred on <code class="paramref">center</code>, rounded to whole pixels.

```csharp
public static void DrawCentered(this IFont font, IRenderer renderer, ReadOnlySpan<char> text, Vector2 center, Color color)
```

#### Parameters

`font` [IFont](MicroEngine.Graphics.IFont.md)

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`text` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[char](https://learn.microsoft.com/dotnet/api/system.char)\>

`center` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`color` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Graphics_FontExtensions_DrawCentered_MicroEngine_Graphics_IFont_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_InterpolatedText_System_Numerics_Vector2_MicroEngine_Color_"></a> DrawCentered\(IFont, IRenderer, InterpolatedText, Vector2, Color\)

Draws an interpolated string (<code>$"{minutes}:{seconds:00}"</code>) centred on <code class="paramref">center</code>, without making a string.

```csharp
public static void DrawCentered(this IFont font, IRenderer renderer, InterpolatedText text, Vector2 center, Color color)
```

#### Parameters

`font` [IFont](MicroEngine.Graphics.IFont.md)

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`text` [InterpolatedText](MicroEngine.Graphics.InterpolatedText.md)

`center` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`color` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Graphics_FontExtensions_DrawRight_MicroEngine_Graphics_IFont_MicroEngine_Graphics_IRenderer_System_String_System_Numerics_Vector2_MicroEngine_Color_"></a> DrawRight\(IFont, IRenderer, string, Vector2, Color\)

Draws <code class="paramref">text</code> so that it ends at <code class="paramref">topRight</code>: a score or a price against the
right edge of a panel, whatever its number of digits. Rounded to whole pixels.

```csharp
public static void DrawRight(this IFont font, IRenderer renderer, string text, Vector2 topRight, Color color)
```

#### Parameters

`font` [IFont](MicroEngine.Graphics.IFont.md)

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

`topRight` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`color` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Graphics_FontExtensions_DrawRight_MicroEngine_Graphics_IFont_MicroEngine_Graphics_IRenderer_System_ReadOnlySpan_System_Char__System_Numerics_Vector2_MicroEngine_Color_"></a> DrawRight\(IFont, IRenderer, ReadOnlySpan<char\>, Vector2, Color\)

Draws <code class="paramref">text</code> so that it ends at <code class="paramref">topRight</code>: a score or a price against the
right edge of a panel, whatever its number of digits. Rounded to whole pixels.

```csharp
public static void DrawRight(this IFont font, IRenderer renderer, ReadOnlySpan<char> text, Vector2 topRight, Color color)
```

#### Parameters

`font` [IFont](MicroEngine.Graphics.IFont.md)

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`text` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[char](https://learn.microsoft.com/dotnet/api/system.char)\>

`topRight` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`color` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Graphics_FontExtensions_DrawRight_MicroEngine_Graphics_IFont_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_InterpolatedText_System_Numerics_Vector2_MicroEngine_Color_"></a> DrawRight\(IFont, IRenderer, InterpolatedText, Vector2, Color\)

Draws an interpolated string (<code>$"{score}"</code>) so that it ends at <code class="paramref">topRight</code>, without making a string.

```csharp
public static void DrawRight(this IFont font, IRenderer renderer, InterpolatedText text, Vector2 topRight, Color color)
```

#### Parameters

`font` [IFont](MicroEngine.Graphics.IFont.md)

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`text` [InterpolatedText](MicroEngine.Graphics.InterpolatedText.md)

`topRight` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`color` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Graphics_FontExtensions_DrawShadowed_MicroEngine_Graphics_IFont_MicroEngine_Graphics_IRenderer_System_String_System_Numerics_Vector2_MicroEngine_Color_MicroEngine_Color_System_Nullable_System_Numerics_Vector2__"></a> DrawShadowed\(IFont, IRenderer, string, Vector2, Color, Color, Vector2?\)

Draws <code class="paramref">text</code> over a copy of itself in <code class="paramref">shadow</code>, moved by <code class="paramref">offset</code> (one pixel down and right by default).

```csharp
public static void DrawShadowed(this IFont font, IRenderer renderer, string text, Vector2 position, Color color, Color shadow, Vector2? offset = null)
```

#### Parameters

`font` [IFont](MicroEngine.Graphics.IFont.md)

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`color` [Color](MicroEngine.Color.md)

`shadow` [Color](MicroEngine.Color.md)

`offset` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)?

### <a id="MicroEngine_Graphics_FontExtensions_DrawShadowed_MicroEngine_Graphics_IFont_MicroEngine_Graphics_IRenderer_System_ReadOnlySpan_System_Char__System_Numerics_Vector2_MicroEngine_Color_MicroEngine_Color_System_Nullable_System_Numerics_Vector2__"></a> DrawShadowed\(IFont, IRenderer, ReadOnlySpan<char\>, Vector2, Color, Color, Vector2?\)

Draws <code class="paramref">text</code> over a copy of itself in <code class="paramref">shadow</code>, moved by <code class="paramref">offset</code> (one pixel down and right by default).

```csharp
public static void DrawShadowed(this IFont font, IRenderer renderer, ReadOnlySpan<char> text, Vector2 position, Color color, Color shadow, Vector2? offset = null)
```

#### Parameters

`font` [IFont](MicroEngine.Graphics.IFont.md)

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`text` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[char](https://learn.microsoft.com/dotnet/api/system.char)\>

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`color` [Color](MicroEngine.Color.md)

`shadow` [Color](MicroEngine.Color.md)

`offset` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)?

### <a id="MicroEngine_Graphics_FontExtensions_DrawShadowed_MicroEngine_Graphics_IFont_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_InterpolatedText_System_Numerics_Vector2_MicroEngine_Color_MicroEngine_Color_System_Nullable_System_Numerics_Vector2__"></a> DrawShadowed\(IFont, IRenderer, InterpolatedText, Vector2, Color, Color, Vector2?\)

Draws an interpolated string (<code>$"{coins}"</code>) over a copy of itself in <code class="paramref">shadow</code>, without making a string.

```csharp
public static void DrawShadowed(this IFont font, IRenderer renderer, InterpolatedText text, Vector2 position, Color color, Color shadow, Vector2? offset = null)
```

#### Parameters

`font` [IFont](MicroEngine.Graphics.IFont.md)

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`text` [InterpolatedText](MicroEngine.Graphics.InterpolatedText.md)

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`color` [Color](MicroEngine.Color.md)

`shadow` [Color](MicroEngine.Color.md)

`offset` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)?

### <a id="MicroEngine_Graphics_FontExtensions_MeasureWidth_MicroEngine_Graphics_IFont_System_String_"></a> MeasureWidth\(IFont, string\)

Width of the widest line of <code class="paramref">text</code> in canvas pixels.

```csharp
public static float MeasureWidth(this IFont font, string text)
```

#### Parameters

`font` [IFont](MicroEngine.Graphics.IFont.md)

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_FontExtensions_MeasureWidth_MicroEngine_Graphics_IFont_System_ReadOnlySpan_System_Char__"></a> MeasureWidth\(IFont, ReadOnlySpan<char\>\)

Width of the widest line of <code class="paramref">text</code> in canvas pixels.

```csharp
public static float MeasureWidth(this IFont font, ReadOnlySpan<char> text)
```

#### Parameters

`font` [IFont](MicroEngine.Graphics.IFont.md)

`text` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[char](https://learn.microsoft.com/dotnet/api/system.char)\>

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_FontExtensions_MeasureWidth_MicroEngine_Graphics_IFont_MicroEngine_Graphics_InterpolatedText_"></a> MeasureWidth\(IFont, InterpolatedText\)

Width of an interpolated string (<code>$"{score}"</code>), measured without making a string.

```csharp
public static float MeasureWidth(this IFont font, InterpolatedText text)
```

#### Parameters

`font` [IFont](MicroEngine.Graphics.IFont.md)

`text` [InterpolatedText](MicroEngine.Graphics.InterpolatedText.md)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_FontExtensions_Wrap_MicroEngine_Graphics_IFont_System_String_System_Single_"></a> Wrap\(IFont, string, float\)

Greedy word wrap that respects explicit newlines: no line is wider than <code class="paramref">maxWidth</code>, and a word wider
than that on its own is broken between characters.

```csharp
public static IReadOnlyList<string> Wrap(this IFont font, string text, float maxWidth)
```

#### Parameters

`font` [IFont](MicroEngine.Graphics.IFont.md)

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

`maxWidth` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

</div>
