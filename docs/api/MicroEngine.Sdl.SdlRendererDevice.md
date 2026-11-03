# Class SdlRendererDevice

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Sdl](MicroEngine.Sdl.md)  
Assembly: MicroEngine.Sdl.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

The tier-0 graphics device on SDL_Renderer, with SDL's default render driver unless
[Api](MicroEngine.Graphics.Devices.GraphicsDeviceOptions.md#MicroEngine_Graphics_Devices_GraphicsDeviceOptions_Api) names one ("direct3d11", "direct3d12", "opengl", "vulkan", "metal", "gpu",
"software"...). Buffers are device-owned CPU memory submitted through <code>SDL_RenderGeometryRaw</code> (the
[Vertex2D](MicroEngine.Graphics.Devices.Vertex2D.md) layout is passed with strides, without conversion); the built-in pipeline is an SDL blend mode;
samplers are SDL scale and address modes applied per draw; command lists execute inside
[Submit](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_Submit_MicroEngine_Graphics_Devices_CommandList_) into SDL's own queue, which SDL flushes at readback and present. Declared
features: render targets, repeat addressing and, where the driver composes every blend factor, custom blend states.
SDL's <code>software</code> driver is a diagnostic device: its triangle coverage differs from the contract's pixel-centre
rule along rotated edges, and it does not claim conformance.

```csharp
public sealed class SdlRendererDevice : GraphicsDeviceBase, IGraphicsDevice, IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
GraphicsDeviceBase ← 
[SdlRendererDevice](MicroEngine.Sdl.SdlRendererDevice.md)

#### Implements

IGraphicsDevice, 
[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

#### Inherited Members

GraphicsDeviceBase.CreateTexture\(in TextureDescription, ReadOnlySpan<byte\>\), 
GraphicsDeviceBase.CreateRenderTarget\(in RenderTargetDescription\), 
GraphicsDeviceBase.GetSampler\(in SamplerDescription\), 
GraphicsDeviceBase.CreateBuffer\(in BufferDescription\), 
GraphicsDeviceBase.CreateShader\(in ShaderDescription\), 
GraphicsDeviceBase.CreatePipeline\(PipelineDescription\), 
GraphicsDeviceBase.GetBuiltInPipeline\(BuiltInPipeline, BlendState, TextureFormat, TextureFormat?\), 
GraphicsDeviceBase.BeginCommands\(\), 
GraphicsDeviceBase.Submit\(CommandList\), 
GraphicsDeviceBase.ReadPixels\(RenderTarget, RectI\), 
GraphicsDeviceBase.TryAcquireSurfaceTarget\(out RenderTarget?\), 
GraphicsDeviceBase.Present\(\), 
GraphicsDeviceBase.Dispose\(\), 
GraphicsDeviceBase.TryGetResource<T\>\(Handle<GraphicsResource\>, out T?\), 
GraphicsDeviceBase.Capabilities, 
GraphicsDeviceBase.IsDisposed, 
GraphicsDeviceBase.Counters, 
GraphicsDeviceBase.PendingReleases, 
GraphicsDeviceBase.DeviceLost, 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring), 
[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode)

## Constructors

### <a id="MicroEngine_Sdl_SdlRendererDevice__ctor_MicroEngine_Platform_ISurface_MicroEngine_Graphics_Devices_GraphicsDeviceOptions_"></a> SdlRendererDevice\(ISurface, GraphicsDeviceOptions\)

Creates the device and its SDL renderer for an SDL window surface.

```csharp
public SdlRendererDevice(ISurface surface, GraphicsDeviceOptions options)
```

#### Parameters

`surface` ISurface

A surface whose [NativeWindow](MicroEngine.Platform.ISurface.md#MicroEngine_Platform_ISurface_NativeWindow) is an <code>SDL_Window*</code>.

`options` GraphicsDeviceOptions

The present mode and, optionally, the render driver to ask SDL for.

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

SDL could not create the renderer (the message names SDL's error).

 GraphicsCapabilityException

The present mode is not VSync or Immediate.

## Fields

### <a id="MicroEngine_Sdl_SdlRendererDevice_BackendName"></a> BackendName

The backend name.

```csharp
public const string BackendName = "sdl-renderer"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Properties

### <a id="MicroEngine_Sdl_SdlRendererDevice_Capabilities"></a> Capabilities

Declared capabilities.

```csharp
public override GraphicsCapabilities Capabilities { get; }
```

#### Property Value

 GraphicsCapabilities

### <a id="MicroEngine_Sdl_SdlRendererDevice_Renderer"></a> Renderer

The SDL renderer the device draws with (owned by the device; destroyed with it).

```csharp
public SDL_Renderer* Renderer { get; }
```

#### Property Value

 SDL\_Renderer\*

## Methods

### <a id="MicroEngine_Sdl_SdlRendererDevice_CreateBufferCore_MicroEngine_Graphics_Devices_BufferDescription__"></a> CreateBufferCore\(in BufferDescription\)

Backend: create a validated buffer.

```csharp
protected override GpuBuffer CreateBufferCore(in BufferDescription description)
```

#### Parameters

`description` BufferDescription

#### Returns

 GpuBuffer

### <a id="MicroEngine_Sdl_SdlRendererDevice_CreateBuiltInPipelineCore_MicroEngine_Graphics_Devices_BuiltInPipeline_MicroEngine_Graphics_Devices_BlendState_MicroEngine_Graphics_Devices_TextureFormat_System_Nullable_MicroEngine_Graphics_Devices_TextureFormat__"></a> CreateBuiltInPipelineCore\(BuiltInPipeline, BlendState, TextureFormat, TextureFormat?\)

Backend: create a built-in pipeline variant (core caches it).

```csharp
protected override Pipeline CreateBuiltInPipelineCore(BuiltInPipeline kind, BlendState blend, TextureFormat colorFormat, TextureFormat? depthFormat)
```

#### Parameters

`kind` BuiltInPipeline

`blend` BlendState

`colorFormat` TextureFormat

`depthFormat` TextureFormat?

#### Returns

 Pipeline

### <a id="MicroEngine_Sdl_SdlRendererDevice_CreatePipelineCore_MicroEngine_Graphics_Devices_PipelineDescription_"></a> CreatePipelineCore\(PipelineDescription\)

Backend: create a validated custom pipeline (tier 0 is never called: core refuses first).

```csharp
protected override Pipeline CreatePipelineCore(PipelineDescription description)
```

#### Parameters

`description` PipelineDescription

#### Returns

 Pipeline

### <a id="MicroEngine_Sdl_SdlRendererDevice_CreateRenderTargetCore_MicroEngine_Graphics_Devices_RenderTargetDescription__"></a> CreateRenderTargetCore\(in RenderTargetDescription\)

Backend: create a validated render target.

```csharp
protected override RenderTarget CreateRenderTargetCore(in RenderTargetDescription description)
```

#### Parameters

`description` RenderTargetDescription

#### Returns

 RenderTarget

### <a id="MicroEngine_Sdl_SdlRendererDevice_CreateSamplerCore_MicroEngine_Graphics_Devices_SamplerDescription__"></a> CreateSamplerCore\(in SamplerDescription\)

Backend: create a validated sampler (core caches it).

```csharp
protected override Sampler CreateSamplerCore(in SamplerDescription description)
```

#### Parameters

`description` SamplerDescription

#### Returns

 Sampler

### <a id="MicroEngine_Sdl_SdlRendererDevice_CreateShaderCore_MicroEngine_Graphics_Devices_ShaderDescription__"></a> CreateShaderCore\(in ShaderDescription\)

Backend: create a validated shader (tier 0 is never called: core refuses first).

```csharp
protected override Shader CreateShaderCore(in ShaderDescription description)
```

#### Parameters

`description` ShaderDescription

#### Returns

 Shader

### <a id="MicroEngine_Sdl_SdlRendererDevice_CreateTextureCore_MicroEngine_Graphics_Devices_TextureDescription__System_ReadOnlySpan_System_Byte__"></a> CreateTextureCore\(in TextureDescription, ReadOnlySpan<byte\>\)

Backend: create a validated texture.

```csharp
protected override Texture CreateTextureCore(in TextureDescription description, ReadOnlySpan<byte> initialData)
```

#### Parameters

`description` TextureDescription

`initialData` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[byte](https://learn.microsoft.com/dotnet/api/system.byte)\>

#### Returns

 Texture

### <a id="MicroEngine_Sdl_SdlRendererDevice_DisposeCore_System_Collections_Generic_IReadOnlyList_MicroEngine_Graphics_Devices_Pipeline__System_Collections_Generic_IReadOnlyList_MicroEngine_Graphics_Devices_Sampler__"></a> DisposeCore\(IReadOnlyList<Pipeline\>, IReadOnlyList<Sampler\>\)

Backend: release everything, the device-owned built-ins and samplers included.

```csharp
protected override void DisposeCore(IReadOnlyList<Pipeline> builtIns, IReadOnlyList<Sampler> samplers)
```

#### Parameters

`builtIns` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<Pipeline\>

`samplers` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<Sampler\>

### <a id="MicroEngine_Sdl_SdlRendererDevice_PresentCore_MicroEngine_Graphics_Devices_RenderTarget_"></a> PresentCore\(RenderTarget\)

Backend: present and expire the surface target.

```csharp
protected override void PresentCore(RenderTarget surfaceTarget)
```

#### Parameters

`surfaceTarget` RenderTarget

### <a id="MicroEngine_Sdl_SdlRendererDevice_ReadPixelsCore_MicroEngine_Graphics_Devices_RenderTarget_MicroEngine_Graphics_Devices_RectI_"></a> ReadPixelsCore\(RenderTarget, RectI\)

Backend: start a validated readback.

```csharp
protected override ReadbackRequest ReadPixelsCore(RenderTarget target, RectI region)
```

#### Parameters

`target` RenderTarget

`region` RectI

#### Returns

 ReadbackRequest

### <a id="MicroEngine_Sdl_SdlRendererDevice_SubmitCore_MicroEngine_Graphics_Devices_CommandList_"></a> SubmitCore\(CommandList\)

Backend: execute a validated, sealed list after everything submitted before it.

```csharp
protected override void SubmitCore(CommandList commands)
```

#### Parameters

`commands` CommandList

### <a id="MicroEngine_Sdl_SdlRendererDevice_TryAcquireSurfaceTargetCore"></a> TryAcquireSurfaceTargetCore\(\)

Backend: acquire this frame's surface target or return null.

```csharp
protected override RenderTarget? TryAcquireSurfaceTargetCore()
```

#### Returns

 RenderTarget?

</div>
