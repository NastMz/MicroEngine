# Class DesktopOptions

Namespace: [MicroEngine.Raylib](MicroEngine.Raylib.md)  
Assembly: MicroEngine.Raylib.dll  
Guide: [Backends and platforms](../manual/backends.md)  

Desktop window options decided by the launcher.

```csharp
public sealed record DesktopOptions : IEquatable<DesktopOptions>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[DesktopOptions](MicroEngine.Raylib.DesktopOptions.md)

#### Implements

[IEquatable<DesktopOptions\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Raylib_DesktopOptions__ctor_System_Nullable_System_Int32__System_Boolean_System_Boolean_System_Boolean_System_Int32_"></a> DesktopOptions\(int?, bool, bool, bool, int\)

Desktop window options decided by the launcher.

```csharp
public DesktopOptions(int? Scale = null, bool Hidden = false, bool FixedStep = false, bool Mute = false, int CaptureScale = 3)
```

#### Parameters

`Scale` [int](https://learn.microsoft.com/dotnet/api/system.int32)?

`Hidden` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`FixedStep` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`Mute` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`CaptureScale` [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Properties

### <a id="MicroEngine_Raylib_DesktopOptions_CaptureScale"></a> CaptureScale

```csharp
public int CaptureScale { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Raylib_DesktopOptions_FixedStep"></a> FixedStep

```csharp
public bool FixedStep { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Raylib_DesktopOptions_Hidden"></a> Hidden

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

 [PixelImage](https://github.com/NastMz/MicroEngine/blob/4d9999002843ba740441ef7bb60461c13b2a816e/src/MicroEngine/Graphics/PixelImage.cs)?

### <a id="MicroEngine_Raylib_DesktopOptions_Mute"></a> Mute

```csharp
public bool Mute { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Raylib_DesktopOptions_Scale"></a> Scale

```csharp
public int? Scale { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

## Methods

### <a id="MicroEngine_Raylib_DesktopOptions__Clone__"></a> <Clone\>$\(\)

```csharp
public DesktopOptions <Clone>$()
```

#### Returns

 [DesktopOptions](MicroEngine.Raylib.DesktopOptions.md)

### <a id="MicroEngine_Raylib_DesktopOptions_Deconstruct_System_Nullable_System_Int32___System_Boolean__System_Boolean__System_Boolean__System_Int32__"></a> Deconstruct\(out int?, out bool, out bool, out bool, out int\)

```csharp
public void Deconstruct(out int? Scale, out bool Hidden, out bool FixedStep, out bool Mute, out int CaptureScale)
```

#### Parameters

`Scale` [int](https://learn.microsoft.com/dotnet/api/system.int32)?

`Hidden` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`FixedStep` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`Mute` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`CaptureScale` [int](https://learn.microsoft.com/dotnet/api/system.int32)

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
