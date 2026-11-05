# Class DesktopLauncher

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Raylib](MicroEngine.Raylib.md)  
Assembly: MicroEngine.Raylib.dll  
Guide: [Backends and platforms](../manual/backends.md)  

Entry point for desktop games on raylib only: the shared launcher model ([GameHost](MicroEngine.Core.GameHost.md), the arguments of
[GameLauncher](MicroEngine.Core.GameLauncher.md)) with the <code>raylib</code> and <code>headless</code> platforms. Games that should run on the
recommended platform of each system use <code>MicroEngine.Desktop.DesktopLauncher</code> instead.

```csharp
public static class DesktopLauncher
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[DesktopLauncher](MicroEngine.Raylib.DesktopLauncher.md)

## Methods

### <a id="MicroEngine_Raylib_DesktopLauncher_CreateCatalog"></a> CreateCatalog\(\)

A new catalog with the <code>raylib</code> and <code>headless</code> platforms.

```csharp
public static PlatformCatalog CreateCatalog()
```

#### Returns

 [PlatformCatalog](MicroEngine.Core.PlatformCatalog.md)

### <a id="MicroEngine_Raylib_DesktopLauncher_Run_System_Action_MicroEngine_Hosting_HostBuilder__System_String___"></a> Run\(Action<HostBuilder\>, string\[\]\)

Runs the game a composition chooses (the game's <code>Configure</code> entry point); returns the process exit code.

```csharp
public static int Run(Action<HostBuilder> configure, string[] args)
```

#### Parameters

`configure` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[HostBuilder](MicroEngine.Hosting.HostBuilder.md)\>

`args` [string](https://learn.microsoft.com/dotnet/api/system.string)\[\]

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Raylib_DesktopLauncher_Run_MicroEngine_Core_Game_System_String___"></a> Run\(Game, string\[\]\)

Runs <code class="paramref">game</code> with the arguments of [GameLauncher](MicroEngine.Core.GameLauncher.md); returns the process exit code.

```csharp
public static int Run(Game game, string[] args)
```

#### Parameters

`game` [Game](MicroEngine.Core.Game.md)

`args` [string](https://learn.microsoft.com/dotnet/api/system.string)\[\]

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

</div>
