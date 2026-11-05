# Class PromptText

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.UI](MicroEngine.UI.md)  
Assembly: MicroEngine.dll  
Guide: [UI widgets](../manual/ui.md)  

Draws text with action prompts as glyphs: <code>"Press {attack} to swing"</code> shows a key cap with Z on a keyboard, a
green A on an Xbox pad, a × button on a PlayStation pad and the touch button's label in a ring on a phone. Text that
does not name an action is drawn as with [Draw](MicroEngine.Graphics.BitmapFont.md#MicroEngine_Graphics_BitmapFont_Draw_MicroEngine_Graphics_IRenderer_System_String_System_Numerics_Vector2_MicroEngine_Color_System_Single_). Glyphs are as tall as a line of the font.

```csharp
public static class PromptText
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PromptText](MicroEngine.UI.PromptText.md)

## Methods

### <a id="MicroEngine_UI_PromptText_Draw_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_BitmapFont_MicroEngine_Input_ActionPrompts_System_String_System_Numerics_Vector2_MicroEngine_Color_System_Nullable_MicroEngine_Color__MicroEngine_UI_PromptGlyphStyle_"></a> Draw\(IRenderer, BitmapFont, ActionPrompts, string, Vector2, Color, Color?, PromptGlyphStyle?\)

Draws one line of <code class="paramref">template</code> with its top-left at <code class="paramref">position</code>.

```csharp
public static void Draw(IRenderer r, BitmapFont font, ActionPrompts prompts, string template, Vector2 position, Color color, Color? shadow = null, PromptGlyphStyle? style = null)
```

#### Parameters

`r` [IRenderer](MicroEngine.Graphics.IRenderer.md)

The renderer.

`font` [BitmapFont](MicroEngine.Graphics.BitmapFont.md)

The font of the text and of the glyph labels.

`prompts` [ActionPrompts](MicroEngine.Input.ActionPrompts.md)

Resolves the actions ([Prompts](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Prompts)).

`template` [string](https://learn.microsoft.com/dotnet/api/system.string)

Text with <code>{action}</code> names (see [Format](MicroEngine.Input.ActionPrompts.md#MicroEngine_Input_ActionPrompts_Format_System_String_System_Func_MicroEngine_Input_ActionPrompt_System_String__)).

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Top-left of the line, in virtual pixels.

`color` [Color](MicroEngine.Color.md)

Colour of the plain text.

`shadow` [Color](MicroEngine.Color.md)?

A 1px shadow under the plain text; null draws none.

`style` [PromptGlyphStyle](MicroEngine.UI.PromptGlyphStyle.md)?

Glyph colours ([Default](MicroEngine.UI.PromptGlyphStyle.md#MicroEngine_UI_PromptGlyphStyle_Default) when null).

### <a id="MicroEngine_UI_PromptText_DrawCentered_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_BitmapFont_MicroEngine_Input_ActionPrompts_System_String_System_Single_System_Single_MicroEngine_Color_System_Nullable_MicroEngine_Color__MicroEngine_UI_PromptGlyphStyle_"></a> DrawCentered\(IRenderer, BitmapFont, ActionPrompts, string, float, float, Color, Color?, PromptGlyphStyle?\)

Draws <code class="paramref">template</code> centred horizontally on <code class="paramref">centerX</code>.

```csharp
public static void DrawCentered(IRenderer r, BitmapFont font, ActionPrompts prompts, string template, float centerX, float y, Color color, Color? shadow = null, PromptGlyphStyle? style = null)
```

#### Parameters

`r` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`font` [BitmapFont](MicroEngine.Graphics.BitmapFont.md)

`prompts` [ActionPrompts](MicroEngine.Input.ActionPrompts.md)

`template` [string](https://learn.microsoft.com/dotnet/api/system.string)

`centerX` [float](https://learn.microsoft.com/dotnet/api/system.single)

`y` [float](https://learn.microsoft.com/dotnet/api/system.single)

`color` [Color](MicroEngine.Color.md)

`shadow` [Color](MicroEngine.Color.md)?

`style` [PromptGlyphStyle](MicroEngine.UI.PromptGlyphStyle.md)?

### <a id="MicroEngine_UI_PromptText_DrawGlyph_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_BitmapFont_MicroEngine_Input_ActionPrompt_System_Numerics_Vector2_MicroEngine_UI_PromptGlyphStyle_"></a> DrawGlyph\(IRenderer, BitmapFont, ActionPrompt, Vector2, PromptGlyphStyle?\)

Draws one prompt with its top-left at <code class="paramref">position</code>: a key cap for a key, a round button for a
gamepad face button (in its colour on Xbox and Nintendo pads), a pill for other gamepad controls, a ring for a
touch control, plain text for a label.

```csharp
public static void DrawGlyph(IRenderer r, BitmapFont font, ActionPrompt prompt, Vector2 position, PromptGlyphStyle? style = null)
```

#### Parameters

`r` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`font` [BitmapFont](MicroEngine.Graphics.BitmapFont.md)

`prompt` [ActionPrompt](MicroEngine.Input.ActionPrompt.md)

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`style` [PromptGlyphStyle](MicroEngine.UI.PromptGlyphStyle.md)?

### <a id="MicroEngine_UI_PromptText_GlyphWidth_MicroEngine_Graphics_BitmapFont_MicroEngine_Input_ActionPrompt_"></a> GlyphWidth\(BitmapFont, ActionPrompt\)

Width of one prompt's glyph in pixels.

```csharp
public static int GlyphWidth(BitmapFont font, ActionPrompt prompt)
```

#### Parameters

`font` [BitmapFont](MicroEngine.Graphics.BitmapFont.md)

`prompt` [ActionPrompt](MicroEngine.Input.ActionPrompt.md)

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_UI_PromptText_Measure_MicroEngine_Graphics_BitmapFont_MicroEngine_Input_ActionPrompts_System_String_"></a> Measure\(BitmapFont, ActionPrompts, string\)

Width of <code class="paramref">template</code> drawn by [Draw](MicroEngine.UI.PromptText.md#MicroEngine_UI_PromptText_Draw_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_BitmapFont_MicroEngine_Input_ActionPrompts_System_String_System_Numerics_Vector2_MicroEngine_Color_System_Nullable_MicroEngine_Color__MicroEngine_UI_PromptGlyphStyle_), in pixels (one line).

```csharp
public static int Measure(BitmapFont font, ActionPrompts prompts, string template)
```

#### Parameters

`font` [BitmapFont](MicroEngine.Graphics.BitmapFont.md)

`prompts` [ActionPrompts](MicroEngine.Input.ActionPrompts.md)

`template` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

</div>
