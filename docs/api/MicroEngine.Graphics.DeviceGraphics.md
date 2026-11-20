# Class DeviceGraphics

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

The textures, render targets and materials of the drawing API ([IGraphicsDevice](MicroEngine.Graphics.IGraphicsDevice.md)) made on a graphics
device of the device contract: each texture is a device texture whose straight-alpha pixels are premultiplied when
they are uploaded, and its [Filter](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_Filter) chooses the sampler it is drawn with. A device texture or
render target the game made itself becomes a texture of the drawing API with [Wrap](MicroEngine.Graphics.DeviceGraphics.md#MicroEngine_Graphics_DeviceGraphics_Wrap_MicroEngine_Graphics_Devices_Texture_System_Nullable_MicroEngine_Graphics_TextureFilter__System_Boolean_).
Backends built on a device use it as their [Graphics](MicroEngine.Core.IPlatform.md#MicroEngine_Core_IPlatform_Graphics).

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

`device` [IGraphicsDevice](MicroEngine.Graphics.Devices.IGraphicsDevice.md)

## Properties

### <a id="MicroEngine_Graphics_DeviceGraphics_DefaultFilter"></a> DefaultFilter

The filter of textures created without one: [CreateTexture](MicroEngine.Graphics.IGraphicsDevice.md#MicroEngine_Graphics_IGraphicsDevice_CreateTexture_MicroEngine_Graphics_PixelImage_MicroEngine_Graphics_TextureFilter_), atlases. The engine sets it
from the game settings (<code>TextureFilter</code>, or its presentation's default sampler) before <code>Game.Load</code>.
Glyph pages of a scalable font follow it too unless the font's options choose a filter.

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

### <a id="MicroEngine_Graphics_DeviceGraphics_CreateMaterial_MicroEngine_Graphics_Devices_CompiledShader_MicroEngine_Graphics_MaterialFallback_"></a> CreateMaterial\(CompiledShader, MaterialFallback\)

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

### <a id="MicroEngine_Graphics_DeviceGraphics_CreateRenderTarget_System_Int32_System_Int32_"></a> CreateRenderTarget\(int, int\)

Creates a render target whose texture is sampled with [DefaultFilter](MicroEngine.Graphics.IGraphicsDevice.md#MicroEngine_Graphics_IGraphicsDevice_DefaultFilter).

```csharp
public RenderTarget2D CreateRenderTarget(int width, int height)
```

#### Parameters

`width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [RenderTarget2D](MicroEngine.Graphics.RenderTarget2D.md)

### <a id="MicroEngine_Graphics_DeviceGraphics_CreateRenderTarget_System_Int32_System_Int32_MicroEngine_Graphics_TextureFilter_"></a> CreateRenderTarget\(int, int, TextureFilter\)

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

### <a id="MicroEngine_Graphics_DeviceGraphics_CreateTexture_MicroEngine_Graphics_PixelImage_"></a> CreateTexture\(PixelImage\)

Uploads <code class="paramref">image</code> as a texture sampled with [DefaultFilter](MicroEngine.Graphics.IGraphicsDevice.md#MicroEngine_Graphics_IGraphicsDevice_DefaultFilter). The caller owns the texture and disposes it.

```csharp
public Texture CreateTexture(PixelImage image)
```

#### Parameters

`image` [PixelImage](MicroEngine.Graphics.PixelImage.md)

#### Returns

 [Texture](MicroEngine.Graphics.Texture.md)

### <a id="MicroEngine_Graphics_DeviceGraphics_DisposeAll"></a> DisposeAll\(\)

Disposes every texture not disposed yet (the platform is closing).

```csharp
public void DisposeAll()
```

### <a id="MicroEngine_Graphics_DeviceGraphics_Wrap_MicroEngine_Graphics_Devices_Texture_System_Nullable_MicroEngine_Graphics_TextureFilter__System_Boolean_"></a> Wrap\(Texture, TextureFilter?, bool\)

A device texture the game created itself (in any format the device samples: a single channel, a compressed
format, with mip levels) as a texture of the drawing API, so [IRenderer](MicroEngine.Graphics.IRenderer.md) draws it and a
[Sprite](MicroEngine.Graphics.Sprite.md) can cut from it. Its pixels are taken as premultiplied, as the device holds them.

```csharp
public Texture Wrap(Texture native, TextureFilter? filter = null, bool ownsNative = false)
```

#### Parameters

`native` Texture

A texture of [Device](MicroEngine.Graphics.DeviceGraphics.md#MicroEngine_Graphics_DeviceGraphics_Device), not a render target's colour (wrap the target instead).

`filter` [TextureFilter](MicroEngine.Graphics.TextureFilter.md)?

How it is sampled; null for [DefaultFilter](MicroEngine.Graphics.DeviceGraphics.md#MicroEngine_Graphics_DeviceGraphics_DefaultFilter).

`ownsNative` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether disposing the returned texture disposes <code class="paramref">native</code> too.

#### Returns

 Texture

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

A texture of another device, or a render target's colour texture.

 [ObjectDisposedException](https://learn.microsoft.com/dotnet/api/system.objectdisposedexception)

A disposed texture.

### <a id="MicroEngine_Graphics_DeviceGraphics_Wrap_MicroEngine_Graphics_Devices_RenderTarget_System_Nullable_MicroEngine_Graphics_TextureFilter__System_Boolean_"></a> Wrap\(RenderTarget, TextureFilter?, bool\)

A device render target the game created itself as a render target of the drawing API: [PushTarget](MicroEngine.Graphics.IRenderer.md#MicroEngine_Graphics_IRenderer_PushTarget_MicroEngine_Graphics_RenderTarget2D_)
draws into it and its texture is drawn like any other.

```csharp
public RenderTarget2D Wrap(RenderTarget native, TextureFilter? filter = null, bool ownsNative = false)
```

#### Parameters

`native` [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)

An offscreen target of [Device](MicroEngine.Graphics.DeviceGraphics.md#MicroEngine_Graphics_DeviceGraphics_Device).

`filter` [TextureFilter](MicroEngine.Graphics.TextureFilter.md)?

How its texture is sampled; null for [DefaultFilter](MicroEngine.Graphics.DeviceGraphics.md#MicroEngine_Graphics_DeviceGraphics_DefaultFilter).

`ownsNative` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether disposing the returned target disposes <code class="paramref">native</code> too.

#### Returns

 [RenderTarget2D](MicroEngine.Graphics.RenderTarget2D.md)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

A target of another device, or the surface target.

 [ObjectDisposedException](https://learn.microsoft.com/dotnet/api/system.objectdisposedexception)

A disposed target.

</div>
