# Class Texture

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

A GPU (or headless) texture owned by the active graphics device.

```csharp
public abstract class Texture : IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Texture](MicroEngine.Graphics.Texture.md)

#### Implements

[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Constructors

### <a id="MicroEngine_Graphics_Texture__ctor_System_Int32_System_Int32_MicroEngine_Graphics_TextureFilter_"></a> Texture\(int, int, TextureFilter\)

Records the size and filter of a texture created by a graphics device.

```csharp
protected Texture(int width, int height, TextureFilter filter = TextureFilter.Nearest)
```

#### Parameters

`width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`filter` [TextureFilter](MicroEngine.Graphics.TextureFilter.md)

### <a id="MicroEngine_Graphics_Texture__ctor_MicroEngine_Graphics_IGraphicsDevice_System_Int32_System_Int32_MicroEngine_Graphics_TextureFilter_"></a> Texture\(IGraphicsDevice, int, int, TextureFilter\)

Records the size, filter and creating <code class="paramref">device</code> of a texture.

```csharp
protected Texture(IGraphicsDevice device, int width, int height, TextureFilter filter = TextureFilter.Nearest)
```

#### Parameters

`device` [IGraphicsDevice](MicroEngine.Graphics.IGraphicsDevice.md)

`width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`filter` [TextureFilter](MicroEngine.Graphics.TextureFilter.md)

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">device</code> is null.

## Properties

### <a id="MicroEngine_Graphics_Texture_Bounds"></a> Bounds

The whole texture as a source rectangle: (0, 0, [Width](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_Width), [Height](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_Height)).

```csharp
public RectF Bounds { get; }
```

#### Property Value

 RectF

### <a id="MicroEngine_Graphics_Texture_Device"></a> Device

The graphics device that created the texture, or null for a texture whose device did not say (one made with the
constructor that takes no device). Renderers draw only the textures of their own device.

```csharp
public IGraphicsDevice? Device { get; }
```

#### Property Value

 [IGraphicsDevice](MicroEngine.Graphics.IGraphicsDevice.md)?

### <a id="MicroEngine_Graphics_Texture_Filter"></a> Filter

How the texture is sampled; it can change at any time and applies to the next draws.

```csharp
public TextureFilter Filter { get; set; }
```

#### Property Value

 [TextureFilter](MicroEngine.Graphics.TextureFilter.md)

### <a id="MicroEngine_Graphics_Texture_Height"></a> Height

Height in pixels.

```csharp
public int Height { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Texture_IsDisposed"></a> IsDisposed

Whether [Dispose](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_Dispose) was called. Drawing a disposed texture throws `ObjectDisposedException`
on every renderer, and nothing is drawn.

```csharp
public bool IsDisposed { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Texture_SizeInBytes"></a> SizeInBytes

Memory the texture's pixels take on the device, in bytes (4 per pixel).

```csharp
public long SizeInBytes { get; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Graphics_Texture_Width"></a> Width

Width in pixels.

```csharp
public int Width { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Graphics_Texture_ApplyFilter_MicroEngine_Graphics_TextureFilter_"></a> ApplyFilter\(TextureFilter\)

Applies a new filter on the device; called by [Filter](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_Filter) when it changes.

```csharp
protected abstract void ApplyFilter(TextureFilter filter)
```

#### Parameters

`filter` [TextureFilter](MicroEngine.Graphics.TextureFilter.md)

### <a id="MicroEngine_Graphics_Texture_CheckDrawable__1_MicroEngine_Graphics_Texture_MicroEngine_Graphics_IGraphicsDevice_"></a> CheckDrawable<TTexture\>\(Texture, IGraphicsDevice\)

The checks every renderer makes before it draws <code class="paramref">texture</code>, so that all backends fail alike: a
disposed texture throws `ObjectDisposedException`, and a texture another graphics device created (one
that is not a <code class="typeparamref">TTexture</code>, or whose [Device](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_Device) is not <code class="paramref">device</code>)
throws `ArgumentException`. Backend authors call it first in every drawing method that takes a texture.

```csharp
public static TTexture CheckDrawable<TTexture>(Texture texture, IGraphicsDevice device) where TTexture : Texture
```

#### Parameters

`texture` [Texture](MicroEngine.Graphics.Texture.md)

The texture a game asked to draw.

`device` [IGraphicsDevice](MicroEngine.Graphics.IGraphicsDevice.md)

The renderer's graphics device.

#### Returns

 TTexture

<code class="paramref">texture</code> as the device's own type.

#### Type Parameters

`TTexture` 

The texture type of the renderer's device.

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">texture</code> is null.

 [ObjectDisposedException](https://learn.microsoft.com/dotnet/api/system.objectdisposedexception)

The texture was disposed.

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

Another graphics device created the texture.

### <a id="MicroEngine_Graphics_Texture_Dispose"></a> Dispose\(\)

Releases the device texture. Safe to call more than once. Whoever created the texture (or the atlas or font that owns it) disposes it.

```csharp
public void Dispose()
```

### <a id="MicroEngine_Graphics_Texture_Release"></a> Release\(\)

Frees the device resource; called once by [Dispose](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_Dispose).

```csharp
protected abstract void Release()
```

### <a id="MicroEngine_Graphics_Texture_Update_System_Int32_System_Int32_System_Int32_System_Int32_System_ReadOnlySpan_MicroEngine_Color__"></a> Update\(int, int, int, int, ReadOnlySpan<Color\>\)

Replaces the pixels of a rectangle of the texture (glyphs added to a font page, a painted canvas). The pixels are
row-major, <code class="paramref">width</code> per row.

```csharp
public void Update(int x, int y, int width, int height, ReadOnlySpan<Color> pixels)
```

#### Parameters

`x` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`pixels` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<Color\>

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The rectangle leaves the texture.

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">pixels</code> holds fewer than <code class="paramref">width</code> × <code class="paramref">height</code> colours.

### <a id="MicroEngine_Graphics_Texture_Update_MicroEngine_Graphics_PixelImage_System_Int32_System_Int32_"></a> Update\(PixelImage, int, int\)

Copies a whole image into the texture at (<code class="paramref">x</code>, <code class="paramref">y</code>).

```csharp
public void Update(PixelImage image, int x = 0, int y = 0)
```

#### Parameters

`image` [PixelImage](MicroEngine.Graphics.PixelImage.md)

`x` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Texture_Upload_System_Int32_System_Int32_System_Int32_System_Int32_System_ReadOnlySpan_MicroEngine_Color__"></a> Upload\(int, int, int, int, ReadOnlySpan<Color\>\)

Sends <code class="paramref">pixels</code> (exactly width × height, already checked) to the device.

```csharp
protected abstract void Upload(int x, int y, int width, int height, ReadOnlySpan<Color> pixels)
```

#### Parameters

`x` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`pixels` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<Color\>

</div>
