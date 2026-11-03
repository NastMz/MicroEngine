# Class DeviceGraphics

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

The textures of the drawing API ([IGraphicsDevice](MicroEngine.Graphics.IGraphicsDevice.md)) made on a graphics device of the device contract: each
texture is a device texture whose straight-alpha pixels are premultiplied when they are uploaded, and its
[Filter](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_Filter) chooses the sampler it is drawn with. Backends built on a device use it as their
[Graphics](MicroEngine.Core.IPlatform.md#MicroEngine_Core_IPlatform_Graphics).

```csharp
public sealed class DeviceGraphics : IGraphicsDevice
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[DeviceGraphics](MicroEngine.Graphics.DeviceGraphics.md)

#### Implements

[IGraphicsDevice](MicroEngine.Graphics.IGraphicsDevice.md)

## Constructors

### <a id="MicroEngine_Graphics_DeviceGraphics__ctor_MicroEngine_Graphics_Devices_IGraphicsDevice_"></a> DeviceGraphics\(IGraphicsDevice\)

Makes textures on <code class="paramref">device</code>.

```csharp
public DeviceGraphics(IGraphicsDevice device)
```

#### Parameters

`device` IGraphicsDevice

## Properties

### <a id="MicroEngine_Graphics_DeviceGraphics_DefaultFilter"></a> DefaultFilter

The filter of textures created without one: [CreateTexture](MicroEngine.Graphics.IGraphicsDevice.md#MicroEngine_Graphics_IGraphicsDevice_CreateTexture_MicroEngine_Graphics_PixelImage_MicroEngine_Graphics_TextureFilter_), atlases. The engine sets it
from [TextureFilter](MicroEngine.Core.GameSettings.md#MicroEngine_Core_GameSettings_TextureFilter) before <code>Game.Load</code>. Glyph pages of a
[ScalableFont](MicroEngine.Graphics.ScalableFont.md) do not follow it: their filter is [Filter](MicroEngine.Graphics.FontOptions.md#MicroEngine_Graphics_FontOptions_Filter), or linear for
antialiased text and nearest otherwise.

```csharp
public TextureFilter DefaultFilter { get; set; }
```

#### Property Value

 [TextureFilter](MicroEngine.Graphics.TextureFilter.md)

### <a id="MicroEngine_Graphics_DeviceGraphics_Device"></a> Device

The device the textures live on.

```csharp
public IGraphicsDevice Device { get; }
```

#### Property Value

 IGraphicsDevice

### <a id="MicroEngine_Graphics_DeviceGraphics_LiveTextures"></a> LiveTextures

Textures created and not disposed yet.

```csharp
public int LiveTextures { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Graphics_DeviceGraphics_CreateTexture_MicroEngine_Graphics_PixelImage_MicroEngine_Graphics_TextureFilter_"></a> CreateTexture\(PixelImage, TextureFilter\)

Uploads <code class="paramref">image</code> as a texture sampled with <code class="paramref">filter</code>. The caller owns the texture and disposes it.

```csharp
public Texture CreateTexture(PixelImage image, TextureFilter filter)
```

#### Parameters

`image` [PixelImage](MicroEngine.Graphics.PixelImage.md)

`filter` [TextureFilter](MicroEngine.Graphics.TextureFilter.md)

#### Returns

 [Texture](MicroEngine.Graphics.Texture.md)

### <a id="MicroEngine_Graphics_DeviceGraphics_DisposeAll"></a> DisposeAll\(\)

Disposes every texture not disposed yet (the platform is closing).

```csharp
public void DisposeAll()
```

</div>
