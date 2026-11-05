# Class BrowserLauncher

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Sdl](MicroEngine.Sdl.md)  
Assembly: MicroEngine.Sdl.dll  
Guide: [Backends and platforms](../manual/backends.md)  

Entry point for browser builds (SDL 3 compiled to WebAssembly): the shared launcher model ([GameHost](MicroEngine.Core.GameHost.md))
for a page. A page cannot block its only thread in a game loop, so this returns a [GameSession](MicroEngine.Core.GameSession.md): the page
starts it with a [LaunchRequest](MicroEngine.Hosting.LaunchRequest.md) (a script in development packages), ticks it once per
<code>requestAnimationFrame</code> and may start it again after the game ends. Every run composes the game with the same
<code>Configure</code> entry point its desktop <code>Program.cs</code> uses, so what the game configures there (platforms, request
changes, engine services) reaches the browser too; a scripted run is in deterministic time with seed 1, exactly as on
desktop and phones. Saves go to localStorage by default ([DefaultStorage](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_DefaultStorage_System_String_)).

```csharp
public static class BrowserLauncher
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[BrowserLauncher](MicroEngine.Sdl.BrowserLauncher.md)

## Methods

### <a id="MicroEngine_Sdl_BrowserLauncher_CreateSession_System_Action_MicroEngine_Hosting_HostBuilder__MicroEngine_Sdl_BrowserPage_"></a> CreateSession\(Action<HostBuilder\>, BrowserPage\)

The session a browser page drives. The host's defaults ([Defaults](MicroEngine.Sdl.BrowserLauncher.md#MicroEngine_Sdl_BrowserLauncher_Defaults_MicroEngine_Hosting_HostBuilder_MicroEngine_Sdl_BrowserPage_)) come first, then
<code class="paramref">configure</code> (the game's <code>Configure</code> entry point), for every run.

```csharp
public static GameSession CreateSession(Action<HostBuilder> configure, BrowserPage page)
```

#### Parameters

`configure` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<HostBuilder\>

The game's composition.

`page` [BrowserPage](MicroEngine.Sdl.BrowserPage.md)

The page whose touches, screen, visibility and vibration the platforms read.

#### Returns

 GameSession

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

An argument is null.

### <a id="MicroEngine_Sdl_BrowserLauncher_Defaults_MicroEngine_Hosting_HostBuilder_MicroEngine_Sdl_BrowserPage_"></a> Defaults\(HostBuilder, BrowserPage\)

The browser host's defaults: the SDL platform of <code class="paramref">page</code> as its only platform.

```csharp
public static void Defaults(HostBuilder host, BrowserPage page)
```

#### Parameters

`host` HostBuilder

`page` [BrowserPage](MicroEngine.Sdl.BrowserPage.md)

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

An argument is null.

</div>
