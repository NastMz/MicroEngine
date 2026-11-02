# Class HeadlessGraphicsDevice

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

A graphics device for tests: textures keep their size and filter but hold no pixels (unless [KeepsPixels](MicroEngine.Core.HeadlessGraphicsDevice.md#MicroEngine_Core_HeadlessGraphicsDevice_KeepsPixels));
creations, uploads and live textures are counted.

```csharp
public sealed class HeadlessGraphicsDevice : IGraphicsDevice
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[HeadlessGraphicsDevice](MicroEngine.Core.HeadlessGraphicsDevice.md)

#### Implements

[IGraphicsDevice](MicroEngine.Graphics.IGraphicsDevice.md)

## Constructors

### <a id="MicroEngine_Core_HeadlessGraphicsDevice__ctor"></a> HeadlessGraphicsDevice\(\)

```csharp
public HeadlessGraphicsDevice()
```

## Properties

### <a id="MicroEngine_Core_HeadlessGraphicsDevice_DefaultFilter"></a> DefaultFilter

The filter of textures created without one: [CreateTexture](MicroEngine.Graphics.IGraphicsDevice.md#MicroEngine_Graphics_IGraphicsDevice_CreateTexture_MicroEngine_Graphics_PixelImage_MicroEngine_Graphics_TextureFilter_), atlases. The engine sets it
from [TextureFilter](MicroEngine.Core.GameSettings.md#MicroEngine_Core_GameSettings_TextureFilter) before <code>Game.Load</code>. Glyph pages of a
[ScalableFont](MicroEngine.Graphics.ScalableFont.md) do not follow it: their filter is [Filter](MicroEngine.Graphics.FontOptions.md#MicroEngine_Graphics_FontOptions_Filter), or linear for
antialiased text and nearest otherwise.

```csharp
public TextureFilter DefaultFilter { get; set; }
```

#### Property Value

 [TextureFilter](MicroEngine.Graphics.TextureFilter.md)

### <a id="MicroEngine_Core_HeadlessGraphicsDevice_KeepsPixels"></a> KeepsPixels

Whether textures keep a copy of their pixels (creations and updates), which a rasterizing
[HeadlessRenderer](MicroEngine.Core.HeadlessRenderer.md) needs to draw them. False by default.

```csharp
public bool KeepsPixels { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_HeadlessGraphicsDevice_LiveTextures"></a> LiveTextures

Number of textures created and not disposed yet.

```csharp
public int LiveTextures { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_HeadlessGraphicsDevice_PixelsUploaded"></a> PixelsUploaded

Pixels sent by creations and updates so far.

```csharp
public long PixelsUploaded { get; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Core_HeadlessGraphicsDevice_TexturesCreated"></a> TexturesCreated

Number of textures created so far.

```csharp
public int TexturesCreated { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_HeadlessGraphicsDevice_Updates"></a> Updates

Number of region updates ([Update](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_Update_System_Int32_System_Int32_System_Int32_System_Int32_System_ReadOnlySpan_MicroEngine_Color__)) so far.

```csharp
public int Updates { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Core_HeadlessGraphicsDevice_CreateTexture_MicroEngine_Graphics_PixelImage_"></a> CreateTexture\(PixelImage\)

Uploads <code class="paramref">image</code> as a texture sampled with [DefaultFilter](MicroEngine.Graphics.IGraphicsDevice.md#MicroEngine_Graphics_IGraphicsDevice_DefaultFilter). The caller owns the texture and disposes it.

```csharp
public Texture CreateTexture(PixelImage image)
```

#### Parameters

`image` [PixelImage](MicroEngine.Graphics.PixelImage.md)

#### Returns

 [Texture](MicroEngine.Graphics.Texture.md)

### <a id="MicroEngine_Core_HeadlessGraphicsDevice_CreateTexture_MicroEngine_Graphics_PixelImage_MicroEngine_Graphics_TextureFilter_"></a> CreateTexture\(PixelImage, TextureFilter\)

Uploads <code class="paramref">image</code> as a texture sampled with <code class="paramref">filter</code>. The caller owns the texture and disposes it.

```csharp
public Texture CreateTexture(PixelImage image, TextureFilter filter)
```

#### Parameters

`image` [PixelImage](MicroEngine.Graphics.PixelImage.md)

`filter` [TextureFilter](MicroEngine.Graphics.TextureFilter.md)

#### Returns

 [Texture](MicroEngine.Graphics.Texture.md)

</div>
