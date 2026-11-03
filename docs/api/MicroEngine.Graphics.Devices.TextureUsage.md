# Enum TextureUsage

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

What a texture of a given format may be used for; queried per format.

```csharp
[Flags]
public enum TextureUsage
```

## Fields

`DepthStencil = 4` 

Depth(-stencil) attachment of a pass.



`None = 0` 

No usage.



`RenderTarget = 2` 

Colour attachment of a pass.



`Sampled = 1` 

Sampled by pipelines ([SetTexture](MicroEngine.Graphics.Devices.CommandList.md#MicroEngine_Graphics_Devices_CommandList_SetTexture_System_Int32_MicroEngine_Graphics_Devices_Texture_MicroEngine_Graphics_Devices_Sampler_)).

</div>
