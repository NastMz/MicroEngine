# Interface IGraphicsDevice

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

Creates device resources. Separated so headless tests can provide fakes.

```csharp
public interface IGraphicsDevice
```

## Properties

### <a id="MicroEngine_Graphics_IGraphicsDevice_DefaultFilter"></a> DefaultFilter

The filter of textures created without one: [CreateTexture](MicroEngine.Graphics.IGraphicsDevice.md#MicroEngine_Graphics_IGraphicsDevice_CreateTexture_MicroEngine_Graphics_PixelImage_MicroEngine_Graphics_TextureFilter_), atlases. The engine sets it
from the game settings (<code>TextureFilter</code>, or its presentation's default sampler) before <code>Game.Load</code>.
Glyph pages of a scalable font follow it too unless the font's options choose a filter.

```csharp
TextureFilter DefaultFilter { get; set; }
```

#### Property Value

 [TextureFilter](MicroEngine.Graphics.TextureFilter.md)

## Methods

### <a id="MicroEngine_Graphics_IGraphicsDevice_CreateMaterial_MicroEngine_Graphics_Devices_CompiledShader_MicroEngine_Graphics_MaterialFallback_"></a> CreateMaterial\(CompiledShader, MaterialFallback\)

Creates a material from a fragment shader compiled ahead of time (a <code>.shader</code> file of
<code>microengine shaders</code>); the engine supplies the vertex stage. Where the device cannot run shaders the
material is still created, checks its parameters the same way, and draws with <code class="paramref">fallback</code>
([IsSupported](MicroEngine.Graphics.Material2D.md#MicroEngine_Graphics_Material2D_IsSupported) says which). The caller owns it and disposes it; <code>Assets.Material</code>
gives one a scope owns.

```csharp
Material2D CreateMaterial(CompiledShader fragment, MaterialFallback fallback = default)
```

#### Parameters

`fragment` [CompiledShader](MicroEngine.Graphics.Devices.CompiledShader.md)

`fallback` [MaterialFallback](MicroEngine.Graphics.MaterialFallback.md)

#### Returns

 [Material2D](MicroEngine.Graphics.Material2D.md)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">fragment</code> is not a fragment shader.

### <a id="MicroEngine_Graphics_IGraphicsDevice_CreateRenderTarget_System_Int32_System_Int32_MicroEngine_Graphics_TextureFilter_"></a> CreateRenderTarget\(int, int, TextureFilter\)

Creates a picture off screen of <code class="paramref">width</code> x <code class="paramref">height</code> pixels that a renderer draws
into ([PushTarget](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_PushTarget_MicroEngine_Graphics_RenderTarget2D_)) and whose texture is sampled with <code class="paramref">filter</code>. It starts
transparent. The caller owns it and disposes it; <code>Assets.RenderTarget</code> gives one a scope owns.

```csharp
RenderTarget2D CreateRenderTarget(int width, int height, TextureFilter filter)
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

### <a id="MicroEngine_Graphics_IGraphicsDevice_CreateRenderTarget_System_Int32_System_Int32_"></a> CreateRenderTarget\(int, int\)

Creates a render target whose texture is sampled with [DefaultFilter](MicroEngine.Graphics.IGraphicsDevice.md#MicroEngine_Graphics_IGraphicsDevice_DefaultFilter).

```csharp
RenderTarget2D CreateRenderTarget(int width, int height)
```

#### Parameters

`width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [RenderTarget2D](MicroEngine.Graphics.RenderTarget2D.md)

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
