# Class GameLauncher

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Configuration and launch options](../reference/configuration.md)  

The command line every desktop game accepts, on any backend. Supported arguments:

<ul><li><code>--script FILE</code> play scripted input (see [InputScript](MicroEngine.Core.InputScript.md)); implies deterministic time unless
<code>--time</code> says otherwise.</li><li><code>--captures DIR</code> directory for script captures (default ./captures).</li><li><code>--platform NAME</code> run on that platform of the catalog (default: <code>MICROENGINE_PLATFORM</code>, else the
first one that can run here); <code>--list-platforms</code> prints them and exits.</li><li><code>--headless</code> run without a window (no captures are written); the same as <code>--platform headless</code>. A
headless run is in deterministic time unless <code>--time</code> says otherwise (the headless platform's
[RunPreset](MicroEngine.Core.RunPreset.md)).</li><li><code>--hidden</code> render in a hidden window (captures still work); it implies nothing about time or sound.</li><li><code>--time realtime|deterministic</code> how the loop advances time ([TimeMode](MicroEngine.Kernel.TimeMode.md)): with the
wall clock, or one fixed step per frame; by default the game's choice, else the platform's [RunPreset](MicroEngine.Core.RunPreset.md)
(headless: deterministic), else real time; a script means deterministic.</li><li><code>--frames N</code> stop after N updates; <code>--seed N</code>; <code>--scale N</code>; <code>--mute</code>.</li><li><code>--report</code> print the platform that runs and whether it has audio before the game starts (package checks).</li><li><code>--audio auto|device|silent</code> where sound may go ([AudioOutput](MicroEngine.Core.AudioOutput.md)); <code>auto</code> (the default)
asks for no physical device when the run is in deterministic time (scripts, <code>--time deterministic</code>), so checks
are never heard, and for one otherwise (the headless platform has none).</li><li><code>--audio-capture FILE</code> play nothing aloud and write what would have been heard as a WAV
([CaptureAudioDevice](MicroEngine.Audio.CaptureAudioDevice.md)).</li><li><code>--gpu auto|high-performance|power-saving</code> the GPU to ask for on a machine with two
([GpuPreference](MicroEngine.Core.GpuPreference.md)).</li><li><code>--renderer NAME[,NAME...]</code> graphics APIs to try first ([Renderers](MicroEngine.Core.GraphicsSettings.md#MicroEngine_Core_GraphicsSettings_Renderers)).</li><li><code>--vsync on|off</code> wait for the display's refresh or not ([VSync](MicroEngine.Core.GraphicsSettings.md#MicroEngine_Core_GraphicsSettings_VSync)).</li><li><code>--fps-limit N</code> draw at most N frames per second; 0 removes the game's limit
([FrameRateLimit](MicroEngine.Core.GraphicsSettings.md#MicroEngine_Core_GraphicsSettings_FrameRateLimit)).</li><li><code>--window windowed|fullscreen|exclusive</code> the window mode ([WindowMode](MicroEngine.Core.GraphicsSettings.md#MicroEngine_Core_GraphicsSettings_WindowMode)).</li><li><code>--display N</code> open on display N, counted from 0 ([Display](MicroEngine.Core.GraphicsSettings.md#MicroEngine_Core_GraphicsSettings_Display)).</li><li><code>--pacing display|measured</code> count frame time in refresh periods of the display (the default) or as
measured ([PaceToDisplay](MicroEngine.Core.RunOptions.md#MicroEngine_Core_RunOptions_PaceToDisplay)).</li><li><code>--profile FILE</code> measure every frame and the start phases ([FrameProfiler](MicroEngine.Core.FrameProfiler.md)), print a summary and
write the results as JSON, or CSV when FILE ends in .csv.</li></ul>

A run in deterministic time also asks the platform, through explicit request options, not to wait for the display
([WaitForDisplay](MicroEngine.Core.PlatformRequest.md#MicroEngine_Core_PlatformRequest_WaitForDisplay)) and for no sound device (<code>--audio auto</code>); a run in real time
whose game shows a start screen asks it to start gamepads after the first frame
([StartGamepadsAfterFirstFrame](MicroEngine.Core.PlatformRequest.md#MicroEngine_Core_PlatformRequest_StartGamepadsAfterFirstFrame)). The platform itself never knows the time mode.
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
public const string Usage = "Usage: [--script FILE] [--captures DIR] [--platform NAME] [--list-platforms] [--headless] [--hidden] [--time realtime|deterministic] [--frames N] [--seed N] [--scale N] [--mute] [--report] [--audio auto|device|silent] [--audio-capture FILE] [--gpu auto|high-performance|power-saving] [--renderer NAME[,NAME...]] [--vsync on|off] [--fps-limit N] [--window windowed|fullscreen|exclusive] [--display N] [--pacing display|measured] [--profile FILE]"
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

### <a id="MicroEngine_Core_GameLauncher_Preferred_MicroEngine_Core_PlatformCatalog_System_String_"></a> Preferred\(PlatformCatalog, string?\)

The platforms tried when the game prefers <code class="paramref">backend</code> ([Backend](MicroEngine.Core.GraphicsSettings.md#MicroEngine_Core_GraphicsSettings_Backend)) and
nobody named one: that platform first, then the [Fallbacks](MicroEngine.Core.GameLauncher.md#MicroEngine_Core_GameLauncher_Fallbacks_MicroEngine_Core_PlatformCatalog_). A preference this catalog lacks or that
cannot run here is reported and skipped.

```csharp
public static IReadOnlyList<PlatformProfile> Preferred(PlatformCatalog catalog, string? backend)
```

#### Parameters

`catalog` [PlatformCatalog](MicroEngine.Core.PlatformCatalog.md)

`backend` [string](https://learn.microsoft.com/dotnet/api/system.string)?

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
