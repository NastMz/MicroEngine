# Class HeadlessGraphicsDevice

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

A graphics device for tests: textures keep their size and filter but hold no pixels (unless [KeepsPixels](MicroEngine.Core.HeadlessGraphicsDevice.md#MicroEngine_Core_HeadlessGraphicsDevice_KeepsPixels));
creations, uploads and live textures are counted. Render targets are textures too; materials are created without a
shader to run, so they draw with their fallback ([IsSupported](MicroEngine.Graphics.Material2D.md#MicroEngine_Graphics_Material2D_IsSupported) is false).

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
from the game settings (<code>TextureFilter</code>, or its presentation's default sampler) before <code>Game.Load</code>.
Glyph pages of a scalable font follow it too unless the font's options choose a filter.

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

Number of textures created and not disposed yet, render targets included.

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

Number of textures created so far, render targets included.

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

### <a id="MicroEngine_Core_HeadlessGraphicsDevice_CreateMaterial_MicroEngine_Graphics_Devices_CompiledShader_MicroEngine_Graphics_MaterialFallback_"></a> CreateMaterial\(CompiledShader, MaterialFallback\)

Creates a material from a fragment shader compiled ahead of time (a <code>.shader</code> file of
<code>microengine shaders</code>); the engine supplies the vertex stage. Where the device cannot run shaders the
material is still created, checks its parameters the same way, and draws with <code class="paramref">fallback</code>
([IsSupported](MicroEngine.Graphics.Material2D.md#MicroEngine_Graphics_Material2D_IsSupported) says which). The caller owns it and disposes it; <code>Assets.Material</code>
gives one a scope owns.

```csharp
public Material2D CreateMaterial(CompiledShader fragment, MaterialFallback fallback = default)
```

#### Parameters

`fragment` [CompiledShader](MicroEngine.Graphics.Devices.CompiledShader.md)

`fallback` [MaterialFallback](MicroEngine.Graphics.MaterialFallback.md)

#### Returns

 [Material2D](MicroEngine.Graphics.Material2D.md)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">fragment</code> is not a fragment shader.

### <a id="MicroEngine_Core_HeadlessGraphicsDevice_CreateRenderTarget_System_Int32_System_Int32_"></a> CreateRenderTarget\(int, int\)

Creates a render target whose texture is sampled with [DefaultFilter](MicroEngine.Graphics.IGraphicsDevice.md#MicroEngine_Graphics_IGraphicsDevice_DefaultFilter).

```csharp
public RenderTarget2D CreateRenderTarget(int width, int height)
```

#### Parameters

`width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [RenderTarget2D](MicroEngine.Graphics.RenderTarget2D.md)

### <a id="MicroEngine_Core_HeadlessGraphicsDevice_CreateRenderTarget_System_Int32_System_Int32_MicroEngine_Graphics_TextureFilter_"></a> CreateRenderTarget\(int, int, TextureFilter\)

Creates a picture off screen of <code class="paramref">width</code> x <code class="paramref">height</code> pixels that a renderer draws
into ([PushTarget](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_PushTarget_MicroEngine_Graphics_RenderTarget2D_)) and whose texture is sampled with <code class="paramref">filter</code>. It starts
transparent. The caller owns it and disposes it; <code>Assets.RenderTarget</code> gives one a scope owns.

```csharp
public RenderTarget2D CreateRenderTarget(int width, int height, TextureFilter filter)
```

#### Parameters

`width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`filter` [TextureFilter](MicroEngine.Graphics.TextureFilter.md)

#### Returns

 [RenderTarget2D](MicroEngine.Graphics.RenderTarget2D.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

A size that is not positive.

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
