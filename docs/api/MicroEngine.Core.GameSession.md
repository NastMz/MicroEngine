# Class GameSession

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

Runs one game at a time for hosts that drive frames themselves and outlive a run, such as a browser
page: each [Start](MicroEngine.Core.GameSession.md#MicroEngine_Core_GameSession_Start_MicroEngine_Core_RunOptions_) builds a fresh game and platform, [Tick](MicroEngine.Core.GameSession.md#MicroEngine_Core_GameSession_Tick) runs one frame, and
the end of a run (quit, error, [Stop](MicroEngine.Core.GameSession.md#MicroEngine_Core_GameSession_Stop)) shuts the game down and releases the platform, so the
host can show its own screen and start again without reloading.

```csharp
public sealed class GameSession
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GameSession](MicroEngine.Core.GameSession.md)

## Remarks

A session made from a composition ([#ctor](MicroEngine.Core.GameSession.md), the browser host's) decides
every run with the launcher model all hosts share ([GameHost](MicroEngine.Core.GameHost.md)): a run with a script is in deterministic
time with seed 1 exactly as on desktop and phones, so its captures are reproducible.

## Constructors

### <a id="MicroEngine_Core_GameSession__ctor_System_Func_MicroEngine_Core_Game__System_Func_MicroEngine_Core_Game_MicroEngine_Core_IPlatform__"></a> GameSession\(Func<Game\>, Func<Game, IPlatform\>\)

A session that builds each run's game and platform with the two functions given.

```csharp
public GameSession(Func<Game> createGame, Func<Game, IPlatform> createPlatform)
```

#### Parameters

`createGame` [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<[Game](MicroEngine.Core.Game.md)\>

`createPlatform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[Game](MicroEngine.Core.Game.md), [IPlatform](MicroEngine.Core.IPlatform.md)\>

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

An argument is null.

### <a id="MicroEngine_Core_GameSession__ctor_System_Func_MicroEngine_Hosting_LaunchRequest_MicroEngine_Core_GameHost__"></a> GameSession\(Func<LaunchRequest, GameHost\>\)

A session whose every run is composed by <code class="paramref">compose</code> for that run's launch (the game's
<code>Configure</code> entry point with the host's defaults), then decided by the shared launcher model.

```csharp
public GameSession(Func<LaunchRequest, GameHost> compose)
```

#### Parameters

`compose` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<LaunchRequest, [GameHost](MicroEngine.Core.GameHost.md)\>

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">compose</code> is null.

## Properties

### <a id="MicroEngine_Core_GameSession_Frames"></a> Frames

Frames rendered by the current or last run.

```csharp
public long Frames { get; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Core_GameSession_Game"></a> Game

The game of the current or last run.

```csharp
public Game? Game { get; }
```

#### Property Value

 [Game](MicroEngine.Core.Game.md)?

### <a id="MicroEngine_Core_GameSession_Host"></a> Host

The composition of the current or last run of a composed session; null otherwise.

```csharp
public GameHost? Host { get; }
```

#### Property Value

 [GameHost](MicroEngine.Core.GameHost.md)?

### <a id="MicroEngine_Core_GameSession_IsRunning"></a> IsRunning

Whether a run is in progress.

```csharp
public bool IsRunning { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_GameSession_Platform"></a> Platform

The platform of the current run; null between runs.

```csharp
public IPlatform? Platform { get; }
```

#### Property Value

 [IPlatform](MicroEngine.Core.IPlatform.md)?

### <a id="MicroEngine_Core_GameSession_Result"></a> Result

How the last run ended; null while a run is in progress or before the first one.

```csharp
public RunResult? Result { get; }
```

#### Property Value

 [RunResult](MicroEngine.Core.RunResult.md)?

## Methods

### <a id="MicroEngine_Core_GameSession_Start_MicroEngine_Core_RunOptions_"></a> Start\(RunOptions?\)

Builds and initializes a new game with <code class="paramref">options</code> as they are (a session made from two functions).
False when that failed; the error is in [Result](MicroEngine.Core.GameSession.md#MicroEngine_Core_GameSession_Result).

```csharp
public bool Start(RunOptions? options = null)
```

#### Parameters

`options` [RunOptions](MicroEngine.Core.RunOptions.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

A run is in progress, or the session was made from a composition (use [Start](MicroEngine.Core.GameSession.md#MicroEngine_Core_GameSession_Start_MicroEngine_Core_RunOptions_)).

### <a id="MicroEngine_Core_GameSession_Start_MicroEngine_Hosting_LaunchRequest_System_Func_MicroEngine_Core_RunOptions_MicroEngine_Core_RunOptions__"></a> Start\(LaunchRequest, Func<RunOptions, RunOptions\>?\)

Composes and starts a new run for <code class="paramref">request</code>: its script, captures, seed and time mode follow the
launcher model ([CreateRunOptions](MicroEngine.Core.GameHost.md#MicroEngine_Core_GameHost_CreateRunOptions_MicroEngine_Core_Game_MicroEngine_Core_FrameProfiler_MicroEngine_Core_InputScript_)), its platform the composition's catalog and request.
False when that failed; the error is in [Result](MicroEngine.Core.GameSession.md#MicroEngine_Core_GameSession_Result).

```csharp
public bool Start(LaunchRequest request, Func<RunOptions, RunOptions>? change = null)
```

#### Parameters

`request` LaunchRequest

The launch.

`change` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[RunOptions](MicroEngine.Core.RunOptions.md), [RunOptions](MicroEngine.Core.RunOptions.md)\>?

Changes the run's options before the run starts (a phone host's first-frame callback), or null.

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [FormatException](https://learn.microsoft.com/dotnet/api/system.formatexception)

The request's script is invalid (nothing was started).

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

A run is in progress, or the session was made from two functions.

### <a id="MicroEngine_Core_GameSession_Stop"></a> Stop\(\)

Ends a running game (for example when the page is closing). Does nothing otherwise.

```csharp
public void Stop()
```

### <a id="MicroEngine_Core_GameSession_Tick"></a> Tick\(\)

Runs one frame. Returns false once the run has ended (see [Result](MicroEngine.Core.GameSession.md#MicroEngine_Core_GameSession_Result)).

```csharp
public bool Tick()
```

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
