# Class RenderTarget

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

A colour attachment (plus optional depth) that passes render to. Offscreen targets come from
[CreateRenderTarget](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_CreateRenderTarget_MicroEngine_Graphics_Devices_RenderTargetDescription__) and can be sampled through [Color](MicroEngine.Graphics.Devices.RenderTarget.md#MicroEngine_Graphics_Devices_RenderTarget_Color) and read back. The
surface target comes from [TryAcquireSurfaceTarget](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_TryAcquireSurfaceTarget_MicroEngine_Graphics_Devices_RenderTarget__), is valid until the next
[Present](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_Present), cannot be sampled and cannot be read back.

```csharp
public abstract class RenderTarget : GraphicsResource, IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GraphicsResource](MicroEngine.Graphics.Devices.GraphicsResource.md) ← 
[RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md)

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

### <a id="MicroEngine_Graphics_Devices_RenderTarget__ctor_MicroEngine_Graphics_Devices_IGraphicsDevice_MicroEngine_Graphics_Devices_RenderTargetDescription__System_Boolean_"></a> RenderTarget\(IGraphicsDevice, in RenderTargetDescription, bool\)

Creates the target object.

```csharp
protected RenderTarget(IGraphicsDevice device, in RenderTargetDescription description, bool isSurface)
```

#### Parameters

`device` [IGraphicsDevice](MicroEngine.Graphics.Devices.IGraphicsDevice.md)

`description` [RenderTargetDescription](MicroEngine.Graphics.Devices.RenderTargetDescription.md)

`isSurface` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Properties

### <a id="MicroEngine_Graphics_Devices_RenderTarget_Bounds"></a> Bounds

The whole target as a rectangle.

```csharp
public RectI Bounds { get; }
```

#### Property Value

 [RectI](MicroEngine.Graphics.Devices.RectI.md)

### <a id="MicroEngine_Graphics_Devices_RenderTarget_Color"></a> Color

The colour texture for sampling; null for the surface target. Owned by the target.

```csharp
public abstract Texture? Color { get; }
```

#### Property Value

 [Texture](MicroEngine.Graphics.Devices.Texture.md)?

### <a id="MicroEngine_Graphics_Devices_RenderTarget_ColorFormat"></a> ColorFormat

Colour format: what a pipeline drawing into it must declare.

```csharp
public TextureFormat ColorFormat { get; }
```

#### Property Value

 [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)

### <a id="MicroEngine_Graphics_Devices_RenderTarget_DepthFormat"></a> DepthFormat

Depth format, or null when the target has no depth texture.

```csharp
public TextureFormat? DepthFormat { get; }
```

#### Property Value

 [TextureFormat](MicroEngine.Graphics.Devices.TextureFormat.md)?

### <a id="MicroEngine_Graphics_Devices_RenderTarget_Description"></a> Description

Size and formats.

```csharp
public RenderTargetDescription Description { get; }
```

#### Property Value

 [RenderTargetDescription](MicroEngine.Graphics.Devices.RenderTargetDescription.md)

### <a id="MicroEngine_Graphics_Devices_RenderTarget_Height"></a> Height

Height in pixels.

```csharp
public int Height { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_RenderTarget_IsSurface"></a> IsSurface

True for the surface (back buffer) target.

```csharp
public bool IsSurface { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_RenderTarget_Width"></a> Width

Width in pixels.

```csharp
public int Width { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

</div>
