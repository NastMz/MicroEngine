# Class PlatformCatalog

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Backends and platforms](../manual/backends.md)  

The platforms a host can choose from, in order of preference: the first one that can run here is the default.
Hosts select by name (a <code>--platform</code> argument, a setting), never game code.

```csharp
public sealed class PlatformCatalog
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PlatformCatalog](MicroEngine.Core.PlatformCatalog.md)

## Constructors

### <a id="MicroEngine_Core_PlatformCatalog__ctor"></a> PlatformCatalog\(\)

```csharp
public PlatformCatalog()
```

## Properties

### <a id="MicroEngine_Core_PlatformCatalog_Default"></a> Default

The first registered profile that can run in this process.

```csharp
public PlatformProfile Default { get; }
```

#### Property Value

 [PlatformProfile](MicroEngine.Core.PlatformProfile.md)

### <a id="MicroEngine_Core_PlatformCatalog_Profiles"></a> Profiles

The registered profiles in order of preference.

```csharp
public IReadOnlyList<PlatformProfile> Profiles { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[PlatformProfile](MicroEngine.Core.PlatformProfile.md)\>

## Methods

### <a id="MicroEngine_Core_PlatformCatalog_Add_MicroEngine_Core_PlatformProfile_"></a> Add\(PlatformProfile\)

Registers a profile after the existing ones.

```csharp
public PlatformCatalog Add(PlatformProfile profile)
```

#### Parameters

`profile` [PlatformProfile](MicroEngine.Core.PlatformProfile.md)

#### Returns

 [PlatformCatalog](MicroEngine.Core.PlatformCatalog.md)

This catalog, for chaining.

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

A profile with the same name (ignoring case) is already registered.

### <a id="MicroEngine_Core_PlatformCatalog_Find_System_String_"></a> Find\(string\)

The profile named <code class="paramref">name</code> (ignoring case), or null.

```csharp
public PlatformProfile? Find(string name)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [PlatformProfile](MicroEngine.Core.PlatformProfile.md)?

### <a id="MicroEngine_Core_PlatformCatalog_Resolve_System_String_"></a> Resolve\(string?\)

The named profile, or [Default](MicroEngine.Core.PlatformCatalog.md#MicroEngine_Core_PlatformCatalog_Default) for null or empty.

```csharp
public PlatformProfile Resolve(string? name)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Returns

 [PlatformProfile](MicroEngine.Core.PlatformProfile.md)

</div>
