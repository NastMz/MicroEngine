# Class RunPreset

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

Defaults a composition states for the runs it hosts when neither the run ([RunOptions](MicroEngine.Core.RunOptions.md)) nor the game
([Configure](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Configure_MicroEngine_Kernel_EngineBuilder_)) chooses: a stated preset value, never a platform behaviour. A platform offers it as a
capability ([TryGetCapability``1](MicroEngine.Platform.IPlatformHost.md#MicroEngine_Platform_IPlatformHost_TryGetCapability__1___0__)); the headless platform's preset is
[Deterministic](MicroEngine.Kernel.TimeMode.md), so tests that run a game headless are reproducible with no option. The
platform behaves the same whatever the mode the run ends up with.

```csharp
public sealed record RunPreset : IEquatable<RunPreset>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[RunPreset](MicroEngine.Core.RunPreset.md)

#### Implements

[IEquatable<RunPreset\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Core_RunPreset__ctor"></a> RunPreset\(\)

```csharp
public RunPreset()
```

## Properties

### <a id="MicroEngine_Core_RunPreset_Testing"></a> Testing

The preset for testing compositions: one fixed step per frame.

```csharp
public static RunPreset Testing { get; }
```

#### Property Value

 [RunPreset](MicroEngine.Core.RunPreset.md)

### <a id="MicroEngine_Core_RunPreset_TimeMode"></a> TimeMode

The time mode of runs that choose none; null leaves the default, real time.

```csharp
public TimeMode? TimeMode { get; init; }
```

#### Property Value

 TimeMode?

## Methods

### <a id="MicroEngine_Core_RunPreset__Clone__"></a> <Clone\>$\(\)

```csharp
public RunPreset <Clone>$()
```

#### Returns

 [RunPreset](MicroEngine.Core.RunPreset.md)

### <a id="MicroEngine_Core_RunPreset_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_RunPreset_Equals_MicroEngine_Core_RunPreset_"></a> Equals\(RunPreset?\)

```csharp
public bool Equals(RunPreset? other)
```

#### Parameters

`other` [RunPreset](MicroEngine.Core.RunPreset.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_RunPreset_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_RunPreset_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Core_RunPreset_op_Equality_MicroEngine_Core_RunPreset_MicroEngine_Core_RunPreset_"></a> operator ==\(RunPreset?, RunPreset?\)

```csharp
public static bool operator ==(RunPreset? left, RunPreset? right)
```

#### Parameters

`left` [RunPreset](MicroEngine.Core.RunPreset.md)?

`right` [RunPreset](MicroEngine.Core.RunPreset.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_RunPreset_op_Inequality_MicroEngine_Core_RunPreset_MicroEngine_Core_RunPreset_"></a> operator \!=\(RunPreset?, RunPreset?\)

```csharp
public static bool operator !=(RunPreset? left, RunPreset? right)
```

#### Parameters

`left` [RunPreset](MicroEngine.Core.RunPreset.md)?

`right` [RunPreset](MicroEngine.Core.RunPreset.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
