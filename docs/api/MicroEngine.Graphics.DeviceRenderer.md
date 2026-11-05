# Class DeviceRenderer

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

The drawing API ([IRenderer](MicroEngine.Graphics.IRenderer.md)) as a façade over the 2D batcher ([Renderer2D](MicroEngine.Graphics.Renderer2D.md)) on a graphics
device: every call is recorded into the batcher, and the frame reaches the device in as few draw calls as the draws'
textures and states allow. Straight-alpha colours are premultiplied on the way in, so the blend modes keep their
written equations (see [SetBlendMode](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_SetBlendMode_MicroEngine_Graphics_BlendMode_)).

```csharp
public sealed class DeviceRenderer : IRenderer
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[DeviceRenderer](MicroEngine.Graphics.DeviceRenderer.md)

#### Implements

IRenderer

## Constructors

### <a id="MicroEngine_Graphics_DeviceRenderer__ctor_MicroEngine_Graphics_Renderer2D_MicroEngine_Graphics_DeviceGraphics_MicroEngine_Graphics_Devices_RenderTarget_"></a> DeviceRenderer\(Renderer2D, DeviceGraphics, RenderTarget\)

A renderer drawing into <code class="paramref">target</code> (the canvas) with the textures of <code class="paramref">graphics</code>.

```csharp
public DeviceRenderer(Renderer2D batcher, DeviceGraphics graphics, RenderTarget target)
```

#### Parameters

`batcher` Renderer2D

`graphics` [DeviceGraphics](MicroEngine.Graphics.DeviceGraphics.md)

`target` RenderTarget

### <a id="MicroEngine_Graphics_DeviceRenderer__ctor_MicroEngine_Graphics_Renderer2D_MicroEngine_Graphics_DeviceGraphics_MicroEngine_Graphics_Devices_RenderTarget_System_Single_"></a> DeviceRenderer\(Renderer2D, DeviceGraphics, RenderTarget, float\)

A renderer whose units are <code class="paramref">scale</code> target pixels each (the UI view: UI units at the window's
resolution). [Width](MicroEngine.Graphics.DeviceRenderer.md#MicroEngine_Graphics_DeviceRenderer_Width) and [Height](MicroEngine.Graphics.DeviceRenderer.md#MicroEngine_Graphics_DeviceRenderer_Height) are the target's size in those units; the clip is in them too.

```csharp
public DeviceRenderer(Renderer2D batcher, DeviceGraphics graphics, RenderTarget target, float scale)
```

#### Parameters

`batcher` Renderer2D

`graphics` [DeviceGraphics](MicroEngine.Graphics.DeviceGraphics.md)

`target` RenderTarget

`scale` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">scale</code> is not positive.

## Properties

### <a id="MicroEngine_Graphics_DeviceRenderer_Height"></a> Height

Height of the virtual canvas in pixels.

```csharp
public int Height { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_DeviceRenderer_Statistics"></a> Statistics

The batcher's counts for the last frame (sprites, draw calls, flushes): exact and the same on every device.

```csharp
public Renderer2DStatistics Statistics { get; }
```

#### Property Value

 Renderer2DStatistics

### <a id="MicroEngine_Graphics_DeviceRenderer_Width"></a> Width

Width of the virtual canvas in pixels.

```csharp
public int Width { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Graphics_DeviceRenderer_Begin"></a> Begin\(\)

Starts a frame: the canvas keeps its pixels, blending is [Alpha](MicroEngine.Graphics.BlendMode.md), no clip, no camera.

```csharp
public void Begin()
```

### <a id="MicroEngine_Graphics_DeviceRenderer_Clear_MicroEngine_Color_"></a> Clear\(Color\)

Replaces every pixel of the canvas with <code class="paramref">color</code>, alpha included, whatever the clip
([SetClip](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_SetClip_System_Nullable_MicroEngine_RectF__)) and blend mode, and replaces what was drawn before it in the frame. The clip still applies
to the draws after it. The game clears to its settings' <code>Background</code> colour before the scenes draw.

```csharp
public void Clear(Color color)
```

#### Parameters

`color` Color

### <a id="MicroEngine_Graphics_DeviceRenderer_Draw_MicroEngine_Graphics_Texture_MicroEngine_RectF_System_Numerics_Vector2_MicroEngine_Color_MicroEngine_Graphics_DrawOptions__"></a> Draw\(Texture, RectF, Vector2, Color, in DrawOptions\)

```csharp
public void Draw(Texture texture, RectF source, Vector2 position, Color tint, in DrawOptions options = default)
```

#### Parameters

`texture` Texture

`source` RectF

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`tint` Color

`options` DrawOptions

### <a id="MicroEngine_Graphics_DeviceRenderer_DrawLine_System_Numerics_Vector2_System_Numerics_Vector2_System_Single_MicroEngine_Color_"></a> DrawLine\(Vector2, Vector2, float, Color\)

Draws a straight line <code class="paramref">thickness</code> pixels wide. A zero-length line draws nothing.

```csharp
public void DrawLine(Vector2 from, Vector2 to, float thickness, Color color)
```

#### Parameters

`from` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`to` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`thickness` [float](https://learn.microsoft.com/dotnet/api/system.single)

`color` Color

### <a id="MicroEngine_Graphics_DeviceRenderer_DrawStretched_MicroEngine_Graphics_Texture_MicroEngine_RectF_MicroEngine_RectF_MicroEngine_Color_"></a> DrawStretched\(Texture, RectF, RectF, Color\)

Draws the <code class="paramref">source</code> part of <code class="paramref">texture</code> scaled to fill <code class="paramref">destination</code>.

```csharp
public void DrawStretched(Texture texture, RectF source, RectF destination, Color tint)
```

#### Parameters

`texture` Texture

`source` RectF

`destination` RectF

`tint` Color

#### Exceptions

 [ObjectDisposedException](https://learn.microsoft.com/dotnet/api/system.objectdisposedexception)

<code class="paramref">texture</code> was disposed; nothing is drawn.

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

Another graphics device created <code class="paramref">texture</code>; nothing is drawn.

### <a id="MicroEngine_Graphics_DeviceRenderer_End"></a> End\(\)

Ends the frame and submits it.

```csharp
public void End()
```

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

A camera pushed this frame was not popped.

### <a id="MicroEngine_Graphics_DeviceRenderer_FillCircle_System_Numerics_Vector2_System_Single_MicroEngine_Color_"></a> FillCircle\(Vector2, float, Color\)

Fills a circle; <code class="paramref">radius</code> is in pixels.

```csharp
public void FillCircle(Vector2 center, float radius, Color color)
```

#### Parameters

`center` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

`color` Color

### <a id="MicroEngine_Graphics_DeviceRenderer_FillRect_MicroEngine_RectF_MicroEngine_Color_"></a> FillRect\(RectF, Color\)

Fills a rectangle: the pixels whose centre lies inside it (see the rules of [IRenderer](MicroEngine.Graphics.IRenderer.md)). A fully transparent colour draws nothing.

```csharp
public void FillRect(RectF rect, Color color)
```

#### Parameters

`rect` RectF

`color` Color

### <a id="MicroEngine_Graphics_DeviceRenderer_FillTriangle_System_Numerics_Vector2_System_Numerics_Vector2_System_Numerics_Vector2_MicroEngine_Color_"></a> FillTriangle\(Vector2, Vector2, Vector2, Color\)

Fills a triangle.

```csharp
public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
```

#### Parameters

`a` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`b` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`c` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`color` Color

### <a id="MicroEngine_Graphics_DeviceRenderer_PopCamera"></a> PopCamera\(\)

Returns to the coordinates in use before the last [PushCamera](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_PushCamera_MicroEngine_Graphics_Camera2D_).

```csharp
public void PopCamera()
```

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

No camera was pushed.

### <a id="MicroEngine_Graphics_DeviceRenderer_PushCamera_MicroEngine_Graphics_Camera2D_"></a> PushCamera\(Camera2D\)

Draws in the world coordinates of <code class="paramref">camera</code> until [PopCamera](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_PopCamera): its [RenderOffset](MicroEngine.Graphics.Camera2D.md#MicroEngine_Graphics_Camera2D_RenderOffset)
replaces the current offset (cameras do not combine). Every push needs a pop in the same frame.

```csharp
public void PushCamera(Camera2D camera)
```

#### Parameters

`camera` Camera2D

### <a id="MicroEngine_Graphics_DeviceRenderer_SetBlendMode_MicroEngine_Graphics_BlendMode_"></a> SetBlendMode\(BlendMode\)

Sets how later draws combine with the canvas. Every frame starts with [Alpha](MicroEngine.Graphics.BlendMode.md). With source
colour s (after the tint), its alpha a and canvas colour d: <code>Alpha</code> writes s·a + d·(1 − a),
<code>Additive</code> s·a + d, <code>Multiply</code> s·a·d + d·(1 − a) (the colour multiplies the canvas in proportion to its
opacity), each channel clamped to 0..1.

```csharp
public void SetBlendMode(BlendMode mode)
```

#### Parameters

`mode` BlendMode

### <a id="MicroEngine_Graphics_DeviceRenderer_SetClip_System_Nullable_MicroEngine_RectF__"></a> SetClip\(RectF?\)

Restricts drawing to a screen-space rectangle (the camera does not move it); pass null to remove. X, Y, Width and
Height are truncated to whole pixels. [Clear](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_Clear_MicroEngine_Color_) ignores the clip.

```csharp
public void SetClip(RectF? screenRect)
```

#### Parameters

`screenRect` RectF?

</div>
