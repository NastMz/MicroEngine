# Class GameLoop

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

The fixed-timestep loop, one frame per [Tick](MicroEngine.Core.GameLoop.md#MicroEngine_Core_GameLoop_Tick): input is sampled once per step, the
simulation advances in exact steps (at most five per frame to avoid a spiral of death), and
rendering happens once per frame. Headless platforms advance exactly one step per frame, which
makes scripted runs fully deterministic. Hosts that cannot block, such as a browser driving frames
from <code>requestAnimationFrame</code>, call [Tick](MicroEngine.Core.GameLoop.md#MicroEngine_Core_GameLoop_Tick) themselves; errors are captured, never thrown.

```csharp
public sealed class GameLoop
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[GameLoop](MicroEngine.Core.GameLoop.md)

## Constructors

### <a id="MicroEngine_Core_GameLoop__ctor_MicroEngine_Core_Game_MicroEngine_Core_IPlatform_MicroEngine_Core_RunOptions_"></a> GameLoop\(Game, IPlatform, RunOptions?\)

Prepares a loop; nothing runs until [Start](MicroEngine.Core.GameLoop.md#MicroEngine_Core_GameLoop_Start).

```csharp
public GameLoop(Game game, IPlatform platform, RunOptions? options = null)
```

#### Parameters

`game` [Game](MicroEngine.Core.Game.md)

`platform` [IPlatform](MicroEngine.Core.IPlatform.md)

`options` [RunOptions](MicroEngine.Core.RunOptions.md)?

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">game</code> or <code class="paramref">platform</code> is null.

## Properties

### <a id="MicroEngine_Core_GameLoop_Frames"></a> Frames

Frames rendered so far.

```csharp
public long Frames { get; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Core_GameLoop_IsRunning"></a> IsRunning

Whether the next [Tick](MicroEngine.Core.GameLoop.md#MicroEngine_Core_GameLoop_Tick) will run a frame.

```csharp
public bool IsRunning { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_Core_GameLoop_Finish"></a> Finish\(\)

Shuts the game down (once) and reports the run.

```csharp
public RunResult Finish()
```

#### Returns

 [RunResult](MicroEngine.Core.RunResult.md)

### <a id="MicroEngine_Core_GameLoop_Start"></a> Start\(\)

Initializes the game; false when loading failed (the error is in [Finish](MicroEngine.Core.GameLoop.md#MicroEngine_Core_GameLoop_Finish)).

```csharp
public bool Start()
```

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_GameLoop_Tick"></a> Tick\(\)

Runs one frame. Returns false once the game quit, the platform closed, a limit was hit or an error occurred.

```csharp
public bool Tick()
```

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
