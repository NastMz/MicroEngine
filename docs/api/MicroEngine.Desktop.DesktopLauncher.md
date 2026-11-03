# Class DesktopLauncher

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Desktop](MicroEngine.Desktop.md)  
Assembly: MicroEngine.Desktop.dll  
Guide: [Backends and platforms](../manual/backends.md)  

Standard entry point for desktop games: the shared launcher model ([GameHost](MicroEngine.Core.GameHost.md), the command line of
[GameLauncher](MicroEngine.Core.GameLauncher.md)) with the official desktop platforms, in order of preference: <code>sdl</code> (the default on
Windows, Linux and macOS), <code>raylib</code>, <code>sdl-software</code> (SDL's CPU renderer, for machines without a usable GPU
such as virtual machines and CI runners) and <code>headless</code>. A game's <code>Program.cs</code> is one line that hands
over its <code>Configure</code> entry point, the same one the browser and phone builds call; choosing a platform is the
player's or the test's business (<code>--platform</code>, <code>MICROENGINE_PLATFORM</code>), never the game's.

```csharp
public static class DesktopLauncher
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[DesktopLauncher](MicroEngine.Desktop.DesktopLauncher.md)

## Examples

```csharp
return DesktopLauncher.Run(MainGame.Configure, args);
```

## Methods

### <a id="MicroEngine_Desktop_DesktopLauncher_BundleContentRoot_System_String_"></a> BundleContentRoot\(string\)

The content folder of a macOS application bundle (<code>Name.app/Contents/Resources/Content</code>) when the executable
runs from <code>Contents/MacOS</code>, where a signed bundle keeps only code; null anywhere else, so the
<code>Content</code> folder next to the executable stays the default.

```csharp
public static string? BundleContentRoot(string executableDirectory)
```

#### Parameters

`executableDirectory` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Desktop_DesktopLauncher_CreateCatalog"></a> CreateCatalog\(\)

A new catalog of the official desktop platforms; add a profile of your own to offer it with <code>--platform</code>.

```csharp
public static PlatformCatalog CreateCatalog()
```

#### Returns

 PlatformCatalog

### <a id="MicroEngine_Desktop_DesktopLauncher_Defaults_MicroEngine_Hosting_HostBuilder_"></a> Defaults\(HostBuilder\)

The desktop host's defaults: the official platforms ([CreateCatalog](MicroEngine.Desktop.DesktopLauncher.md#MicroEngine_Desktop_DesktopLauncher_CreateCatalog)) and, inside a macOS application
bundle, its <code>Contents/Resources/Content</code> folder as the content ([BundleContentRoot](MicroEngine.Desktop.DesktopLauncher.md#MicroEngine_Desktop_DesktopLauncher_BundleContentRoot_System_String_)).

```csharp
public static void Defaults(HostBuilder host)
```

#### Parameters

`host` HostBuilder

### <a id="MicroEngine_Desktop_DesktopLauncher_Run_System_Action_MicroEngine_Hosting_HostBuilder__System_String___"></a> Run\(Action<HostBuilder\>, string\[\]\)

Runs the game a composition chooses (the game's <code>Configure</code> entry point, which calls
[UseGame](MicroEngine.Core.HostBuilderExtensions.md#MicroEngine_Core_HostBuilderExtensions_UseGame_MicroEngine_Hosting_HostBuilder_System_Func_MicroEngine_Core_Game__)) with the command line <code class="paramref">args</code>. The desktop host's
defaults come first ([Defaults](MicroEngine.Desktop.DesktopLauncher.md#MicroEngine_Desktop_DesktopLauncher_Defaults_MicroEngine_Hosting_HostBuilder_)); <code class="paramref">configure</code> may replace any of them.

```csharp
public static int Run(Action<HostBuilder> configure, string[] args)
```

#### Parameters

`configure` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<HostBuilder\>

`args` [string](https://learn.microsoft.com/dotnet/api/system.string)\[\]

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

The process exit code: 0 on success, 1 on a game or platform error, 2 on bad arguments.

### <a id="MicroEngine_Desktop_DesktopLauncher_Run_MicroEngine_Core_Game_System_String___"></a> Run\(Game, string\[\]\)

Runs <code class="paramref">game</code> with the arguments of [GameLauncher](MicroEngine.Core.GameLauncher.md) on the official desktop platforms; call it from <code>Program.cs</code>.

```csharp
public static int Run(Game game, string[] args)
```

#### Parameters

`game` Game

The game to run.

`args` [string](https://learn.microsoft.com/dotnet/api/system.string)\[\]

The process arguments.

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

The process exit code: 0 on success, 1 on a game or platform error, 2 on bad arguments.

### <a id="MicroEngine_Desktop_DesktopLauncher_Run_MicroEngine_Core_Game_System_String___MicroEngine_Core_PlatformCatalog_System_Func_MicroEngine_Core_PlatformRequest_MicroEngine_Core_PlatformRequest__"></a> Run\(Game, string\[\], PlatformCatalog, Func<PlatformRequest, PlatformRequest\>?\)

Runs with a catalog and request changes of the host's own (a custom platform, another audio device).

```csharp
public static int Run(Game game, string[] args, PlatformCatalog catalog, Func<PlatformRequest, PlatformRequest>? configure = null)
```

#### Parameters

`game` Game

`args` [string](https://learn.microsoft.com/dotnet/api/system.string)\[\]

`catalog` PlatformCatalog

`configure` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<PlatformRequest, PlatformRequest\>?

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

</div>
