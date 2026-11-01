# Class BrowserLauncher

Namespace: [MicroEngine.Raylib](MicroEngine.Raylib.md)  
Assembly: MicroEngine.Raylib.dll  
Guide: [Backends and platforms](../manual/backends.md)  

Entry point for browser builds (raylib compiled to WebAssembly). A page cannot block its only thread in
a game loop, so this returns a [GameSession](MicroEngine.Core.GameSession.md): the page starts it, ticks it once per
<code>requestAnimationFrame</code> and may start it again after the game ends. Saves go to localStorage
([BrowserSaveStorage](MicroEngine.Raylib.BrowserSaveStorage.md)); that choice is made here, before any game is built, so games
construct their [SaveStore](MicroEngine.Core.SaveStore.md) exactly as on desktop.

```csharp
public static class BrowserLauncher
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[BrowserLauncher](MicroEngine.Raylib.BrowserLauncher.md)

## Methods

### <a id="MicroEngine_Raylib_BrowserLauncher_CreateSession_System_Func_MicroEngine_Core_Game__"></a> CreateSession\(Func<Game\>\)

Creates the session a browser page drives; each start builds a new game with <code class="paramref">createGame</code> on the raylib platform.

```csharp
public static GameSession CreateSession(Func<Game> createGame)
```

#### Parameters

`createGame` [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<[Game](https://github.com/NastMz/MicroEngine/blob/4d9999002843ba740441ef7bb60461c13b2a816e/src/MicroEngine/Core/Game.cs)\>

#### Returns

 [GameSession](https://github.com/NastMz/MicroEngine/blob/4d9999002843ba740441ef7bb60461c13b2a816e/src/MicroEngine/Core/GameSession.cs)
