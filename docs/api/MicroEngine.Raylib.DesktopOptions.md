# Class DesktopOptions

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Raylib](MicroEngine.Raylib.md)  
Assembly: MicroEngine.Raylib.dll  
Guide: [Backends and platforms](../manual/backends.md)  

Window options of a raylib platform created directly (browser hosts); hosts with a [PlatformRequest](MicroEngine.Core.PlatformRequest.md) use
[#ctor](MicroEngine.Raylib.RaylibPlatform.md). Every option is set by name
(<code>new DesktopOptions { Hidden = true, Mute = true }</code>): there is no positional constructor, so a call written for
the earlier positional shape (whose third argument was a fixed-step flag) fails to compile instead of meaning
something else.

```csharp
public sealed record DesktopOptions : IEquatable<DesktopOptions>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[DesktopOptions](MicroEngine.Raylib.DesktopOptions.md)

#### Implements

[IEquatable<DesktopOptions\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Raylib_DesktopOptions__ctor"></a> DesktopOptions\(\)

```csharp
public DesktopOptions()
```

## Properties

### <a id="MicroEngine_Raylib_DesktopOptions_CaptureScale"></a> CaptureScale

Size factor of screenshots (3 by default).

```csharp
public int CaptureScale { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Raylib_DesktopOptions_Hidden"></a> Hidden

Renders into a hidden window, which never waits for the display (false by default).

```csharp
public bool Hidden { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Raylib_DesktopOptions_Icon"></a> Icon

The window's icon (the game's [Icon](MicroEngine.Core.AppIdentity.md#MicroEngine_Core_AppIdentity_Icon)); ignored in browsers.

```csharp
public PixelImage? Icon { get; init; }
```

#### Property Value

 [PixelImage](MicroEngine.Graphics.PixelImage.md)?

### <a id="MicroEngine_Raylib_DesktopOptions_Mute"></a> Mute

Opens no audio device (false by default).

```csharp
public bool Mute { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Raylib_DesktopOptions_Scale"></a> Scale

Window scale; null (the default) uses [Scale](MicroEngine.Core.GameSettings.md#MicroEngine_Core_GameSettings_Scale).

```csharp
public int? Scale { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="MicroEngine_Raylib_DesktopOptions_WaitForDisplay"></a> WaitForDisplay

Whether a visible desktop window's frames may wait for the display (true by default; see [WaitForDisplay](MicroEngine.Core.PlatformRequest.md#MicroEngine_Core_PlatformRequest_WaitForDisplay)).

```csharp
public bool WaitForDisplay { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_Raylib_DesktopOptions__Clone__"></a> <Clone\>$\(\)

```csharp
public DesktopOptions <Clone>$()
```

#### Returns

 [DesktopOptions](MicroEngine.Raylib.DesktopOptions.md)

### <a id="MicroEngine_Raylib_DesktopOptions_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Raylib_DesktopOptions_Equals_MicroEngine_Raylib_DesktopOptions_"></a> Equals\(DesktopOptions?\)

```csharp
public bool Equals(DesktopOptions? other)
```

#### Parameters

`other` [DesktopOptions](MicroEngine.Raylib.DesktopOptions.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Raylib_DesktopOptions_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Raylib_DesktopOptions_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Raylib_DesktopOptions_op_Equality_MicroEngine_Raylib_DesktopOptions_MicroEngine_Raylib_DesktopOptions_"></a> operator ==\(DesktopOptions?, DesktopOptions?\)

```csharp
public static bool operator ==(DesktopOptions? left, DesktopOptions? right)
```

#### Parameters

`left` [DesktopOptions](MicroEngine.Raylib.DesktopOptions.md)?

`right` [DesktopOptions](MicroEngine.Raylib.DesktopOptions.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Raylib_DesktopOptions_op_Inequality_MicroEngine_Raylib_DesktopOptions_MicroEngine_Raylib_DesktopOptions_"></a> operator \!=\(DesktopOptions?, DesktopOptions?\)

```csharp
public static bool operator !=(DesktopOptions? left, DesktopOptions? right)
```

#### Parameters

`left` [DesktopOptions](MicroEngine.Raylib.DesktopOptions.md)?

`right` [DesktopOptions](MicroEngine.Raylib.DesktopOptions.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
