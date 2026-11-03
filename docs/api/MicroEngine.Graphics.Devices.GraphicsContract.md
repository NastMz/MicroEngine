# Class GraphicsContract

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

The graphics device contract this assembly implements. The contract text is the XML documentation of the types in
<code>MicroEngine.Graphics.Devices</code>; every rule it states is checked by the device conformance suite. A change to a
rule or to the shape of a type is a contract revision: it raises [Version](MicroEngine.Graphics.Devices.GraphicsContract.md#MicroEngine_Graphics_Devices_GraphicsContract_Version) and records its evidence.

```csharp
public static class GraphicsContract
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GraphicsContract](MicroEngine.Graphics.Devices.GraphicsContract.md)

## Remarks

Written rules (each is repeated on the member it governs):

<ul><li>Colours are premultiplied ([ColorF](MicroEngine.ColorF.md) channels already multiplied by alpha) and stored as written:
the device applies no transfer function to <code>Rgba8Unorm</code> data.</li><li>Built-in vertices ([Vertex2D](MicroEngine.Graphics.Devices.Vertex2D.md)) are target pixels relative to the viewport origin, y down; pixel
(x, y) is the square [x, x + 1) × [y, y + 1) with its centre at (x + 0.5, y + 0.5).</li><li>Coverage: a triangle covers exactly the pixels whose centre lies inside it; a centre on an edge belongs to the
triangle when the edge is a top or a left edge (the top-left rule of Direct3D, Vulkan, Metal and WebGPU).</li><li>Clears ([Clear](MicroEngine.Graphics.Devices.LoadOp.md)) ignore viewport and scissor; scissor is absolute in target pixels.</li><li>Bindings, pipeline, viewport and scissor are pass-scoped; uniform data persist for the rest of the list.</li><li>Command lists execute in [Submit](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_Submit_MicroEngine_Graphics_Devices_CommandList_) order and, within a list, in recording order
(updates included); [ReadPixels](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_ReadPixels_MicroEngine_Graphics_Devices_RenderTarget_MicroEngine_Graphics_Devices_RectI_) observes every list submitted before it.</li><li>Surface targets expire at [Present](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_Present) and cannot be sampled or read back.</li><li>Numeric conventions: System.Numerics right-handed matrices uploaded as-is, <code>mul(M, v)</code> in HLSL,
counter-clockwise front faces, depth 0 (near) .. 1 (far), NDC y up, row 0 of a target at the top.</li></ul>

## Fields

### <a id="MicroEngine_Graphics_Devices_GraphicsContract_Version"></a> Version

The contract version: 1 (frozen; the built-in pipeline is [Textured](MicroEngine.Graphics.Devices.BuiltInPipeline.md)).

```csharp
public const int Version = 1
```

#### Field Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

</div>
