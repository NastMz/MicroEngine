# Interface IGraphicsDevice

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

The single graphics device contract for every tier. Tiers differ only in [Capabilities](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_Capabilities); a request
outside them throws [GraphicsCapabilityException](MicroEngine.Graphics.Devices.GraphicsCapabilityException.md) at creation or recording time.

<p>Ordering rule: command lists execute in [Submit](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_Submit_MicroEngine_Graphics_Devices_CommandList_) order, and within a list in recording order
(updates included); [ReadPixels](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_ReadPixels_MicroEngine_Graphics_Devices_RenderTarget_MicroEngine_Graphics_Devices_RectI_) observes every list submitted before it.</p>
<p>Threading: one thread (the render thread) creates, records, submits and presents.</p>

```csharp
public interface IGraphicsDevice : IDisposable
```

#### Implements

[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Properties

### <a id="MicroEngine_Graphics_Devices_IGraphicsDevice_Capabilities"></a> Capabilities

Declared capabilities.

```csharp
GraphicsCapabilities Capabilities { get; }
```

#### Property Value

 [GraphicsCapabilities](MicroEngine.Graphics.Devices.GraphicsCapabilities.md)

### <a id="MicroEngine_Graphics_Devices_IGraphicsDevice_Counters"></a> Counters

Deterministic counts of the work submitted to this device since it was created (draw calls, passes, uploads, live
resources). Every device built on [GraphicsDeviceBase](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md) counts; a device that does not reports zeros.

```csharp
GraphicsCounters Counters { get; }
```

#### Property Value

 [GraphicsCounters](MicroEngine.Graphics.Devices.GraphicsCounters.md)

## Methods

### <a id="MicroEngine_Graphics_Devices_IGraphicsDevice_BeginCommands"></a> BeginCommands\(\)

Starts an empty command list for this device.

```csharp
CommandList BeginCommands()
```

#### Returns

 [CommandList](MicroEngine.Graphics.Devices.CommandList.md)

### <a id="MicroEngine_Graphics_Devices_IGraphicsDevice_CreateBuffer_MicroEngine_Graphics_Devices_BufferDescription__"></a> CreateBuffer\(in BufferDescription\)

Creates a buffer (every tier).

```csharp
GpuBuffer CreateBuffer(in BufferDescription description)
```

#### Parameters

`description` [BufferDescription](MicroEngine.Graphics.Devices.BufferDescription.md)

#### Returns

 [GpuBuffer](MicroEngine.Graphics.Devices.GpuBuffer.md)

### <a id="MicroEngine_Graphics_Devices_IGraphicsDevice_CreatePipeline_MicroEngine_Graphics_Devices_PipelineDescription_"></a> CreatePipeline\(PipelineDescription\)

Creates a custom pipeline; requires [Shaders](MicroEngine.Graphics.Devices.GraphicsFeatures.md), and [DepthBuffer](MicroEngine.Graphics.Devices.GraphicsFeatures.md) for depth.

```csharp
Pipeline CreatePipeline(PipelineDescription description)
```

#### Parameters

`description` [PipelineDescription](MicroEngine.Graphics.Devices.PipelineDescription.md)

#### Returns

 [Pipeline](MicroEngine.Graphics.Devices.Pipeline.md)

### <a id="MicroEngine_Graphics_Devices_IGraphicsDevice_CreateRenderTarget_MicroEngine_Graphics_Devices_RenderTargetDescription__"></a> CreateRenderTarget\(in RenderTargetDescription\)

Creates an offscreen target; requires [RenderTargets](MicroEngine.Graphics.Devices.GraphicsFeatures.md). Contents are undefined until a pass clears or draws.

```csharp
RenderTarget CreateRenderTarget(in RenderTargetDescription description)
```

#### Parameters

`description` [RenderTargetDescription](MicroEngine.Graphics.Devices.RenderTargetDescription.md)

#### Returns

 [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)

### <a id="MicroEngine_Graphics_Devices_IGraphicsDevice_CreateShader_MicroEngine_Graphics_Devices_ShaderDescription__"></a> CreateShader\(in ShaderDescription\)

Creates a custom shader; requires [Shaders](MicroEngine.Graphics.Devices.GraphicsFeatures.md) and a declared format. Invalid bytecode fails here or at [CreatePipeline](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_CreatePipeline_MicroEngine_Graphics_Devices_PipelineDescription_), never at draw.

```csharp
Shader CreateShader(in ShaderDescription description)
```

#### Parameters

`description` [ShaderDescription](MicroEngine.Graphics.Devices.ShaderDescription.md)

#### Returns

 [Shader](MicroEngine.Graphics.Devices.Shader.md)

### <a id="MicroEngine_Graphics_Devices_IGraphicsDevice_CreateTexture_MicroEngine_Graphics_Devices_TextureDescription__System_ReadOnlySpan_System_Byte__"></a> CreateTexture\(in TextureDescription, ReadOnlySpan<byte\>\)

Creates a sampled texture, optionally with initial data (exactly [ByteCount](MicroEngine.Graphics.Devices.TextureFormats.md#MicroEngine_Graphics_Devices_TextureFormats_ByteCount_MicroEngine_Graphics_Devices_TextureFormat_System_Int32_System_Int32_) bytes of level 0, or empty).

```csharp
Texture CreateTexture(in TextureDescription description, ReadOnlySpan<byte> initialData = default)
```

#### Parameters

`description` [TextureDescription](MicroEngine.Graphics.Devices.TextureDescription.md)

`initialData` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[byte](https://learn.microsoft.com/dotnet/api/system.byte)\>

#### Returns

 [Texture](MicroEngine.Graphics.Devices.Texture.md)

### <a id="MicroEngine_Graphics_Devices_IGraphicsDevice_GetBuiltInPipeline_MicroEngine_Graphics_Devices_BuiltInPipeline_MicroEngine_Graphics_Devices_BlendState_MicroEngine_Graphics_Devices_TextureFormat_System_Nullable_MicroEngine_Graphics_Devices_TextureFormat__"></a> GetBuiltInPipeline\(BuiltInPipeline, BlendState, TextureFormat, TextureFormat?\)

Returns the device-owned built-in pipeline for a kind, a blend state and the attachment formats it renders to
(every tier; the 2D batcher needs nothing else). A blend state outside
[BuiltInBlendStates](MicroEngine.Graphics.Devices.GraphicsCapabilities.md#MicroEngine_Graphics_Devices_GraphicsCapabilities_BuiltInBlendStates) requires [CustomBlend](MicroEngine.Graphics.Devices.GraphicsFeatures.md).

```csharp
Pipeline GetBuiltInPipeline(BuiltInPipeline kind, BlendState blend, TextureFormat colorFormat, TextureFormat? depthFormat = null)
```

#### Parameters

`kind` [BuiltInPipeline](MicroEngine.Graphics.Devices.BuiltInPipeline.md)

`blend` [BlendState](MicroEngine.Graphics.Devices.BlendState.md)

`colorFormat` [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)

`depthFormat` [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)?

#### Returns

 [Pipeline](MicroEngine.Graphics.Devices.Pipeline.md)

### <a id="MicroEngine_Graphics_Devices_IGraphicsDevice_GetSampler_MicroEngine_Graphics_Devices_SamplerDescription__"></a> GetSampler\(in SamplerDescription\)

Returns the cached sampler for a description.

```csharp
Sampler GetSampler(in SamplerDescription description)
```

#### Parameters

`description` [SamplerDescription](MicroEngine.Graphics.Devices.SamplerDescription.md)

#### Returns

 [Sampler](MicroEngine.Graphics.Devices.Sampler.md)

### <a id="MicroEngine_Graphics_Devices_IGraphicsDevice_Present"></a> Present\(\)

Presents the acquired surface target (no-op when none was acquired) and expires it.

```csharp
void Present()
```

### <a id="MicroEngine_Graphics_Devices_IGraphicsDevice_ReadPixels_MicroEngine_Graphics_Devices_RenderTarget_MicroEngine_Graphics_Devices_RectI_"></a> ReadPixels\(RenderTarget, RectI\)

Starts reading <code class="paramref">region</code> of an offscreen target; completes through the returned fence. Surface
targets cannot be read: captures render offscreen, read back, then draw the target to the surface.

```csharp
ReadbackRequest ReadPixels(RenderTarget target, RectI region)
```

#### Parameters

`target` [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)

`region` [RectI](MicroEngine.Graphics.Devices.RectI.md)

#### Returns

 [ReadbackRequest](MicroEngine.Graphics.Devices.ReadbackRequest.md)

### <a id="MicroEngine_Graphics_Devices_IGraphicsDevice_Submit_MicroEngine_Graphics_Devices_CommandList_"></a> Submit\(CommandList\)

Executes a finished list (no open pass) after every list submitted before it. A list is submitted once.

```csharp
void Submit(CommandList commands)
```

#### Parameters

`commands` [CommandList](MicroEngine.Graphics.Devices.CommandList.md)

### <a id="MicroEngine_Graphics_Devices_IGraphicsDevice_TryAcquireSurfaceTarget_MicroEngine_Graphics_Devices_RenderTarget__"></a> TryAcquireSurfaceTarget\(out RenderTarget?\)

Acquires this frame's surface target. False when there is nothing to present to (no surface, minimised, zero
size): the frame then skips its surface passes and presentation. The target expires at [Present](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_Present). Its
[ColorFormat](MicroEngine.Graphics.Devices.RenderTarget.md#MicroEngine_Graphics_Devices_RenderTarget_ColorFormat) is the format pipelines in its passes must declare; a tier-0 backend, whose
built-in pipelines do not depend on the format, reports the nominal format of its back buffer.

```csharp
bool TryAcquireSurfaceTarget(out RenderTarget? target)
```

#### Parameters

`target` [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_IGraphicsDevice_DeviceLost"></a> DeviceLost

Optional: raised only by backends that can detect loss (SDL_GPU never raises it); resources are marked [IsLost](MicroEngine.Graphics.Devices.GraphicsResource.md#MicroEngine_Graphics_Devices_GraphicsResource_IsLost).

```csharp
event Action<DeviceLostReason>? DeviceLost
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[DeviceLostReason](MicroEngine.Graphics.Devices.DeviceLostReason.md)\>?

</div>
