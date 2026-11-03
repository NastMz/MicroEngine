# Enum BuiltInPipeline

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

The built-in pipeline every tier implements with every blend state of
[BuiltInBlendStates](MicroEngine.Graphics.Devices.GraphicsCapabilities.md#MicroEngine_Graphics_Devices_GraphicsCapabilities_BuiltInBlendStates): [Vertex2D](MicroEngine.Graphics.Devices.Vertex2D.md) vertices, one texture in slot 0, no
uniforms, no depth and no culling; colour = sample(texture) × vertex colour.

```csharp
public enum BuiltInPipeline
```

## Fields

`Textured = 0` 

Textured triangles: sprites, quads and arbitrary geometry (an untextured draw binds a white texel).

</div>
