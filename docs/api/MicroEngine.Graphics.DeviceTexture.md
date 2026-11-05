# Class DeviceTexture

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.dll  
Guide: [Graphics](../manual/rendering.md)  

A texture of the drawing API backed by a device texture ([DeviceGraphics](MicroEngine.Graphics.DeviceGraphics.md)).

```csharp
public sealed class DeviceTexture : Texture, IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
Texture ← 
[DeviceTexture](MicroEngine.Graphics.DeviceTexture.md)

#### Implements

[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

#### Inherited Members

Texture.CheckDrawable<TTexture\>\(Texture, IGraphicsDevice\), 
Texture.Update\(int, int, int, int, ReadOnlySpan<Color\>\), 
Texture.Update\(PixelImage, int, int\), 
Texture.Dispose\(\), 
Texture.Width, 
Texture.Height, 
Texture.IsDisposed, 
Texture.Device, 
Texture.Bounds, 
Texture.SizeInBytes, 
Texture.Filter, 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring), 
[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode)

## Properties

### <a id="MicroEngine_Graphics_DeviceTexture_Native"></a> Native

The device texture holding the premultiplied pixels.

```csharp
public Texture Native { get; }
```

#### Property Value

 Texture

### <a id="MicroEngine_Graphics_DeviceTexture_Sampler"></a> Sampler

The sampler the texture's filter draws with (clamped at the edges).

```csharp
public SamplerDescription Sampler { get; }
```

#### Property Value

 SamplerDescription

## Methods

### <a id="MicroEngine_Graphics_DeviceTexture_ApplyFilter_MicroEngine_Graphics_TextureFilter_"></a> ApplyFilter\(TextureFilter\)

Applies a new filter on the device; called by [Filter](MicroEngine.Graphics.Texture.md#MicroEngine_Graphics_Texture_Filter) when it changes.

```csharp
protected override void ApplyFilter(TextureFilter filter)
```

#### Parameters

`filter` TextureFilter

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

`pixels` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<Color\>

</div>
