# Struct EntityList<T\>.TypedEntities<TEntity\>.Enumerator

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.World](MicroEngine.World.md)  
Assembly: MicroEngine.dll  
Guide: [Tile maps and collision](../manual/world.md)  

Walks the living entities of type <code class="typeparamref">TEntity</code>.

```csharp
public struct EntityList<T>.TypedEntities<TEntity>.Enumerator : IEnumerator<TEntity>, IEnumerator, IDisposable
```

#### Implements

[IEnumerator<TEntity\>](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerator\-1), 
[IEnumerator](https://learn.microsoft.com/dotnet/api/system.collections.ienumerator), 
[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Properties

### <a id="MicroEngine_World_EntityList_1_TypedEntities_1_Enumerator_Current"></a> Current

The current entity.

```csharp
public readonly TEntity Current { get; }
```

#### Property Value

 TEntity

## Methods

### <a id="MicroEngine_World_EntityList_1_TypedEntities_1_Enumerator_Dispose"></a> Dispose\(\)

Nothing to release.

```csharp
public readonly void Dispose()
```

### <a id="MicroEngine_World_EntityList_1_TypedEntities_1_Enumerator_MoveNext"></a> MoveNext\(\)

Moves to the next living entity of the type.

```csharp
public bool MoveNext()
```

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_World_EntityList_1_TypedEntities_1_Enumerator_Reset"></a> Reset\(\)

Starts over.

```csharp
public void Reset()
```

</div>
