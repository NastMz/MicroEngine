# Class GraphicsResource

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

Base of every device object. A resource belongs to the device that created it: using it with another device is an
`ArgumentException`, using it after [Dispose](MicroEngine.Graphics.Devices.GraphicsResource.md#MicroEngine_Graphics_Devices_GraphicsResource_Dispose) an `ObjectDisposedException`, both
raised by core validation before a backend sees the call. Every resource has a [Handle](MicroEngine.Graphics.Devices.GraphicsResource.md#MicroEngine_Graphics_Devices_GraphicsResource_Handle) with a
generation: once the resource is disposed the handle no longer resolves
([TryGetResource``1](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md#MicroEngine_Graphics_Devices_GraphicsDeviceBase_TryGetResource__1_MicroEngine_Kernel_Handle_MicroEngine_Graphics_Devices_GraphicsResource____0__)), even if its slot is reused. Destruction is deferred: the
backend object is released only when no submitted work uses it any more (at once on devices that execute command
lists at [Submit](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_Submit_MicroEngine_Graphics_Devices_CommandList_)).

```csharp
public abstract class GraphicsResource : IDisposable
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GraphicsResource](MicroEngine.Graphics.Devices.GraphicsResource.md)

#### Derived

[GpuBuffer](MicroEngine.Graphics.Devices.GpuBuffer.md), 
[Pipeline](MicroEngine.Graphics.Devices.Pipeline.md), 
[RenderTarget](MicroEngine.Graphics.Devices.RenderTarget.md), 
[Shader](MicroEngine.Graphics.Devices.Shader.md), 
[Texture](MicroEngine.Graphics.Devices.Texture.md)

#### Implements

[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Constructors

### <a id="MicroEngine_Graphics_Devices_GraphicsResource__ctor_MicroEngine_Graphics_Devices_IGraphicsDevice_System_String_"></a> GraphicsResource\(IGraphicsDevice, string?\)

Creates a resource owned by <code class="paramref">device</code>.

```csharp
protected GraphicsResource(IGraphicsDevice device, string? name)
```

#### Parameters

`device` [IGraphicsDevice](MicroEngine.Graphics.Devices.IGraphicsDevice.md)

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)?

## Properties

### <a id="MicroEngine_Graphics_Devices_GraphicsResource_Device"></a> Device

The owning device.

```csharp
public IGraphicsDevice Device { get; }
```

#### Property Value

 [IGraphicsDevice](MicroEngine.Graphics.Devices.IGraphicsDevice.md)

### <a id="MicroEngine_Graphics_Devices_GraphicsResource_Handle"></a> Handle

The resource's handle on its device: a slot and a generation. [None](MicroEngine.Kernel.md) until the device
registers the resource (core does it when the create call returns) and after a device that is not a
[GraphicsDeviceBase](MicroEngine.Graphics.Devices.GraphicsDeviceBase.md) created it.

```csharp
public Handle<GraphicsResource> Handle { get; }
```

#### Property Value

 [Handle](MicroEngine.Kernel.Handle-1.md)<[GraphicsResource](MicroEngine.Graphics.Devices.GraphicsResource.md)\>

### <a id="MicroEngine_Graphics_Devices_GraphicsResource_IsDisposed"></a> IsDisposed

True after [Dispose](MicroEngine.Graphics.Devices.GraphicsResource.md#MicroEngine_Graphics_Devices_GraphicsResource_Dispose) (or, for a surface target, after [Present](MicroEngine.Graphics.Devices.IGraphicsDevice.md#MicroEngine_Graphics_Devices_IGraphicsDevice_Present)).

```csharp
public bool IsDisposed { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_GraphicsResource_IsLost"></a> IsLost

True when a backend that detects device loss marked the resource lost; restorable resources are re-uploaded by their owner.

```csharp
public bool IsLost { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_Devices_GraphicsResource_Name"></a> Name

Debug name.

```csharp
public string? Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Graphics_Devices_GraphicsResource_OwnedBy"></a> OwnedBy

When not null, [Dispose](MicroEngine.Graphics.Devices.GraphicsResource.md#MicroEngine_Graphics_Devices_GraphicsResource_Dispose) throws `InvalidOperationException` with this text: device-owned
objects (built-in pipelines, a render target's colour texture, the surface target) are not disposed by callers.

```csharp
protected virtual string? OwnedBy { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

## Methods

### <a id="MicroEngine_Graphics_Devices_GraphicsResource_Dispose"></a> Dispose\(\)

Releases the resource (deferred until submitted work no longer uses it). Idempotent.

```csharp
public void Dispose()
```

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The resource is owned by its device or its render target.

### <a id="MicroEngine_Graphics_Devices_GraphicsResource_Expire"></a> Expire\(\)

Lets a backend end the life of an owned object (a surface target at present, a target's colour texture with its target).

```csharp
protected void Expire()
```

### <a id="MicroEngine_Graphics_Devices_GraphicsResource_MarkLost"></a> MarkLost\(\)

Lets a backend that detects device loss mark the resource lost.

```csharp
protected void MarkLost()
```

### <a id="MicroEngine_Graphics_Devices_GraphicsResource_Release"></a> Release\(\)

Backend release: called once, when no submitted work uses the resource any more.

```csharp
protected abstract void Release()
```

</div>
