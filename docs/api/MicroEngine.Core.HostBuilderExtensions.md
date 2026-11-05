# Class HostBuilderExtensions

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Composing a game for every platform](../manual/backends.md#one-composition-for-every-platform)  

The game's side of a [HostBuilder](MicroEngine.Hosting.HostBuilder.md): choosing the game, the platforms a host offers and changes to the
platform request. A game's <code>Configure</code> entry point calls these; every host (desktop, browser, phones, tests)
reads them through [GameHost](MicroEngine.Core.GameHost.md).

```csharp
public static class HostBuilderExtensions
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[HostBuilderExtensions](MicroEngine.Core.HostBuilderExtensions.md)

## Methods

### <a id="MicroEngine_Core_HostBuilderExtensions_ConfigureRequest_MicroEngine_Hosting_HostBuilder_System_Func_MicroEngine_Core_PlatformRequest_MicroEngine_Core_PlatformRequest__"></a> ConfigureRequest\(HostBuilder, Func<PlatformRequest, PlatformRequest\>\)

Changes the platform request before the platform is built (an audio device or input sources of the host's own,
[PlatformRequest](MicroEngine.Core.PlatformRequest.md)); changes apply in the order added.

```csharp
public static HostBuilder ConfigureRequest(this HostBuilder host, Func<PlatformRequest, PlatformRequest> change)
```

#### Parameters

`host` [HostBuilder](MicroEngine.Hosting.HostBuilder.md)

`change` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[PlatformRequest](MicroEngine.Core.PlatformRequest.md), [PlatformRequest](MicroEngine.Core.PlatformRequest.md)\>

#### Returns

 [HostBuilder](MicroEngine.Hosting.HostBuilder.md)

The builder, for chaining.

### <a id="MicroEngine_Core_HostBuilderExtensions_Platforms_MicroEngine_Hosting_HostBuilder_"></a> Platforms\(HostBuilder\)

The platforms the host offers, in order of preference (filled with the host's own before the game's
<code>Configure</code> runs; add a profile to offer it with <code>--platform</code>).

```csharp
public static PlatformCatalog Platforms(this HostBuilder host)
```

#### Parameters

`host` [HostBuilder](MicroEngine.Hosting.HostBuilder.md)

#### Returns

 [PlatformCatalog](MicroEngine.Core.PlatformCatalog.md)

### <a id="MicroEngine_Core_HostBuilderExtensions_SaveStore_MicroEngine_Hosting_HostBuilder_System_String_"></a> SaveStore\(HostBuilder, string\)

A save store for <code class="paramref">gameId</code> on the composition's medium ([Storage](MicroEngine.Hosting.HostBuilder.md#MicroEngine_Hosting_HostBuilder_Storage)), else the
platform's default ([DefaultStorage](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_DefaultStorage_System_String_)).

```csharp
public static SaveStore SaveStore(this HostBuilder host, string gameId)
```

#### Parameters

`host` [HostBuilder](MicroEngine.Hosting.HostBuilder.md)

`gameId` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [SaveStore](MicroEngine.Core.SaveStore.md)

### <a id="MicroEngine_Core_HostBuilderExtensions_UseGame_MicroEngine_Hosting_HostBuilder_System_Func_MicroEngine_Core_Game__"></a> UseGame\(HostBuilder, Func<Game\>\)

The game this composition runs: <code class="paramref">create</code> builds a new one for every run.

```csharp
public static HostBuilder UseGame(this HostBuilder host, Func<Game> create)
```

#### Parameters

`host` [HostBuilder](MicroEngine.Hosting.HostBuilder.md)

`create` [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<[Game](MicroEngine.Core.Game.md)\>

#### Returns

 [HostBuilder](MicroEngine.Hosting.HostBuilder.md)

The builder, for chaining.

### <a id="MicroEngine_Core_HostBuilderExtensions_UsePlatforms_MicroEngine_Hosting_HostBuilder_MicroEngine_Core_PlatformCatalog_"></a> UsePlatforms\(HostBuilder, PlatformCatalog\)

Replaces the platforms the host offers.

```csharp
public static HostBuilder UsePlatforms(this HostBuilder host, PlatformCatalog catalog)
```

#### Parameters

`host` [HostBuilder](MicroEngine.Hosting.HostBuilder.md)

`catalog` [PlatformCatalog](MicroEngine.Core.PlatformCatalog.md)

#### Returns

 [HostBuilder](MicroEngine.Hosting.HostBuilder.md)

The builder, for chaining.

</div>
