# Class Texture

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

A texture that pipelines sample.

```csharp
public abstract class Texture : GraphicsResource, IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GraphicsResource](MicroEngine.Graphics.Devices.GraphicsResource.md) ← 
[Texture](MicroEngine.Graphics.Devices.Texture.md)

#### Implements

[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

#### Inherited Members

[GraphicsResource.Dispose\(\)](MicroEngine.Graphics.Devices.GraphicsResource.md\#MicroEngine\_Graphics\_Devices\_GraphicsResource\_Dispose), 
[GraphicsResource.Release\(\)](MicroEngine.Graphics.Devices.GraphicsResource.md\#MicroEngine\_Graphics\_Devices\_GraphicsResource\_Release), 
[GraphicsResource.Expire\(\)](MicroEngine.Graphics.Devices.GraphicsResource.md\#MicroEngine\_Graphics\_Devices\_GraphicsResource\_Expire), 
[GraphicsResource.MarkLost\(\)](MicroEngine.Graphics.Devices.GraphicsResource.md\#MicroEngine\_Graphics\_Devices\_GraphicsResource\_MarkLost), 
[GraphicsResource.Device](MicroEngine.Graphics.Devices.GraphicsResource.md\#MicroEngine\_Graphics\_Devices\_GraphicsResource\_Device), 
[GraphicsResource.Name](MicroEngine.Graphics.Devices.GraphicsResource.md\#MicroEngine\_Graphics\_Devices\_GraphicsResource\_Name), 
[GraphicsResource.Handle](MicroEngine.Graphics.Devices.GraphicsResource.md\#MicroEngine\_Graphics\_Devices\_GraphicsResource\_Handle), 
[GraphicsResource.IsDisposed](MicroEngine.Graphics.Devices.GraphicsResource.md\#MicroEngine\_Graphics\_Devices\_GraphicsResource\_IsDisposed), 
[GraphicsResource.IsLost](MicroEngine.Graphics.Devices.GraphicsResource.md\#MicroEngine\_Graphics\_Devices\_GraphicsResource\_IsLost), 
[GraphicsResource.OwnedBy](MicroEngine.Graphics.Devices.GraphicsResource.md\#MicroEngine\_Graphics\_Devices\_GraphicsResource\_OwnedBy), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.MemberwiseClone\(\)](https://learn.microsoft.com/dotnet/api/system.object.memberwiseclone), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring), 
[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode)

## Constructors

### <a id="MicroEngine_Graphics_Devices_Texture__ctor_MicroEngine_Graphics_Devices_IGraphicsDevice_MicroEngine_Graphics_Devices_TextureDescription__"></a> Texture\(IGraphicsDevice, in TextureDescription\)

Creates the texture object.

```csharp
protected Texture(IGraphicsDevice device, in TextureDescription description)
```

#### Parameters

`device` [IGraphicsDevice](MicroEngine.Graphics.Devices.IGraphicsDevice.md)

`description` [TextureDescription](MicroEngine.Graphics.Devices.TextureDescription.md)

## Properties

### <a id="MicroEngine_Graphics_Devices_Texture_Description"></a> Description

The creation description (for a render target's colour texture: its size and format).

```csharp
public TextureDescription Description { get; }
```

#### Property Value

 [TextureDescription](MicroEngine.Graphics.Devices.TextureDescription.md)

### <a id="MicroEngine_Graphics_Devices_Texture_Format"></a> Format

Texel format.

```csharp
public TextureFormat Format { get; }
```

#### Property Value

 [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)

### <a id="MicroEngine_Graphics_Devices_Texture_Height"></a> Height

Height in texels.

```csharp
public int Height { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_Texture_OwnerTarget"></a> OwnerTarget

The render target this texture is the colour of, or null for an ordinary texture.

```csharp
public virtual RenderTarget? OwnerTarget { get; }
```

#### Property Value

 [RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)?

### <a id="MicroEngine_Graphics_Devices_Texture_Width"></a> Width

Width in texels.

```csharp
public int Width { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

</div>
