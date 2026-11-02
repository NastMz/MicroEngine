# Interface IGraphicsDevice

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

Creates device resources. Separated so headless tests can provide fakes.

```csharp
public interface IGraphicsDevice
```

## Properties

### <a id="MicroEngine_Graphics_IGraphicsDevice_DefaultFilter"></a> DefaultFilter

The filter of textures created without one: [CreateTexture](MicroEngine.Graphics.IGraphicsDevice.md#MicroEngine_Graphics_IGraphicsDevice_CreateTexture_MicroEngine_Graphics_PixelImage_MicroEngine_Graphics_TextureFilter_), atlases. The engine sets it
from [TextureFilter](MicroEngine.Core.GameSettings.md#MicroEngine_Core_GameSettings_TextureFilter) before <code>Game.Load</code>. Glyph pages of a
[ScalableFont](MicroEngine.Graphics.ScalableFont.md) do not follow it: their filter is [Filter](MicroEngine.Graphics.FontOptions.md#MicroEngine_Graphics_FontOptions_Filter), or linear for
antialiased text and nearest otherwise.

```csharp
TextureFilter DefaultFilter { get; set; }
```

#### Property Value

 [TextureFilter](MicroEngine.Graphics.TextureFilter.md)

## Methods

### <a id="MicroEngine_Graphics_IGraphicsDevice_CreateTexture_MicroEngine_Graphics_PixelImage_MicroEngine_Graphics_TextureFilter_"></a> CreateTexture\(PixelImage, TextureFilter\)

Uploads <code class="paramref">image</code> as a texture sampled with <code class="paramref">filter</code>. The caller owns the texture and disposes it.

```csharp
Texture CreateTexture(PixelImage image, TextureFilter filter)
```

#### Parameters

`image` [PixelImage](MicroEngine.Graphics.PixelImage.md)

`filter` [TextureFilter](MicroEngine.Graphics.TextureFilter.md)

#### Returns

 [Texture](MicroEngine.Graphics.Texture.md)

### <a id="MicroEngine_Graphics_IGraphicsDevice_CreateTexture_MicroEngine_Graphics_PixelImage_"></a> CreateTexture\(PixelImage\)

Uploads <code class="paramref">image</code> as a texture sampled with [DefaultFilter](MicroEngine.Graphics.IGraphicsDevice.md#MicroEngine_Graphics_IGraphicsDevice_DefaultFilter). The caller owns the texture and disposes it.

```csharp
Texture CreateTexture(PixelImage image)
```

#### Parameters

`image` [PixelImage](MicroEngine.Graphics.PixelImage.md)

#### Returns

 [Texture](MicroEngine.Graphics.Texture.md)

</div>
