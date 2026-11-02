# Class GraphicsSettings

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

The graphics options a game starts with ([Graphics](MicroEngine.Core.GameSettings.md#MicroEngine_Core_GameSettings_Graphics)). Every option has a sensible automatic
default; the launcher's options (<code>--platform</code>, <code>--renderer</code>, <code>--vsync</code>, <code>--fps-limit</code>, <code>--gpu</code>,
<code>--window</code>, <code>--display</code>) override them for one run, and [Screen](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Screen) changes the ones a platform
can change while the game runs. A game that offers a graphics menu saves [Current](MicroEngine.Core.GameScreen.md#MicroEngine_Core_GameScreen_Current) and returns
it from [Settings](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Settings) at its next start. VSync, the frame limit and [UpdateRate](MicroEngine.Core.GameSettings.md#MicroEngine_Core_GameSettings_UpdateRate)
are independent: VSync waits for the display, the limit caps frames per second, the update rate is the simulation's.

```csharp
public sealed record GraphicsSettings : IEquatable<GraphicsSettings>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GraphicsSettings](MicroEngine.Core.GraphicsSettings.md)

#### Implements

[IEquatable<GraphicsSettings\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Core_GraphicsSettings__ctor"></a> GraphicsSettings\(\)

```csharp
public GraphicsSettings()
```

## Properties

### <a id="MicroEngine_Core_GraphicsSettings_Backend"></a> Backend

The platform to run on first ("sdl", "raylib", "sdl-software"...); null tries the catalog in order. A platform that
cannot start here falls back to the next one, saying why; a platform named on the command line does not.
Read at start only.

```csharp
public string? Backend { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Core_GraphicsSettings_Display"></a> Display

The display to open on (an index of [Displays](MicroEngine.Core.GameScreen.md#MicroEngine_Core_GameScreen_Displays)); null lets the system choose. An index the system does not have is ignored.

```csharp
public int? Display { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="MicroEngine_Core_GraphicsSettings_FrameRateLimit"></a> FrameRateLimit

The most frames per second to draw; 0 (the default) sets no limit. Independent of VSync: with VSync off it paces
frames on its own; with VSync on, a limit below the display's rate skips refreshes (choose a divisor of the
display's rate, or draw with [FrameBlend](MicroEngine.Core.Game.md#MicroEngine_Core_Game_FrameBlend)). Not applied to fixed-step (scripted) runs.

```csharp
public int FrameRateLimit { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_GraphicsSettings_Gpu"></a> Gpu

Which GPU to ask for on a machine with two ([GpuPreference](MicroEngine.Core.GpuPreference.md)). Read at start only.

```csharp
public GpuPreference Gpu { get; init; }
```

#### Property Value

 [GpuPreference](MicroEngine.Core.GpuPreference.md)

### <a id="MicroEngine_Core_GraphicsSettings_Renderers"></a> Renderers

Graphics APIs to try in order on platforms that offer several (SDL: "direct3d11", "direct3d12", "vulkan",
"opengl", "metal", "gpu"...); after them, the platform chooses as when the list is empty (the default). Names the
system lacks are skipped. Read at start only.

```csharp
public IReadOnlyList<string> Renderers { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Core_GraphicsSettings_VSync"></a> VSync

Whether frames wait for the display's refresh (true by default): no tearing, and frames paced to the display.

```csharp
public bool VSync { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_GraphicsSettings_WindowMode"></a> WindowMode

Windowed (the default), borderless full screen, or exclusive full screen. Phones and browsers decide this themselves.

```csharp
public WindowMode WindowMode { get; init; }
```

#### Property Value

 [WindowMode](MicroEngine.Core.WindowMode.md)

## Methods

### <a id="MicroEngine_Core_GraphicsSettings__Clone__"></a> <Clone\>$\(\)

```csharp
public GraphicsSettings <Clone>$()
```

#### Returns

 [GraphicsSettings](MicroEngine.Core.GraphicsSettings.md)

### <a id="MicroEngine_Core_GraphicsSettings_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_GraphicsSettings_Equals_MicroEngine_Core_GraphicsSettings_"></a> Equals\(GraphicsSettings?\)

```csharp
public bool Equals(GraphicsSettings? other)
```

#### Parameters

`other` [GraphicsSettings](MicroEngine.Core.GraphicsSettings.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_GraphicsSettings_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_GraphicsSettings_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Core_GraphicsSettings_op_Equality_MicroEngine_Core_GraphicsSettings_MicroEngine_Core_GraphicsSettings_"></a> operator ==\(GraphicsSettings?, GraphicsSettings?\)

```csharp
public static bool operator ==(GraphicsSettings? left, GraphicsSettings? right)
```

#### Parameters

`left` [GraphicsSettings](MicroEngine.Core.GraphicsSettings.md)?

`right` [GraphicsSettings](MicroEngine.Core.GraphicsSettings.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_GraphicsSettings_op_Inequality_MicroEngine_Core_GraphicsSettings_MicroEngine_Core_GraphicsSettings_"></a> operator \!=\(GraphicsSettings?, GraphicsSettings?\)

```csharp
public static bool operator !=(GraphicsSettings? left, GraphicsSettings? right)
```

#### Parameters

`left` [GraphicsSettings](MicroEngine.Core.GraphicsSettings.md)?

`right` [GraphicsSettings](MicroEngine.Core.GraphicsSettings.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
