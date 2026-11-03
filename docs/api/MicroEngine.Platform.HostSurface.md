# Class HostSurface

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Platform](MicroEngine.Platform.md)  
Assembly: MicroEngine.Platform.dll  
Guide: [The platform host](../manual/backends.md#the-platform-host)  

An [ISurface](MicroEngine.Platform.ISurface.md) for hosts: it reads its size, density and state from the host's window when asked, and
[Pump](MicroEngine.Platform.HostSurface.md#MicroEngine_Platform_HostSurface_Pump_MicroEngine_Platform_IEngineEventSink_) reports a size change the way the platform contract requires ([Resized](MicroEngine.Platform.HostSurface.md#MicroEngine_Platform_HostSurface_Resized) and the
sink's [Resized](MicroEngine.Platform.IEngineEventSink.md#MicroEngine_Platform_IEngineEventSink_Resized_MicroEngine_Kernel_SurfaceResized_), once per change).

```csharp
public sealed class HostSurface : ISurface
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[HostSurface](MicroEngine.Platform.HostSurface.md)

#### Implements

[ISurface](MicroEngine.Platform.ISurface.md)

## Constructors

### <a id="MicroEngine_Platform_HostSurface__ctor_System_IntPtr_System_Func_System_ValueTuple_System_Int32_System_Int32___System_Func_System_Single__System_Func_System_Boolean__"></a> HostSurface\(nint, Func<\(int Width, int Height\)\>, Func<float\>, Func<bool\>\)

Creates a surface over a host's window.

```csharp
public HostSurface(nint nativeWindow, Func<(int Width, int Height)> pixelSize, Func<float> density, Func<bool> minimized)
```

#### Parameters

`nativeWindow` [nint](https://learn.microsoft.com/dotnet/api/system.intptr)

The opaque native window, or 0 when there is none.

`pixelSize` [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<\([int](https://learn.microsoft.com/dotnet/api/system.int32) [Width](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.width), [int](https://learn.microsoft.com/dotnet/api/system.int32) [Height](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.height)\)\>

Reads the drawable size in pixels, as it is now.

`density` [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>

Reads the pixels per logical unit, as it is now.

`minimized` [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<[bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>

Reads whether the window is minimised; a zero size counts as minimised anyway.

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

A reader is null.

## Properties

### <a id="MicroEngine_Platform_HostSurface_IsMinimized"></a> IsMinimized

True while the surface has nothing to present to (minimised, zero size).

```csharp
public bool IsMinimized { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Platform_HostSurface_NativeWindow"></a> NativeWindow

Opaque native window (an <code>SDL_Window*</code> for SDL hosts, raylib's window handle for raylib); 0 when there is none (headless). Only backends read it.

```csharp
public nint NativeWindow { get; }
```

#### Property Value

 [nint](https://learn.microsoft.com/dotnet/api/system.intptr)

### <a id="MicroEngine_Platform_HostSurface_PixelDensity"></a> PixelDensity

Pixels per logical unit.

```csharp
public float PixelDensity { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Platform_HostSurface_PixelSize"></a> PixelSize

Drawable size in pixels.

```csharp
public (int Width, int Height) PixelSize { get; }
```

#### Property Value

 \([int](https://learn.microsoft.com/dotnet/api/system.int32) [Width](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.width), [int](https://learn.microsoft.com/dotnet/api/system.int32) [Height](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.height)\)

## Methods

### <a id="MicroEngine_Platform_HostSurface_Pump_MicroEngine_Platform_IEngineEventSink_"></a> Pump\(IEngineEventSink\)

Compares the size with the one last reported; when it changed, raises [Resized](MicroEngine.Platform.HostSurface.md#MicroEngine_Platform_HostSurface_Resized) and delivers
[Resized](MicroEngine.Platform.IEngineEventSink.md#MicroEngine_Platform_IEngineEventSink_Resized_MicroEngine_Kernel_SurfaceResized_) to <code class="paramref">sink</code>. Hosts call it from their event pump.

```csharp
public bool Pump(IEngineEventSink sink)
```

#### Parameters

`sink` [IEngineEventSink](MicroEngine.Platform.IEngineEventSink.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether the size changed.

### <a id="MicroEngine_Platform_HostSurface_RaiseLost"></a> RaiseLost\(\)

Raises [Lost](MicroEngine.Platform.HostSurface.md#MicroEngine_Platform_HostSurface_Lost): the system destroyed the surface.

```csharp
public void RaiseLost()
```

### <a id="MicroEngine_Platform_HostSurface_Lost"></a> Lost

Raised when a mobile surface is destroyed.

```csharp
public event Action? Lost
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action)?

### <a id="MicroEngine_Platform_HostSurface_Resized"></a> Resized

Raised when [PixelSize](MicroEngine.Platform.ISurface.md#MicroEngine_Platform_ISurface_PixelSize) changes, during the host's event pump.

```csharp
public event Action? Resized
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action)?

</div>
