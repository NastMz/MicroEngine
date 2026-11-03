# Class Sampler

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

Device-owned, cached sampler object for one [SamplerDescription](MicroEngine.Graphics.Devices.SamplerDescription.md) (never disposed by callers).

```csharp
public abstract class Sampler
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Sampler](MicroEngine.Graphics.Devices.Sampler.md)

## Constructors

### <a id="MicroEngine_Graphics_Devices_Sampler__ctor_MicroEngine_Graphics_Devices_IGraphicsDevice_MicroEngine_Graphics_Devices_SamplerDescription__"></a> Sampler\(IGraphicsDevice, in SamplerDescription\)

Creates the sampler object.

```csharp
protected Sampler(IGraphicsDevice device, in SamplerDescription description)
```

#### Parameters

`device` [IGraphicsDevice](MicroEngine.Graphics.Devices.IGraphicsDevice.md)

`description` [SamplerDescription](MicroEngine.Graphics.Devices.SamplerDescription.md)

## Properties

### <a id="MicroEngine_Graphics_Devices_Sampler_Description"></a> Description

The sampler state.

```csharp
public SamplerDescription Description { get; }
```

#### Property Value

 [SamplerDescription](MicroEngine.Graphics.Devices.SamplerDescription.md)

### <a id="MicroEngine_Graphics_Devices_Sampler_Device"></a> Device

The owning device.

```csharp
public IGraphicsDevice Device { get; }
```

#### Property Value

 [IGraphicsDevice](MicroEngine.Graphics.Devices.IGraphicsDevice.md)

</div>
