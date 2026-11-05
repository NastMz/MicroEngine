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

`renderer` IRenderer

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

`center` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`color` Color

### <a id="MicroEngine_Graphics_FontExtensions_DrawShadowed_MicroEngine_Graphics_IFont_MicroEngine_Graphics_IRenderer_System_String_System_Numerics_Vector2_MicroEngine_Color_MicroEngine_Color_System_Nullable_System_Numerics_Vector2__"></a> DrawShadowed\(IFont, IRenderer, string, Vector2, Color, Color, Vector2?\)

Draws <code class="paramref">text</code> over a copy of itself in <code class="paramref">shadow</code>, moved by <code class="paramref">offset</code> (one pixel down and right by default).

```csharp
public static void DrawShadowed(this IFont font, IRenderer renderer, string text, Vector2 position, Color color, Color shadow, Vector2? offset = null)
```

#### Parameters

`font` [IFont](MicroEngine.Graphics.IFont.md)

`renderer` IRenderer

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`color` Color

`shadow` Color

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
