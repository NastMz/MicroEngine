# Class AssetBatch

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

Assets started by [Preload](MicroEngine.Core.AssetCache.md#MicroEngine_Core_AssetCache_Preload_System_String___): progress for a loading screen, errors, and a way to wait.

```csharp
public sealed class AssetBatch
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AssetBatch](MicroEngine.Core.AssetBatch.md)

## Properties

### <a id="MicroEngine_Core_AssetBatch_Completed"></a> Completed

How many are finished (loaded or failed).

```csharp
public int Completed { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_AssetBatch_Count"></a> Count

How many assets the batch holds.

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_AssetBatch_Errors"></a> Errors

The assets that could not be loaded.

```csharp
public IReadOnlyList<AssetLoadException> Errors { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<AssetLoadException\>

### <a id="MicroEngine_Core_AssetBatch_IsDone"></a> IsDone

Whether every asset is finished.

```csharp
public bool IsDone { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_AssetBatch_Progress"></a> Progress

Finished share, from 0 to 1 (1 for an empty batch).

```csharp
public float Progress { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Core_AssetBatch_Wait"></a> Wait\(\)

Finishes every asset of the batch now, on this thread (decoding what workers have not finished).

```csharp
public void Wait()
```

#### Exceptions

 [AggregateException](https://learn.microsoft.com/dotnet/api/system.aggregateexception)

Some assets could not be loaded; [Errors](MicroEngine.Core.AssetBatch.md#MicroEngine_Core_AssetBatch_Errors) lists them.

</div>
