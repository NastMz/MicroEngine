# Class HostBuilder

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Hosting](MicroEngine.Hosting.md)  
Assembly: MicroEngine.Hosting.dll  
Guide: [Composing a game for every platform](../manual/backends.md#one-composition-for-every-platform)  

The composition of one host, configured once by the game's <code>Configure</code> entry point and reused by every host
that runs it: the desktop launcher (<code>Program.cs</code>), the browser page, the Android and iOS apps and tests. The
host fills its defaults first (its platforms, where content files are, where saves go), then calls the game's
<code>Configure</code>, which may replace any of them and adds what the game needs (the game itself, platforms of its own,
engine services). Nothing here is process-wide: two hosts in one process (an editor, parallel tests) never share a
composition.

```csharp
public sealed class HostBuilder
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[HostBuilder](MicroEngine.Hosting.HostBuilder.md)

## Examples

```csharp
public sealed class MainGame : Game
{
    public static void Configure(HostBuilder host) => host.UseGame(() => new MainGame());
    ...
}
```

## Remarks

The engine's modules keep their own parts in [Services](MicroEngine.Hosting.HostBuilder.md#MicroEngine_Hosting_HostBuilder_Services); the game façade adds methods to choose the game
and change the platform catalog and request (<code>UseGame</code>, <code>Platforms</code>, <code>ConfigureRequest</code> in
<code>MicroEngine.Core</code>).

## Constructors

### <a id="MicroEngine_Hosting_HostBuilder__ctor_MicroEngine_Hosting_HostEnvironment_MicroEngine_Hosting_LaunchRequest_"></a> HostBuilder\(HostEnvironment, LaunchRequest\)

A composition for one launch of <code class="paramref">request</code> in <code class="paramref">environment</code>.

```csharp
public HostBuilder(HostEnvironment environment, LaunchRequest request)
```

#### Parameters

`environment` [HostEnvironment](MicroEngine.Hosting.HostEnvironment.md)

`request` [LaunchRequest](MicroEngine.Hosting.LaunchRequest.md)

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

An argument is null.

## Properties

### <a id="MicroEngine_Hosting_HostBuilder_Content"></a> Content

Where the game's content files come from when the game names no folder of its own; null (the default) for the
<code>Content</code> folder next to the program. The desktop host sets a macOS bundle's resources, the Android app the
folder it unpacked its assets to, the iOS app its bundle's folder. Any source works: a folder
([FolderContentSource](MicroEngine.Platform.FolderContentSource.md)), a zip archive (<code>ZipContentSource</code>) or files a host lists and opens itself
(<code>ListedContentSource</code>).

```csharp
public IContentSource? Content { get; set; }
```

#### Property Value

 IContentSource?

### <a id="MicroEngine_Hosting_HostBuilder_EngineConfiguration"></a> EngineConfiguration

Changes applied to every engine this host composes, after the game's own <code>Game.Configure</code>.

```csharp
public IReadOnlyList<Action<EngineBuilder>> EngineConfiguration { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<EngineBuilder\>\>

### <a id="MicroEngine_Hosting_HostBuilder_Environment"></a> Environment

What the host knows about its process: its kind, developer variables and output.

```csharp
public HostEnvironment Environment { get; }
```

#### Property Value

 [HostEnvironment](MicroEngine.Hosting.HostEnvironment.md)

### <a id="MicroEngine_Hosting_HostBuilder_Kind"></a> Kind

The kind of host composing ([Kind](MicroEngine.Hosting.HostEnvironment.md#MicroEngine_Hosting_HostEnvironment_Kind)).

```csharp
public HostKind Kind { get; }
```

#### Property Value

 [HostKind](MicroEngine.Hosting.HostKind.md)

### <a id="MicroEngine_Hosting_HostBuilder_Request"></a> Request

What this launch asked for (script, captures, platform, time...).

```csharp
public LaunchRequest Request { get; }
```

#### Property Value

 [LaunchRequest](MicroEngine.Hosting.LaunchRequest.md)

### <a id="MicroEngine_Hosting_HostBuilder_Services"></a> Services

The composition's own parts, by type: the engine's modules keep theirs here; a game may add its own.

```csharp
public ServiceRegistry Services { get; }
```

#### Property Value

 ServiceRegistry

### <a id="MicroEngine_Hosting_HostBuilder_Storage"></a> Storage

The save medium for a game id, for games that ask the composition for it; null (the default) for the platform's
default (files in the user's data folder, the page's localStorage in browsers).

```csharp
public Func<string, IStorage>? Storage { get; set; }
```

#### Property Value

 [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), IStorage\>?

## Methods

### <a id="MicroEngine_Hosting_HostBuilder_ConfigureEngine_System_Action_MicroEngine_Kernel_EngineBuilder__"></a> ConfigureEngine\(Action<EngineBuilder\>\)

Adds a change to every engine this host composes (services, systems, settings), applied in the order added.

```csharp
public HostBuilder ConfigureEngine(Action<EngineBuilder> configure)
```

#### Parameters

`configure` [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<EngineBuilder\>

#### Returns

 [HostBuilder](MicroEngine.Hosting.HostBuilder.md)

This builder, for chaining.

</div>
