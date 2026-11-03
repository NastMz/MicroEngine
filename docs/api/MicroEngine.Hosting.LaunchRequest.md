# Class LaunchRequest

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Hosting](MicroEngine.Hosting.md)  
Assembly: MicroEngine.Hosting.dll  
Guide: [Composing a game for every platform](../manual/backends.md#one-composition-for-every-platform)  

What one launch of a game asks for, whichever host received it: a desktop command line ([Parse](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_Parse_System_Collections_Generic_IReadOnlyList_System_String__System_Func_System_String_System_Boolean__)), a
browser page's query string, a phone's launch extras or a test. Every host turns its input into this record and
hands it to the same launcher model, so the rules that follow from it are written once: a run with a script is in
deterministic time unless [Time](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_Time) says otherwise ([EffectiveTime](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_EffectiveTime)) and has seed 1 unless
[Seed](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_Seed) says otherwise ([ResolveSeed](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_ResolveSeed_System_Int32_)).

```csharp
public sealed record LaunchRequest : IEquatable<LaunchRequest>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[LaunchRequest](MicroEngine.Hosting.LaunchRequest.md)

#### Implements

[IEquatable<LaunchRequest\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Remarks

Options that name a value of a module the hosting layer does not know (the GPU, the window mode, where sound may go)
are kept as the words of the command line, already checked against the words allowed; the engine maps them to its
own types.

## Constructors

### <a id="MicroEngine_Hosting_LaunchRequest__ctor"></a> LaunchRequest\(\)

```csharp
public LaunchRequest()
```

## Fields

### <a id="MicroEngine_Hosting_LaunchRequest_HeadlessName"></a> HeadlessName

The name of the windowless platform <code>--headless</code> stands for.

```csharp
public const string HeadlessName = "headless"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Hosting_LaunchRequest_Usage"></a> Usage

The one-line usage text desktop launchers print after an argument error.

```csharp
public const string Usage = "Usage: [--script FILE] [--captures DIR] [--platform NAME] [--list-platforms] [--headless] [--hidden] [--time realtime|deterministic] [--frames N] [--seed N] [--scale N] [--mute] [--report] [--audio auto|device|silent] [--audio-capture FILE] [--gpu auto|high-performance|power-saving] [--renderer NAME[,NAME...]] [--vsync on|off] [--fps-limit N] [--window windowed|fullscreen|exclusive] [--display N] [--pacing display|measured] [--profile FILE]"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Properties

### <a id="MicroEngine_Hosting_LaunchRequest_Audio"></a> Audio

<code>--audio</code>: where sound may go, one of [AudioWords](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_AudioWords) ("auto" by default).

```csharp
public string Audio { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Hosting_LaunchRequest_AudioCapturePath"></a> AudioCapturePath

<code>--audio-capture</code>: the WAV file to write what would have been heard to, or null.

```csharp
public string? AudioCapturePath { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Hosting_LaunchRequest_AudioWords"></a> AudioWords

The words [Audio](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_Audio) accepts.

```csharp
public static IReadOnlyList<string> AudioWords { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Hosting_LaunchRequest_CaptureDirectory"></a> CaptureDirectory

<code>--captures</code>: where script captures are written ("captures" by default).

```csharp
public string CaptureDirectory { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Hosting_LaunchRequest_Display"></a> Display

<code>--display</code>: the display to open on, counted from 0, or null to keep the game's choice.

```csharp
public int? Display { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="MicroEngine_Hosting_LaunchRequest_EffectiveTime"></a> EffectiveTime

The time mode the run asks the loop for: [Time](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_Time) when given, else [Deterministic](MicroEngine.Kernel.TimeMode.md)
for a run with a script, else null (the game's choice, then the composition's preset, then real time). The rule is
the same on every host: a scripted run in a browser or on a phone advances one fixed step per frame exactly as on
desktop, so its captures are reproducible.

```csharp
public TimeMode? EffectiveTime { get; }
```

#### Property Value

 TimeMode?

### <a id="MicroEngine_Hosting_LaunchRequest_FrameRateLimit"></a> FrameRateLimit

<code>--fps-limit</code>: the frame limit (0 for none), or null to keep the game's choice.

```csharp
public int? FrameRateLimit { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="MicroEngine_Hosting_LaunchRequest_Frames"></a> Frames

<code>--frames</code>: stop after this many updates; 0 (the default) runs until the game quits.

```csharp
public long Frames { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Hosting_LaunchRequest_Gpu"></a> Gpu

<code>--gpu</code>: one of [GpuWords](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_GpuWords), or null to keep the game's choice.

```csharp
public string? Gpu { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Hosting_LaunchRequest_GpuWords"></a> GpuWords

The words [Gpu](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_Gpu) accepts.

```csharp
public static IReadOnlyList<string> GpuWords { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Hosting_LaunchRequest_HasScript"></a> HasScript

Whether the run plays an input script.

```csharp
public bool HasScript { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Hosting_LaunchRequest_Headless"></a> Headless

<code>--headless</code>: run without a window, the same as <code>--platform headless</code>.

```csharp
public bool Headless { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Hosting_LaunchRequest_Hidden"></a> Hidden

<code>--hidden</code>: render into a hidden window; it implies nothing about time or sound.

```csharp
public bool Hidden { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Hosting_LaunchRequest_IsDeterministic"></a> IsDeterministic

Whether the run asks for deterministic time ([EffectiveTime](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_EffectiveTime)).

```csharp
public bool IsDeterministic { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Hosting_LaunchRequest_ListPlatforms"></a> ListPlatforms

<code>--list-platforms</code>: print the host's platforms and exit.

```csharp
public bool ListPlatforms { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Hosting_LaunchRequest_Mute"></a> Mute

<code>--mute</code>: no sound.

```csharp
public bool Mute { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Hosting_LaunchRequest_PaceToDisplay"></a> PaceToDisplay

<code>--pacing</code>: false for <code>measured</code>, true (the default) for <code>display</code>.

```csharp
public bool PaceToDisplay { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Hosting_LaunchRequest_Platform"></a> Platform

<code>--platform</code>: the platform profile to run on, or null to let the host choose.

```csharp
public string? Platform { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Hosting_LaunchRequest_ProfilePath"></a> ProfilePath

<code>--profile</code>: the file to write frame measurements to, or null to measure nothing.

```csharp
public string? ProfilePath { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Hosting_LaunchRequest_Renderers"></a> Renderers

<code>--renderer</code>: graphics APIs to try first, or null to keep the game's choice.

```csharp
public IReadOnlyList<string>? Renderers { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>?

### <a id="MicroEngine_Hosting_LaunchRequest_Report"></a> Report

<code>--report</code>: print the platform that runs and whether it has audio before the game starts.

```csharp
public bool Report { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Hosting_LaunchRequest_Scale"></a> Scale

<code>--scale</code>: the window scale, or null for the game's.

```csharp
public int? Scale { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="MicroEngine_Hosting_LaunchRequest_ScriptName"></a> ScriptName

The script's name for messages: the file name of [ScriptPath](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_ScriptPath), or the one the host gave with [ScriptText](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_ScriptText).

```csharp
public string? ScriptName { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Hosting_LaunchRequest_ScriptPath"></a> ScriptPath

<code>--script</code>: the input script file to read, or null. Hosts that hold the text itself use [ScriptText](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_ScriptText).

```csharp
public string? ScriptPath { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Hosting_LaunchRequest_ScriptText"></a> ScriptText

The input script's text, for hosts that receive it rather than a file (a browser page, a phone's bundled script); wins over [ScriptPath](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_ScriptPath).

```csharp
public string? ScriptText { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Hosting_LaunchRequest_Seed"></a> Seed

<code>--seed</code>: the seed of the game's randomness, or null for [ResolveSeed](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_ResolveSeed_System_Int32_)'s rule.

```csharp
public int? Seed { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="MicroEngine_Hosting_LaunchRequest_Time"></a> Time

<code>--time</code>: how the loop advances time, or null to leave it to [EffectiveTime](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_EffectiveTime)'s rule.

```csharp
public TimeMode? Time { get; init; }
```

#### Property Value

 TimeMode?

### <a id="MicroEngine_Hosting_LaunchRequest_VSync"></a> VSync

<code>--vsync</code>: on or off, or null to keep the game's choice.

```csharp
public bool? VSync { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)?

### <a id="MicroEngine_Hosting_LaunchRequest_Window"></a> Window

<code>--window</code>: one of [WindowWords](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_WindowWords), or null to keep the game's choice.

```csharp
public string? Window { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Hosting_LaunchRequest_WindowWords"></a> WindowWords

The words [Window](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_Window) accepts.

```csharp
public static IReadOnlyList<string> WindowWords { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

## Methods

### <a id="MicroEngine_Hosting_LaunchRequest__Clone__"></a> <Clone\>$\(\)

```csharp
public LaunchRequest <Clone>$()
```

#### Returns

 [LaunchRequest](MicroEngine.Hosting.LaunchRequest.md)

### <a id="MicroEngine_Hosting_LaunchRequest_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Hosting_LaunchRequest_Equals_MicroEngine_Hosting_LaunchRequest_"></a> Equals\(LaunchRequest?\)

```csharp
public bool Equals(LaunchRequest? other)
```

#### Parameters

`other` [LaunchRequest](MicroEngine.Hosting.LaunchRequest.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Hosting_LaunchRequest_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Hosting_LaunchRequest_Parse_System_Collections_Generic_IReadOnlyList_System_String__System_Func_System_String_System_Boolean__"></a> Parse\(IReadOnlyList<string\>, Func<string, bool\>?\)

Parses a desktop command line (the options of [Usage](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_Usage)). A script file must exist when
<code class="paramref">fileExists</code> (by default `Exists`) says so.

```csharp
public static LaunchRequest Parse(IReadOnlyList<string> args, Func<string, bool>? fileExists = null)
```

#### Parameters

`args` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

`fileExists` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>?

#### Returns

 [LaunchRequest](MicroEngine.Hosting.LaunchRequest.md)

#### Exceptions

 [FormatException](https://learn.microsoft.com/dotnet/api/system.formatexception)

An argument is unknown, misses its value or has an invalid one, <code>--headless</code> contradicts <code>--platform</code>, or the script file does not exist.

### <a id="MicroEngine_Hosting_LaunchRequest_ResolveSeed_System_Int32_"></a> ResolveSeed\(int\)

The seed of the run: [Seed](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_Seed) when given, else 1 for a run with a script, else <code class="paramref">clockSeed</code>.

```csharp
public int ResolveSeed(int clockSeed)
```

#### Parameters

`clockSeed` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Hosting_LaunchRequest_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Hosting_LaunchRequest_op_Equality_MicroEngine_Hosting_LaunchRequest_MicroEngine_Hosting_LaunchRequest_"></a> operator ==\(LaunchRequest?, LaunchRequest?\)

```csharp
public static bool operator ==(LaunchRequest? left, LaunchRequest? right)
```

#### Parameters

`left` [LaunchRequest](MicroEngine.Hosting.LaunchRequest.md)?

`right` [LaunchRequest](MicroEngine.Hosting.LaunchRequest.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Hosting_LaunchRequest_op_Inequality_MicroEngine_Hosting_LaunchRequest_MicroEngine_Hosting_LaunchRequest_"></a> operator \!=\(LaunchRequest?, LaunchRequest?\)

```csharp
public static bool operator !=(LaunchRequest? left, LaunchRequest? right)
```

#### Parameters

`left` [LaunchRequest](MicroEngine.Hosting.LaunchRequest.md)?

`right` [LaunchRequest](MicroEngine.Hosting.LaunchRequest.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
