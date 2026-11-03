# Class RaylibDeviceFactory

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Raylib](MicroEngine.Raylib.md)  
Assembly: MicroEngine.Raylib.dll  
Guide: [Backends and platforms](../manual/backends.md)  

Factory of the tier-0 raylib device ("raylib"). raylib owns one window and one OpenGL context per process, so the
device accepts only raylib's own window: the surface whose native window is raylib's, once raylib has opened it.

```csharp
public sealed class RaylibDeviceFactory : IGraphicsBackendFactory
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[RaylibDeviceFactory](MicroEngine.Raylib.RaylibDeviceFactory.md)

#### Implements

IGraphicsBackendFactory

## Constructors

### <a id="MicroEngine_Raylib_RaylibDeviceFactory__ctor"></a> RaylibDeviceFactory\(\)

```csharp
public RaylibDeviceFactory()
```

## Properties

### <a id="MicroEngine_Raylib_RaylibDeviceFactory_Name"></a> Name

Backend name: "sdl-renderer", "raylib", "reference" (and, later, "sdl-gpu").

```csharp
public string Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Raylib_RaylibDeviceFactory_Create_MicroEngine_Platform_ISurface_MicroEngine_Graphics_Devices_GraphicsDeviceOptions_"></a> Create\(ISurface?, GraphicsDeviceOptions\)

Creates a device for <code class="paramref">surface</code> (null = offscreen only, where the backend supports it).

```csharp
public IGraphicsDevice Create(ISurface? surface, GraphicsDeviceOptions options)
```

#### Parameters

`surface` ISurface?

`options` GraphicsDeviceOptions

#### Returns

 IGraphicsDevice

### <a id="MicroEngine_Raylib_RaylibDeviceFactory_UnsupportedReason_MicroEngine_Platform_ISurface_"></a> UnsupportedReason\(ISurface?\)

Null when [Create](MicroEngine.Graphics.Devices.IGraphicsBackendFactory.md#MicroEngine_Graphics_Devices_IGraphicsBackendFactory_Create_MicroEngine_Platform_ISurface_MicroEngine_Graphics_Devices_GraphicsDeviceOptions_) can succeed for <code class="paramref">surface</code>, otherwise why not.

```csharp
public string? UnsupportedReason(ISurface? surface)
```

#### Parameters

`surface` ISurface?

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

</div>
