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
[Texture](MicroEngine.Graphics.Texture.md) ← 
[DeviceTexture](MicroEngine.Graphics.DeviceTexture.md)

#### Implements

[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

#### Inherited Members

[Texture.CheckDrawable<TTexture\>\(Texture, IGraphicsDevice\)](MicroEngine.Graphics.Texture.md\#MicroEngine\_Graphics\_Texture\_CheckDrawable\_\_1\_MicroEngine\_Graphics\_Texture\_MicroEngine\_Graphics\_IGraphicsDevice\_), 
[Texture.Update\(int, int, int, int, ReadOnlySpan<Color\>\)](MicroEngine.Graphics.Texture.md\#MicroEngine\_Graphics\_Texture\_Update\_System\_Int32\_System\_Int32\_System\_Int32\_System\_Int32\_System\_ReadOnlySpan\_MicroEngine\_Color\_\_), 
[Texture.Update\(PixelImage, int, int\)](MicroEngine.Graphics.Texture.md\#MicroEngine\_Graphics\_Texture\_Update\_MicroEngine\_Graphics\_PixelImage\_System\_Int32\_System\_Int32\_), 
[Texture.Dispose\(\)](MicroEngine.Graphics.Texture.md\#MicroEngine\_Graphics\_Texture\_Dispose), 
[Texture.Width](MicroEngine.Graphics.Texture.md\#MicroEngine\_Graphics\_Texture\_Width), 
[Texture.Height](MicroEngine.Graphics.Texture.md\#MicroEngine\_Graphics\_Texture\_Height), 
[Texture.IsDisposed](MicroEngine.Graphics.Texture.md\#MicroEngine\_Graphics\_Texture\_IsDisposed), 
[Texture.Device](MicroEngine.Graphics.Texture.md\#MicroEngine\_Graphics\_Texture\_Device), 
[Texture.Bounds](MicroEngine.Graphics.Texture.md\#MicroEngine\_Graphics\_Texture\_Bounds), 
[Texture.SizeInBytes](MicroEngine.Graphics.Texture.md\#MicroEngine\_Graphics\_Texture\_SizeInBytes), 
[Texture.Filter](MicroEngine.Graphics.Texture.md\#MicroEngine\_Graphics\_Texture\_Filter), 
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

`pixels` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<Color\>

</div>
