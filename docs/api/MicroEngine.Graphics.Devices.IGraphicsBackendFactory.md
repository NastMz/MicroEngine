# Interface IGraphicsBackendFactory

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics.Devices](MicroEngine.Graphics.Devices.md)  
Assembly: MicroEngine.Graphics.dll  
Guide: [The graphics device](../manual/rendering.md#the-graphics-device)  

Creates devices of one backend. A backend that accepts only its own surfaces says so through [UnsupportedReason](MicroEngine.Graphics.Devices.IGraphicsBackendFactory.md#MicroEngine_Graphics_Devices_IGraphicsBackendFactory_UnsupportedReason_MicroEngine_Platform_ISurface_).

```csharp
public interface IGraphicsBackendFactory
```

## Properties

### <a id="MicroEngine_Graphics_Devices_IGraphicsBackendFactory_Name"></a> Name

Backend name: "sdl-renderer", "raylib", "reference" (and, later, "sdl-gpu").

```csharp
string Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Graphics_Devices_IGraphicsBackendFactory_Create_MicroEngine_Platform_ISurface_MicroEngine_Graphics_Devices_GraphicsDeviceOptions_"></a> Create\(ISurface?, GraphicsDeviceOptions\)

Creates a device for <code class="paramref">surface</code> (null = offscreen only, where the backend supports it).

```csharp
IGraphicsDevice Create(ISurface? surface, GraphicsDeviceOptions options)
```

#### Parameters

`surface` [ISurface](MicroEngine.Platform.ISurface.md)?

`options` [GraphicsDeviceOptions](MicroEngine.Graphics.Devices.GraphicsDeviceOptions.md)

#### Returns

 [IGraphicsDevice](MicroEngine.Graphics.Devices.IGraphicsDevice.md)

### <a id="MicroEngine_Graphics_Devices_IGraphicsBackendFactory_UnsupportedReason_MicroEngine_Platform_ISurface_"></a> UnsupportedReason\(ISurface?\)

Null when [Create](MicroEngine.Graphics.Devices.IGraphicsBackendFactory.md#MicroEngine_Graphics_Devices_IGraphicsBackendFactory_Create_MicroEngine_Platform_ISurface_MicroEngine_Graphics_Devices_GraphicsDeviceOptions_) can succeed for <code class="paramref">surface</code>, otherwise why not.

```csharp
string? UnsupportedReason(ISurface? surface)
```

#### Parameters

`surface` [ISurface](MicroEngine.Platform.ISurface.md)?

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

</div>
