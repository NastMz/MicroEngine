# Class GameLauncher

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Configuration and launch options](../reference/configuration.md)  

The command line every desktop game accepts, on any backend. Supported arguments:

<ul><li><code>--script FILE</code> play scripted input (see [InputScript](MicroEngine.Core.InputScript.md)); implies a fixed step.</li><li><code>--captures DIR</code> directory for script captures (default ./captures).</li><li><code>--platform NAME</code> run on that platform of the catalog (default: <code>MICROENGINE_PLATFORM</code>, else the
first one that can run here); <code>--list-platforms</code> prints them and exits.</li><li><code>--headless</code> run without a window (no captures are written); the same as <code>--platform headless</code>.</li><li><code>--hidden</code> render in a hidden window (captures still work).</li><li><code>--frames N</code> stop after N updates; <code>--seed N</code>; <code>--scale N</code>; <code>--mute</code>.</li><li><code>--report</code> print the platform that runs and whether it has audio before the game starts (package checks).</li><li><code>--audio auto|device|silent</code> where sound may go ([AudioOutput](MicroEngine.Core.AudioOutput.md)); by default hidden and
scripted runs open no physical device, so checks and tests are never heard.</li><li><code>--audio-capture FILE</code> play nothing aloud and write what would have been heard as a WAV
([CaptureAudioDevice](MicroEngine.Audio.CaptureAudioDevice.md)).</li><li><code>--gpu auto|high-performance|power-saving</code> the GPU to ask for on a machine with two
([GpuPreference](MicroEngine.Core.GpuPreference.md)).</li><li><code>--pacing display|measured</code> count frame time in refresh periods of the display (the default) or as
measured ([PaceToDisplay](MicroEngine.Core.RunOptions.md#MicroEngine_Core_RunOptions_PaceToDisplay)).</li><li><code>--profile FILE</code> measure every frame and the start phases ([FrameProfiler](MicroEngine.Core.FrameProfiler.md)), print a summary and
write the results as JSON, or CSV when FILE ends in .csv.</li></ul>

Returns a process exit code: 0 on success, 1 on a game or platform error, 2 on bad arguments.

```csharp
public static class GameLauncher
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GameLauncher](MicroEngine.Core.GameLauncher.md)

## Fields

### <a id="MicroEngine_Core_GameLauncher_ProfileWarmupFrames"></a> ProfileWarmupFrames

Frames of a profiled run left out of its summary: the first second or so, while the JIT compiles and caches warm up.

```csharp
public const int ProfileWarmupFrames = 60
```

#### Field Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_GameLauncher_Usage"></a> Usage

The one-line usage text printed after an argument error.

```csharp
public const string Usage = "Usage: [--script FILE] [--captures DIR] [--platform NAME] [--list-platforms] [--headless] [--hidden] [--frames N] [--seed N] [--scale N] [--mute] [--report] [--audio auto|device|silent] [--audio-capture FILE] [--gpu auto|high-performance|power-saving] [--pacing display|measured] [--profile FILE]"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Core_GameLauncher_Fallbacks_MicroEngine_Core_PlatformCatalog_"></a> Fallbacks\(PlatformCatalog\)

The platforms tried, in order, when nobody named one: every platform of the catalog that can run here except the
headless one (a player who sees no window would not know why), or the catalog's default when that leaves none.

```csharp
public static IReadOnlyList<PlatformProfile> Fallbacks(PlatformCatalog catalog)
```

#### Parameters

`catalog` [PlatformCatalog](MicroEngine.Core.PlatformCatalog.md)

#### Returns

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[PlatformProfile](MicroEngine.Core.PlatformProfile.md)\>

### <a id="MicroEngine_Core_GameLauncher_Report_MicroEngine_Core_PlatformProfile_MicroEngine_Core_IPlatform_"></a> Report\(PlatformProfile, IPlatform\)

What <code>--report</code> prints: the profile that runs, with what the platform says about itself when it overrides
`ToString` (SDL names its graphics API), and whether an audio device is open.

```csharp
public static IReadOnlyList<string> Report(PlatformProfile profile, IPlatform platform)
```

#### Parameters

`profile` [PlatformProfile](MicroEngine.Core.PlatformProfile.md)

`platform` [IPlatform](MicroEngine.Core.IPlatform.md)

#### Returns

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Core_GameLauncher_Run_MicroEngine_Core_Game_System_String___MicroEngine_Core_PlatformCatalog_System_Func_MicroEngine_Core_PlatformRequest_MicroEngine_Core_PlatformRequest__"></a> Run\(Game, string\[\], PlatformCatalog, Func<PlatformRequest, PlatformRequest\>?\)

```csharp
public static int Run(Game game, string[] args, PlatformCatalog catalog, Func<PlatformRequest, PlatformRequest>? configure = null)
```

#### Parameters

`game` [Game](MicroEngine.Core.Game.md)

`args` [string](https://learn.microsoft.com/dotnet/api/system.string)\[\]

`catalog` [PlatformCatalog](MicroEngine.Core.PlatformCatalog.md)

`configure` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[PlatformRequest](MicroEngine.Core.PlatformRequest.md), [PlatformRequest](MicroEngine.Core.PlatformRequest.md)\>?

Changes the platform request before the platform is built: an audio device or input
            sources of the host's own (see [PlatformRequest](MicroEngine.Core.PlatformRequest.md)).

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

</div>
