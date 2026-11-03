# Class BrowserLifecycle

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Raylib](MicroEngine.Raylib.md)  
Assembly: MicroEngine.Raylib.dll  
Guide: [Backends and platforms](../manual/backends.md)  

The page's visibility: a hidden page (another tab, the phone locked or switched away) suspends the game.

```csharp
public static class BrowserLifecycle
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[BrowserLifecycle](MicroEngine.Raylib.BrowserLifecycle.md)

## Properties

### <a id="MicroEngine_Raylib_BrowserLifecycle_Hidden"></a> Hidden

Set by the page from <code>visibilitychange</code>; the raylib platform reports it as a suspension ([Suspending](MicroEngine.Platform.IEngineEventSink.md#MicroEngine_Platform_IEngineEventSink_Suspending)).

```csharp
public static bool Hidden { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
