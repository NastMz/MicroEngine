# Struct SaveLoadResult<T\>

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.Saves.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

The outcome of loading a versioned save: the value when there is one, and otherwise why there is none.

```csharp
public readonly struct SaveLoadResult<T> where T : class
```

#### Type Parameters

`T` 

The saved type.

## Properties

### <a id="MicroEngine_Core_SaveLoadResult_1_Error"></a> Error

What went wrong, for [Corrupt](MicroEngine.Core.SaveLoadStatus.md) and [MigrationFailed](MicroEngine.Core.SaveLoadStatus.md) (to log); null otherwise.

```csharp
public Exception? Error { get; }
```

#### Property Value

 [Exception](https://learn.microsoft.com/dotnet/api/system.exception)?

### <a id="MicroEngine_Core_SaveLoadResult_1_Protected"></a> Protected

True when the slot holds a save this build could not use and must not replace ([TooNew](MicroEngine.Core.SaveLoadStatus.md),
[MigrationFailed](MicroEngine.Core.SaveLoadStatus.md), [Unreadable](MicroEngine.Core.SaveLoadStatus.md)): the store returns false
from every save to that slot until the game deletes it.

```csharp
public bool Protected { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_SaveLoadResult_1_Status"></a> Status

What the slot held.

```csharp
public SaveLoadStatus Status { get; }
```

#### Property Value

 [SaveLoadStatus](MicroEngine.Core.SaveLoadStatus.md)

### <a id="MicroEngine_Core_SaveLoadResult_1_Value"></a> Value

The save in its current shape, for [Loaded](MicroEngine.Core.SaveLoadStatus.md) and [Migrated](MicroEngine.Core.SaveLoadStatus.md); null otherwise.

```csharp
public T? Value { get; }
```

#### Property Value

 T?

### <a id="MicroEngine_Core_SaveLoadResult_1_Version"></a> Version

The version the save was written in: the older version for [Migrated](MicroEngine.Core.SaveLoadStatus.md) and
[MigrationFailed](MicroEngine.Core.SaveLoadStatus.md), the newer one for [TooNew](MicroEngine.Core.SaveLoadStatus.md), the current
one for [Loaded](MicroEngine.Core.SaveLoadStatus.md). 0 when no version was read (missing, corrupt, unreadable).

```csharp
public int Version { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

</div>
