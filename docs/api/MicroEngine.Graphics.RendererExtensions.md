# Class RendererExtensions

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

Drawing helpers built on [IRenderer](MicroEngine.Graphics.IRenderer.md): sprites, outlines, ellipses, gradients and panels.

```csharp
public static class RendererExtensions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[RendererExtensions](MicroEngine.Graphics.RendererExtensions.md)

## Methods

### <a id="MicroEngine_Graphics_RendererExtensions_Draw_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_Sprite_System_Numerics_Vector2_System_Nullable_MicroEngine_Color__System_Boolean_"></a> Draw\(IRenderer, Sprite, Vector2, Color?, bool\)

Draws a sprite so its [Origin](MicroEngine.Graphics.Sprite.md#MicroEngine_Graphics_Sprite_Origin) lands on <code class="paramref">position</code>.

```csharp
public static void Draw(this IRenderer r, Sprite sprite, Vector2 position, Color? tint = null, bool flipX = false)
```

#### Parameters

`r` [IRenderer](MicroEngine.Graphics.IRenderer.md)

The renderer.

`sprite` [Sprite](MicroEngine.Graphics.Sprite.md)

The sprite.

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Where the sprite's origin goes, in virtual (or camera world) pixels.

`tint` Color?

Colour multiplier; null for white (unchanged).

`flipX` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Mirrors the sprite horizontally; the origin is mirrored too, so the sprite turns around its anchor.

### <a id="MicroEngine_Graphics_RendererExtensions_Draw_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_Texture_System_Numerics_Vector2_System_Nullable_MicroEngine_Color__"></a> Draw\(IRenderer, Texture, Vector2, Color?\)

Draws a whole texture with its top-left at <code class="paramref">position</code>.

```csharp
public static void Draw(this IRenderer r, Texture texture, Vector2 position, Color? tint = null)
```

#### Parameters

`r` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`texture` [Texture](MicroEngine.Graphics.Texture.md)

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`tint` Color?

### <a id="MicroEngine_Graphics_RendererExtensions_DrawPivoted_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_Sprite_System_Numerics_Vector2_System_Single_System_Nullable_System_Numerics_Vector2__System_Nullable_MicroEngine_Color__System_Boolean_"></a> DrawPivoted\(IRenderer, Sprite, Vector2, float, Vector2?, Color?, bool\)

Draws a sprite so its origin lands on <code class="paramref">pivot</code>, rotating and scaling around that
pivot. Use it for squash/stretch, spinning items and anything that must stay anchored at the feet.

```csharp
public static void DrawPivoted(this IRenderer r, Sprite sprite, Vector2 pivot, float rotation = 0, Vector2? scale = null, Color? tint = null, bool flipX = false)
```

#### Parameters

`r` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`sprite` [Sprite](MicroEngine.Graphics.Sprite.md)

`pivot` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`rotation` [float](https://learn.microsoft.com/dotnet/api/system.single)

`scale` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)?

`tint` Color?

`flipX` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_RendererExtensions_DrawRect_MicroEngine_Graphics_IRenderer_MicroEngine_RectF_MicroEngine_Color_System_Single_"></a> DrawRect\(IRenderer, RectF, Color, float\)

Draws the outline of <code class="paramref">rect</code>, <code class="paramref">thickness</code> pixels wide, inside the rectangle.

```csharp
public static void DrawRect(this IRenderer r, RectF rect, Color color, float thickness = 1)
```

#### Parameters

`r` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`rect` RectF

`color` Color

`thickness` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_RendererExtensions_FillEllipse_MicroEngine_Graphics_IRenderer_System_Numerics_Vector2_System_Single_System_Single_MicroEngine_Color_"></a> FillEllipse\(IRenderer, Vector2, float, float, Color\)

Filled ellipse built from horizontal spans; handy for soft drop shadows.

```csharp
public static void FillEllipse(this IRenderer r, Vector2 center, float radiusX, float radiusY, Color color)
```

#### Parameters

`r` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`center` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`radiusX` [float](https://learn.microsoft.com/dotnet/api/system.single)

`radiusY` [float](https://learn.microsoft.com/dotnet/api/system.single)

`color` Color

### <a id="MicroEngine_Graphics_RendererExtensions_FillGradient_MicroEngine_Graphics_IRenderer_MicroEngine_RectF_MicroEngine_Color_MicroEngine_Color_System_Int32_"></a> FillGradient\(IRenderer, RectF, Color, Color, int\)

Vertical gradient approximated with horizontal bands (cheap and pixel-crisp).

```csharp
public static void FillGradient(this IRenderer r, RectF rect, Color top, Color bottom, int bands = 0)
```

#### Parameters

`r` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`rect` RectF

`top` Color

`bottom` Color

`bands` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_RendererExtensions_Panel_MicroEngine_Graphics_IRenderer_MicroEngine_RectF_MicroEngine_Graphics_PanelStyle_"></a> Panel\(IRenderer, RectF, PanelStyle\)

Draws a rounded "pixel" panel: border, fill and an inner highlight line.

```csharp
public static void Panel(this IRenderer r, RectF rect, PanelStyle style)
```

#### Parameters

`r` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`rect` RectF

`style` [PanelStyle](MicroEngine.Graphics.PanelStyle.md)

</div>
