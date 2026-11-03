# Class CommandList

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

A list of commands for one device, recorded on the render thread and validated here, in core, as it is recorded
(before any backend sees it). Every violation throws at the call that causes it:

<ul><li>a resource of another device: `ArgumentException`; a disposed one: `ObjectDisposedException`;</li><li>wrong nesting (pass in a pass, draw or binding outside a pass, update inside a pass, recording after submit): `InvalidOperationException`;</li><li>a pipeline whose attachment formats differ from the pass's, a draw without the bindings the pipeline needs, a
range outside a buffer, sampling the pass's own colour target: `InvalidOperationException` or
`ArgumentOutOfRangeException`;</li><li>an undeclared capability (instancing): [GraphicsCapabilityException](MicroEngine.Graphics.Devices.GraphicsCapabilityException.md).</li></ul>

Rules: commands execute in recording order (an update affects only draws recorded after it); bindings, pipeline,
viewport and scissor are pass-scoped (each pass starts with none bound, the whole target as viewport and no scissor);
uniform data persist for the rest of the list once set.

```csharp
public sealed class CommandList
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[CommandList](MicroEngine.Graphics.Devices.CommandList.md)

## Constructors

### <a id="MicroEngine_Graphics_Devices_CommandList__ctor_MicroEngine_Graphics_Devices_IGraphicsDevice_"></a> CommandList\(IGraphicsDevice\)

Creates an empty list; backends call this from [BeginCommands](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_BeginCommands).

```csharp
public CommandList(IGraphicsDevice device)
```

#### Parameters

`device` [IGraphicsDevice](MicroEngine.Graphics.Devices.IGraphicsDevice.md)

## Properties

### <a id="MicroEngine_Graphics_Devices_CommandList_Commands"></a> Commands

The recorded commands (backend-facing).

```csharp
public ReadOnlySpan<Command> Commands { get; }
```

#### Property Value

 [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[Command](MicroEngine.Graphics.Devices.Command.md)\>

### <a id="MicroEngine_Graphics_Devices_CommandList_Device"></a> Device

The device the list records for.

```csharp
public IGraphicsDevice Device { get; }
```

#### Property Value

 [IGraphicsDevice](MicroEngine.Graphics.Devices.IGraphicsDevice.md)

### <a id="MicroEngine_Graphics_Devices_CommandList_IsInPass"></a> IsInPass

True between [BeginPass](MicroEngine.Graphics.Devices.CommandList.md#MicroEngine_Graphics_Devices_CommandList_BeginPass_MicroEngine_Graphics_Devices_PassDescription__) and [EndPass](MicroEngine.Graphics.Devices.CommandList.md#MicroEngine_Graphics_Devices_CommandList_EndPass).

```csharp
public bool IsInPass { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_CommandList_IsSubmitted"></a> IsSubmitted

True once submitted; a submitted list cannot record or be submitted again.

```csharp
public bool IsSubmitted { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_Graphics_Devices_CommandList_BeginPass_MicroEngine_Graphics_Devices_PassDescription__"></a> BeginPass\(in PassDescription\)

Begins a pass; resets bindings, viewport (whole target) and scissor (none).

```csharp
public void BeginPass(in PassDescription pass)
```

#### Parameters

`pass` [PassDescription](MicroEngine.Graphics.Devices.PassDescription.md)

### <a id="MicroEngine_Graphics_Devices_CommandList_Draw_System_Int32_System_Int32_System_Int32_"></a> Draw\(int, int, int\)

Draws <code class="paramref">vertexCount</code> vertices from <code class="paramref">firstVertex</code>.

```csharp
public void Draw(int vertexCount, int firstVertex = 0, int instances = 1)
```

#### Parameters

`vertexCount` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`firstVertex` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`instances` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_CommandList_DrawIndexed_System_Int32_System_Int32_System_Int32_System_Int32_"></a> DrawIndexed\(int, int, int, int\)

Draws <code class="paramref">indexCount</code> indices from <code class="paramref">firstIndex</code>; each index plus <code class="paramref">baseVertex</code>
selects a vertex. Index values are not validated (they live in device memory): an index that selects a vertex
outside the bound buffer is undefined behaviour (a tier-0 backend may throw at [Submit](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_Submit_MicroEngine_Graphics_Devices_CommandList_)).

```csharp
public void DrawIndexed(int indexCount, int firstIndex = 0, int baseVertex = 0, int instances = 1)
```

#### Parameters

`indexCount` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`firstIndex` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`baseVertex` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`instances` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_CommandList_EndPass"></a> EndPass\(\)

Ends the current pass.

```csharp
public void EndPass()
```

### <a id="MicroEngine_Graphics_Devices_CommandList_Payload_MicroEngine_Graphics_Devices_Command__"></a> Payload\(in Command\)

The data of an update or uniform command (backend-facing).

```csharp
public ReadOnlySpan<byte> Payload(in Command command)
```

#### Parameters

`command` [Command](MicroEngine.Graphics.Devices.Command.md)

#### Returns

 [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[byte](https://learn.microsoft.com/dotnet/api/system.byte)\>

### <a id="MicroEngine_Graphics_Devices_CommandList_SetIndices_MicroEngine_Graphics_Devices_GpuBuffer_MicroEngine_Graphics_Devices_IndexSize_"></a> SetIndices\(GpuBuffer, IndexSize\)

Binds the index buffer.

```csharp
public void SetIndices(GpuBuffer buffer, IndexSize size = IndexSize.Bits16)
```

#### Parameters

`buffer` [GpuBuffer](MicroEngine.Graphics.Devices.GpuBuffer.md)

`size` [IndexSize](MicroEngine.Graphics.Devices.IndexSize.md)

### <a id="MicroEngine_Graphics_Devices_CommandList_SetPipeline_MicroEngine_Graphics_Devices_Pipeline_"></a> SetPipeline\(Pipeline\)

Binds a pipeline; its colour and depth formats must equal the pass attachments'.

```csharp
public void SetPipeline(Pipeline pipeline)
```

#### Parameters

`pipeline` [Pipeline](MicroEngine.Graphics.Devices.Pipeline.md)

### <a id="MicroEngine_Graphics_Devices_CommandList_SetScissor_System_Nullable_MicroEngine_Graphics_Devices_RectI__"></a> SetScissor\(RectI?\)

Sets the scissor in target pixels (absolute, independent of the viewport), clipped to the target; null = none. Clears ignore it.

```csharp
public void SetScissor(RectI? scissor)
```

#### Parameters

`scissor` [RectI](MicroEngine.Graphics.Devices.RectI.md)?

### <a id="MicroEngine_Graphics_Devices_CommandList_SetTexture_System_Int32_MicroEngine_Graphics_Devices_Texture_MicroEngine_Graphics_Devices_Sampler_"></a> SetTexture\(int, Texture, Sampler\)

Binds a texture and sampler to a fragment slot. The pass's own colour target cannot be sampled.

```csharp
public void SetTexture(int slot, Texture texture, Sampler sampler)
```

#### Parameters

`slot` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`texture` [Texture](MicroEngine.Graphics.Devices.Texture.md)

`sampler` [Sampler](MicroEngine.Graphics.Devices.Sampler.md)

### <a id="MicroEngine_Graphics_Devices_CommandList_SetUniforms_MicroEngine_Graphics_Devices_ShaderStage_System_Int32_System_ReadOnlySpan_System_Byte__"></a> SetUniforms\(ShaderStage, int, ReadOnlySpan<byte\>\)

Pushes uniform data for a slot of a stage of the bound custom pipeline (built-in pipelines have none).

```csharp
public void SetUniforms(ShaderStage stage, int slot, ReadOnlySpan<byte> data)
```

#### Parameters

`stage` [ShaderStage](MicroEngine.Graphics.Devices.ShaderStage.md)

`slot` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`data` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[byte](https://learn.microsoft.com/dotnet/api/system.byte)\>

### <a id="MicroEngine_Graphics_Devices_CommandList_SetUniforms__1_MicroEngine_Graphics_Devices_ShaderStage_System_Int32___0__"></a> SetUniforms<T\>\(ShaderStage, int, in T\)

Typed overload of [SetUniforms](MicroEngine.Graphics.Devices.CommandList.md#MicroEngine_Graphics_Devices_CommandList_SetUniforms_MicroEngine_Graphics_Devices_ShaderStage_System_Int32_System_ReadOnlySpan_System_Byte__): the bytes of <code class="paramref">data</code>, as laid out in memory.

```csharp
public void SetUniforms<T>(ShaderStage stage, int slot, in T data) where T : unmanaged
```

#### Parameters

`stage` [ShaderStage](MicroEngine.Graphics.Devices.ShaderStage.md)

`slot` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`data` T

#### Type Parameters

`T` 

### <a id="MicroEngine_Graphics_Devices_CommandList_SetVertices_MicroEngine_Graphics_Devices_GpuBuffer_"></a> SetVertices\(GpuBuffer\)

Binds the vertex buffer (slot 0).

```csharp
public void SetVertices(GpuBuffer buffer)
```

#### Parameters

`buffer` [GpuBuffer](MicroEngine.Graphics.Devices.GpuBuffer.md)

### <a id="MicroEngine_Graphics_Devices_CommandList_SetViewport_MicroEngine_Graphics_Devices_RectI__"></a> SetViewport\(in RectI\)

Sets the viewport: built-in vertex positions are pixels relative to its origin, and it clips. Must lie inside the colour target.

```csharp
public void SetViewport(in RectI viewport)
```

#### Parameters

`viewport` [RectI](MicroEngine.Graphics.Devices.RectI.md)

### <a id="MicroEngine_Graphics_Devices_CommandList_UpdateBuffer_MicroEngine_Graphics_Devices_GpuBuffer_System_Int32_System_ReadOnlySpan_System_Byte__"></a> UpdateBuffer\(GpuBuffer, int, ReadOnlySpan<byte\>\)

Writes <code class="paramref">data</code> into a buffer at <code class="paramref">offset</code>, outside passes; draws recorded later see it.

```csharp
public void UpdateBuffer(GpuBuffer buffer, int offset, ReadOnlySpan<byte> data)
```

#### Parameters

`buffer` [GpuBuffer](MicroEngine.Graphics.Devices.GpuBuffer.md)

`offset` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`data` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[byte](https://learn.microsoft.com/dotnet/api/system.byte)\>

### <a id="MicroEngine_Graphics_Devices_CommandList_UpdateBuffer__1_MicroEngine_Graphics_Devices_GpuBuffer_System_Int32_System_ReadOnlySpan___0__"></a> UpdateBuffer<T\>\(GpuBuffer, int, ReadOnlySpan<T\>\)

Typed overload of [UpdateBuffer](MicroEngine.Graphics.Devices.CommandList.md#MicroEngine_Graphics_Devices_CommandList_UpdateBuffer_MicroEngine_Graphics_Devices_GpuBuffer_System_Int32_System_ReadOnlySpan_System_Byte__).

```csharp
public void UpdateBuffer<T>(GpuBuffer buffer, int offset, ReadOnlySpan<T> data) where T : unmanaged
```

#### Parameters

`buffer` [GpuBuffer](MicroEngine.Graphics.Devices.GpuBuffer.md)

`offset` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`data` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<T\>

#### Type Parameters

`T` 

### <a id="MicroEngine_Graphics_Devices_CommandList_UpdateTexture_MicroEngine_Graphics_Devices_Texture_MicroEngine_Graphics_Devices_RectI__System_ReadOnlySpan_System_Byte__"></a> UpdateTexture\(Texture, in RectI, ReadOnlySpan<byte\>\)

Writes tightly packed texel data into a region of mip level 0, outside passes.

```csharp
public void UpdateTexture(Texture texture, in RectI region, ReadOnlySpan<byte> data)
```

#### Parameters

`texture` [Texture](MicroEngine.Graphics.Devices.Texture.md)

`region` [RectI](MicroEngine.Graphics.Devices.RectI.md)

`data` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[byte](https://learn.microsoft.com/dotnet/api/system.byte)\>

</div>
