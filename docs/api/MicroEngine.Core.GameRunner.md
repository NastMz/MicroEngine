# Class GameRunner

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

Runs a game to completion on platforms that own their thread (desktop, headless):
[GameLoop](MicroEngine.Core.GameLoop.md) ticked in a blocking loop.

```csharp
public static class GameRunner
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GameRunner](MicroEngine.Core.GameRunner.md)

## Methods

### <a id="MicroEngine_Core_GameRunner_Run_MicroEngine_Core_Game_MicroEngine_Core_IPlatform_MicroEngine_Core_RunOptions_"></a> Run\(Game, IPlatform, RunOptions?\)

Starts <code class="paramref">game</code> on <code class="paramref">platform</code>, runs frames until it stops, then shuts it down. Never throws for errors raised by the game: they are in [Error](MicroEngine.Core.RunResult.md#MicroEngine_Core_RunResult_Error).

```csharp
public static RunResult Run(Game game, IPlatform platform, RunOptions? options = null)
```

#### Parameters

`game` [Game](MicroEngine.Core.Game.md)

`platform` [IPlatform](MicroEngine.Core.IPlatform.md)

`options` [RunOptions](MicroEngine.Core.RunOptions.md)?

#### Returns

 [RunResult](MicroEngine.Core.RunResult.md)

#### Remarks

The platform is not disposed; its owner does that.
