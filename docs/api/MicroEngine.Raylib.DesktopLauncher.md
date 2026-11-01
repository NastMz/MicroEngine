# Class DesktopLauncher

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Raylib](MicroEngine.Raylib.md)  
Assembly: MicroEngine.Raylib.dll  
Guide: [Backends and platforms](../manual/backends.md)  

Entry point for desktop games on raylib only: [GameLauncher](MicroEngine.Core.GameLauncher.md) with the <code>raylib</code> and <code>headless</code>
platforms (see [GameLauncher](MicroEngine.Core.GameLauncher.md) for the arguments). Games that should run on the recommended platform of
each system use <code>MicroEngine.Desktop.DesktopLauncher</code> instead.

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

 [PlatformCatalog](https://github.com/NastMz/MicroEngine/blob/6aa7502bc7854ca56ec44cb5b9c7e43cd9d8563d/src/MicroEngine/Core/PlatformProfiles.cs)

### <a id="MicroEngine_Raylib_DesktopLauncher_Run_MicroEngine_Core_Game_System_String___"></a> Run\(Game, string\[\]\)

Runs <code class="paramref">game</code> with the arguments of [GameLauncher](MicroEngine.Core.GameLauncher.md); returns the process exit code.

```csharp
public static int Run(Game game, string[] args)
```

#### Parameters

`game` [Game](https://github.com/NastMz/MicroEngine/blob/6aa7502bc7854ca56ec44cb5b9c7e43cd9d8563d/src/MicroEngine/Core/Game.cs)

`args` [string](https://learn.microsoft.com/dotnet/api/system.string)\[\]

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

</div>
