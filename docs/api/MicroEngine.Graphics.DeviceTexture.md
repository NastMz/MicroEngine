# Class DeviceTexture

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

A texture of the drawing API backed by a device texture ([DeviceGraphics](MicroEngine.Graphics.DeviceGraphics.md)): an uploaded image, a render
target's picture, or a device texture the game wrapped.

```csharp
public sealed class DeviceTexture : Texture, IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Texture](MicroEngine.Graphics.Texture.md) ← 
[DeviceTexture](MicroEngine.Graphics.DeviceTexture.md)

#### Implements

[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

#### Inherited Members

[Texture.CheckDrawable<TTexture\>\(Texture, IGraphicsDevice\)](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_CheckDrawable__1_MicroEngine_Graphics_Texture_MicroEngine_Graphics_IGraphicsDevice_), 
[Texture.Update\(int, int, int, int, ReadOnlySpan<Color\>\)](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_Update_System_Int32_System_Int32_System_Int32_System_Int32_System_ReadOnlySpan_MicroEngine_Color__), 
[Texture.Update\(PixelImage, int, int\)](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_Update_MicroEngine_Graphics_PixelImage_System_Int32_System_Int32_), 
[Texture.Dispose\(\)](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_Dispose), 
[Texture.Width](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_Width), 
[Texture.Height](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_Height), 
[Texture.IsDisposed](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_IsDisposed), 
[Texture.Device](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_Device), 
[Texture.Bounds](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_Bounds), 
[Texture.SizeInBytes](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_SizeInBytes), 
[Texture.Native](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_Native), 
[Texture.IsRenderTarget](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_IsRenderTarget), 
[Texture.MipLevels](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_MipLevels), 
[Texture.Filter](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_Filter), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring), 
[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode)

## Properties

### <a id="MicroEngine_Graphics_DeviceTexture_IsRenderTarget"></a> IsRenderTarget

Whether a renderer can draw into this texture: it is the picture of a [RenderTarget2D](MicroEngine.Graphics.RenderTarget2D.md).

```csharp
public override bool IsRenderTarget { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_DeviceTexture_MipLevels"></a> MipLevels

How many sizes of the picture the texture holds: 1 for the picture alone, more for a texture created with
[LinearMipmap](MicroEngine.Graphics.TextureFilter.md) on a device with mip levels (each level half the size of the one before,
down to one pixel). Updating the texture's pixels makes its smaller levels again.

```csharp
public override int MipLevels { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_DeviceTexture_Native"></a> Native

The device texture holding the premultiplied pixels.

```csharp
public override Texture Native { get; }
```

#### Property Value

 [Texture](MicroEngine.Graphics.Devices.Texture.md)

### <a id="MicroEngine_Graphics_DeviceTexture_Sampler"></a> Sampler

The sampler the texture's filter draws with (clamped at the edges).

```csharp
public SamplerDescription Sampler { get; }
```

#### Property Value

 [SamplerDescription](MicroEngine.Graphics.Devices.SamplerDescription.md)

### <a id="MicroEngine_Graphics_DeviceTexture_SizeInBytes"></a> SizeInBytes

The memory of every mip level in the device texture's format.

```csharp
public override long SizeInBytes { get; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Graphics_DeviceTexture_Target"></a> Target

The device render target this texture is the picture of, or null for an ordinary texture.

```csharp
public RenderTarget? Target { get; }
```

#### Property Value

 [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)?

## Methods

### <a id="MicroEngine_Graphics_DeviceTexture_ApplyFilter_MicroEngine_Graphics_TextureFilter_"></a> ApplyFilter\(TextureFilter\)

Applies a new filter on the device; called by [Filter](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_Filter) when it changes.

```csharp
protected override void ApplyFilter(TextureFilter filter)
```

#### Parameters

`filter` [TextureFilter](MicroEngine.Graphics.TextureFilter.md)

### <a id="MicroEngine_Graphics_DeviceTexture_Release"></a> Release\(\)

Frees the device resource; called once by [Dispose](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_Dispose).

```csharp
protected override void Release()
```

### <a id="MicroEngine_Graphics_DeviceTexture_Upload_System_Int32_System_Int32_System_Int32_System_Int32_System_ReadOnlySpan_MicroEngine_Color__"></a> Upload\(int, int, int, int, ReadOnlySpan<Color\>\)

Sends <code class="paramref">pixels</code> (exactly width × height, already checked) to the device.

```csharp
protected override void Upload(int x, int y, int width, int height, ReadOnlySpan<Color> pixels)
```

#### Parameters

`x` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`y` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`width` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`height` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`pixels` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[Color](MicroEngine.Color.md)\>

</div>
