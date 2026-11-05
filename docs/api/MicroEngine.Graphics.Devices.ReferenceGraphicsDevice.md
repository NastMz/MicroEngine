# Class ReferenceGraphicsDevice

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

The CPU reference device: a tier-0 implementation of the graphics device contract that executes every written rule
literally, in managed code, the same on every machine. It is the oracle the device conformance suite holds backends
to and the device headless runs draw with. It favours clarity over speed.

```csharp
public sealed class ReferenceGraphicsDevice : GraphicsDeviceBase, IGraphicsDevice, IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GraphicsDeviceBase](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md) ← 
[ReferenceGraphicsDevice](MicroEngine.Graphics.Devices.ReferenceGraphicsDevice.md)

#### Implements

[IGraphicsDevice](MicroEngine.Graphics.Devices.IGraphicsDevice.md), 
[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

#### Inherited Members

[GraphicsDeviceBase.CreateTexture\(in TextureDescription, ReadOnlySpan<byte\>\)](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md\#MicroEngine\_Graphics\_Devices\_GraphicsDeviceBase\_CreateTexture\_MicroEngine\_Graphics\_Devices\_TextureDescription\_\_System\_ReadOnlySpan\_System\_Byte\_\_), 
[GraphicsDeviceBase.CreateRenderTarget\(in RenderTargetDescription\)](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md\#MicroEngine\_Graphics\_Devices\_GraphicsDeviceBase\_CreateRenderTarget\_MicroEngine\_Graphics\_Devices\_RenderTargetDescription\_\_), 
[GraphicsDeviceBase.GetSampler\(in SamplerDescription\)](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md\#MicroEngine\_Graphics\_Devices\_GraphicsDeviceBase\_GetSampler\_MicroEngine\_Graphics\_Devices\_SamplerDescription\_\_), 
[GraphicsDeviceBase.CreateBuffer\(in BufferDescription\)](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md\#MicroEngine\_Graphics\_Devices\_GraphicsDeviceBase\_CreateBuffer\_MicroEngine\_Graphics\_Devices\_BufferDescription\_\_), 
[GraphicsDeviceBase.CreateShader\(in ShaderDescription\)](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md\#MicroEngine\_Graphics\_Devices\_GraphicsDeviceBase\_CreateShader\_MicroEngine\_Graphics\_Devices\_ShaderDescription\_\_), 
[GraphicsDeviceBase.CreatePipeline\(PipelineDescription\)](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md\#MicroEngine\_Graphics\_Devices\_GraphicsDeviceBase\_CreatePipeline\_MicroEngine\_Graphics\_Devices\_PipelineDescription\_), 
[GraphicsDeviceBase.GetBuiltInPipeline\(BuiltInPipeline, BlendState, TextureFormat, TextureFormat?\)](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md\#MicroEngine\_Graphics\_Devices\_GraphicsDeviceBase\_GetBuiltInPipeline\_MicroEngine\_Graphics\_Devices\_BuiltInPipeline\_MicroEngine\_Graphics\_Devices\_BlendState\_MicroEngine\_Graphics\_Devices\_TextureFormat\_System\_Nullable\_MicroEngine\_Graphics\_Devices\_TextureFormat\_\_), 
[GraphicsDeviceBase.BeginCommands\(\)](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md\#MicroEngine\_Graphics\_Devices\_GraphicsDeviceBase\_BeginCommands), 
[GraphicsDeviceBase.Submit\(CommandList\)](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md\#MicroEngine\_Graphics\_Devices\_GraphicsDeviceBase\_Submit\_MicroEngine\_Graphics\_Devices\_CommandList\_), 
[GraphicsDeviceBase.ReadPixels\(RenderTarget, RectI\)](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md\#MicroEngine\_Graphics\_Devices\_GraphicsDeviceBase\_ReadPixels\_MicroEngine\_Graphics\_Devices\_RenderTarget\_MicroEngine\_Graphics\_Devices\_RectI\_), 
[GraphicsDeviceBase.TryAcquireSurfaceTarget\(out RenderTarget?\)](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md\#MicroEngine\_Graphics\_Devices\_GraphicsDeviceBase\_TryAcquireSurfaceTarget\_MicroEngine\_Graphics\_Devices\_RenderTarget\_\_), 
[GraphicsDeviceBase.Present\(\)](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md\#MicroEngine\_Graphics\_Devices\_GraphicsDeviceBase\_Present), 
[GraphicsDeviceBase.Dispose\(\)](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md\#MicroEngine\_Graphics\_Devices\_GraphicsDeviceBase\_Dispose), 
[GraphicsDeviceBase.TryGetResource<T\>\(Handle<GraphicsResource\>, out T?\)](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md\#MicroEngine\_Graphics\_Devices\_GraphicsDeviceBase\_TryGetResource\_\_1\_MicroEngine\_Kernel\_Handle\_MicroEngine\_Graphics\_Devices\_GraphicsResource\_\_\_\_0\_\_), 
[GraphicsDeviceBase.Capabilities](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md\#MicroEngine\_Graphics\_Devices\_GraphicsDeviceBase\_Capabilities), 
[GraphicsDeviceBase.IsDisposed](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md\#MicroEngine\_Graphics\_Devices\_GraphicsDeviceBase\_IsDisposed), 
[GraphicsDeviceBase.Counters](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md\#MicroEngine\_Graphics\_Devices\_GraphicsDeviceBase\_Counters), 
[GraphicsDeviceBase.PendingReleases](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md\#MicroEngine\_Graphics\_Devices\_GraphicsDeviceBase\_PendingReleases), 
[GraphicsDeviceBase.DeviceLost](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md\#MicroEngine\_Graphics\_Devices\_GraphicsDeviceBase\_DeviceLost), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring), 
[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode)

## Remarks

<ul><li>Command lists execute completely inside [Submit](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_Submit_MicroEngine_Graphics_Devices_CommandList_); readbacks complete at once.</li><li>Targets store 8-bit channels; every blended value is computed in floating point from the stored 8-bit values,
clamped to 0..1 and rounded to the nearest 8-bit value, as GPUs write UNORM targets.</li><li>Coverage follows the top-left rule at pixel centres, with vertex positions snapped to 1/256 pixel (the
sub-pixel precision of Direct3D and Vulkan rasterizers).</li><li>Texture coordinates and colours are interpolated linearly (2D: no perspective) at pixel centres. Nearest
sampling takes the texel containing the sample point; linear sampling mixes the four nearest texel centres with the
sampler's address mode. Minification (more than one texel per pixel along either axis) uses the sampler's
minification filter, magnification its magnification filter.</li><li>The surface target, when the device has a non-minimised surface, is a [Bgra8Unorm](MicroEngine.Graphics.Devices.TextureFormat.md)
target of the surface's pixel size; [Present](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_Present) keeps its pixels as
[PresentedFrame](MicroEngine.Graphics.Devices.ReferenceGraphicsDevice.md#MicroEngine_Graphics_Devices_ReferenceGraphicsDevice_PresentedFrame).</li></ul>

Declared features: render targets, repeat and mirror addressing, custom blend states and separate filters; no
mipmaps, shaders, depth or instancing.

## Constructors

### <a id="MicroEngine_Graphics_Devices_ReferenceGraphicsDevice__ctor_MicroEngine_Platform_ISurface_MicroEngine_Graphics_Devices_GraphicsDeviceOptions_"></a> ReferenceGraphicsDevice\(ISurface?, GraphicsDeviceOptions?\)

Creates a reference device, with a surface or offscreen only (<code class="paramref">surface</code> null).

```csharp
public ReferenceGraphicsDevice(ISurface? surface = null, GraphicsDeviceOptions? options = null)
```

#### Parameters

`surface` [ISurface](MicroEngine.Platform.ISurface.md)?

`options` [GraphicsDeviceOptions](MicroEngine.Graphics.Devices.GraphicsDeviceOptions.md)?

#### Exceptions

 [GraphicsCapabilityException](MicroEngine.Graphics.Devices.GraphicsCapabilityException.md)

<code class="paramref">options</code> asks for a present mode it lacks (Mailbox).

## Fields

### <a id="MicroEngine_Graphics_Devices_ReferenceGraphicsDevice_BackendName"></a> BackendName

The backend name.

```csharp
public const string BackendName = "reference"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Graphics_Devices_ReferenceGraphicsDevice_DeclaredFeatures"></a> DeclaredFeatures

The features the reference device declares.

```csharp
public const GraphicsFeatures DeclaredFeatures = RenderTargets | AddressRepeat | AddressMirror | CustomBlend | SeparateFilters
```

#### Field Value

 [GraphicsFeatures](MicroEngine.Graphics.Devices.GraphicsFeatures.md)

## Properties

### <a id="MicroEngine_Graphics_Devices_ReferenceGraphicsDevice_Capabilities"></a> Capabilities

Declared capabilities.

```csharp
public override GraphicsCapabilities Capabilities { get; }
```

#### Property Value

 [GraphicsCapabilities](MicroEngine.Graphics.Devices.GraphicsCapabilities.md)

### <a id="MicroEngine_Graphics_Devices_ReferenceGraphicsDevice_PresentMode"></a> PresentMode

The present mode chosen at creation (presenting never waits: there is no display).

```csharp
public PresentMode PresentMode { get; }
```

#### Property Value

 [PresentMode](MicroEngine.Graphics.Devices.PresentMode.md)

### <a id="MicroEngine_Graphics_Devices_ReferenceGraphicsDevice_PresentedFrame"></a> PresentedFrame

The pixels of the last presented surface target: width, height and tightly packed RGBA bytes (opaque or not, as
drawn); null before the first present. Not part of the device contract (surfaces are not read back): headless
hosts use it to capture what a window would have shown.

```csharp
public (int Width, int Height, byte[] Rgba)? PresentedFrame { get; }
```

#### Property Value

 \([int](https://learn.microsoft.com/dotnet/api/system.int32) [Width](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32,system.byte\[\]\-.width), [int](https://learn.microsoft.com/dotnet/api/system.int32) [Height](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32,system.byte\[\]\-.height), [byte](https://learn.microsoft.com/dotnet/api/system.byte)\[\] [Rgba](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32,system.byte\[\]\-.rgba)\)?

## Methods

### <a id="MicroEngine_Graphics_Devices_ReferenceGraphicsDevice_CreateBufferCore_MicroEngine_Graphics_Devices_BufferDescription__"></a> CreateBufferCore\(in BufferDescription\)

Backend: create a validated buffer.

```csharp
protected override GpuBuffer CreateBufferCore(in BufferDescription description)
```

#### Parameters

`description` [BufferDescription](MicroEngine.Graphics.Devices.BufferDescription.md)

#### Returns

 [GpuBuffer](MicroEngine.Graphics.Devices.GpuBuffer.md)

### <a id="MicroEngine_Graphics_Devices_ReferenceGraphicsDevice_CreateBuiltInPipelineCore_MicroEngine_Graphics_Devices_BuiltInPipeline_MicroEngine_Graphics_Devices_BlendState_MicroEngine_Graphics_Devices_TextureFormat_System_Nullable_MicroEngine_Graphics_Devices_TextureFormat__"></a> CreateBuiltInPipelineCore\(BuiltInPipeline, BlendState, TextureFormat, TextureFormat?\)

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

### <a id="MicroEngine_Graphics_Devices_ReferenceGraphicsDevice_CreatePipelineCore_MicroEngine_Graphics_Devices_PipelineDescription_"></a> CreatePipelineCore\(PipelineDescription\)

Backend: create a validated custom pipeline (tier 0 is never called: core refuses first).

```csharp
protected override Pipeline CreatePipelineCore(PipelineDescription description)
```

#### Parameters

`description` [PipelineDescription](MicroEngine.Graphics.Devices.PipelineDescription.md)

#### Returns

 [Pipeline](MicroEngine.Graphics.Devices.Pipeline.md)

### <a id="MicroEngine_Graphics_Devices_ReferenceGraphicsDevice_CreateRenderTargetCore_MicroEngine_Graphics_Devices_RenderTargetDescription__"></a> CreateRenderTargetCore\(in RenderTargetDescription\)

Backend: create a validated render target.

```csharp
protected override RenderTarget CreateRenderTargetCore(in RenderTargetDescription description)
```

#### Parameters

`description` [RenderTargetDescription](MicroEngine.Graphics.Devices.RenderTargetDescription.md)

#### Returns

 [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)

### <a id="MicroEngine_Graphics_Devices_ReferenceGraphicsDevice_CreateSamplerCore_MicroEngine_Graphics_Devices_SamplerDescription__"></a> CreateSamplerCore\(in SamplerDescription\)

Backend: create a validated sampler (core caches it).

```csharp
protected override Sampler CreateSamplerCore(in SamplerDescription description)
```

#### Parameters

`description` [SamplerDescription](MicroEngine.Graphics.Devices.SamplerDescription.md)

#### Returns

 [Sampler](MicroEngine.Graphics.Devices.Sampler.md)

### <a id="MicroEngine_Graphics_Devices_ReferenceGraphicsDevice_CreateShaderCore_MicroEngine_Graphics_Devices_ShaderDescription__"></a> CreateShaderCore\(in ShaderDescription\)

Backend: create a validated shader (tier 0 is never called: core refuses first).

```csharp
protected override Shader CreateShaderCore(in ShaderDescription description)
```

#### Parameters

`description` [ShaderDescription](MicroEngine.Graphics.Devices.ShaderDescription.md)

#### Returns

 [Shader](MicroEngine.Graphics.Devices.Shader.md)

### <a id="MicroEngine_Graphics_Devices_ReferenceGraphicsDevice_CreateTextureCore_MicroEngine_Graphics_Devices_TextureDescription__System_ReadOnlySpan_System_Byte__"></a> CreateTextureCore\(in TextureDescription, ReadOnlySpan<byte\>\)

Backend: create a validated texture.

```csharp
protected override Texture CreateTextureCore(in TextureDescription description, ReadOnlySpan<byte> initialData)
```

#### Parameters

`description` [TextureDescription](MicroEngine.Graphics.Devices.TextureDescription.md)

`initialData` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[byte](https://learn.microsoft.com/dotnet/api/system.byte)\>

#### Returns

 [Texture](MicroEngine.Graphics.Devices.Texture.md)

### <a id="MicroEngine_Graphics_Devices_ReferenceGraphicsDevice_DisposeCore_System_Collections_Generic_IReadOnlyList_MicroEngine_Graphics_Devices_Pipeline__System_Collections_Generic_IReadOnlyList_MicroEngine_Graphics_Devices_Sampler__"></a> DisposeCore\(IReadOnlyList<Pipeline\>, IReadOnlyList<Sampler\>\)

Backend: release everything, the device-owned built-ins and samplers included.

```csharp
protected override void DisposeCore(IReadOnlyList<Pipeline> builtIns, IReadOnlyList<Sampler> samplers)
```

#### Parameters

`builtIns` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[Pipeline](MicroEngine.Graphics.Devices.Pipeline.md)\>

`samplers` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[Sampler](MicroEngine.Graphics.Devices.Sampler.md)\>

### <a id="MicroEngine_Graphics_Devices_ReferenceGraphicsDevice_PresentCore_MicroEngine_Graphics_Devices_RenderTarget_"></a> PresentCore\(RenderTarget\)

Backend: present and expire the surface target.

```csharp
protected override void PresentCore(RenderTarget surfaceTarget)
```

#### Parameters

`surfaceTarget` [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)

### <a id="MicroEngine_Graphics_Devices_ReferenceGraphicsDevice_ReadPixelsCore_MicroEngine_Graphics_Devices_RenderTarget_MicroEngine_Graphics_Devices_RectI_"></a> ReadPixelsCore\(RenderTarget, RectI\)

Backend: start a validated readback.

```csharp
protected override ReadbackRequest ReadPixelsCore(RenderTarget target, RectI region)
```

#### Parameters

`target` [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)

`region` [RectI](MicroEngine.Graphics.Devices.RectI.md)

#### Returns

 [ReadbackRequest](MicroEngine.Graphics.Devices.ReadbackRequest.md)

### <a id="MicroEngine_Graphics_Devices_ReferenceGraphicsDevice_SubmitCore_MicroEngine_Graphics_Devices_CommandList_"></a> SubmitCore\(CommandList\)

Backend: execute a validated, sealed list after everything submitted before it.

```csharp
protected override void SubmitCore(CommandList commands)
```

#### Parameters

`commands` [CommandList](MicroEngine.Graphics.Devices.CommandList.md)

### <a id="MicroEngine_Graphics_Devices_ReferenceGraphicsDevice_TryAcquireSurfaceTargetCore"></a> TryAcquireSurfaceTargetCore\(\)

Backend: acquire this frame's surface target or return null.

```csharp
protected override RenderTarget? TryAcquireSurfaceTargetCore()
```

#### Returns

 [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)?

</div>
