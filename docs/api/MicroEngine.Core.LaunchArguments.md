# Class LaunchArguments

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

The parsed launcher command line (see [GameLauncher](MicroEngine.Core.GameLauncher.md)).

```csharp
public sealed record LaunchArguments : IEquatable<LaunchArguments>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[LaunchArguments](MicroEngine.Core.LaunchArguments.md)

#### Implements

[IEquatable<LaunchArguments\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Core_LaunchArguments__ctor_System_String_System_String_System_Boolean_System_Boolean_System_Int64_System_Int32_System_Nullable_System_Int32__System_Boolean_"></a> LaunchArguments\(string?, string, bool, bool, long, int, int?, bool\)

The parsed launcher command line (see [GameLauncher](MicroEngine.Core.GameLauncher.md)).

```csharp
public LaunchArguments(string? ScriptPath, string CaptureDirectory, bool Headless, bool Hidden, long Frames, int Seed, int? Scale, bool Mute)
```

#### Parameters

`ScriptPath` [string](https://learn.microsoft.com/dotnet/api/system.string)?

<code>--script</code>: the input script file, or null.

`CaptureDirectory` [string](https://learn.microsoft.com/dotnet/api/system.string)

<code>--captures</code>: where script captures are written ("captures" by default).

`Headless` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

<code>--headless</code>: run without a window.

`Hidden` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

<code>--hidden</code>: render into a hidden window.

`Frames` [long](https://learn.microsoft.com/dotnet/api/system.int64)

<code>--frames</code>: stop after this many updates; 0 runs until the game quits.

`Seed` [int](https://learn.microsoft.com/dotnet/api/system.int32)

<code>--seed</code>: the seed of [Random](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Random). Without it, a run with a script uses 1 and any other run a clock-based seed.

`Scale` [int](https://learn.microsoft.com/dotnet/api/system.int32)?

<code>--scale</code>: the window scale, or null for [Scale](MicroEngine.Core.GameSettings.md#MicroEngine_Core_GameSettings_Scale).

`Mute` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

<code>--mute</code>: no sound.

## Properties

### <a id="MicroEngine_Core_LaunchArguments_Audio"></a> Audio

<code>--audio</code>: where sound may go.

```csharp
public AudioOutput Audio { get; init; }
```

#### Property Value

 [AudioOutput](MicroEngine.Core.AudioOutput.md)

### <a id="MicroEngine_Core_LaunchArguments_AudioCapturePath"></a> AudioCapturePath

<code>--audio-capture</code>: the WAV file to write what would have been heard to, or null.

```csharp
public string? AudioCapturePath { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Core_LaunchArguments_CaptureDirectory"></a> CaptureDirectory

<code>--captures</code>: where script captures are written ("captures" by default).

```csharp
public string CaptureDirectory { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Core_LaunchArguments_Display"></a> Display

<code>--display</code>: the display to open on, or null to keep the game's choice.

```csharp
public int? Display { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="MicroEngine_Core_LaunchArguments_FrameRateLimit"></a> FrameRateLimit

<code>--fps-limit</code>: the frame limit (0 for none), or null to keep the game's choice.

```csharp
public int? FrameRateLimit { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="MicroEngine_Core_LaunchArguments_Frames"></a> Frames

<code>--frames</code>: stop after this many updates; 0 runs until the game quits.

```csharp
public long Frames { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Core_LaunchArguments_Gpu"></a> Gpu

<code>--gpu</code>: the GPU to ask for, or null to keep the game's choice.

```csharp
public GpuPreference? Gpu { get; init; }
```

#### Property Value

 [GpuPreference](MicroEngine.Core.GpuPreference.md)?

### <a id="MicroEngine_Core_LaunchArguments_Headless"></a> Headless

<code>--headless</code>: run without a window.

```csharp
public bool Headless { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_LaunchArguments_Hidden"></a> Hidden

<code>--hidden</code>: render into a hidden window.

```csharp
public bool Hidden { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_LaunchArguments_ListPlatforms"></a> ListPlatforms

<code>--list-platforms</code>: print the catalog and exit.

```csharp
public bool ListPlatforms { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_LaunchArguments_Mute"></a> Mute

<code>--mute</code>: no sound.

```csharp
public bool Mute { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_LaunchArguments_PaceToDisplay"></a> PaceToDisplay

<code>--pacing</code>: false for <code>measured</code>, true (the default) for <code>display</code>.

```csharp
public bool PaceToDisplay { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_LaunchArguments_Platform"></a> Platform

The platform named with <code>--platform</code>, or null for the default.

```csharp
public string? Platform { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Core_LaunchArguments_ProfilePath"></a> ProfilePath

<code>--profile</code>: the file to write frame measurements to, or null to measure nothing.

```csharp
public string? ProfilePath { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Core_LaunchArguments_Renderers"></a> Renderers

<code>--renderer</code>: graphics APIs to try first, or null to keep the game's choice.

```csharp
public IReadOnlyList<string>? Renderers { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>?

### <a id="MicroEngine_Core_LaunchArguments_Report"></a> Report

<code>--report</code>: print the platform and its audio state before running.

```csharp
public bool Report { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_LaunchArguments_Scale"></a> Scale

<code>--scale</code>: the window scale, or null for [Scale](MicroEngine.Core.GameSettings.md#MicroEngine_Core_GameSettings_Scale).

```csharp
public int? Scale { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="MicroEngine_Core_LaunchArguments_ScriptPath"></a> ScriptPath

<code>--script</code>: the input script file, or null.

```csharp
public string? ScriptPath { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Core_LaunchArguments_Seed"></a> Seed

<code>--seed</code>: the seed of [Random](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Random). Without it, a run with a script uses 1 and any other run a clock-based seed.

```csharp
public int Seed { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_LaunchArguments_VSync"></a> VSync

<code>--vsync</code>: on or off, or null to keep the game's choice.

```csharp
public bool? VSync { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)?

### <a id="MicroEngine_Core_LaunchArguments_WindowMode"></a> WindowMode

<code>--window</code>: the window mode, or null to keep the game's choice.

```csharp
public WindowMode? WindowMode { get; init; }
```

#### Property Value

 [WindowMode](MicroEngine.Core.WindowMode.md)?

## Methods

### <a id="MicroEngine_Core_LaunchArguments__Clone__"></a> <Clone\>$\(\)

```csharp
public LaunchArguments <Clone>$()
```

#### Returns

 [LaunchArguments](MicroEngine.Core.LaunchArguments.md)

### <a id="MicroEngine_Core_LaunchArguments_ApplyTo_MicroEngine_Core_GraphicsSettings_"></a> ApplyTo\(GraphicsSettings\)

The game's graphics options with the ones given on the command line in their place.

```csharp
public GraphicsSettings ApplyTo(GraphicsSettings graphics)
```

#### Parameters

`graphics` [GraphicsSettings](MicroEngine.Core.GraphicsSettings.md)

#### Returns

 [GraphicsSettings](MicroEngine.Core.GraphicsSettings.md)

### <a id="MicroEngine_Core_LaunchArguments_Deconstruct_System_String__System_String__System_Boolean__System_Boolean__System_Int64__System_Int32__System_Nullable_System_Int32___System_Boolean__"></a> Deconstruct\(out string?, out string, out bool, out bool, out long, out int, out int?, out bool\)

```csharp
public void Deconstruct(out string? ScriptPath, out string CaptureDirectory, out bool Headless, out bool Hidden, out long Frames, out int Seed, out int? Scale, out bool Mute)
```

#### Parameters

`ScriptPath` [string](https://learn.microsoft.com/dotnet/api/system.string)?

`CaptureDirectory` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Headless` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`Hidden` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`Frames` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`Seed` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Scale` [int](https://learn.microsoft.com/dotnet/api/system.int32)?

`Mute` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_LaunchArguments_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_LaunchArguments_Equals_MicroEngine_Core_LaunchArguments_"></a> Equals\(LaunchArguments?\)

```csharp
public bool Equals(LaunchArguments? other)
```

#### Parameters

`other` [LaunchArguments](MicroEngine.Core.LaunchArguments.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_LaunchArguments_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_LaunchArguments_Parse_System_Collections_Generic_IReadOnlyList_System_String__"></a> Parse\(IReadOnlyList<string\>\)

Parses command-line arguments.

```csharp
public static LaunchArguments Parse(IReadOnlyList<string> args)
```

#### Parameters

`args` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

#### Returns

 [LaunchArguments](MicroEngine.Core.LaunchArguments.md)

#### Exceptions

 [FormatException](https://learn.microsoft.com/dotnet/api/system.formatexception)

An argument is unknown, misses its value or has an invalid one, <code>--headless</code> contradicts <code>--platform</code>, or the script file does not exist.

### <a id="MicroEngine_Core_LaunchArguments_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Core_LaunchArguments_op_Equality_MicroEngine_Core_LaunchArguments_MicroEngine_Core_LaunchArguments_"></a> operator ==\(LaunchArguments?, LaunchArguments?\)

```csharp
public static bool operator ==(LaunchArguments? left, LaunchArguments? right)
```

#### Parameters

`left` [LaunchArguments](MicroEngine.Core.LaunchArguments.md)?

`right` [LaunchArguments](MicroEngine.Core.LaunchArguments.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_LaunchArguments_op_Inequality_MicroEngine_Core_LaunchArguments_MicroEngine_Core_LaunchArguments_"></a> operator \!=\(LaunchArguments?, LaunchArguments?\)

```csharp
public static bool operator !=(LaunchArguments? left, LaunchArguments? right)
```

#### Parameters

`left` [LaunchArguments](MicroEngine.Core.LaunchArguments.md)?

`right` [LaunchArguments](MicroEngine.Core.LaunchArguments.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
