# Class ReferenceGraphicsBackendFactory

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

Factory of the CPU reference device ("reference"): offscreen, or over any surface (it never touches the window).

```csharp
public sealed class ReferenceGraphicsBackendFactory : IGraphicsBackendFactory
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ReferenceGraphicsBackendFactory](MicroEngine.Graphics.Devices.ReferenceGraphicsBackendFactory.md)

#### Implements

[IGraphicsBackendFactory](MicroEngine.Graphics.Devices.IGraphicsBackendFactory.md)

## Constructors

### <a id="MicroEngine_Graphics_Devices_ReferenceGraphicsBackendFactory__ctor"></a> ReferenceGraphicsBackendFactory\(\)

```csharp
public ReferenceGraphicsBackendFactory()
```

## Properties

### <a id="MicroEngine_Graphics_Devices_ReferenceGraphicsBackendFactory_Name"></a> Name

Backend name: "sdl-renderer", "raylib", "reference" (and, later, "sdl-gpu").

```csharp
public string Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Graphics_Devices_ReferenceGraphicsBackendFactory_Create_MicroEngine_Platform_ISurface_MicroEngine_Graphics_Devices_GraphicsDeviceOptions_"></a> Create\(ISurface?, GraphicsDeviceOptions\)

Creates a device for <code class="paramref">surface</code> (null = offscreen only, where the backend supports it).

```csharp
public IGraphicsDevice Create(ISurface? surface, GraphicsDeviceOptions options)
```

#### Parameters

`surface` [ISurface](MicroEngine.Platform.ISurface.md)?

`options` [GraphicsDeviceOptions](MicroEngine.Graphics.Devices.GraphicsDeviceOptions.md)

#### Returns

 [IGraphicsDevice](MicroEngine.Graphics.Devices.IGraphicsDevice.md)

### <a id="MicroEngine_Graphics_Devices_ReferenceGraphicsBackendFactory_UnsupportedReason_MicroEngine_Platform_ISurface_"></a> UnsupportedReason\(ISurface?\)

Always null: the reference device works with any surface and without one.

```csharp
public string? UnsupportedReason(ISurface? surface)
```

#### Parameters

`surface` [ISurface](MicroEngine.Platform.ISurface.md)?

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

</div>
