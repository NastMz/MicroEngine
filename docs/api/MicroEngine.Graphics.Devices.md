# Namespace MicroEngine.Graphics.Devices

<div data-pagefind-weight="0.3">

The graphics device contract every backend implements: capabilities and the typed capability error, textures, render targets, samplers, buffers, pipelines, command lists with passes, readback and the surface target; the core validation backends inherit and the CPU reference device.

Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

### Classes

 [BlendState](MicroEngine.Graphics.Devices.BlendState.md)

Fixed-function blending: <code>result = src·SrcFactor (op) dst·DstFactor</code>, separately for colour and alpha, computed
in float and quantised to the target format. The presets assume premultiplied colours.

 [CommandList](MicroEngine.Graphics.Devices.CommandList.md)

A list of commands for one device, recorded on the render thread and validated here, in core, as it is recorded
(before any backend sees it). Every violation throws at the call that causes it:

<ul><li>a resource of another device: `ArgumentException`; a disposed one: `ObjectDisposedException`;</li><li>wrong nesting (pass in a pass, draw or binding outside a pass, update inside a pass, recording after submit): `InvalidOperationException`;</li><li>a pipeline whose attachment formats differ from the pass's, a draw without the bindings the pipeline needs, a
range outside a buffer, sampling the pass's own colour target: `InvalidOperationException` or
`ArgumentOutOfRangeException`;</li><li>an undeclared capability (instancing): [GraphicsCapabilityException](MicroEngine.Graphics.Devices.GraphicsCapabilityException.md).</li></ul>

Rules: commands execute in recording order (an update affects only draws recorded after it); bindings, pipeline,
viewport and scissor are pass-scoped (each pass starts with none bound, the whole target as viewport and no scissor);
uniform data persist for the rest of the list once set.

 [CompletedReadback](MicroEngine.Graphics.Devices.CompletedReadback.md)

A readback whose pixels were copied when it was requested (devices that execute at submit).

 [GpuBuffer](MicroEngine.Graphics.Devices.GpuBuffer.md)

A vertex and/or index buffer.

 [GraphicsCapabilities](MicroEngine.Graphics.Devices.GraphicsCapabilities.md)

What a device declares. Read after creation: the backend identity is what the device reports, not what was requested.

 [GraphicsCapabilityException](MicroEngine.Graphics.Devices.GraphicsCapabilityException.md)

The one typed error for a missing capability: names the operation, the device and what it lacks. Thrown at
creation or recording time, never deferred to a draw.

 [GraphicsContract](MicroEngine.Graphics.Devices.GraphicsContract.md)

The graphics device contract this assembly implements. The contract text is the XML documentation of the types in
<code>MicroEngine.Graphics.Devices</code>; every rule it states is checked by the device conformance suite. A change to a
rule or to the shape of a type is a contract revision: it raises [Version](MicroEngine.Graphics.Devices.GraphicsContract.md#MicroEngine_Graphics_Devices_GraphicsContract_Version) and records its evidence.

 [GraphicsDeviceBase](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md)

Core half of every device: validates each call against the written rules and the declared capabilities, then hands
the backend a call it can execute. Backends derive from it and implement only the <code>*Core</code> members, so ownership,
disposal, ranges and capability errors behave identically on every tier.

 [GraphicsDeviceOptions](MicroEngine.Graphics.Devices.GraphicsDeviceOptions.md)

Device creation options.

 [GraphicsLimits](MicroEngine.Graphics.Devices.GraphicsLimits.md)

Limits. Each value is a guaranteed minimum: a backend that cannot query its API reports the minimum the API
specification guarantees, so a limit may be conservative but never optimistic.

 [GraphicsResource](MicroEngine.Graphics.Devices.GraphicsResource.md)

Base of every device object. A resource belongs to the device that created it: using it with another device is an
`ArgumentException`, using it after [Dispose](MicroEngine.Graphics.Devices.GraphicsResource.md#MicroEngine_Graphics_Devices_GraphicsResource_Dispose) an `ObjectDisposedException`, both
raised by core validation before a backend sees the call. Every resource has a [Handle](MicroEngine.Graphics.Devices.GraphicsResource.md#MicroEngine_Graphics_Devices_GraphicsResource_Handle) with a
generation: once the resource is disposed the handle no longer resolves
([TryGetResource``1](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md#MicroEngine_Graphics_Devices_GraphicsDeviceBase_TryGetResource__1_MicroEngine_Kernel_Handle_MicroEngine_Graphics_Devices_GraphicsResource____0__)), even if its slot is reused. Destruction is deferred: the
backend object is released only when no submitted work uses it any more (at once on devices that execute command
lists at [Submit](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_Submit_MicroEngine_Graphics_Devices_CommandList_)).

 [PipelineDescription](MicroEngine.Graphics.Devices.PipelineDescription.md)

A custom pipeline: requires [Shaders](MicroEngine.Graphics.Devices.GraphicsFeatures.md). The pipeline is bound to the attachment formats
it renders to: a pass accepts it only when its colour format equals <code class="paramref">ColorFormat</code> and its depth
attachment format equals <code class="paramref">DepthFormat</code> (null = the pass has no depth attachment).

 [Pipeline](MicroEngine.Graphics.Devices.Pipeline.md)

A graphics pipeline: built-in (device-owned) or custom.

 [PremultipliedColor](MicroEngine.Graphics.Devices.PremultipliedColor.md)

Conversions between 8-bit colours and the premultiplied float colours of the device contract.

 [ReadbackRequest](MicroEngine.Graphics.Devices.ReadbackRequest.md)

An asynchronous read of a render target region, completed by a fence. The data are the pixels of every command
list submitted before [ReadPixels](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_ReadPixels_MicroEngine_Graphics_Devices_RenderTarget_MicroEngine_Graphics_Devices_RectI_): rows top to bottom, tightly packed, in the target's
colour format.

 [ReferenceGraphicsBackendFactory](MicroEngine.Graphics.Devices.ReferenceGraphicsBackendFactory.md)

Factory of the CPU reference device ("reference"): offscreen, or over any surface (it never touches the window).

 [ReferenceGraphicsDevice](MicroEngine.Graphics.Devices.ReferenceGraphicsDevice.md)

The CPU reference device: a tier-0 implementation of the graphics device contract that executes every written rule
literally, in managed code, the same on every machine. It is the oracle the device conformance suite holds backends
to and the device headless runs draw with. It favours clarity over speed.

 [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)

A colour attachment (plus optional depth) that passes render to. Offscreen targets come from
[CreateRenderTarget](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_CreateRenderTarget_MicroEngine_Graphics_Devices_RenderTargetDescription__) and can be sampled through [Color](MicroEngine.Graphics.Devices.RenderTarget.md#MicroEngine_Graphics_Devices_RenderTarget_Color) and read back. The
surface target comes from [TryAcquireSurfaceTarget](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_TryAcquireSurfaceTarget_MicroEngine_Graphics_Devices_RenderTarget__), is valid until the next
[Present](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_Present), cannot be sampled and cannot be read back.

 [Sampler](MicroEngine.Graphics.Devices.Sampler.md)

Device-owned, cached sampler object for one [SamplerDescription](MicroEngine.Graphics.Devices.SamplerDescription.md) (never disposed by callers).

 [Shader](MicroEngine.Graphics.Devices.Shader.md)

A custom shader stage.

 [TextureFormats](MicroEngine.Graphics.Devices.TextureFormats.md)

Byte-size rules of texture formats, shared by validation and backends.

 [Texture](MicroEngine.Graphics.Devices.Texture.md)

A texture that pipelines sample.

 [VertexLayout](MicroEngine.Graphics.Devices.VertexLayout.md)

Interleaved vertex layout of one vertex buffer (slot 0).

### Structs

 [BufferDescription](MicroEngine.Graphics.Devices.BufferDescription.md)

A buffer, available on every tier (tier 0: device-owned CPU memory submitted to the backend's geometry call; tier 1:
GPU memory). Contents are written only by [UpdateBuffer](MicroEngine.Graphics.Devices.CommandList.md#MicroEngine_Graphics_Devices_CommandList_UpdateBuffer_MicroEngine_Graphics_Devices_GpuBuffer_System_Int32_System_ReadOnlySpan_System_Byte__) and are undefined until written.

 [ColorAttachment](MicroEngine.Graphics.Devices.ColorAttachment.md)

The colour attachment of a pass.

 [Command](MicroEngine.Graphics.Devices.Command.md)

One validated command, as a backend reads it from [Commands](MicroEngine.Graphics.Devices.CommandList.md#MicroEngine_Graphics_Devices_CommandList_Commands).

 [DepthAttachment](MicroEngine.Graphics.Devices.DepthAttachment.md)

The depth attachment of a pass: the depth texture of a target created with [Depth](MicroEngine.Graphics.Devices.RenderTargetDescription.md#MicroEngine_Graphics_Devices_RenderTargetDescription_Depth).

 [DepthState](MicroEngine.Graphics.Devices.DepthState.md)

Depth test and write. Depth is 0 (near) .. 1 (far); anything but [None](MicroEngine.Graphics.Devices.DepthState.md#MicroEngine_Graphics_Devices_DepthState_None) requires [DepthBuffer](MicroEngine.Graphics.Devices.GraphicsFeatures.md).

 [GraphicsCounters](MicroEngine.Graphics.Devices.GraphicsCounters.md)

Deterministic counts of the work a device received ([Counters](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md#MicroEngine_Graphics_Devices_GraphicsDeviceBase_Counters)): the same calls give
the same counts on every backend and every run, so tests can assert draw calls, passes and uploads exactly as a
performance budget. Every count is cumulative since the device was created; subtract two readings to measure a span.

 [PassDescription](MicroEngine.Graphics.Devices.PassDescription.md)

A render pass: one colour attachment and an optional depth attachment (several colour attachments are a later,
additive extension). Viewport and scissor start as the whole colour target; bindings start empty.

 [RasterState](MicroEngine.Graphics.Devices.RasterState.md)

Rasterizer state. Scissor is not here: it is dynamic pass state, always on ([SetScissor](MicroEngine.Graphics.Devices.CommandList.md#MicroEngine_Graphics_Devices_CommandList_SetScissor_System_Nullable_MicroEngine_Graphics_Devices_RectI__)).

 [RectI](MicroEngine.Graphics.Devices.RectI.md)

An integer rectangle in pixels: origin top-left, x to the right, y down; right and bottom exclusive.

 [RenderTargetDescription](MicroEngine.Graphics.Devices.RenderTargetDescription.md)

An offscreen target: one colour texture that can also be sampled, and an optional depth(-stencil) texture.

 [SamplerDescription](MicroEngine.Graphics.Devices.SamplerDescription.md)

Immutable sampler state, a value: [GetSampler](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_GetSampler_MicroEngine_Graphics_Devices_SamplerDescription__) returns a cached device object for it.
Sampling is never texture state. <code class="paramref">Min</code> different from <code class="paramref">Mag</code> requires
[SeparateFilters](MicroEngine.Graphics.Devices.GraphicsFeatures.md). <code class="paramref">Mip</code> applies only to textures with more than one
mip level and is otherwise ignored (so [LinearClamp](MicroEngine.Graphics.Devices.SamplerDescription.md#MicroEngine_Graphics_Devices_SamplerDescription_LinearClamp) works on every tier).

 [ShaderDescription](MicroEngine.Graphics.Devices.ShaderDescription.md)

A custom shader; requires [Shaders](MicroEngine.Graphics.Devices.GraphicsFeatures.md) and a [Format](MicroEngine.Graphics.Devices.ShaderDescription.md#MicroEngine_Graphics_Devices_ShaderDescription_Format) in [ShaderFormats](MicroEngine.Graphics.Devices.GraphicsCapabilities.md#MicroEngine_Graphics_Devices_GraphicsCapabilities_ShaderFormats).

 [ShaderResources](MicroEngine.Graphics.Devices.ShaderResources.md)

Resource counts of a shader, produced by the offline tool's reflection and validated at creation. Bindings follow
the SDL_GPU register-space rules (vertex uniforms space1; fragment samplers space2, fragment uniforms space3).

 [TextureDescription](MicroEngine.Graphics.Devices.TextureDescription.md)

A sampled texture. Texture data uploads go through [CreateTexture](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_CreateTexture_MicroEngine_Graphics_Devices_TextureDescription__System_ReadOnlySpan_System_Byte__) and [UpdateTexture](MicroEngine.Graphics.Devices.CommandList.md#MicroEngine_Graphics_Devices_CommandList_UpdateTexture_MicroEngine_Graphics_Devices_Texture_MicroEngine_Graphics_Devices_RectI__System_ReadOnlySpan_System_Byte__).

 [Vertex2D](MicroEngine.Graphics.Devices.Vertex2D.md)

The vertex of the built-in pipeline ([Textured](MicroEngine.Graphics.Devices.BuiltInPipeline.md)), 32 bytes: position in target pixels
relative to the viewport origin (y down), texture coordinate normalised to 0..1 over the bound texture, premultiplied
colour that multiplies the texel. The layout is fixed and identical on every tier: [Vertex2D](MicroEngine.Graphics.Devices.VertexLayout.md#MicroEngine_Graphics_Devices_VertexLayout_Vertex2D).

 [VertexAttribute](MicroEngine.Graphics.Devices.VertexAttribute.md)

One vertex attribute.

### Interfaces

 [IGraphicsBackendFactory](MicroEngine.Graphics.Devices.IGraphicsBackendFactory.md)

Creates devices of one backend. A backend that accepts only its own surfaces says so through [UnsupportedReason](MicroEngine.Graphics.Devices.IGraphicsBackendFactory.md#MicroEngine_Graphics_Devices_IGraphicsBackendFactory_UnsupportedReason_MicroEngine_Platform_ISurface_).

 [IGraphicsDevice](MicroEngine.Graphics.Devices.IGraphicsDevice.md)

The single graphics device contract for every tier. Tiers differ only in [Capabilities](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_Capabilities); a request
outside them throws [GraphicsCapabilityException](MicroEngine.Graphics.Devices.GraphicsCapabilityException.md) at creation or recording time.

<p>Ordering rule: command lists execute in [Submit](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_Submit_MicroEngine_Graphics_Devices_CommandList_) order, and within a list in recording order
(updates included); [ReadPixels](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_ReadPixels_MicroEngine_Graphics_Devices_RenderTarget_MicroEngine_Graphics_Devices_RectI_) observes every list submitted before it.</p>
<p>Threading: one thread (the render thread) creates, records, submits and presents.</p>

### Enums

 [AddressMode](MicroEngine.Graphics.Devices.AddressMode.md)

What sampling does outside 0..1.

 [AlphaMode](MicroEngine.Graphics.Devices.AlphaMode.md)

How the colour channels of texel data relate to alpha.

 [BlendFactor](MicroEngine.Graphics.Devices.BlendFactor.md)

Blend factor (the set every tier-0 and tier-1 backend can express).

 [BlendOp](MicroEngine.Graphics.Devices.BlendOp.md)

Blend operation.

 [BufferUsage](MicroEngine.Graphics.Devices.BufferUsage.md)

Buffer usage. Uniform data is not a buffer: it is pushed with [SetUniforms](MicroEngine.Graphics.Devices.CommandList.md#MicroEngine_Graphics_Devices_CommandList_SetUniforms_MicroEngine_Graphics_Devices_ShaderStage_System_Int32_System_ReadOnlySpan_System_Byte__).

 [BuiltInPipeline](MicroEngine.Graphics.Devices.BuiltInPipeline.md)

The built-in pipeline every tier implements with every blend state of
[BuiltInBlendStates](MicroEngine.Graphics.Devices.GraphicsCapabilities.md#MicroEngine_Graphics_Devices_GraphicsCapabilities_BuiltInBlendStates): [Vertex2D](MicroEngine.Graphics.Devices.Vertex2D.md) vertices, one texture in slot 0, no
uniforms, no depth and no culling; colour = sample(texture) × vertex colour.

 [CommandKind](MicroEngine.Graphics.Devices.CommandKind.md)

Kind of a recorded command (backend-facing; see [Command](MicroEngine.Graphics.Devices.Command.md) for the fields each kind uses).

 [CompareOp](MicroEngine.Graphics.Devices.CompareOp.md)

Depth comparison.

 [CullMode](MicroEngine.Graphics.Devices.CullMode.md)

Face culling.

 [DeviceLostReason](MicroEngine.Graphics.Devices.DeviceLostReason.md)

Why a device was lost.

 [Filter](MicroEngine.Graphics.Devices.Filter.md)

Texel filter.

 [FrontFace](MicroEngine.Graphics.Devices.FrontFace.md)

Front-face winding in framebuffer space (y up in clip space). The engine convention is counter-clockwise.

 [GraphicsFeatures](MicroEngine.Graphics.Devices.GraphicsFeatures.md)

Optional features. Everything not listed here is mandatory on every tier: textures (Rgba8Unorm sampled and
rendered), samplers with clamp addressing, buffers, the built-in pipeline with the four blend presets, scissor,
viewport, readback and the surface target. A request that needs an undeclared feature fails with
[GraphicsCapabilityException](MicroEngine.Graphics.Devices.GraphicsCapabilityException.md) naming it; there is no silent degradation.

 [GraphicsTier](MicroEngine.Graphics.Devices.GraphicsTier.md)

Capability tier. Tiers differ only in declared capabilities, never in API shape.

 [IndexSize](MicroEngine.Graphics.Devices.IndexSize.md)

Index element size.

 [LoadOp](MicroEngine.Graphics.Devices.LoadOp.md)

What a pass does with an attachment's previous contents.

 [PresentMode](MicroEngine.Graphics.Devices.PresentMode.md)

When [Present](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_Present) waits for the display.

 [ShaderFormat](MicroEngine.Graphics.Devices.ShaderFormat.md)

Offline-compiled shader bytecode formats (produced by the tool from one HLSL source).

 [ShaderStage](MicroEngine.Graphics.Devices.ShaderStage.md)

Programmable stage.

 [StoreOp](MicroEngine.Graphics.Devices.StoreOp.md)

What a pass does with an attachment's contents at its end.

 [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)

Texel formats. Whether a format can be used for a purpose is a per-format, per-usage capability:
[Supports](MicroEngine.Graphics.Devices.GraphicsCapabilities.md#MicroEngine_Graphics_Devices_GraphicsCapabilities_Supports_MicroEngine_Graphics_Devices_TextureFormat_MicroEngine_Graphics_Devices_TextureUsage_) (depth formats in particular differ between
adapters: AMD Vulkan has no 24-bit depth). Using an unsupported combination throws
[GraphicsCapabilityException](MicroEngine.Graphics.Devices.GraphicsCapabilityException.md).

 [TextureUsage](MicroEngine.Graphics.Devices.TextureUsage.md)

What a texture of a given format may be used for; queried per format.

 [VertexFormat](MicroEngine.Graphics.Devices.VertexFormat.md)

Vertex attribute element format.

</div>
