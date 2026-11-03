# Class GraphicsDeviceBase

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

Core half of every device: validates each call against the written rules and the declared capabilities, then hands
the backend a call it can execute. Backends derive from it and implement only the <code>*Core</code> members, so ownership,
disposal, ranges and capability errors behave identically on every tier.

```csharp
public abstract class GraphicsDeviceBase : IGraphicsDevice, IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GraphicsDeviceBase](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md)

#### Derived

[ReferenceGraphicsDevice](MicroEngine.Graphics.Devices.ReferenceGraphicsDevice.md)

#### Implements

[IGraphicsDevice](MicroEngine.Graphics.Devices.IGraphicsDevice.md), 
[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Constructors

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase__ctor"></a> GraphicsDeviceBase\(\)

```csharp
protected GraphicsDeviceBase()
```

## Properties

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_Capabilities"></a> Capabilities

Declared capabilities.

```csharp
public abstract GraphicsCapabilities Capabilities { get; }
```

#### Property Value

 [GraphicsCapabilities](MicroEngine.Graphics.Devices.GraphicsCapabilities.md)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_CompletedSubmissions"></a> CompletedSubmissions

The number of submitted command lists the backend has finished executing. Resources disposed after the n-th
submission are released once this reaches n. The default, the number of lists submitted, suits devices that
execute a list completely inside [Submit](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_Submit_MicroEngine_Graphics_Devices_CommandList_); a device that executes later overrides it
and calls [CollectRetired](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md#MicroEngine_Graphics_Devices_GraphicsDeviceBase_CollectRetired) when work completes.

```csharp
protected virtual long CompletedSubmissions { get; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_Counters"></a> Counters

Deterministic counts of the work submitted to this device since it was created.

```csharp
public GraphicsCounters Counters { get; }
```

#### Property Value

 [GraphicsCounters](MicroEngine.Graphics.Devices.GraphicsCounters.md)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_IsDisposed"></a> IsDisposed

True after [Dispose](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md#MicroEngine_Graphics_Devices_GraphicsDeviceBase_Dispose).

```csharp
public bool IsDisposed { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_PendingReleases"></a> PendingReleases

Disposed resources whose backend release still waits for submitted work.

```csharp
public int PendingReleases { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_BeginCommands"></a> BeginCommands\(\)

Starts an empty command list for this device.

```csharp
public CommandList BeginCommands()
```

#### Returns

 [CommandList](MicroEngine.Graphics.Devices.CommandList.md)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_CollectRetired"></a> CollectRetired\(\)

Releases the disposed resources no submitted work uses any more (see [CompletedSubmissions](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md#MicroEngine_Graphics_Devices_GraphicsDeviceBase_CompletedSubmissions)).

```csharp
protected void CollectRetired()
```

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_CreateBuffer_MicroEngine_Graphics_Devices_BufferDescription__"></a> CreateBuffer\(in BufferDescription\)

```csharp
public GpuBuffer CreateBuffer(in BufferDescription description)
```

#### Parameters

`description` [BufferDescription](MicroEngine.Graphics.Devices.BufferDescription.md)

#### Returns

 [GpuBuffer](MicroEngine.Graphics.Devices.GpuBuffer.md)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_CreateBufferCore_MicroEngine_Graphics_Devices_BufferDescription__"></a> CreateBufferCore\(in BufferDescription\)

Backend: create a validated buffer.

```csharp
protected abstract GpuBuffer CreateBufferCore(in BufferDescription description)
```

#### Parameters

`description` [BufferDescription](MicroEngine.Graphics.Devices.BufferDescription.md)

#### Returns

 [GpuBuffer](MicroEngine.Graphics.Devices.GpuBuffer.md)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_CreateBuiltInPipelineCore_MicroEngine_Graphics_Devices_BuiltInPipeline_MicroEngine_Graphics_Devices_BlendState_MicroEngine_Graphics_Devices_TextureFormat_System_Nullable_MicroEngine_Graphics_Devices_TextureFormat__"></a> CreateBuiltInPipelineCore\(BuiltInPipeline, BlendState, TextureFormat, TextureFormat?\)

Backend: create a built-in pipeline variant (core caches it).

```csharp
protected abstract Pipeline CreateBuiltInPipelineCore(BuiltInPipeline kind, BlendState blend, TextureFormat colorFormat, TextureFormat? depthFormat)
```

#### Parameters

`kind` [BuiltInPipeline](MicroEngine.Graphics.Devices.BuiltInPipeline.md)

`blend` [BlendState](MicroEngine.Graphics.Devices.BlendState.md)

`colorFormat` [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)

`depthFormat` [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)?

#### Returns

 [Pipeline](MicroEngine.Graphics.Devices.Pipeline.md)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_CreatePipeline_MicroEngine_Graphics_Devices_PipelineDescription_"></a> CreatePipeline\(PipelineDescription\)

Creates a custom pipeline; requires [Shaders](MicroEngine.Graphics.Devices.GraphicsFeatures.md), and [DepthBuffer](MicroEngine.Graphics.Devices.GraphicsFeatures.md) for depth.

```csharp
public Pipeline CreatePipeline(PipelineDescription description)
```

#### Parameters

`description` [PipelineDescription](MicroEngine.Graphics.Devices.PipelineDescription.md)

#### Returns

 [Pipeline](MicroEngine.Graphics.Devices.Pipeline.md)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_CreatePipelineCore_MicroEngine_Graphics_Devices_PipelineDescription_"></a> CreatePipelineCore\(PipelineDescription\)

Backend: create a validated custom pipeline (tier 0 is never called: core refuses first).

```csharp
protected abstract Pipeline CreatePipelineCore(PipelineDescription description)
```

#### Parameters

`description` [PipelineDescription](MicroEngine.Graphics.Devices.PipelineDescription.md)

#### Returns

 [Pipeline](MicroEngine.Graphics.Devices.Pipeline.md)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_CreateRenderTarget_MicroEngine_Graphics_Devices_RenderTargetDescription__"></a> CreateRenderTarget\(in RenderTargetDescription\)

```csharp
public RenderTarget CreateRenderTarget(in RenderTargetDescription description)
```

#### Parameters

`description` [RenderTargetDescription](MicroEngine.Graphics.Devices.RenderTargetDescription.md)

#### Returns

 [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_CreateRenderTargetCore_MicroEngine_Graphics_Devices_RenderTargetDescription__"></a> CreateRenderTargetCore\(in RenderTargetDescription\)

Backend: create a validated render target.

```csharp
protected abstract RenderTarget CreateRenderTargetCore(in RenderTargetDescription description)
```

#### Parameters

`description` [RenderTargetDescription](MicroEngine.Graphics.Devices.RenderTargetDescription.md)

#### Returns

 [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_CreateSamplerCore_MicroEngine_Graphics_Devices_SamplerDescription__"></a> CreateSamplerCore\(in SamplerDescription\)

Backend: create a validated sampler (core caches it).

```csharp
protected abstract Sampler CreateSamplerCore(in SamplerDescription description)
```

#### Parameters

`description` [SamplerDescription](MicroEngine.Graphics.Devices.SamplerDescription.md)

#### Returns

 [Sampler](MicroEngine.Graphics.Devices.Sampler.md)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_CreateShader_MicroEngine_Graphics_Devices_ShaderDescription__"></a> CreateShader\(in ShaderDescription\)

```csharp
public Shader CreateShader(in ShaderDescription description)
```

#### Parameters

`description` [ShaderDescription](MicroEngine.Graphics.Devices.ShaderDescription.md)

#### Returns

 [Shader](MicroEngine.Graphics.Devices.Shader.md)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_CreateShaderCore_MicroEngine_Graphics_Devices_ShaderDescription__"></a> CreateShaderCore\(in ShaderDescription\)

Backend: create a validated shader (tier 0 is never called: core refuses first).

```csharp
protected abstract Shader CreateShaderCore(in ShaderDescription description)
```

#### Parameters

`description` [ShaderDescription](MicroEngine.Graphics.Devices.ShaderDescription.md)

#### Returns

 [Shader](MicroEngine.Graphics.Devices.Shader.md)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_CreateTexture_MicroEngine_Graphics_Devices_TextureDescription__System_ReadOnlySpan_System_Byte__"></a> CreateTexture\(in TextureDescription, ReadOnlySpan<byte\>\)

```csharp
public Texture CreateTexture(in TextureDescription description, ReadOnlySpan<byte> initialData = default)
```

#### Parameters

`description` [TextureDescription](MicroEngine.Graphics.Devices.TextureDescription.md)

`initialData` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[byte](https://learn.microsoft.com/dotnet/api/system.byte)\>

#### Returns

 [Texture](MicroEngine.Graphics.Devices.Texture.md)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_CreateTextureCore_MicroEngine_Graphics_Devices_TextureDescription__System_ReadOnlySpan_System_Byte__"></a> CreateTextureCore\(in TextureDescription, ReadOnlySpan<byte\>\)

Backend: create a validated texture.

```csharp
protected abstract Texture CreateTextureCore(in TextureDescription description, ReadOnlySpan<byte> initialData)
```

#### Parameters

`description` [TextureDescription](MicroEngine.Graphics.Devices.TextureDescription.md)

`initialData` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[byte](https://learn.microsoft.com/dotnet/api/system.byte)\>

#### Returns

 [Texture](MicroEngine.Graphics.Devices.Texture.md)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_Dispose"></a> Dispose\(\)

Releases backend objects (waiting for the GPU first). Resources disposed later are ignored by the backend.

```csharp
public void Dispose()
```

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_DisposeCore_System_Collections_Generic_IReadOnlyList_MicroEngine_Graphics_Devices_Pipeline__System_Collections_Generic_IReadOnlyList_MicroEngine_Graphics_Devices_Sampler__"></a> DisposeCore\(IReadOnlyList<Pipeline\>, IReadOnlyList<Sampler\>\)

Backend: release everything, the device-owned built-ins and samplers included.

```csharp
protected abstract void DisposeCore(IReadOnlyList<Pipeline> builtIns, IReadOnlyList<Sampler> samplers)
```

#### Parameters

`builtIns` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[Pipeline](MicroEngine.Graphics.Devices.Pipeline.md)\>

`samplers` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[Sampler](MicroEngine.Graphics.Devices.Sampler.md)\>

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_GetBuiltInPipeline_MicroEngine_Graphics_Devices_BuiltInPipeline_MicroEngine_Graphics_Devices_BlendState_MicroEngine_Graphics_Devices_TextureFormat_System_Nullable_MicroEngine_Graphics_Devices_TextureFormat__"></a> GetBuiltInPipeline\(BuiltInPipeline, BlendState, TextureFormat, TextureFormat?\)

Returns the device-owned built-in pipeline for a kind, a blend state and the attachment formats it renders to
(every tier; the 2D batcher needs nothing else). A blend state outside
[BuiltInBlendStates](MicroEngine.Graphics.Devices.GraphicsCapabilities.md#MicroEngine_Graphics_Devices_GraphicsCapabilities_BuiltInBlendStates) requires [CustomBlend](MicroEngine.Graphics.Devices.GraphicsFeatures.md).

```csharp
public Pipeline GetBuiltInPipeline(BuiltInPipeline kind, BlendState blend, TextureFormat colorFormat, TextureFormat? depthFormat = null)
```

#### Parameters

`kind` [BuiltInPipeline](MicroEngine.Graphics.Devices.BuiltInPipeline.md)

`blend` [BlendState](MicroEngine.Graphics.Devices.BlendState.md)

`colorFormat` [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)

`depthFormat` [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)?

#### Returns

 [Pipeline](MicroEngine.Graphics.Devices.Pipeline.md)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_GetSampler_MicroEngine_Graphics_Devices_SamplerDescription__"></a> GetSampler\(in SamplerDescription\)

```csharp
public Sampler GetSampler(in SamplerDescription description)
```

#### Parameters

`description` [SamplerDescription](MicroEngine.Graphics.Devices.SamplerDescription.md)

#### Returns

 [Sampler](MicroEngine.Graphics.Devices.Sampler.md)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_Present"></a> Present\(\)

Presents the acquired surface target (no-op when none was acquired) and expires it.

```csharp
public void Present()
```

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_PresentCore_MicroEngine_Graphics_Devices_RenderTarget_"></a> PresentCore\(RenderTarget\)

Backend: present and expire the surface target.

```csharp
protected abstract void PresentCore(RenderTarget surfaceTarget)
```

#### Parameters

`surfaceTarget` [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_RaiseDeviceLost_MicroEngine_Graphics_Devices_DeviceLostReason_"></a> RaiseDeviceLost\(DeviceLostReason\)

For backends that detect loss.

```csharp
protected void RaiseDeviceLost(DeviceLostReason reason)
```

#### Parameters

`reason` [DeviceLostReason](MicroEngine.Graphics.Devices.DeviceLostReason.md)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_ReadPixels_MicroEngine_Graphics_Devices_RenderTarget_MicroEngine_Graphics_Devices_RectI_"></a> ReadPixels\(RenderTarget, RectI\)

Starts reading <code class="paramref">region</code> of an offscreen target; completes through the returned fence. Surface
targets cannot be read: captures render offscreen, read back, then draw the target to the surface.

```csharp
public ReadbackRequest ReadPixels(RenderTarget target, RectI region)
```

#### Parameters

`target` [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)

`region` [RectI](MicroEngine.Graphics.Devices.RectI.md)

#### Returns

 [ReadbackRequest](MicroEngine.Graphics.Devices.ReadbackRequest.md)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_ReadPixelsCore_MicroEngine_Graphics_Devices_RenderTarget_MicroEngine_Graphics_Devices_RectI_"></a> ReadPixelsCore\(RenderTarget, RectI\)

Backend: start a validated readback.

```csharp
protected abstract ReadbackRequest ReadPixelsCore(RenderTarget target, RectI region)
```

#### Parameters

`target` [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)

`region` [RectI](MicroEngine.Graphics.Devices.RectI.md)

#### Returns

 [ReadbackRequest](MicroEngine.Graphics.Devices.ReadbackRequest.md)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_Submit_MicroEngine_Graphics_Devices_CommandList_"></a> Submit\(CommandList\)

Executes a finished list (no open pass) after every list submitted before it. A list is submitted once.

```csharp
public void Submit(CommandList commands)
```

#### Parameters

`commands` [CommandList](MicroEngine.Graphics.Devices.CommandList.md)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_SubmitCore_MicroEngine_Graphics_Devices_CommandList_"></a> SubmitCore\(CommandList\)

Backend: execute a validated, sealed list after everything submitted before it.

```csharp
protected abstract void SubmitCore(CommandList commands)
```

#### Parameters

`commands` [CommandList](MicroEngine.Graphics.Devices.CommandList.md)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_TryAcquireSurfaceTarget_MicroEngine_Graphics_Devices_RenderTarget__"></a> TryAcquireSurfaceTarget\(out RenderTarget?\)

Acquires this frame's surface target. False when there is nothing to present to (no surface, minimised, zero
size): the frame then skips its surface passes and presentation. The target expires at [Present](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_Present). Its
[ColorFormat](MicroEngine.Graphics.Devices.RenderTarget.md#MicroEngine_Graphics_Devices_RenderTarget_ColorFormat) is the format pipelines in its passes must declare; a tier-0 backend, whose
built-in pipelines do not depend on the format, reports the nominal format of its back buffer.

```csharp
public bool TryAcquireSurfaceTarget(out RenderTarget? target)
```

#### Parameters

`target` [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_TryAcquireSurfaceTargetCore"></a> TryAcquireSurfaceTargetCore\(\)

Backend: acquire this frame's surface target or return null.

```csharp
protected abstract RenderTarget? TryAcquireSurfaceTargetCore()
```

#### Returns

 [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)?

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_TryGetResource__1_MicroEngine_Kernel_Handle_MicroEngine_Graphics_Devices_GraphicsResource____0__"></a> TryGetResource<T\>\(Handle<GraphicsResource\>, out T?\)

Finds a live resource of this device by its handle: false once the resource was disposed, even when a new
resource reuses its slot, or when it is not a <code class="typeparamref">T</code>.

```csharp
public bool TryGetResource<T>(Handle<GraphicsResource> handle, out T? resource) where T : GraphicsResource
```

#### Parameters

`handle` Handle<[GraphicsResource](MicroEngine.Graphics.Devices.GraphicsResource.md)\>

`resource` T?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Type Parameters

`T` 

### <a id="MicroEngine_Graphics_Devices_GraphicsDeviceBase_DeviceLost"></a> DeviceLost

Optional: raised only by backends that can detect loss (SDL_GPU never raises it); resources are marked [IsLost](MicroEngine.Graphics.Devices.GraphicsResource.md#MicroEngine_Graphics_Devices_GraphicsResource_IsLost).

```csharp
public event Action<DeviceLostReason>? DeviceLost
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[DeviceLostReason](MicroEngine.Graphics.Devices.DeviceLostReason.md)\>?

</div>
