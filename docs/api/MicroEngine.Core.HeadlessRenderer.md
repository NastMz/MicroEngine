# Class HeadlessRenderer

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

Counts draw calls and tracks camera balance so tests can assert rendering ran correctly. Created with a graphics
device, it also rasterizes: it records each draw as a line of text ([Commands](MicroEngine.Core.HeadlessRenderer.md#MicroEngine_Core_HeadlessRenderer_Commands)) and draws it on
[Frame](MicroEngine.Core.HeadlessRenderer.md#MicroEngine_Core_HeadlessRenderer_Frame) with the CPU reference rasterizer, which follows the engine's rendering rules exactly.

```csharp
public sealed class HeadlessRenderer : IRenderer
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[HeadlessRenderer](MicroEngine.Core.HeadlessRenderer.md)

#### Implements

[IRenderer](MicroEngine.Graphics.IRenderer.md)

#### Extension Methods

[RendererExtensions.Draw\(IRenderer, Sprite, Vector2, Color?, bool\)](MicroEngine.Graphics.RendererExtensions.md\#MicroEngine\_Graphics\_RendererExtensions\_Draw\_MicroEngine\_Graphics\_IRenderer\_MicroEngine\_Graphics\_Sprite\_System\_Numerics\_Vector2\_System\_Nullable\_MicroEngine\_Color\_\_System\_Boolean\_), 
[RendererExtensions.Draw\(IRenderer, Texture, Vector2, Color?\)](MicroEngine.Graphics.RendererExtensions.md\#MicroEngine\_Graphics\_RendererExtensions\_Draw\_MicroEngine\_Graphics\_IRenderer\_MicroEngine\_Graphics\_Texture\_System\_Numerics\_Vector2\_System\_Nullable\_MicroEngine\_Color\_\_), 
[RendererExtensions.DrawPivoted\(IRenderer, Sprite, Vector2, float, Vector2?, Color?, bool\)](MicroEngine.Graphics.RendererExtensions.md\#MicroEngine\_Graphics\_RendererExtensions\_DrawPivoted\_MicroEngine\_Graphics\_IRenderer\_MicroEngine\_Graphics\_Sprite\_System\_Numerics\_Vector2\_System\_Single\_System\_Nullable\_System\_Numerics\_Vector2\_\_System\_Nullable\_MicroEngine\_Color\_\_System\_Boolean\_), 
[RendererExtensions.DrawRect\(IRenderer, RectF, Color, float\)](MicroEngine.Graphics.RendererExtensions.md\#MicroEngine\_Graphics\_RendererExtensions\_DrawRect\_MicroEngine\_Graphics\_IRenderer\_MicroEngine\_RectF\_MicroEngine\_Color\_System\_Single\_), 
[RendererExtensions.FillEllipse\(IRenderer, Vector2, float, float, Color\)](MicroEngine.Graphics.RendererExtensions.md\#MicroEngine\_Graphics\_RendererExtensions\_FillEllipse\_MicroEngine\_Graphics\_IRenderer\_System\_Numerics\_Vector2\_System\_Single\_System\_Single\_MicroEngine\_Color\_), 
[RendererExtensions.FillGradient\(IRenderer, RectF, Color, Color, int\)](MicroEngine.Graphics.RendererExtensions.md\#MicroEngine\_Graphics\_RendererExtensions\_FillGradient\_MicroEngine\_Graphics\_IRenderer\_MicroEngine\_RectF\_MicroEngine\_Color\_MicroEngine\_Color\_System\_Int32\_), 
[RendererExtensions.Panel\(IRenderer, RectF, PanelStyle\)](MicroEngine.Graphics.RendererExtensions.md\#MicroEngine\_Graphics\_RendererExtensions\_Panel\_MicroEngine\_Graphics\_IRenderer\_MicroEngine\_RectF\_MicroEngine\_Graphics\_PanelStyle\_)

## Constructors

### <a id="MicroEngine_Core_HeadlessRenderer__ctor_System_Int32_System_Int32_"></a> HeadlessRenderer\(int, int\)

A counting renderer: nothing is drawn, every texture of any headless device is accepted.

```csharp
public HeadlessRenderer(int width, int height)
```

#### Parameters

`width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_HeadlessRenderer__ctor_System_Int32_System_Int32_MicroEngine_Core_HeadlessGraphicsDevice_"></a> HeadlessRenderer\(int, int, HeadlessGraphicsDevice\)

A rasterizing renderer for <code class="paramref">device</code>: it draws only that device's textures, which must keep
their pixels ([KeepsPixels](MicroEngine.Core.HeadlessGraphicsDevice.md#MicroEngine_Core_HeadlessGraphicsDevice_KeepsPixels)).

```csharp
public HeadlessRenderer(int width, int height, HeadlessGraphicsDevice device)
```

#### Parameters

`width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`device` [HeadlessGraphicsDevice](MicroEngine.Core.HeadlessGraphicsDevice.md)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">device</code> does not keep pixels.

## Properties

### <a id="MicroEngine_Core_HeadlessRenderer_CameraDepth"></a> CameraDepth

Cameras pushed and not yet popped.

```csharp
public int CameraDepth { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_HeadlessRenderer_Commands"></a> Commands

This frame's draws as text, one line per call, in screen pixels after the camera offset (a backend-neutral form
tests can compare exactly): <code>clear</code>, <code>rect</code>, <code>sprite</code>, <code>circle</code>, <code>triangle</code>,
<code>line</code>, <code>blend</code>, <code>clip</code>, <code>camera</code>. Empty when the renderer does not rasterize.

```csharp
public IReadOnlyList<string> Commands { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Core_HeadlessRenderer_DrawCalls"></a> DrawCalls

Draw calls since the frame began (cleared by [Reset](MicroEngine.Core.HeadlessRenderer.md#MicroEngine_Core_HeadlessRenderer_Reset)).

```csharp
public int DrawCalls { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_HeadlessRenderer_Frame"></a> Frame

A copy of the canvas as a capture stores it (opaque), or null when the renderer does not rasterize. The canvas
keeps its pixels between frames, as a backend's does.

```csharp
public PixelImage? Frame { get; }
```

#### Property Value

 [PixelImage](MicroEngine.Graphics.PixelImage.md)?

### <a id="MicroEngine_Core_HeadlessRenderer_Height"></a> Height

Height of the virtual canvas in pixels.

```csharp
public int Height { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_HeadlessRenderer_Rasterizes"></a> Rasterizes

Whether this renderer rasterizes (it was created with a graphics device).

```csharp
public bool Rasterizes { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_HeadlessRenderer_Width"></a> Width

Width of the virtual canvas in pixels.

```csharp
public int Width { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Core_HeadlessRenderer_Clear_MicroEngine_Color_"></a> Clear\(Color\)

Replaces every pixel of the canvas with <code class="paramref">color</code>, alpha included, whatever the clip
([SetClip](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_SetClip_System_Nullable_MicroEngine_RectF__)) and blend mode, and replaces what was drawn before it in the frame. The clip still applies
to the draws after it. The game clears to [Background](MicroEngine.Core.GameSettings.md#MicroEngine_Core_GameSettings_Background) before the scenes draw.

```csharp
public void Clear(Color color)
```

#### Parameters

`color` Color

### <a id="MicroEngine_Core_HeadlessRenderer_Draw_MicroEngine_Graphics_Texture_MicroEngine_RectF_System_Numerics_Vector2_MicroEngine_Color_MicroEngine_Graphics_DrawOptions__"></a> Draw\(Texture, RectF, Vector2, Color, in DrawOptions\)

Counts the call (and rasterizes it); throws `ObjectDisposedException` for a disposed texture and, when rasterizing, `ArgumentException` for another device's texture.

```csharp
public void Draw(Texture texture, RectF source, Vector2 position, Color tint, in DrawOptions options = default)
```

#### Parameters

`texture` [Texture](MicroEngine.Graphics.Texture.md)

`source` RectF

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`tint` Color

`options` [DrawOptions](MicroEngine.Graphics.DrawOptions.md)

### <a id="MicroEngine_Core_HeadlessRenderer_DrawLine_System_Numerics_Vector2_System_Numerics_Vector2_System_Single_MicroEngine_Color_"></a> DrawLine\(Vector2, Vector2, float, Color\)

Draws a straight line <code class="paramref">thickness</code> pixels wide. A zero-length line draws nothing.

```csharp
public void DrawLine(Vector2 from, Vector2 to, float thickness, Color color)
```

#### Parameters

`from` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`to` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`thickness` [float](https://learn.microsoft.com/dotnet/api/system.single)

`color` Color

### <a id="MicroEngine_Core_HeadlessRenderer_DrawStretched_MicroEngine_Graphics_Texture_MicroEngine_RectF_MicroEngine_RectF_MicroEngine_Color_"></a> DrawStretched\(Texture, RectF, RectF, Color\)

Counts the call (and rasterizes it), with the checks of [Draw](MicroEngine.Core.HeadlessRenderer.md#MicroEngine_Core_HeadlessRenderer_Draw_MicroEngine_Graphics_Texture_MicroEngine_RectF_System_Numerics_Vector2_MicroEngine_Color_MicroEngine_Graphics_DrawOptions__).

```csharp
public void DrawStretched(Texture texture, RectF source, RectF destination, Color tint)
```

#### Parameters

`texture` [Texture](MicroEngine.Graphics.Texture.md)

`source` RectF

`destination` RectF

`tint` Color

### <a id="MicroEngine_Core_HeadlessRenderer_FillCircle_System_Numerics_Vector2_System_Single_MicroEngine_Color_"></a> FillCircle\(Vector2, float, Color\)

Fills a circle; <code class="paramref">radius</code> is in pixels.

```csharp
public void FillCircle(Vector2 center, float radius, Color color)
```

#### Parameters

`center` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

`color` Color

### <a id="MicroEngine_Core_HeadlessRenderer_FillRect_MicroEngine_RectF_MicroEngine_Color_"></a> FillRect\(RectF, Color\)

Fills a rectangle: the pixels whose centre lies inside it (see the rules of [IRenderer](MicroEngine.Graphics.IRenderer.md)). A fully transparent colour draws nothing.

```csharp
public void FillRect(RectF rect, Color color)
```

#### Parameters

`rect` RectF

`color` Color

### <a id="MicroEngine_Core_HeadlessRenderer_FillTriangle_System_Numerics_Vector2_System_Numerics_Vector2_System_Numerics_Vector2_MicroEngine_Color_"></a> FillTriangle\(Vector2, Vector2, Vector2, Color\)

Fills a triangle.

```csharp
public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
```

#### Parameters

`a` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`b` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`c` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`color` Color

### <a id="MicroEngine_Core_HeadlessRenderer_PopCamera"></a> PopCamera\(\)

Returns to the coordinates in use before the last [PushCamera](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_PushCamera_MicroEngine_Graphics_Camera2D_).

```csharp
public void PopCamera()
```

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

No camera was pushed.

### <a id="MicroEngine_Core_HeadlessRenderer_PushCamera_MicroEngine_Graphics_Camera2D_"></a> PushCamera\(Camera2D\)

Draws in the world coordinates of <code class="paramref">camera</code> until [PopCamera](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_PopCamera): its [RenderOffset](MicroEngine.Graphics.Camera2D.md#MicroEngine_Graphics_Camera2D_RenderOffset)
replaces the current offset (cameras do not combine). Every push needs a pop in the same frame.

```csharp
public void PushCamera(Camera2D camera)
```

#### Parameters

`camera` Camera2D

### <a id="MicroEngine_Core_HeadlessRenderer_Reset"></a> Reset\(\)

Starts a new frame: clears [DrawCalls](MicroEngine.Core.HeadlessRenderer.md#MicroEngine_Core_HeadlessRenderer_DrawCalls) and [Commands](MicroEngine.Core.HeadlessRenderer.md#MicroEngine_Core_HeadlessRenderer_Commands), blending returns to [Alpha](MicroEngine.Graphics.BlendMode.md) and the clip is removed.

```csharp
public void Reset()
```

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The previous frame pushed a camera it did not pop.

### <a id="MicroEngine_Core_HeadlessRenderer_SetBlendMode_MicroEngine_Graphics_BlendMode_"></a> SetBlendMode\(BlendMode\)

Sets how later draws combine with the canvas. Every frame starts with [Alpha](MicroEngine.Graphics.BlendMode.md). With source
colour s (after the tint), its alpha a and canvas colour d: <code>Alpha</code> writes s·a + d·(1 − a),
<code>Additive</code> s·a + d, <code>Multiply</code> s·a·d + d·(1 − a) (the colour multiplies the canvas in proportion to its
opacity), each channel clamped to 0..1.

```csharp
public void SetBlendMode(BlendMode mode)
```

#### Parameters

`mode` [BlendMode](MicroEngine.Graphics.BlendMode.md)

### <a id="MicroEngine_Core_HeadlessRenderer_SetClip_System_Nullable_MicroEngine_RectF__"></a> SetClip\(RectF?\)

Restricts drawing to a screen-space rectangle (the camera does not move it); pass null to remove. X, Y, Width and
Height are truncated to whole pixels. [Clear](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_Clear_MicroEngine_Color_) ignores the clip.

```csharp
public void SetClip(RectF? screenRect)
```

#### Parameters

`screenRect` RectF?

</div>
