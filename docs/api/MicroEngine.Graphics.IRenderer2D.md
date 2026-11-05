# Interface IRenderer2D

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [The 2D batcher and the camera](../manual/rendering.md#the-2d-batcher-and-the-camera)  

The 2D renderer over the graphics device, implemented once in core: it writes vertices and indices into device
buffers and draws them with the built-in [Textured](MicroEngine.Graphics.Devices.BuiltInPipeline.md) pipeline only, so it runs unchanged
on every tier. Colours (tints, vertex colours) are premultiplied, and the default blend state is premultiplied alpha.

```csharp
public interface IRenderer2D
```

## Remarks

Rules: a draw's vertices are transformed on the CPU by its world matrix, then the pushed transforms, then the batch's
view, into target pixels; consecutive draws (after sorting) with the same texture and [RenderState](MicroEngine.Graphics.RenderState.md) share
one indexed draw; a [Clear](MicroEngine.Graphics.IRenderer2D.md#MicroEngine_Graphics_IRenderer2D_Clear_MicroEngine_ColorF_) or [Flush](MicroEngine.Graphics.IRenderer2D.md#MicroEngine_Graphics_IRenderer2D_Flush) submits what was recorded so far; nothing reaches the
device before [End](MicroEngine.Graphics.IRenderer2D.md#MicroEngine_Graphics_IRenderer2D_End), [Clear](MicroEngine.Graphics.IRenderer2D.md#MicroEngine_Graphics_IRenderer2D_Clear_MicroEngine_ColorF_) or [Flush](MicroEngine.Graphics.IRenderer2D.md#MicroEngine_Graphics_IRenderer2D_Flush).

## Properties

### <a id="MicroEngine_Graphics_IRenderer2D_State"></a> State

The state draws are recorded with; set it, or push and pop it.

```csharp
RenderState State { get; set; }
```

#### Property Value

 [RenderState](MicroEngine.Graphics.RenderState.md)

### <a id="MicroEngine_Graphics_IRenderer2D_Statistics"></a> Statistics

Counts of the current (or last) batch.

```csharp
Renderer2DStatistics Statistics { get; }
```

#### Property Value

 [Renderer2DStatistics](MicroEngine.Graphics.Renderer2DStatistics.md)

### <a id="MicroEngine_Graphics_IRenderer2D_Target"></a> Target

The target of the current batch, or null outside a batch.

```csharp
RenderTarget? Target { get; }
```

#### Property Value

 [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)?

## Methods

### <a id="MicroEngine_Graphics_IRenderer2D_Begin_MicroEngine_Graphics_BatchSettings__"></a> Begin\(in BatchSettings\)

Starts a batch.

```csharp
void Begin(in BatchSettings settings)
```

#### Parameters

`settings` [BatchSettings](MicroEngine.Graphics.BatchSettings.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

A batch is already open.

### <a id="MicroEngine_Graphics_IRenderer2D_Clear_MicroEngine_ColorF_"></a> Clear\(ColorF\)

Submits what was recorded, then clears the whole target (viewport and scissor ignored) to a premultiplied colour.

```csharp
void Clear(ColorF color)
```

#### Parameters

`color` [ColorF](MicroEngine.ColorF.md)

### <a id="MicroEngine_Graphics_IRenderer2D_Draw_MicroEngine_Graphics_Devices_Texture_MicroEngine_RectF_System_Numerics_Matrix3x2__MicroEngine_ColorF_System_Single_MicroEngine_Graphics_SpriteFlip_"></a> Draw\(Texture, RectF, in Matrix3x2, ColorF, float, SpriteFlip\)

Draws the <code class="paramref">source</code> texels of <code class="paramref">texture</code> as a quad of source.Width × source.Height
local units placed by <code class="paramref">world</code>, multiplied by the premultiplied <code class="paramref">tint</code>.

```csharp
void Draw(Texture texture, RectF source, in Matrix3x2 world, ColorF tint, float depth = 0, SpriteFlip flip = SpriteFlip.None)
```

#### Parameters

`texture` [Texture](MicroEngine.Graphics.Devices.Texture.md)

`source` [RectF](MicroEngine.RectF.md)

`world` [Matrix3x2](https://learn.microsoft.com/dotnet/api/system.numerics.matrix3x2)

`tint` [ColorF](MicroEngine.ColorF.md)

`depth` [float](https://learn.microsoft.com/dotnet/api/system.single)

`flip` [SpriteFlip](MicroEngine.Graphics.SpriteFlip.md)

### <a id="MicroEngine_Graphics_IRenderer2D_DrawGeometry_MicroEngine_Graphics_Devices_Texture_System_ReadOnlySpan_MicroEngine_Graphics_Devices_Vertex2D__System_ReadOnlySpan_System_UInt16__System_Single_"></a> DrawGeometry\(Texture?, ReadOnlySpan<Vertex2D\>, ReadOnlySpan<ushort\>, float\)

Draws triangles (positions transformed like a draw's local units); a null texture draws the vertex colours.

```csharp
void DrawGeometry(Texture? texture, ReadOnlySpan<Vertex2D> vertices, ReadOnlySpan<ushort> indices, float depth = 0)
```

#### Parameters

`texture` [Texture](MicroEngine.Graphics.Devices.Texture.md)?

`vertices` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[Vertex2D](MicroEngine.Graphics.Devices.Vertex2D.md)\>

`indices` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[ushort](https://learn.microsoft.com/dotnet/api/system.uint16)\>

`depth` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

An index selects no vertex, or more than 65,535 vertices.

### <a id="MicroEngine_Graphics_IRenderer2D_End"></a> End\(\)

Submits what remains and ends the batch.

```csharp
void End()
```

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

No batch is open, or a pushed transform or state was not popped.

### <a id="MicroEngine_Graphics_IRenderer2D_Flush"></a> Flush\(\)

Submits what was recorded so far (for example before reading the target back).

```csharp
void Flush()
```

### <a id="MicroEngine_Graphics_IRenderer2D_PopState"></a> PopState\(\)

Restores the state saved by the last [PushState](MicroEngine.Graphics.IRenderer2D.md#MicroEngine_Graphics_IRenderer2D_PushState_MicroEngine_Graphics_RenderState__).

```csharp
void PopState()
```

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Nothing was pushed.

### <a id="MicroEngine_Graphics_IRenderer2D_PopTransform"></a> PopTransform\(\)

Returns to the transform before the last [PushTransform](MicroEngine.Graphics.IRenderer2D.md#MicroEngine_Graphics_IRenderer2D_PushTransform_System_Numerics_Matrix3x2__).

```csharp
void PopTransform()
```

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Nothing was pushed.

### <a id="MicroEngine_Graphics_IRenderer2D_PushState_MicroEngine_Graphics_RenderState__"></a> PushState\(in RenderState\)

Saves [State](MicroEngine.Graphics.IRenderer2D.md#MicroEngine_Graphics_IRenderer2D_State) and sets <code class="paramref">state</code> until [PopState](MicroEngine.Graphics.IRenderer2D.md#MicroEngine_Graphics_IRenderer2D_PopState).

```csharp
void PushState(in RenderState state)
```

#### Parameters

`state` [RenderState](MicroEngine.Graphics.RenderState.md)

### <a id="MicroEngine_Graphics_IRenderer2D_PushTransform_System_Numerics_Matrix3x2__"></a> PushTransform\(in Matrix3x2\)

Multiplies later draws' transforms by <code class="paramref">transform</code> (before the view) until [PopTransform](MicroEngine.Graphics.IRenderer2D.md#MicroEngine_Graphics_IRenderer2D_PopTransform).

```csharp
void PushTransform(in Matrix3x2 transform)
```

#### Parameters

`transform` [Matrix3x2](https://learn.microsoft.com/dotnet/api/system.numerics.matrix3x2)

</div>
