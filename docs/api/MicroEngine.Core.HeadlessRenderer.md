# Class HeadlessRenderer

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

Counts draw calls and tracks camera balance so tests can assert rendering ran correctly.

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

Counts draw calls and tracks camera balance so tests can assert rendering ran correctly.

```csharp
public HeadlessRenderer(int width, int height)
```

#### Parameters

`width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Properties

### <a id="MicroEngine_Core_HeadlessRenderer_CameraDepth"></a> CameraDepth

Cameras pushed and not yet popped.

```csharp
public int CameraDepth { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_HeadlessRenderer_DrawCalls"></a> DrawCalls

Draw calls since the frame began (cleared by [Reset](MicroEngine.Core.HeadlessRenderer.md#MicroEngine_Core_HeadlessRenderer_Reset)).

```csharp
public int DrawCalls { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_HeadlessRenderer_Height"></a> Height

Height of the virtual canvas in pixels.

```csharp
public int Height { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_HeadlessRenderer_Width"></a> Width

Width of the virtual canvas in pixels.

```csharp
public int Width { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Core_HeadlessRenderer_Clear_MicroEngine_Color_"></a> Clear\(Color\)

Fills the whole canvas with <code class="paramref">color</code>. The game clears to [Background](MicroEngine.Core.GameSettings.md#MicroEngine_Core_GameSettings_Background) before the scenes draw.

```csharp
public void Clear(Color color)
```

#### Parameters

`color` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Core_HeadlessRenderer_Draw_MicroEngine_Graphics_Texture_MicroEngine_RectF_System_Numerics_Vector2_MicroEngine_Color_MicroEngine_Graphics_DrawOptions__"></a> Draw\(Texture, RectF, Vector2, Color, in DrawOptions\)

Counts the call; throws `ObjectDisposedException` for a disposed texture, as real renderers fail.

```csharp
public void Draw(Texture texture, RectF source, Vector2 position, Color tint, in DrawOptions options = default)
```

#### Parameters

`texture` [Texture](MicroEngine.Graphics.Texture.md)

`source` [RectF](MicroEngine.RectF.md)

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`tint` [Color](MicroEngine.Color.md)

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

`color` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Core_HeadlessRenderer_DrawStretched_MicroEngine_Graphics_Texture_MicroEngine_RectF_MicroEngine_RectF_MicroEngine_Color_"></a> DrawStretched\(Texture, RectF, RectF, Color\)

Draws the <code class="paramref">source</code> part of <code class="paramref">texture</code> scaled to fill <code class="paramref">destination</code>.

```csharp
public void DrawStretched(Texture texture, RectF source, RectF destination, Color tint)
```

#### Parameters

`texture` [Texture](MicroEngine.Graphics.Texture.md)

`source` [RectF](MicroEngine.RectF.md)

`destination` [RectF](MicroEngine.RectF.md)

`tint` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Core_HeadlessRenderer_FillCircle_System_Numerics_Vector2_System_Single_MicroEngine_Color_"></a> FillCircle\(Vector2, float, Color\)

Fills a circle; <code class="paramref">radius</code> is in pixels.

```csharp
public void FillCircle(Vector2 center, float radius, Color color)
```

#### Parameters

`center` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

`color` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Core_HeadlessRenderer_FillRect_MicroEngine_RectF_MicroEngine_Color_"></a> FillRect\(RectF, Color\)

Fills a rectangle.

```csharp
public void FillRect(RectF rect, Color color)
```

#### Parameters

`rect` [RectF](MicroEngine.RectF.md)

`color` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Core_HeadlessRenderer_FillTriangle_System_Numerics_Vector2_System_Numerics_Vector2_System_Numerics_Vector2_MicroEngine_Color_"></a> FillTriangle\(Vector2, Vector2, Vector2, Color\)

Fills a triangle.

```csharp
public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
```

#### Parameters

`a` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`b` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`c` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`color` [Color](MicroEngine.Color.md)

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

`camera` [Camera2D](MicroEngine.Graphics.Camera2D.md)

### <a id="MicroEngine_Core_HeadlessRenderer_Reset"></a> Reset\(\)

Starts a new frame: clears [DrawCalls](MicroEngine.Core.HeadlessRenderer.md#MicroEngine_Core_HeadlessRenderer_DrawCalls).

```csharp
public void Reset()
```

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The previous frame pushed a camera it did not pop.

### <a id="MicroEngine_Core_HeadlessRenderer_SetBlendMode_MicroEngine_Graphics_BlendMode_"></a> SetBlendMode\(BlendMode\)

Sets how later draws combine with the canvas. Every frame starts with [Alpha](MicroEngine.Graphics.BlendMode.md).

```csharp
public void SetBlendMode(BlendMode mode)
```

#### Parameters

`mode` [BlendMode](MicroEngine.Graphics.BlendMode.md)

### <a id="MicroEngine_Core_HeadlessRenderer_SetClip_System_Nullable_MicroEngine_RectF__"></a> SetClip\(RectF?\)

Restricts drawing to a screen-space rectangle; pass null to remove.

```csharp
public void SetClip(RectF? screenRect)
```

#### Parameters

`screenRect` [RectF](MicroEngine.RectF.md)?
