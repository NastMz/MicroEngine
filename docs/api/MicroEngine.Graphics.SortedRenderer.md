# Class SortedRenderer

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

Draws in an order that does not depend on the order of the calls: between [Begin](MicroEngine.Graphics.SortedRenderer.md#MicroEngine_Graphics_SortedRenderer_Begin_MicroEngine_Graphics_IRenderer_) and [End](MicroEngine.Graphics.SortedRenderer.md#MicroEngine_Graphics_SortedRenderer_End)
every draw is kept with the [Layer](MicroEngine.Graphics.SortedRenderer.md#MicroEngine_Graphics_SortedRenderer_Layer) and [Depth](MicroEngine.Graphics.SortedRenderer.md#MicroEngine_Graphics_SortedRenderer_Depth) of that moment, and [End](MicroEngine.Graphics.SortedRenderer.md#MicroEngine_Graphics_SortedRenderer_End) sends
them to the renderer by layer (lower first), then by depth (smaller first, so a larger depth covers), then in the
order they were made. It is a renderer itself, so sprites, fonts, tiles and particles draw into it as into any other,
on every platform. Objects standing on the ground cover each other correctly with their feet's Y as the depth.

```csharp
public sealed class SortedRenderer : IRenderer
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SortedRenderer](MicroEngine.Graphics.SortedRenderer.md)

#### Implements

[IRenderer](MicroEngine.Graphics.IRenderer.md)

#### Extension Methods

[RendererExtensions.Draw\(IRenderer, Sprite, Vector2, Color?, bool\)](MicroEngine.Graphics.RendererExtensions.md\#MicroEngine\_Graphics\_RendererExtensions\_Draw\_MicroEngine\_Graphics\_IRenderer\_MicroEngine\_Graphics\_Sprite\_System\_Numerics\_Vector2\_System\_Nullable\_MicroEngine\_Color\_\_System\_Boolean\_), 
[RendererExtensions.Draw\(IRenderer, Texture, Vector2, Color?\)](MicroEngine.Graphics.RendererExtensions.md\#MicroEngine\_Graphics\_RendererExtensions\_Draw\_MicroEngine\_Graphics\_IRenderer\_MicroEngine\_Graphics\_Texture\_System\_Numerics\_Vector2\_System\_Nullable\_MicroEngine\_Color\_\_), 
[RendererExtensions.DrawNineSlice\(IRenderer, NineSlice, RectF, Color?\)](MicroEngine.Graphics.RendererExtensions.md\#MicroEngine\_Graphics\_RendererExtensions\_DrawNineSlice\_MicroEngine\_Graphics\_IRenderer\_MicroEngine\_Graphics\_NineSlice\_MicroEngine\_RectF\_System\_Nullable\_MicroEngine\_Color\_\_), 
[RendererExtensions.DrawPivoted\(IRenderer, Sprite, Vector2, float, Vector2?, Color?, bool\)](MicroEngine.Graphics.RendererExtensions.md\#MicroEngine\_Graphics\_RendererExtensions\_DrawPivoted\_MicroEngine\_Graphics\_IRenderer\_MicroEngine\_Graphics\_Sprite\_System\_Numerics\_Vector2\_System\_Single\_System\_Nullable\_System\_Numerics\_Vector2\_\_System\_Nullable\_MicroEngine\_Color\_\_System\_Boolean\_), 
[RendererExtensions.DrawRect\(IRenderer, RectF, Color, float\)](MicroEngine.Graphics.RendererExtensions.md\#MicroEngine\_Graphics\_RendererExtensions\_DrawRect\_MicroEngine\_Graphics\_IRenderer\_MicroEngine\_RectF\_MicroEngine\_Color\_System\_Single\_), 
[RendererExtensions.FillEllipse\(IRenderer, Vector2, float, float, Color\)](MicroEngine.Graphics.RendererExtensions.md\#MicroEngine\_Graphics\_RendererExtensions\_FillEllipse\_MicroEngine\_Graphics\_IRenderer\_System\_Numerics\_Vector2\_System\_Single\_System\_Single\_MicroEngine\_Color\_), 
[RendererExtensions.FillGradient\(IRenderer, RectF, Color, Color, int\)](MicroEngine.Graphics.RendererExtensions.md\#MicroEngine\_Graphics\_RendererExtensions\_FillGradient\_MicroEngine\_Graphics\_IRenderer\_MicroEngine\_RectF\_MicroEngine\_Color\_MicroEngine\_Color\_System\_Int32\_), 
[RendererExtensions.Panel\(IRenderer, RectF, PanelStyle\)](MicroEngine.Graphics.RendererExtensions.md\#MicroEngine\_Graphics\_RendererExtensions\_Panel\_MicroEngine\_Graphics\_IRenderer\_MicroEngine\_RectF\_MicroEngine\_Graphics\_PanelStyle\_)

## Examples

```csharp
var world = _sorted.Begin(renderer);                    // a SortedRenderer the scene keeps
foreach (var tree in _trees) { world.Depth = tree.Feet.Y; tree.Draw(world); }
world.Depth = _player.Feet.Y; _player.Draw(world);       // behind the trees below it, in front of those above
world.At(layer: 1).FillRect(_marker, Color.White);       // a higher layer: over everything in layer 0
_sorted.End();
```

## Remarks

Each draw keeps the camera, blend mode, clip and states it was recorded under, whatever the order it ends up in. A
sorted block starts like a frame (no camera, no clip, [Alpha](MicroEngine.Graphics.BlendMode.md)) except that a camera, a state
or a clip the renderer already has stays under it; a block that sets a clip leaves the renderer without one. What
has no place in an order is refused: [Clear](MicroEngine.Graphics.SortedRenderer.md#MicroEngine_Graphics_SortedRenderer_Clear_MicroEngine_Color_) and render targets. Keep one instance and reuse it every
frame: it keeps its memory.

## Constructors

### <a id="MicroEngine_Graphics_SortedRenderer__ctor"></a> SortedRenderer\(\)

```csharp
public SortedRenderer()
```

## Properties

### <a id="MicroEngine_Graphics_SortedRenderer_Count"></a> Count

Draws recorded since [Begin](MicroEngine.Graphics.SortedRenderer.md#MicroEngine_Graphics_SortedRenderer_Begin_MicroEngine_Graphics_IRenderer_) and not yet sent.

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_SortedRenderer_Depth"></a> Depth

The depth of the draws made from now on, within their layer: a draw with a larger depth is drawn later and covers
those with a smaller one (the Y of an object's feet, for objects on the ground). 0 at [Begin](MicroEngine.Graphics.SortedRenderer.md#MicroEngine_Graphics_SortedRenderer_Begin_MicroEngine_Graphics_IRenderer_).
A depth that is not a number is refused.

```csharp
public float Depth { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

The value is NaN.

### <a id="MicroEngine_Graphics_SortedRenderer_Height"></a> Height

The renderer's height, in the units draws are made in.

```csharp
public int Height { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

No block is open.

### <a id="MicroEngine_Graphics_SortedRenderer_IsOpen"></a> IsOpen

Whether a block is open (between [Begin](MicroEngine.Graphics.SortedRenderer.md#MicroEngine_Graphics_SortedRenderer_Begin_MicroEngine_Graphics_IRenderer_) and [End](MicroEngine.Graphics.SortedRenderer.md#MicroEngine_Graphics_SortedRenderer_End)).

```csharp
public bool IsOpen { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_SortedRenderer_Layer"></a> Layer

The layer of the draws made from now on: lower layers are drawn first, whatever their depth. 0 at [Begin](MicroEngine.Graphics.SortedRenderer.md#MicroEngine_Graphics_SortedRenderer_Begin_MicroEngine_Graphics_IRenderer_).

```csharp
public int Layer { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_SortedRenderer_Width"></a> Width

The renderer's width, in the units draws are made in.

```csharp
public int Width { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

No block is open.

## Methods

### <a id="MicroEngine_Graphics_SortedRenderer_At_System_Int32_System_Single_"></a> At\(int, float\)

Sets [Layer](MicroEngine.Graphics.SortedRenderer.md#MicroEngine_Graphics_SortedRenderer_Layer) and [Depth](MicroEngine.Graphics.SortedRenderer.md#MicroEngine_Graphics_SortedRenderer_Depth) for the draws made from now on and returns this renderer.

```csharp
public SortedRenderer At(int layer, float depth = 0)
```

#### Parameters

`layer` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`depth` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [SortedRenderer](MicroEngine.Graphics.SortedRenderer.md)

### <a id="MicroEngine_Graphics_SortedRenderer_Begin_MicroEngine_Graphics_IRenderer_"></a> Begin\(IRenderer\)

Starts a sorted block over <code class="paramref">renderer</code>: draws made into this renderer are kept until
[End](MicroEngine.Graphics.SortedRenderer.md#MicroEngine_Graphics_SortedRenderer_End). Layer and depth start at 0.

```csharp
public SortedRenderer Begin(IRenderer renderer)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

#### Returns

 [SortedRenderer](MicroEngine.Graphics.SortedRenderer.md)

This renderer, to draw into.

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

A block is already open.

### <a id="MicroEngine_Graphics_SortedRenderer_Clear_MicroEngine_Color_"></a> Clear\(Color\)

Refused: a clear has no place in an order. Clear the renderer before [Begin](MicroEngine.Graphics.SortedRenderer.md#MicroEngine_Graphics_SortedRenderer_Begin_MicroEngine_Graphics_IRenderer_).

```csharp
public void Clear(Color color)
```

#### Parameters

`color` [Color](MicroEngine.Color.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Always.

### <a id="MicroEngine_Graphics_SortedRenderer_Draw_MicroEngine_Graphics_Texture_MicroEngine_RectF_System_Numerics_Vector2_MicroEngine_Color_MicroEngine_Graphics_DrawOptions__"></a> Draw\(Texture, RectF, Vector2, Color, in DrawOptions\)

```csharp
public void Draw(Texture texture, RectF source, Vector2 position, Color tint, in DrawOptions options = default)
```

#### Parameters

`texture` [Texture](MicroEngine.Graphics.Texture.md)

`source` [RectF](MicroEngine.RectF.md)

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`tint` [Color](MicroEngine.Color.md)

`options` [DrawOptions](MicroEngine.Graphics.DrawOptions.md)

#### Exceptions

 [ObjectDisposedException](https://learn.microsoft.com/dotnet/api/system.objectdisposedexception)

<code class="paramref">texture</code> was disposed; nothing is recorded.

### <a id="MicroEngine_Graphics_SortedRenderer_DrawLine_System_Numerics_Vector2_System_Numerics_Vector2_System_Single_MicroEngine_Color_"></a> DrawLine\(Vector2, Vector2, float, Color\)

Draws a straight line <code class="paramref">thickness</code> pixels wide. A zero-length line draws nothing.

```csharp
public void DrawLine(Vector2 from, Vector2 to, float thickness, Color color)
```

#### Parameters

`from` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`to` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`thickness` [float](https://learn.microsoft.com/dotnet/api/system.single)

`color` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Graphics_SortedRenderer_DrawStretched_MicroEngine_Graphics_Texture_MicroEngine_RectF_MicroEngine_RectF_MicroEngine_Color_"></a> DrawStretched\(Texture, RectF, RectF, Color\)

Draws the <code class="paramref">source</code> part of <code class="paramref">texture</code> scaled to fill <code class="paramref">destination</code>.

```csharp
public void DrawStretched(Texture texture, RectF source, RectF destination, Color tint)
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

 [ObjectDisposedException](https://learn.microsoft.com/dotnet/api/system.objectdisposedexception)

<code class="paramref">texture</code> was disposed; nothing is recorded.

### <a id="MicroEngine_Graphics_SortedRenderer_End"></a> End\(\)

Sends the recorded draws to the renderer, by layer, then depth, then the order they were made, and closes the
block. A block that left a camera or a state pushed draws nothing and is closed.

```csharp
public void End()
```

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

No block is open, or a camera or a state pushed in it was not popped.

### <a id="MicroEngine_Graphics_SortedRenderer_FillCircle_System_Numerics_Vector2_System_Single_MicroEngine_Color_"></a> FillCircle\(Vector2, float, Color\)

Fills a circle; <code class="paramref">radius</code> is in pixels.

```csharp
public void FillCircle(Vector2 center, float radius, Color color)
```

#### Parameters

`center` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

`color` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Graphics_SortedRenderer_FillRect_MicroEngine_RectF_MicroEngine_Color_"></a> FillRect\(RectF, Color\)

Fills a rectangle: the pixels whose centre lies inside it (see the rules of [IRenderer](MicroEngine.Graphics.IRenderer.md)). A fully transparent colour draws nothing.

```csharp
public void FillRect(RectF rect, Color color)
```

#### Parameters

`rect` [RectF](MicroEngine.RectF.md)

`color` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Graphics_SortedRenderer_FillTriangle_System_Numerics_Vector2_System_Numerics_Vector2_System_Numerics_Vector2_MicroEngine_Color_"></a> FillTriangle\(Vector2, Vector2, Vector2, Color\)

Fills a triangle.

```csharp
public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
```

#### Parameters

`a` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`b` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`c` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`color` [Color](MicroEngine.Color.md)

### <a id="MicroEngine_Graphics_SortedRenderer_PopCamera"></a> PopCamera\(\)

Returns to the coordinates in use before the last [PushCamera](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_PushCamera_MicroEngine_Graphics_Camera2D_).

```csharp
public void PopCamera()
```

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

No camera was pushed.

### <a id="MicroEngine_Graphics_SortedRenderer_PopState"></a> PopState\(\)

Returns to what was being drawn with before the last [PushState](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_PushState_MicroEngine_Graphics_DrawState__).

```csharp
public void PopState()
```

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

No state was pushed (in this target).

### <a id="MicroEngine_Graphics_SortedRenderer_PopTarget"></a> PopTarget\(\)

Refused: see [PushTarget](MicroEngine.Graphics.SortedRenderer.md#MicroEngine_Graphics_SortedRenderer_PushTarget_MicroEngine_Graphics_RenderTarget2D_).

```csharp
public void PopTarget()
```

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Always.

### <a id="MicroEngine_Graphics_SortedRenderer_PushCamera_MicroEngine_Graphics_Camera2D_"></a> PushCamera\(Camera2D\)

Draws in the world coordinates of <code class="paramref">camera</code> until [PopCamera](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_PopCamera): its [RenderOffset](MicroEngine.Graphics.Camera2D.md#MicroEngine_Graphics_Camera2D_RenderOffset)
replaces the current offset (cameras do not combine). Every push needs a pop in the same frame.

```csharp
public void PushCamera(Camera2D camera)
```

#### Parameters

`camera` [Camera2D](MicroEngine.Graphics.Camera2D.md)

### <a id="MicroEngine_Graphics_SortedRenderer_PushState_MicroEngine_Graphics_DrawState__"></a> PushState\(in DrawState\)

```csharp
public void PushState(in DrawState state)
```

#### Parameters

`state` [DrawState](MicroEngine.Graphics.DrawState.md)

### <a id="MicroEngine_Graphics_SortedRenderer_PushTarget_MicroEngine_Graphics_RenderTarget2D_"></a> PushTarget\(RenderTarget2D\)

Refused: draw into a render target with the renderer itself, outside the sorted block.

```csharp
public void PushTarget(RenderTarget2D target)
```

#### Parameters

`target` [RenderTarget2D](MicroEngine.Graphics.RenderTarget2D.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Always.

### <a id="MicroEngine_Graphics_SortedRenderer_SetBlendMode_MicroEngine_Graphics_BlendMode_"></a> SetBlendMode\(BlendMode\)

Sets how later draws combine with the canvas. Every frame starts with [Alpha](MicroEngine.Graphics.BlendMode.md). With source
colour s (after the tint), its alpha a and canvas colour d: <code>Alpha</code> writes s·a + d·(1 − a),
<code>Additive</code> s·a + d, <code>Multiply</code> s·a·d + d·(1 − a) (the colour multiplies the canvas in proportion to its
opacity), each channel clamped to 0..1.

```csharp
public void SetBlendMode(BlendMode mode)
```

#### Parameters

`mode` [BlendMode](MicroEngine.Graphics.BlendMode.md)

### <a id="MicroEngine_Graphics_SortedRenderer_SetClip_System_Nullable_MicroEngine_RectF__"></a> SetClip\(RectF?\)

Restricts the draws made from now on to a rectangle of the renderer (see [SetClip](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_SetClip_System_Nullable_MicroEngine_RectF__)); null
removes it. A block that sets a clip replaces the one the renderer had and leaves it without one.

```csharp
public void SetClip(RectF? screenRect)
```

#### Parameters

`screenRect` [RectF](MicroEngine.RectF.md)?

</div>
