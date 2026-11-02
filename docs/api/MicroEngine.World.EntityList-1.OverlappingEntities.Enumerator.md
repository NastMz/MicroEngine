# Struct EntityList<T\>.OverlappingEntities.Enumerator

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.World](MicroEngine.World.md)  
Assembly: MicroEngine.dll  
Guide: [Tile maps and collision](../manual/world.md)  

Walks the living entities overlapping the area.

```csharp
public struct EntityList<T>.OverlappingEntities.Enumerator : IEnumerator<T>, IEnumerator, IDisposable
```

#### Implements

[IEnumerator<T\>](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerator\-1), 
[IEnumerator](https://learn.microsoft.com/dotnet/api/system.collections.ienumerator), 
[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Properties

### <a id="MicroEngine_World_EntityList_1_OverlappingEntities_Enumerator_Current"></a> Current

The current entity.

```csharp
public readonly T Current { get; }
```

#### Property Value

 T

## Methods

### <a id="MicroEngine_World_EntityList_1_OverlappingEntities_Enumerator_Dispose"></a> Dispose\(\)

Nothing to release.

```csharp
public readonly void Dispose()
```

### <a id="MicroEngine_World_EntityList_1_OverlappingEntities_Enumerator_MoveNext"></a> MoveNext\(\)

Moves to the next living entity that overlaps the area.

```csharp
public bool MoveNext()
```

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_World_EntityList_1_OverlappingEntities_Enumerator_Reset"></a> Reset\(\)

Starts over.

```csharp
public void Reset()
```

</div>
