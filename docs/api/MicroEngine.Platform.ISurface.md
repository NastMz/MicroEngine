# Interface ISurface

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Platform](MicroEngine.Platform.md)  
Assembly: MicroEngine.Platform.dll  
Guide: [The platform host](../manual/backends.md#the-platform-host)  

A surface a host produces and a graphics device presents to: a window, a browser canvas, a phone's screen. This is
the graphics contract's surface (version 1); only backends read [NativeWindow](MicroEngine.Platform.ISurface.md#MicroEngine_Platform_ISurface_NativeWindow).

```csharp
public interface ISurface
```

## Properties

### <a id="MicroEngine_Platform_ISurface_IsMinimized"></a> IsMinimized

True while the surface has nothing to present to (minimised, zero size).

```csharp
bool IsMinimized { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Platform_ISurface_NativeWindow"></a> NativeWindow

Opaque native window (an <code>SDL_Window*</code> for SDL hosts, raylib's window handle for raylib); 0 when there is none (headless). Only backends read it.

```csharp
nint NativeWindow { get; }
```

#### Property Value

 [nint](https://learn.microsoft.com/dotnet/api/system.intptr)

### <a id="MicroEngine_Platform_ISurface_PixelDensity"></a> PixelDensity

Pixels per logical unit.

```csharp
float PixelDensity { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Platform_ISurface_PixelSize"></a> PixelSize

Drawable size in pixels.

```csharp
(int Width, int Height) PixelSize { get; }
```

#### Property Value

 \([int](https://learn.microsoft.com/dotnet/api/system.int32) [Width](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.width), [int](https://learn.microsoft.com/dotnet/api/system.int32) [Height](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.height)\)

### <a id="MicroEngine_Platform_ISurface_Lost"></a> Lost

Raised when a mobile surface is destroyed.

```csharp
event Action? Lost
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action)?

### <a id="MicroEngine_Platform_ISurface_Resized"></a> Resized

Raised when [PixelSize](MicroEngine.Platform.ISurface.md#MicroEngine_Platform_ISurface_PixelSize) changes, during the host's event pump.

```csharp
event Action? Resized
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action)?

</div>
