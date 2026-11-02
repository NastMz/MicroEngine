# Struct EntityList<T\>.TypedEntities<TEntity\>

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.World](MicroEngine.World.md)  
Assembly: MicroEngine.dll  
Guide: [Tile maps and collision](../manual/world.md)  

The living entities of one type ([OfType``1](MicroEngine.World.md)), walked without allocating.

```csharp
public readonly struct EntityList<T>.TypedEntities<TEntity> : IEnumerable<TEntity>, IEnumerable where TEntity : T
```

#### Type Parameters

`TEntity` 

#### Implements

[IEnumerable<TEntity\>](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1), 
[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.ienumerable)

## Methods

### <a id="MicroEngine_World_EntityList_1_TypedEntities_1_GetEnumerator"></a> GetEnumerator\(\)

Starts a walk.

```csharp
public EntityList<T>.TypedEntities<TEntity>.Enumerator GetEnumerator()
```

#### Returns

 [EntityList](MicroEngine.World.EntityList\-1.md)<T\>.[TypedEntities](MicroEngine.World.EntityList\-1.TypedEntities\-1.md)<TEntity\>.[Enumerator](MicroEngine.World.EntityList\-1.TypedEntities\-1.Enumerator.md)

</div>
