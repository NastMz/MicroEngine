# Class BrowserLifecycle

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

Set by the page from <code>visibilitychange</code>; the raylib platform reports it as [Suspended](MicroEngine.Core.IPlatform.md#MicroEngine_Core_IPlatform_Suspended).

```csharp
public static bool Hidden { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)
