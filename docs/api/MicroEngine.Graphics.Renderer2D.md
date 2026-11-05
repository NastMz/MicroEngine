# Class Renderer2D

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [The 2D batcher and the camera](../manual/rendering.md#the-2d-batcher-and-the-camera)  

The engine's 2D batcher ([IRenderer2D](MicroEngine.Graphics.IRenderer2D.md)) over one graphics device.

```csharp
public sealed class Renderer2D : IRenderer2D, IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Renderer2D](MicroEngine.Graphics.Renderer2D.md)

#### Implements

[IRenderer2D](MicroEngine.Graphics.IRenderer2D.md), 
[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Constructors

### <a id="MicroEngine_Graphics_Renderer2D__ctor_MicroEngine_Graphics_Devices_IGraphicsDevice_"></a> Renderer2D\(IGraphicsDevice\)

Creates the batcher (and its 1 × 1 white texture, which untextured draws sample).

```csharp
public Renderer2D(IGraphicsDevice device)
```

#### Parameters

`device` [IGraphicsDevice](MicroEngine.Graphics.Devices.IGraphicsDevice.md)

## Properties

### <a id="MicroEngine_Graphics_Renderer2D_Device"></a> Device

The device the batcher draws with.

```csharp
public IGraphicsDevice Device { get; }
```

#### Property Value

 [IGraphicsDevice](MicroEngine.Graphics.Devices.IGraphicsDevice.md)

### <a id="MicroEngine_Graphics_Renderer2D_State"></a> State

The state draws are recorded with; set it, or push and pop it.

```csharp
public RenderState State { get; set; }
```

#### Property Value

 [RenderState](MicroEngine.Graphics.RenderState.md)

### <a id="MicroEngine_Graphics_Renderer2D_Statistics"></a> Statistics

Counts of the current (or last) batch.

```csharp
public Renderer2DStatistics Statistics { get; }
```

#### Property Value

 [Renderer2DStatistics](MicroEngine.Graphics.Renderer2DStatistics.md)

### <a id="MicroEngine_Graphics_Renderer2D_Target"></a> Target

The target of the current batch, or null outside a batch.

```csharp
public RenderTarget? Target { get; }
```

#### Property Value

 [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)?

### <a id="MicroEngine_Graphics_Renderer2D_White"></a> White

The 1 × 1 opaque white texture untextured draws sample.

```csharp
public Texture White { get; }
```

#### Property Value

 [Texture](MicroEngine.Graphics.Devices.Texture.md)

## Methods

### <a id="MicroEngine_Graphics_Renderer2D_Begin_MicroEngine_Graphics_BatchSettings__"></a> Begin\(in BatchSettings\)

```csharp
public void Begin(in BatchSettings settings)
```

#### Parameters

`settings` [BatchSettings](MicroEngine.Graphics.BatchSettings.md)

### <a id="MicroEngine_Graphics_Renderer2D_Clear_MicroEngine_ColorF_"></a> Clear\(ColorF\)

Submits what was recorded, then clears the whole target (viewport and scissor ignored) to a premultiplied colour.

```csharp
public void Clear(ColorF color)
```

#### Parameters

`color` [ColorF](MicroEngine.ColorF.md)

### <a id="MicroEngine_Graphics_Renderer2D_Dispose"></a> Dispose\(\)

Releases the batcher's buffers and white texture.

```csharp
public void Dispose()
```

### <a id="MicroEngine_Graphics_Renderer2D_Draw_MicroEngine_Graphics_Devices_Texture_MicroEngine_RectF_System_Numerics_Matrix3x2__MicroEngine_ColorF_System_Single_MicroEngine_Graphics_SpriteFlip_"></a> Draw\(Texture, RectF, in Matrix3x2, ColorF, float, SpriteFlip\)

```csharp
public void Draw(Texture texture, RectF source, in Matrix3x2 world, ColorF tint, float depth = 0, SpriteFlip flip = SpriteFlip.None)
```

#### Parameters

`texture` [Texture](MicroEngine.Graphics.Devices.Texture.md)

`source` [RectF](MicroEngine.RectF.md)

`world` [Matrix3x2](https://learn.microsoft.com/dotnet/api/system.numerics.matrix3x2)

`tint` [ColorF](MicroEngine.ColorF.md)

`depth` [float](https://learn.microsoft.com/dotnet/api/system.single)

`flip` [SpriteFlip](MicroEngine.Graphics.SpriteFlip.md)

### <a id="MicroEngine_Graphics_Renderer2D_DrawGeometry_MicroEngine_Graphics_Devices_Texture_System_ReadOnlySpan_MicroEngine_Graphics_Devices_Vertex2D__System_ReadOnlySpan_System_UInt16__System_Single_"></a> DrawGeometry\(Texture?, ReadOnlySpan<Vertex2D\>, ReadOnlySpan<ushort\>, float\)

Draws triangles (positions transformed like a draw's local units); a null texture draws the vertex colours.

```csharp
public void DrawGeometry(Texture? texture, ReadOnlySpan<Vertex2D> vertices, ReadOnlySpan<ushort> indices, float depth = 0)
```

#### Parameters

`texture` [Texture](MicroEngine.Graphics.Devices.Texture.md)?

`vertices` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[Vertex2D](MicroEngine.Graphics.Devices.Vertex2D.md)\>

`indices` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[ushort](https://learn.microsoft.com/dotnet/api/system.uint16)\>

`depth` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

An index selects no vertex, or more than 65,535 vertices.

### <a id="MicroEngine_Graphics_Renderer2D_End"></a> End\(\)

Submits what remains and ends the batch.

```csharp
public void End()
```

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

No batch is open, or a pushed transform or state was not popped.

### <a id="MicroEngine_Graphics_Renderer2D_Flush"></a> Flush\(\)

Submits what was recorded so far (for example before reading the target back).

```csharp
public void Flush()
```

### <a id="MicroEngine_Graphics_Renderer2D_PopState"></a> PopState\(\)

Restores the state saved by the last [PushState](MicroEngine.Graphics.IRenderer2D.md#MicroEngine_Graphics_IRenderer2D_PushState_MicroEngine_Graphics_RenderState__).

```csharp
public void PopState()
```

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Nothing was pushed.

### <a id="MicroEngine_Graphics_Renderer2D_PopTransform"></a> PopTransform\(\)

Returns to the transform before the last [PushTransform](MicroEngine.Graphics.IRenderer2D.md#MicroEngine_Graphics_IRenderer2D_PushTransform_System_Numerics_Matrix3x2__).

```csharp
public void PopTransform()
```

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

Nothing was pushed.

### <a id="MicroEngine_Graphics_Renderer2D_PushState_MicroEngine_Graphics_RenderState__"></a> PushState\(in RenderState\)

```csharp
public void PushState(in RenderState state)
```

#### Parameters

`state` [RenderState](MicroEngine.Graphics.RenderState.md)

### <a id="MicroEngine_Graphics_Renderer2D_PushTransform_System_Numerics_Matrix3x2__"></a> PushTransform\(in Matrix3x2\)

```csharp
public void PushTransform(in Matrix3x2 transform)
```

#### Parameters

`transform` [Matrix3x2](https://learn.microsoft.com/dotnet/api/system.numerics.matrix3x2)

</div>
