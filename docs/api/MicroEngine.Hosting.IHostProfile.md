# Interface IHostProfile

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Hosting](MicroEngine.Hosting.md)  
Assembly: MicroEngine.Hosting.dll  
Guide: [Composing a game for every platform](../manual/backends.md#one-composition-for-every-platform)  

A named way to build a platform that a host can choose (the engine's platform profiles implement it).

```csharp
public interface IHostProfile
```

## Properties

### <a id="MicroEngine_Hosting_IHostProfile_Name"></a> Name

The name hosts select it by, compared case-insensitively.

```csharp
string Name { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Hosting_IHostProfile_UnsupportedReason"></a> UnsupportedReason

Why it cannot run in this process, or null when it can.

```csharp
string? UnsupportedReason { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

</div>
