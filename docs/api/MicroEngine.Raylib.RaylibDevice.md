# Class RaylibDevice

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Raylib](MicroEngine.Raylib.md)  
Assembly: MicroEngine.Raylib.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

The tier-0 graphics device on raylib's OpenGL layer (rlgl): textures and render targets are OpenGL textures and
framebuffers, buffers are device-owned CPU memory whose triangles are sent through rlgl's batch, the built-in pipeline
is rlgl's default shader with the pass's blend factors, and samplers are texture parameters set before each draw.
Offscreen targets are rendered with row 0 at the top of the texture's memory, so uploads, sampling and readback need no
flipping and OpenGL's tie rule gives the contract's top-left rule; the window's target (bottom-up in OpenGL) moves
geometry up by 1/128 px for the same rule. Desktop OpenGL declares render targets, repeat and mirror addressing,
separate filters and custom blend states; in browsers (WebGL) only render targets are declared.

```csharp
public sealed class RaylibDevice : GraphicsDeviceBase, IGraphicsDevice, IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GraphicsDeviceBase](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md) ← 
[RaylibDevice](MicroEngine.Raylib.RaylibDevice.md)

#### Implements

[IGraphicsDevice](MicroEngine.Graphics.Devices.IGraphicsDevice.md), 
[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

#### Inherited Members

[GraphicsDeviceBase.CreateTexture\(in TextureDescription, ReadOnlySpan<byte\>\)](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md#MicroEngine_Graphics_Devices_GraphicsDeviceBase_CreateTexture_MicroEngine_Graphics_Devices_TextureDescription__System_ReadOnlySpan_System_Byte__), 
[GraphicsDeviceBase.CreateRenderTarget\(in RenderTargetDescription\)](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md#MicroEngine_Graphics_Devices_GraphicsDeviceBase_CreateRenderTarget_MicroEngine_Graphics_Devices_RenderTargetDescription__), 
[GraphicsDeviceBase.GetSampler\(in SamplerDescription\)](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md#MicroEngine_Graphics_Devices_GraphicsDeviceBase_GetSampler_MicroEngine_Graphics_Devices_SamplerDescription__), 
[GraphicsDeviceBase.CreateBuffer\(in BufferDescription\)](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md#MicroEngine_Graphics_Devices_GraphicsDeviceBase_CreateBuffer_MicroEngine_Graphics_Devices_BufferDescription__), 
[GraphicsDeviceBase.CreateShader\(in ShaderDescription\)](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md#MicroEngine_Graphics_Devices_GraphicsDeviceBase_CreateShader_MicroEngine_Graphics_Devices_ShaderDescription__), 
[GraphicsDeviceBase.CreatePipeline\(PipelineDescription\)](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md#MicroEngine_Graphics_Devices_GraphicsDeviceBase_CreatePipeline_MicroEngine_Graphics_Devices_PipelineDescription_), 
[GraphicsDeviceBase.GetBuiltInPipeline\(BuiltInPipeline, BlendState, TextureFormat, TextureFormat?\)](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md#MicroEngine_Graphics_Devices_GraphicsDeviceBase_GetBuiltInPipeline_MicroEngine_Graphics_Devices_BuiltInPipeline_MicroEngine_Graphics_Devices_BlendState_MicroEngine_Graphics_Devices_TextureFormat_System_Nullable_MicroEngine_Graphics_Devices_TextureFormat__), 
[GraphicsDeviceBase.BeginCommands\(\)](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md#MicroEngine_Graphics_Devices_GraphicsDeviceBase_BeginCommands), 
[GraphicsDeviceBase.Submit\(CommandList\)](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md#MicroEngine_Graphics_Devices_GraphicsDeviceBase_Submit_MicroEngine_Graphics_Devices_CommandList_), 
[GraphicsDeviceBase.ReadPixels\(RenderTarget, RectI\)](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md#MicroEngine_Graphics_Devices_GraphicsDeviceBase_ReadPixels_MicroEngine_Graphics_Devices_RenderTarget_MicroEngine_Graphics_Devices_RectI_), 
[GraphicsDeviceBase.TryAcquireSurfaceTarget\(out RenderTarget?\)](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md#MicroEngine_Graphics_Devices_GraphicsDeviceBase_TryAcquireSurfaceTarget_MicroEngine_Graphics_Devices_RenderTarget__), 
[GraphicsDeviceBase.Present\(\)](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md#MicroEngine_Graphics_Devices_GraphicsDeviceBase_Present), 
[GraphicsDeviceBase.Dispose\(\)](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md#MicroEngine_Graphics_Devices_GraphicsDeviceBase_Dispose), 
[GraphicsDeviceBase.TryGetResource<T\>\(Handle<GraphicsResource\>, out T?\)](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md#MicroEngine_Graphics_Devices_GraphicsDeviceBase_TryGetResource__1_MicroEngine_Kernel_Handle_MicroEngine_Graphics_Devices_GraphicsResource____0__), 
[GraphicsDeviceBase.Capabilities](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md#MicroEngine_Graphics_Devices_GraphicsDeviceBase_Capabilities), 
[GraphicsDeviceBase.IsDisposed](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md#MicroEngine_Graphics_Devices_GraphicsDeviceBase_IsDisposed), 
[GraphicsDeviceBase.Counters](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md#MicroEngine_Graphics_Devices_GraphicsDeviceBase_Counters), 
[GraphicsDeviceBase.PendingReleases](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md#MicroEngine_Graphics_Devices_GraphicsDeviceBase_PendingReleases), 
[GraphicsDeviceBase.DeviceLost](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md#MicroEngine_Graphics_Devices_GraphicsDeviceBase_DeviceLost), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring), 
[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode)

## Constructors

### <a id="MicroEngine_Raylib_RaylibDevice__ctor_MicroEngine_Platform_ISurface_MicroEngine_Graphics_Devices_GraphicsDeviceOptions_"></a> RaylibDevice\(ISurface, GraphicsDeviceOptions\)

Creates the device on raylib's window (raylib must have opened it).

```csharp
public RaylibDevice(ISurface surface, GraphicsDeviceOptions options)
```

#### Parameters

`surface` [ISurface](MicroEngine.Platform.ISurface.md)

raylib's window surface.

`options` [GraphicsDeviceOptions](MicroEngine.Graphics.Devices.GraphicsDeviceOptions.md)

The present mode (raylib's VSync is a window flag chosen when the window opens; the device records the mode).

#### Exceptions

 [GraphicsCapabilityException](MicroEngine.Graphics.Devices.GraphicsCapabilityException.md)

The present mode is Mailbox.

## Fields

### <a id="MicroEngine_Raylib_RaylibDevice_BackendName"></a> BackendName

The backend name.

```csharp
public const string BackendName = "raylib"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Properties

### <a id="MicroEngine_Raylib_RaylibDevice_Capabilities"></a> Capabilities

Declared capabilities.

```csharp
public override GraphicsCapabilities Capabilities { get; }
```

#### Property Value

 [GraphicsCapabilities](MicroEngine.Graphics.Devices.GraphicsCapabilities.md)

### <a id="MicroEngine_Raylib_RaylibDevice_PresentMode"></a> PresentMode

The present mode chosen at creation.

```csharp
public PresentMode PresentMode { get; }
```

#### Property Value

 [PresentMode](MicroEngine.Graphics.Devices.PresentMode.md)

### <a id="MicroEngine_Raylib_RaylibDevice_SwapOnPresent"></a> SwapOnPresent

Whether [Present](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_Present) swaps the window's buffers (true by default). A host whose frame
loop ends with raylib's <code>EndDrawing</code>, which swaps them itself (and polls input), sets it to false: Present
then only finishes the frame's drawing and expires the surface target.

```csharp
public bool SwapOnPresent { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_Raylib_RaylibDevice_CreateBufferCore_MicroEngine_Graphics_Devices_BufferDescription__"></a> CreateBufferCore\(in BufferDescription\)

Backend: create a validated buffer.

```csharp
protected override GpuBuffer CreateBufferCore(in BufferDescription description)
```

#### Parameters

`description` [BufferDescription](MicroEngine.Graphics.Devices.BufferDescription.md)

#### Returns

 [GpuBuffer](MicroEngine.Graphics.Devices.GpuBuffer.md)

### <a id="MicroEngine_Raylib_RaylibDevice_CreateBuiltInPipelineCore_MicroEngine_Graphics_Devices_BuiltInPipeline_MicroEngine_Graphics_Devices_BlendState_MicroEngine_Graphics_Devices_TextureFormat_System_Nullable_MicroEngine_Graphics_Devices_TextureFormat__"></a> CreateBuiltInPipelineCore\(BuiltInPipeline, BlendState, TextureFormat, TextureFormat?\)

Backend: create a built-in pipeline variant (core caches it).

```csharp
protected override Pipeline CreateBuiltInPipelineCore(BuiltInPipeline kind, BlendState blend, TextureFormat colorFormat, TextureFormat? depthFormat)
```

#### Parameters

`kind` [BuiltInPipeline](MicroEngine.Graphics.Devices.BuiltInPipeline.md)

`blend` [BlendState](MicroEngine.Graphics.Devices.BlendState.md)

`colorFormat` [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)

`depthFormat` [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)?

#### Returns

 [Pipeline](MicroEngine.Graphics.Devices.Pipeline.md)

### <a id="MicroEngine_Raylib_RaylibDevice_CreatePipelineCore_MicroEngine_Graphics_Devices_PipelineDescription_"></a> CreatePipelineCore\(PipelineDescription\)

Backend: create a validated custom pipeline (tier 0 is never called: core refuses first).

```csharp
protected override Pipeline CreatePipelineCore(PipelineDescription description)
```

#### Parameters

`description` [PipelineDescription](MicroEngine.Graphics.Devices.PipelineDescription.md)

#### Returns

 [Pipeline](MicroEngine.Graphics.Devices.Pipeline.md)

### <a id="MicroEngine_Raylib_RaylibDevice_CreateRenderTargetCore_MicroEngine_Graphics_Devices_RenderTargetDescription__"></a> CreateRenderTargetCore\(in RenderTargetDescription\)

Backend: create a validated render target.

```csharp
protected override RenderTarget CreateRenderTargetCore(in RenderTargetDescription description)
```

#### Parameters

`description` [RenderTargetDescription](MicroEngine.Graphics.Devices.RenderTargetDescription.md)

#### Returns

 [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)

### <a id="MicroEngine_Raylib_RaylibDevice_CreateSamplerCore_MicroEngine_Graphics_Devices_SamplerDescription__"></a> CreateSamplerCore\(in SamplerDescription\)

Backend: create a validated sampler (core caches it).

```csharp
protected override Sampler CreateSamplerCore(in SamplerDescription description)
```

#### Parameters

`description` [SamplerDescription](MicroEngine.Graphics.Devices.SamplerDescription.md)

#### Returns

 [Sampler](MicroEngine.Graphics.Devices.Sampler.md)

### <a id="MicroEngine_Raylib_RaylibDevice_CreateShaderCore_MicroEngine_Graphics_Devices_ShaderDescription__"></a> CreateShaderCore\(in ShaderDescription\)

Backend: create a validated shader (tier 0 is never called: core refuses first).

```csharp
protected override Shader CreateShaderCore(in ShaderDescription description)
```

#### Parameters

`description` [ShaderDescription](MicroEngine.Graphics.Devices.ShaderDescription.md)

#### Returns

 [Shader](MicroEngine.Graphics.Devices.Shader.md)

### <a id="MicroEngine_Raylib_RaylibDevice_CreateTextureCore_MicroEngine_Graphics_Devices_TextureDescription__System_ReadOnlySpan_System_Byte__"></a> CreateTextureCore\(in TextureDescription, ReadOnlySpan<byte\>\)

Backend: create a validated texture.

```csharp
protected override Texture CreateTextureCore(in TextureDescription description, ReadOnlySpan<byte> initialData)
```

#### Parameters

`description` [TextureDescription](MicroEngine.Graphics.Devices.TextureDescription.md)

`initialData` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[byte](https://learn.microsoft.com/dotnet/api/system.byte)\>

#### Returns

 [Texture](MicroEngine.Graphics.Devices.Texture.md)

### <a id="MicroEngine_Raylib_RaylibDevice_DisposeCore_System_Collections_Generic_IReadOnlyList_MicroEngine_Graphics_Devices_Pipeline__System_Collections_Generic_IReadOnlyList_MicroEngine_Graphics_Devices_Sampler__"></a> DisposeCore\(IReadOnlyList<Pipeline\>, IReadOnlyList<Sampler\>\)

Backend: release everything, the device-owned built-ins and samplers included.

```csharp
protected override void DisposeCore(IReadOnlyList<Pipeline> builtIns, IReadOnlyList<Sampler> samplers)
```

#### Parameters

`builtIns` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[Pipeline](MicroEngine.Graphics.Devices.Pipeline.md)\>

`samplers` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[Sampler](MicroEngine.Graphics.Devices.Sampler.md)\>

### <a id="MicroEngine_Raylib_RaylibDevice_PresentCore_MicroEngine_Graphics_Devices_RenderTarget_"></a> PresentCore\(RenderTarget\)

Backend: present and expire the surface target.

```csharp
protected override void PresentCore(RenderTarget surfaceTarget)
```

#### Parameters

`surfaceTarget` [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)

### <a id="MicroEngine_Raylib_RaylibDevice_ReadPixelsCore_MicroEngine_Graphics_Devices_RenderTarget_MicroEngine_Graphics_Devices_RectI_"></a> ReadPixelsCore\(RenderTarget, RectI\)

Backend: start a validated readback.

```csharp
protected override ReadbackRequest ReadPixelsCore(RenderTarget target, RectI region)
```

#### Parameters

`target` [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)

`region` [RectI](MicroEngine.Graphics.Devices.RectI.md)

#### Returns

 [ReadbackRequest](MicroEngine.Graphics.Devices.ReadbackRequest.md)

### <a id="MicroEngine_Raylib_RaylibDevice_SubmitCore_MicroEngine_Graphics_Devices_CommandList_"></a> SubmitCore\(CommandList\)

Backend: execute a validated, sealed list after everything submitted before it.

```csharp
protected override void SubmitCore(CommandList commands)
```

#### Parameters

`commands` [CommandList](MicroEngine.Graphics.Devices.CommandList.md)

### <a id="MicroEngine_Raylib_RaylibDevice_TryAcquireSurfaceTargetCore"></a> TryAcquireSurfaceTargetCore\(\)

Backend: acquire this frame's surface target or return null.

```csharp
protected override RenderTarget? TryAcquireSurfaceTargetCore()
```

#### Returns

 [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)?

</div>
