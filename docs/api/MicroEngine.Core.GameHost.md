# Class GameHost

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Composing a game for every platform](../manual/backends.md#one-composition-for-every-platform)  

The one launch path of every host: desktop ([GameLauncher](MicroEngine.Core.GameLauncher.md)), browser pages, Android and iOS apps and
tests compose the same way and decide a run the same way. A host states its defaults (its platforms, where content
files are), the game's <code>Configure</code> entry point composes the rest ([HostBuilder](MicroEngine.Hosting.HostBuilder.md)), and the
[LaunchRequest](MicroEngine.Hosting.LaunchRequest.md) decides the run: a script means deterministic time and seed 1 on every host; a run in
deterministic time asks the platform not to wait for the display and for no sound device unless the launch asks for
one; a real-time run whose game shows a start screen starts gamepads after the first frame.

```csharp
public sealed class GameHost
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GameHost](MicroEngine.Core.GameHost.md)

## Examples

A test that runs a game headless through the same path as the desktop launcher:

```csharp
var host = GameHost.Create(new HostEnvironment(HostKind.Test), new LaunchRequest { Frames = 60 },
    h => h.UseGame(() => new MyGame()), h => h.UsePlatforms(new PlatformCatalog().Add(HeadlessPlatform.Profile)));
var result = host.Play();
```

## Properties

### <a id="MicroEngine_Core_GameHost_Arguments"></a> Arguments

The typed launch options ([LaunchArguments](MicroEngine.Core.LaunchArguments.md)), with the seed resolved by the launch's rule.

```csharp
public LaunchArguments Arguments { get; }
```

#### Property Value

 [LaunchArguments](MicroEngine.Core.LaunchArguments.md)

### <a id="MicroEngine_Core_GameHost_Builder"></a> Builder

The composition: the host's defaults, then the game's <code>Configure</code>.

```csharp
public HostBuilder Builder { get; }
```

#### Property Value

 HostBuilder

### <a id="MicroEngine_Core_GameHost_Platforms"></a> Platforms

The platforms this host offers, in order of preference.

```csharp
public PlatformCatalog Platforms { get; }
```

#### Property Value

 [PlatformCatalog](MicroEngine.Core.PlatformCatalog.md)

### <a id="MicroEngine_Core_GameHost_Request"></a> Request

What this launch asked for.

```csharp
public LaunchRequest Request { get; }
```

#### Property Value

 LaunchRequest

## Methods

### <a id="MicroEngine_Core_GameHost_Candidates_MicroEngine_Core_GraphicsSettings_"></a> Candidates\(GraphicsSettings\)

The platforms the run tries, in order ([Choose``1](MicroEngine.Hosting.ProfileSelection.md#MicroEngine_Hosting_ProfileSelection_Choose__1_System_Collections_Generic_IReadOnlyList___0__MicroEngine_Hosting_LaunchRequest_MicroEngine_Hosting_HostEnvironment_System_String___0_)); notes about the choice go to the host's error output.

```csharp
public IReadOnlyList<PlatformProfile> Candidates(GraphicsSettings graphics)
```

#### Parameters

`graphics` [GraphicsSettings](MicroEngine.Core.GraphicsSettings.md)

#### Returns

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[PlatformProfile](MicroEngine.Core.PlatformProfile.md)\>

#### Exceptions

 [PlatformConfigurationException](MicroEngine.Core.PlatformConfigurationException.md)

The launch named a platform that is unknown or cannot run here, or none can.

### <a id="MicroEngine_Core_GameHost_Create_MicroEngine_Hosting_HostEnvironment_MicroEngine_Hosting_LaunchRequest_System_Action_MicroEngine_Hosting_HostBuilder__System_Action_MicroEngine_Hosting_HostBuilder__"></a> Create\(HostEnvironment, LaunchRequest, Action<HostBuilder\>, Action<HostBuilder\>?\)

Composes a host: <code class="paramref">defaults</code> (the host's own platforms, content and storage) first, then
<code class="paramref">configure</code> (the game's <code>Configure</code> entry point), which may replace any of them.

```csharp
public static GameHost Create(HostEnvironment environment, LaunchRequest request, Action<HostBuilder> configure, Action<HostBuilder>? defaults = null)
```

#### Parameters

`environment` HostEnvironment

`request` LaunchRequest

`configure` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<HostBuilder\>

`defaults` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<HostBuilder\>?

#### Returns

 [GameHost](MicroEngine.Core.GameHost.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The composition chose no game ([UseGame](MicroEngine.Core.HostBuilderExtensions.md#MicroEngine_Core_HostBuilderExtensions_UseGame_MicroEngine_Hosting_HostBuilder_System_Func_MicroEngine_Core_Game__)).

### <a id="MicroEngine_Core_GameHost_CreateGame"></a> CreateGame\(\)

A new game, built by the composition's [UseGame](MicroEngine.Core.HostBuilderExtensions.md#MicroEngine_Core_HostBuilderExtensions_UseGame_MicroEngine_Hosting_HostBuilder_System_Func_MicroEngine_Core_Game__).

```csharp
public Game CreateGame()
```

#### Returns

 [Game](MicroEngine.Core.Game.md)

### <a id="MicroEngine_Core_GameHost_CreateLoop_MicroEngine_Core_Game_MicroEngine_Core_IPlatform_MicroEngine_Core_RunOptions_"></a> CreateLoop\(Game, IPlatform, RunOptions\)

A loop for one run of <code class="paramref">game</code> on <code class="paramref">platform</code>, with the composition applied to its engine.

```csharp
public GameLoop CreateLoop(Game game, IPlatform platform, RunOptions options)
```

#### Parameters

`game` [Game](MicroEngine.Core.Game.md)

`platform` [IPlatform](MicroEngine.Core.IPlatform.md)

`options` [RunOptions](MicroEngine.Core.RunOptions.md)

#### Returns

 [GameLoop](MicroEngine.Core.GameLoop.md)

### <a id="MicroEngine_Core_GameHost_CreatePlatform_MicroEngine_Core_PlatformRequest_"></a> CreatePlatform\(PlatformRequest\)

Builds the platform for <code class="paramref">request</code>: the first candidate that starts, reporting each one that could
not (a missing native library, no display) before trying the next.

```csharp
public (IPlatform Platform, PlatformProfile Profile) CreatePlatform(PlatformRequest request)
```

#### Parameters

`request` [PlatformRequest](MicroEngine.Core.PlatformRequest.md)

#### Returns

 \([IPlatform](MicroEngine.Core.IPlatform.md) [Platform](https://learn.microsoft.com/dotnet/api/system.valuetuple\-microengine.core.iplatform,microengine.core.platformprofile\-.platform), [PlatformProfile](MicroEngine.Core.PlatformProfile.md) [Profile](https://learn.microsoft.com/dotnet/api/system.valuetuple\-microengine.core.iplatform,microengine.core.platformprofile\-.profile)\)

#### Exceptions

 [PlatformConfigurationException](MicroEngine.Core.PlatformConfigurationException.md)

No candidate could start; the message is the last one's reason.

### <a id="MicroEngine_Core_GameHost_CreateRequest_MicroEngine_Core_Game_MicroEngine_Core_RunOptions_"></a> CreateRequest\(Game, RunOptions\)

What the run asks of its platform: the launch's window options, no wait for the display and no sound device in
deterministic time (unless the launch asks for a device), gamepads after the first frame for a real-time run with
a start screen, the game's identity; then the composition's request changes
([ConfigureRequest](MicroEngine.Core.HostBuilderExtensions.md#MicroEngine_Core_HostBuilderExtensions_ConfigureRequest_MicroEngine_Hosting_HostBuilder_System_Func_MicroEngine_Core_PlatformRequest_MicroEngine_Core_PlatformRequest__)), in order.

```csharp
public PlatformRequest CreateRequest(Game game, RunOptions options)
```

#### Parameters

`game` [Game](MicroEngine.Core.Game.md)

`options` [RunOptions](MicroEngine.Core.RunOptions.md)

#### Returns

 [PlatformRequest](MicroEngine.Core.PlatformRequest.md)

### <a id="MicroEngine_Core_GameHost_CreateRunOptions_MicroEngine_Core_Game_MicroEngine_Core_FrameProfiler_MicroEngine_Core_InputScript_"></a> CreateRunOptions\(Game, FrameProfiler?, InputScript?\)

The run's options for <code class="paramref">game</code>: the graphics options with the launch's overrides, the script, its
captures, the frame limit, the seed, mute, pacing and the time mode ([EffectiveTime](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_EffectiveTime)).

```csharp
public RunOptions CreateRunOptions(Game game, FrameProfiler? profiler = null, InputScript? script = null)
```

#### Parameters

`game` [Game](MicroEngine.Core.Game.md)

`profiler` [FrameProfiler](MicroEngine.Core.FrameProfiler.md)?

`script` [InputScript](MicroEngine.Core.InputScript.md)?

#### Returns

 [RunOptions](MicroEngine.Core.RunOptions.md)

#### Exceptions

 [FormatException](https://learn.microsoft.com/dotnet/api/system.formatexception)

The script is invalid.

### <a id="MicroEngine_Core_GameHost_CreateSession_System_Func_MicroEngine_Hosting_LaunchRequest_MicroEngine_Core_GameHost__"></a> CreateSession\(Func<LaunchRequest, GameHost\>\)

A session for hosts that drive frames themselves and outlive a run (a browser page): each
[Start](MicroEngine.Core.GameSession.md#MicroEngine_Core_GameSession_Start_MicroEngine_Core_RunOptions_) composes again with <code class="paramref">compose</code> for that launch.

```csharp
public static GameSession CreateSession(Func<LaunchRequest, GameHost> compose)
```

#### Parameters

`compose` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<LaunchRequest, [GameHost](MicroEngine.Core.GameHost.md)\>

#### Returns

 [GameSession](MicroEngine.Core.GameSession.md)

### <a id="MicroEngine_Core_GameHost_Play_System_Action_MicroEngine_Core_RunOptions__"></a> Play\(Action<RunOptions\>?\)

Runs a new game of the composition to completion on the platform the launch chooses, for hosts that own their
thread and tests; the platform is disposed after the run. Never throws for errors raised by the game or a
platform that could not start: they are in [Error](MicroEngine.Core.RunResult.md#MicroEngine_Core_RunResult_Error).

```csharp
public RunResult Play(Action<RunOptions>? inspect = null)
```

#### Parameters

`inspect` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[RunOptions](MicroEngine.Core.RunOptions.md)\>?

#### Returns

 [RunResult](MicroEngine.Core.RunResult.md)

#### Exceptions

 [FormatException](https://learn.microsoft.com/dotnet/api/system.formatexception)

The launch's script is invalid.

### <a id="MicroEngine_Core_GameHost_ReadScript"></a> ReadScript\(\)

The launch's input script, parsed: [ScriptText](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_ScriptText), else the file [ScriptPath](MicroEngine.Hosting.LaunchRequest.md#MicroEngine_Hosting_LaunchRequest_ScriptPath); null without one.

```csharp
public InputScript? ReadScript()
```

#### Returns

 [InputScript](MicroEngine.Core.InputScript.md)?

#### Exceptions

 [FormatException](https://learn.microsoft.com/dotnet/api/system.formatexception)

The script is invalid.

### <a id="MicroEngine_Core_GameHost_Run_MicroEngine_Core_Game_MicroEngine_Core_IPlatform_MicroEngine_Core_RunOptions_"></a> Run\(Game, IPlatform, RunOptions\)

Runs <code class="paramref">game</code> on <code class="paramref">platform</code> to completion with the composition applied; the platform is not disposed.

```csharp
public RunResult Run(Game game, IPlatform platform, RunOptions options)
```

#### Parameters

`game` [Game](MicroEngine.Core.Game.md)

`platform` [IPlatform](MicroEngine.Core.IPlatform.md)

`options` [RunOptions](MicroEngine.Core.RunOptions.md)

#### Returns

 [RunResult](MicroEngine.Core.RunResult.md)

### <a id="MicroEngine_Core_GameHost_Run"></a> Run\(\)

Runs the launch as a desktop process does ([GameLauncher](MicroEngine.Core.GameLauncher.md)): lists the platforms, or builds the game
and its platform, reports and profiles as asked, runs it, writes the audio capture and prints the outcome.

```csharp
public int Run()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

The process exit code: 0 on success, 1 on a game or platform error, 2 on an invalid script.

</div>
