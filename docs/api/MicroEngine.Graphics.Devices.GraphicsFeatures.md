# Enum GraphicsFeatures

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

Optional features. Everything not listed here is mandatory on every tier: textures (Rgba8Unorm sampled and
rendered), samplers with clamp addressing, buffers, the built-in pipeline with the four blend presets, scissor,
viewport, readback and the surface target. A request that needs an undeclared feature fails with
[GraphicsCapabilityException](MicroEngine.Graphics.Devices.GraphicsCapabilityException.md) naming it; there is no silent degradation.

```csharp
[Flags]
public enum GraphicsFeatures
```

## Fields

`AddressMirror = 4` 

[Mirror](MicroEngine.Graphics.Devices.AddressMode.md).



`AddressRepeat = 2` 

[Repeat](MicroEngine.Graphics.Devices.AddressMode.md).



`ComputeShaders = 256` 

Compute shaders (no operation in this contract version uses it).



`CustomBlend = 16` 

Any [BlendState](MicroEngine.Graphics.Devices.BlendState.md), not only those in [BuiltInBlendStates](MicroEngine.Graphics.Devices.GraphicsCapabilities.md#MicroEngine_Graphics_Devices_GraphicsCapabilities_BuiltInBlendStates).



`DepthBuffer = 64` 

Depth attachments, depth test and write.



`Instancing = 128` 

Draws with more than one instance.



`Mipmaps = 8` 

Textures with more than one mip level.



`None = 0` 

No optional feature.



`RenderTargets = 1` 

Offscreen render targets ([CreateRenderTarget](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_CreateRenderTarget_MicroEngine_Graphics_Devices_RenderTargetDescription__)).



`SeparateFilters = 512` 

Samplers whose minification and magnification filters differ.



`Shaders = 32` 

Custom shaders and pipelines.

</div>
