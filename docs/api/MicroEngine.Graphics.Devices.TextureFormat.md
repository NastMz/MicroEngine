# Enum TextureFormat

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

Texel formats. Whether a format can be used for a purpose is a per-format, per-usage capability:
[Supports](MicroEngine.Graphics.Devices.GraphicsCapabilities.md#MicroEngine_Graphics_Devices_GraphicsCapabilities_Supports_MicroEngine_Graphics_Devices_TextureFormat_MicroEngine_Graphics_Devices_TextureUsage_) (depth formats in particular differ between
adapters: AMD Vulkan has no 24-bit depth). Using an unsupported combination throws
[GraphicsCapabilityException](MicroEngine.Graphics.Devices.GraphicsCapabilityException.md).

```csharp
public enum TextureFormat
```

## Fields

`Astc4x4 = 8` 

ASTC 4×4 block compression (16 bytes per 4×4 block).



`Bc7 = 6` 

BC7 block compression (16 bytes per 4×4 block).



`Bgra8Unorm = 2` 

8-bit BGRA, bytes in B, G, R, A order (the usual swapchain format).



`D16Unorm = 9` 

16-bit normalised depth.



`D24UnormS8 = 10` 

24-bit normalised depth with 8-bit stencil.



`D32Float = 11` 

32-bit float depth.



`Etc2Rgba8 = 7` 

ETC2 RGBA8 block compression (16 bytes per 4×4 block).



`R8Unorm = 3` 

One 8-bit channel.



`Rg8Unorm = 4` 

Two 8-bit channels.



`Rgba16Float = 5` 

16-bit float RGBA.



`Rgba8Unorm = 0` 

8-bit RGBA, bytes in R, G, B, A order. Supported for sampling and rendering on every tier.



`Rgba8UnormSrgb = 1` 

8-bit RGBA with sRGB encoding (sampling decodes, rendering encodes).

</div>
