# Class GpuBuffer

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

A vertex and/or index buffer.

```csharp
public abstract class GpuBuffer : GraphicsResource, IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GraphicsResource](MicroEngine.Graphics.Devices.GraphicsResource.md) ← 
[GpuBuffer](MicroEngine.Graphics.Devices.GpuBuffer.md)

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

### <a id="MicroEngine_Graphics_Devices_GpuBuffer__ctor_MicroEngine_Graphics_Devices_IGraphicsDevice_MicroEngine_Graphics_Devices_BufferDescription__"></a> GpuBuffer\(IGraphicsDevice, in BufferDescription\)

Creates the buffer object.

```csharp
protected GpuBuffer(IGraphicsDevice device, in BufferDescription description)
```

#### Parameters

`device` [IGraphicsDevice](MicroEngine.Graphics.Devices.IGraphicsDevice.md)

`description` [BufferDescription](MicroEngine.Graphics.Devices.BufferDescription.md)

## Properties

### <a id="MicroEngine_Graphics_Devices_GpuBuffer_Description"></a> Description

Size and usage.

```csharp
public BufferDescription Description { get; }
```

#### Property Value

 [BufferDescription](MicroEngine.Graphics.Devices.BufferDescription.md)

### <a id="MicroEngine_Graphics_Devices_GpuBuffer_SizeInBytes"></a> SizeInBytes

Size in bytes.

```csharp
public int SizeInBytes { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_Devices_GpuBuffer_Usage"></a> Usage

Usage flags.

```csharp
public BufferUsage Usage { get; }
```

#### Property Value

 [BufferUsage](MicroEngine.Graphics.Devices.BufferUsage.md)

</div>
