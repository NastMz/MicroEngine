# Interface IRenderer

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

Immediate-mode 2D drawing in virtual pixels. Coordinates go through the camera
pushed with [PushCamera](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_PushCamera_MicroEngine_Graphics_Camera2D_); with no camera they are screen space.

```csharp
public interface IRenderer
```

#### Extension Methods

[RendererExtensions.Draw\(IRenderer, Sprite, Vector2, Color?, bool\)](MicroEngine.Graphics.RendererExtensions.md\#MicroEngine\_Graphics\_RendererExtensions\_Draw\_MicroEngine\_Graphics\_IRenderer\_MicroEngine\_Graphics\_Sprite\_System\_Numerics\_Vector2\_System\_Nullable\_MicroEngine\_Color\_\_System\_Boolean\_), 
[RendererExtensions.Draw\(IRenderer, Texture, Vector2, Color?\)](MicroEngine.Graphics.RendererExtensions.md\#MicroEngine\_Graphics\_RendererExtensions\_Draw\_MicroEngine\_Graphics\_IRenderer\_MicroEngine\_Graphics\_Texture\_System\_Numerics\_Vector2\_System\_Nullable\_MicroEngine\_Color\_\_), 
[RendererExtensions.DrawNineSlice\(IRenderer, NineSlice, RectF, Color?\)](MicroEngine.Graphics.RendererExtensions.md\#MicroEngine\_Graphics\_RendererExtensions\_DrawNineSlice\_MicroEngine\_Graphics\_IRenderer\_MicroEngine\_Graphics\_NineSlice\_MicroEngine\_RectF\_System\_Nullable\_MicroEngine\_Color\_\_), 
[RendererExtensions.DrawPivoted\(IRenderer, Sprite, Vector2, float, Vector2?, Color?, bool\)](MicroEngine.Graphics.RendererExtensions.md\#MicroEngine\_Graphics\_RendererExtensions\_DrawPivoted\_MicroEngine\_Graphics\_IRenderer\_MicroEngine\_Graphics\_Sprite\_System\_Numerics\_Vector2\_System\_Single\_System\_Nullable\_System\_Numerics\_Vector2\_\_System\_Nullable\_MicroEngine\_Color\_\_System\_Boolean\_), 
[RendererExtensions.DrawRect\(IRenderer, RectF, Color, float\)](MicroEngine.Graphics.RendererExtensions.md\#MicroEngine\_Graphics\_RendererExtensions\_DrawRect\_MicroEngine\_Graphics\_IRenderer\_MicroEngine\_RectF\_MicroEngine\_Color\_System\_Single\_), 
[RendererExtensions.FillEllipse\(IRenderer, Vector2, float, float, Color\)](MicroEngine.Graphics.RendererExtensions.md\#MicroEngine\_Graphics\_RendererExtensions\_FillEllipse\_MicroEngine\_Graphics\_IRenderer\_System\_Numerics\_Vector2\_System\_Single\_System\_Single\_MicroEngine\_Color\_), 
[RendererExtensions.FillGradient\(IRenderer, RectF, Color, Color, int\)](MicroEngine.Graphics.RendererExtensions.md\#MicroEngine\_Graphics\_RendererExtensions\_FillGradient\_MicroEngine\_Graphics\_IRenderer\_MicroEngine\_RectF\_MicroEngine\_Color\_MicroEngine\_Color\_System\_Int32\_), 
[RendererExtensions.Panel\(IRenderer, RectF, PanelStyle\)](MicroEngine.Graphics.RendererExtensions.md\#MicroEngine\_Graphics\_RendererExtensions\_Panel\_MicroEngine\_Graphics\_IRenderer\_MicroEngine\_RectF\_MicroEngine\_Graphics\_PanelStyle\_)

## Remarks

Every renderer follows the same rules. Pixel (x, y) is the square from (x, y) to (x + 1, y + 1) and its centre is
(x + 0.5, y + 0.5). A rectangle or an unrotated sprite covers exactly the pixels whose centre lies inside it, a
centre on its left or top edge counting as inside and one on its right or bottom edge as outside (the top-left rule
of Direct3D and Vulkan): a 1 × 1 rectangle at (2.5, 2.5) covers pixel (2, 2) only. A sprite pixel shows the texel
under its centre (nearest) or the bilinear mix of the four texels around it (linear, clamped at the texture's edge).
Colours are given straight (not premultiplied); the canvas stores them premultiplied (colour times alpha), so linear
sampling never darkens transparent edges, and 8-bit results are rounded to nearest. Circles, lines and triangles
cover their shape's area but their edge pixels may differ between renderers.

## Properties

### <a id="MicroEngine_Graphics_IRenderer_Height"></a> Height

Height of the virtual canvas in pixels.

```csharp
int Height { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_IRenderer_Width"></a> Width

Width of the virtual canvas in pixels.

```csharp
int Width { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Graphics_IRenderer_Clear_MicroEngine_Color_"></a> Clear\(Color\)

Replaces every pixel of the canvas with <code class="paramref">color</code>, alpha included, whatever the clip
([SetClip](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_SetClip_System_Nullable_MicroEngine_RectF__)) and blend mode, and replaces what was drawn before it in the frame. The clip still applies
to the draws after it. The game clears to its settings' <code>Background</code> colour before the scenes draw.

```csharp
void Clear(Color color)
```

#### Parameters

`color` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Graphics_IRenderer_Draw_MicroEngine_Graphics_Texture_MicroEngine_RectF_System_Numerics_Vector2_MicroEngine_Color_MicroEngine_Graphics_DrawOptions__"></a> Draw\(Texture, RectF, Vector2, Color, in DrawOptions\)

Draws the <code class="paramref">source</code> part of <code class="paramref">texture</code> with its top-left at <code class="paramref">position</code>, multiplied by <code class="paramref">tint</code>.

```csharp
void Draw(Texture texture, RectF source, Vector2 position, Color tint, in DrawOptions options = default)
```

#### Parameters

`texture` [Texture](MicroEngine.Graphics.Texture.md)

A texture of this renderer's graphics device, not disposed.

`source` [RectF](MicroEngine.RectF.md)

The rectangle of the texture to draw, in texture pixels.

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Where the top-left of the unrotated, scaled source lands, in virtual pixels (world pixels under a camera).

`tint` [Color](MicroEngine.Color.md)

Colour multiplier; [White](MicroEngine.Color.md#MicroEngine_Color_White) draws the texture unchanged.

`options` [DrawOptions](MicroEngine.Graphics.DrawOptions.md)

Flip, rotation, pivot and scale.

#### Exceptions

 [ObjectDisposedException](https://learn.microsoft.com/dotnet/api/system.objectdisposedexception)

<code class="paramref">texture</code> was disposed; nothing is drawn.

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

Another graphics device created <code class="paramref">texture</code>; nothing is drawn.

### <a id="MicroEngine_Graphics_IRenderer_DrawLine_System_Numerics_Vector2_System_Numerics_Vector2_System_Single_MicroEngine_Color_"></a> DrawLine\(Vector2, Vector2, float, Color\)

Draws a straight line <code class="paramref">thickness</code> pixels wide. A zero-length line draws nothing.

```csharp
void DrawLine(Vector2 from, Vector2 to, float thickness, Color color)
```

#### Parameters

`from` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`to` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`thickness` [float](https://learn.microsoft.com/dotnet/api/system.single)

`color` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Graphics_IRenderer_DrawStretched_MicroEngine_Graphics_Texture_MicroEngine_RectF_MicroEngine_RectF_MicroEngine_Color_"></a> DrawStretched\(Texture, RectF, RectF, Color\)

Draws the <code class="paramref">source</code> part of <code class="paramref">texture</code> scaled to fill <code class="paramref">destination</code>.

```csharp
void DrawStretched(Texture texture, RectF source, RectF destination, Color tint)
```

#### Parameters

`texture` [Texture](MicroEngine.Graphics.Texture.md)

`source` [RectF](MicroEngine.RectF.md)

`destination` [RectF](MicroEngine.RectF.md)

`tint` [Color](MicroEngine.Color.md)

#### Exceptions

 [ObjectDisposedException](https://learn.microsoft.com/dotnet/api/system.objectdisposedexception)

<code class="paramref">texture</code> was disposed; nothing is drawn.

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

Another graphics device created <code class="paramref">texture</code>; nothing is drawn.

### <a id="MicroEngine_Graphics_IRenderer_FillCircle_System_Numerics_Vector2_System_Single_MicroEngine_Color_"></a> FillCircle\(Vector2, float, Color\)

Fills a circle; <code class="paramref">radius</code> is in pixels.

```csharp
void FillCircle(Vector2 center, float radius, Color color)
```

#### Parameters

`center` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

`color` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Graphics_IRenderer_FillRect_MicroEngine_RectF_MicroEngine_Color_"></a> FillRect\(RectF, Color\)

Fills a rectangle: the pixels whose centre lies inside it (see the rules of [IRenderer](MicroEngine.Graphics.IRenderer.md)). A fully transparent colour draws nothing.

```csharp
void FillRect(RectF rect, Color color)
```

#### Parameters

`rect` [RectF](MicroEngine.RectF.md)

`color` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Graphics_IRenderer_FillTriangle_System_Numerics_Vector2_System_Numerics_Vector2_System_Numerics_Vector2_MicroEngine_Color_"></a> FillTriangle\(Vector2, Vector2, Vector2, Color\)

Fills a triangle.

```csharp
void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
```

#### Parameters

`a` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`b` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`c` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`color` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Graphics_IRenderer_PopCamera"></a> PopCamera\(\)

Returns to the coordinates in use before the last [PushCamera](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_PushCamera_MicroEngine_Graphics_Camera2D_).

```csharp
void PopCamera()
```

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

No camera was pushed.

### <a id="MicroEngine_Graphics_IRenderer_PopState"></a> PopState\(\)

Returns to what was being drawn with before the last [PushState](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_PushState_MicroEngine_Graphics_DrawState__).

```csharp
void PopState()
```

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

No state was pushed (in this target).

### <a id="MicroEngine_Graphics_IRenderer_PopTarget"></a> PopTarget\(\)

Returns to the canvas, or to the target that was being drawn into before the last [PushTarget](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_PushTarget_MicroEngine_Graphics_RenderTarget2D_).

```csharp
void PopTarget()
```

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

No target was pushed, or a camera or a state pushed inside the target was not popped.

### <a id="MicroEngine_Graphics_IRenderer_PushCamera_MicroEngine_Graphics_Camera2D_"></a> PushCamera\(Camera2D\)

Draws in the world coordinates of <code class="paramref">camera</code> until [PopCamera](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_PopCamera): its [RenderOffset](MicroEngine.Graphics.Camera2D.md#MicroEngine_Graphics_Camera2D_RenderOffset)
replaces the current offset (cameras do not combine). Every push needs a pop in the same frame.

```csharp
void PushCamera(Camera2D camera)
```

#### Parameters

`camera` [Camera2D](MicroEngine.Graphics.Camera2D.md)

### <a id="MicroEngine_Graphics_IRenderer_PushState_MicroEngine_Graphics_DrawState__"></a> PushState\(in DrawState\)

Draws with <code class="paramref">state</code> until [PopState](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_PopState): its blend state, its sampler and its material,
each replacing the current one when it is not null. Every draw is affected, shapes and text included. States nest;
every push needs a pop in the same frame, inside the target it was pushed in. [SetBlendMode](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_SetBlendMode_MicroEngine_Graphics_BlendMode_) still
works inside a state and is undone by the pop.

```csharp
void PushState(in DrawState state)
```

#### Parameters

`state` [DrawState](MicroEngine.Graphics.DrawState.md)

#### Remarks

A material whose shader the device cannot run ([IsSupported](MicroEngine.Graphics.Material2D.md#MicroEngine_Graphics_Material2D_IsSupported) false) draws with its
[Fallback](MicroEngine.Graphics.Material2D.md#MicroEngine_Graphics_Material2D_Fallback) instead. A material whose shader samples a texture that was never given is
refused at the draw (`InvalidOperationException` naming the texture), on every device. The default
implementation is for renderers that draw nothing with a state (a test double that only records calls): it refuses.

#### Exceptions

 [ObjectDisposedException](https://learn.microsoft.com/dotnet/api/system.objectdisposedexception)

The state's material was disposed.

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

The state's material belongs to another graphics device.

 [GraphicsCapabilityException](MicroEngine.Graphics.Devices.GraphicsCapabilityException.md)

A blend state that is not one of the four presets, on a device without custom blending.

### <a id="MicroEngine_Graphics_IRenderer_PushTarget_MicroEngine_Graphics_RenderTarget2D_"></a> PushTarget\(RenderTarget2D\)

Draws into <code class="paramref">target</code> instead of the canvas until [PopTarget](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_PopTarget). A target starts like a
frame: [Width](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_Width) and [Height](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_Height) are its size, there is no camera, no clip and no state, and
blending is [Alpha](MicroEngine.Graphics.BlendMode.md); it keeps the pixels it had (call [Clear](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_Clear_MicroEngine_Color_) to start from a
colour). The pop returns to the canvas (or the target under it: targets nest) with the camera, clip, blending and
state it had. Draw the target's [Texture](MicroEngine.Graphics.RenderTarget2D.md#MicroEngine_Graphics_RenderTarget2D_Texture) afterwards like any texture; it cannot be
drawn into itself.

```csharp
void PushTarget(RenderTarget2D target)
```

#### Parameters

`target` [RenderTarget2D](MicroEngine.Graphics.RenderTarget2D.md)

#### Exceptions

 [ObjectDisposedException](https://learn.microsoft.com/dotnet/api/system.objectdisposedexception)

<code class="paramref">target</code> was disposed.

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

Another graphics device created <code class="paramref">target</code>.

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

<code class="paramref">target</code> is already being drawn into.

### <a id="MicroEngine_Graphics_IRenderer_SetBlendMode_MicroEngine_Graphics_BlendMode_"></a> SetBlendMode\(BlendMode\)

Sets how later draws combine with the canvas. Every frame starts with [Alpha](MicroEngine.Graphics.BlendMode.md). With source
colour s (after the tint), its alpha a and canvas colour d: <code>Alpha</code> writes s·a + d·(1 − a),
<code>Additive</code> s·a + d, <code>Multiply</code> s·a·d + d·(1 − a) (the colour multiplies the canvas in proportion to its
opacity), each channel clamped to 0..1.

```csharp
void SetBlendMode(BlendMode mode)
```

#### Parameters

`mode` [BlendMode](MicroEngine.Graphics.BlendMode.md)

### <a id="MicroEngine_Graphics_IRenderer_SetClip_System_Nullable_MicroEngine_RectF__"></a> SetClip\(RectF?\)

Restricts drawing to a screen-space rectangle (the camera does not move it); pass null to remove. X, Y, Width and
Height are truncated to whole pixels. [Clear](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_Clear_MicroEngine_Color_) ignores the clip.

```csharp
void SetClip(RectF? screenRect)
```

#### Parameters

`screenRect` [RectF](MicroEngine.RectF.md)?

</div>
