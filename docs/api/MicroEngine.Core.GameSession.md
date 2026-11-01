# Class GameSession

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

## Constructors

### <a id="MicroEngine_Core_GameSession__ctor_System_Func_MicroEngine_Core_Game__System_Func_MicroEngine_Core_Game_MicroEngine_Core_IPlatform__"></a> GameSession\(Func<Game\>, Func<Game, IPlatform\>\)

Runs one game at a time for hosts that drive frames themselves and outlive a run, such as a browser
page: each [Start](MicroEngine.Core.GameSession.md#MicroEngine_Core_GameSession_Start_MicroEngine_Core_RunOptions_) builds a fresh game and platform, [Tick](MicroEngine.Core.GameSession.md#MicroEngine_Core_GameSession_Tick) runs one frame, and
the end of a run (quit, error, [Stop](MicroEngine.Core.GameSession.md#MicroEngine_Core_GameSession_Stop)) shuts the game down and releases the platform, so the
host can show its own screen and start again without reloading.

```csharp
public GameSession(Func<Game> createGame, Func<Game, IPlatform> createPlatform)
```

#### Parameters

`createGame` [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<[Game](MicroEngine.Core.Game.md)\>

`createPlatform` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[Game](MicroEngine.Core.Game.md), [IPlatform](MicroEngine.Core.IPlatform.md)\>

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

### <a id="MicroEngine_Core_GameSession_IsRunning"></a> IsRunning

Whether a run is in progress.

```csharp
public bool IsRunning { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_GameSession_Result"></a> Result

How the last run ended; null while a run is in progress or before the first one.

```csharp
public RunResult? Result { get; }
```

#### Property Value

 [RunResult](MicroEngine.Core.RunResult.md)?

## Methods

### <a id="MicroEngine_Core_GameSession_Start_MicroEngine_Core_RunOptions_"></a> Start\(RunOptions?\)

Builds and initializes a new game. False when that failed; the error is in [Result](MicroEngine.Core.GameSession.md#MicroEngine_Core_GameSession_Result).

```csharp
public bool Start(RunOptions? options = null)
```

#### Parameters

`options` [RunOptions](MicroEngine.Core.RunOptions.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

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
