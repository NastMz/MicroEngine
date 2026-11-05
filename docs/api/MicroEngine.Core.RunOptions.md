# Class RunOptions

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

Options shared by every launcher: scripted input, captures and run limits.

```csharp
public sealed record RunOptions : IEquatable<RunOptions>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[RunOptions](MicroEngine.Core.RunOptions.md)

#### Implements

[IEquatable<RunOptions\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Core_RunOptions__ctor"></a> RunOptions\(\)

```csharp
public RunOptions()
```

## Properties

### <a id="MicroEngine_Core_RunOptions_CaptureDirectory"></a> CaptureDirectory

Directory where script "capture" commands write PNGs.

```csharp
public string CaptureDirectory { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Core_RunOptions_FirstFrame"></a> FirstFrame

Called once, right after the first frame is presented (the start screen, or the game's first frame), from the
loop's thread. Phone hosts use it to keep the system's launch screen up until the game has something to show.

```csharp
public Action? FirstFrame { get; init; }
```

#### Property Value

 [Action](https://learn.microsoft.com/dotnet/api/system.action)?

### <a id="MicroEngine_Core_RunOptions_Graphics"></a> Graphics

The graphics options the run started with, after the launcher's overrides ([Screen](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Screen) starts from
them); null uses [Graphics](MicroEngine.Core.GameSettings.md#MicroEngine_Core_GameSettings_Graphics).

```csharp
public GraphicsSettings? Graphics { get; init; }
```

#### Property Value

 [GraphicsSettings](MicroEngine.Core.GraphicsSettings.md)?

### <a id="MicroEngine_Core_RunOptions_MaxUpdates"></a> MaxUpdates

Stops after this many updates (0 = unlimited).

```csharp
public long MaxUpdates { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Core_RunOptions_Mute"></a> Mute

Sets the sound effect and music volumes to 0 after <code>Load</code>.

```csharp
public bool Mute { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_RunOptions_PaceToDisplay"></a> PaceToDisplay

Whether real-time runs count elapsed time in refresh periods of the display that paces them
([IDisplayPacing](MicroEngine.Platform.IDisplayPacing.md)), which removes the stutter of an irregular wake-up (true by default). False
takes every frame's time as measured (the launcher's <code>--pacing measured</code>), for comparisons.

```csharp
public bool PaceToDisplay { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_RunOptions_Profiler"></a> Profiler

Measures every frame and the phases of the start (the launcher's <code>--profile</code>), or null to measure nothing.

```csharp
public FrameProfiler? Profiler { get; init; }
```

#### Property Value

 [FrameProfiler](MicroEngine.Core.FrameProfiler.md)?

### <a id="MicroEngine_Core_RunOptions_Script"></a> Script

Scripted input to play, or null for none.

```csharp
public InputScript? Script { get; init; }
```

#### Property Value

 [InputScript](MicroEngine.Core.InputScript.md)?

### <a id="MicroEngine_Core_RunOptions_Seed"></a> Seed

Seed for [Random](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Random) (1 by default).

```csharp
public int Seed { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_RunOptions_StartScreen"></a> StartScreen

Whether the game's start screen ([StartScreen](MicroEngine.Core.StartScreen.md)) is drawn while it loads, for runs whose time mode
([TimeMode](MicroEngine.Core.RunOptions.md#MicroEngine_Core_RunOptions_TimeMode)) is not deterministic and games whose identity keeps it on (true by default).

```csharp
public bool StartScreen { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_RunOptions_StateTrace"></a> StateTrace

Receives the simulation state hash after every fixed step ([ComputeStateHash](MicroEngine.Core.Game.md#MicroEngine_Core_Game_ComputeStateHash_System_Boolean_)), with each
section's hash when [Sections](MicroEngine.Kernel.StateTrace.md#MicroEngine_Kernel_StateTrace_Sections) is true; null (the default) hashes nothing. Two
deterministic runs of the same build, script and seed record the same trace;
[FirstDivergence](MicroEngine.Kernel.StateTrace.md#MicroEngine_Kernel_StateTrace_FirstDivergence_MicroEngine_Kernel_StateTrace_MicroEngine_Kernel_StateTrace_) names the first update and
section where two differ.

```csharp
public StateTrace? StateTrace { get; init; }
```

#### Property Value

 [StateTrace](MicroEngine.Kernel.StateTrace.md)?

### <a id="MicroEngine_Core_RunOptions_TimeMode"></a> TimeMode

How the loop advances time (the launcher's <code>--time</code>): [RealTime](MicroEngine.Kernel.TimeMode.md) with the wall
clock, or [Deterministic](MicroEngine.Kernel.TimeMode.md), one fixed step per frame. It wins over the game's
[Configure](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Configure_MicroEngine_Kernel_EngineBuilder_); null (the default) keeps the game's choice, else the composition's
[RunPreset](MicroEngine.Core.RunPreset.md) (the headless platform's: deterministic), else real time. Nothing a platform does depends on
the mode: it belongs to the loop only.

```csharp
public TimeMode? TimeMode { get; init; }
```

#### Property Value

 [TimeMode](MicroEngine.Kernel.TimeMode.md)?

## Methods

### <a id="MicroEngine_Core_RunOptions__Clone__"></a> <Clone\>$\(\)

```csharp
public RunOptions <Clone>$()
```

#### Returns

 [RunOptions](MicroEngine.Core.RunOptions.md)

### <a id="MicroEngine_Core_RunOptions_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_RunOptions_Equals_MicroEngine_Core_RunOptions_"></a> Equals\(RunOptions?\)

```csharp
public bool Equals(RunOptions? other)
```

#### Parameters

`other` [RunOptions](MicroEngine.Core.RunOptions.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_RunOptions_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_RunOptions_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Core_RunOptions_op_Equality_MicroEngine_Core_RunOptions_MicroEngine_Core_RunOptions_"></a> operator ==\(RunOptions?, RunOptions?\)

```csharp
public static bool operator ==(RunOptions? left, RunOptions? right)
```

#### Parameters

`left` [RunOptions](MicroEngine.Core.RunOptions.md)?

`right` [RunOptions](MicroEngine.Core.RunOptions.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_RunOptions_op_Inequality_MicroEngine_Core_RunOptions_MicroEngine_Core_RunOptions_"></a> operator \!=\(RunOptions?, RunOptions?\)

```csharp
public static bool operator !=(RunOptions? left, RunOptions? right)
```

#### Parameters

`left` [RunOptions](MicroEngine.Core.RunOptions.md)?

`right` [RunOptions](MicroEngine.Core.RunOptions.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
