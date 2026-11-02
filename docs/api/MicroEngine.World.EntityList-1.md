# Class EntityList<T\>

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.World](MicroEngine.World.md)  
Assembly: MicroEngine.dll  
Guide: [Tile maps and collision](../manual/world.md)  

Owns entities of a kind. Additions and removals made during iteration are deferred to
the end of the update, so entities can spawn and destroy each other safely.

```csharp
public sealed class EntityList<T> : IEnumerable<T>, IEnumerable where T : Entity
```

#### Type Parameters

`T` 

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[EntityList<T\>](MicroEngine.World.EntityList\-1.md)

#### Implements

[IEnumerable<T\>](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1), 
[IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.ienumerable)

## Constructors

### <a id="MicroEngine_World_EntityList_1__ctor"></a> EntityList\(\)

```csharp
public EntityList()
```

## Properties

### <a id="MicroEngine_World_EntityList_1_Count"></a> Count

Number of entities held, including dead ones not yet flushed and additions not yet applied.

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_World_EntityList_1_Add__1___0_"></a> Add<TEntity\>\(TEntity\)

Adds an entity. During [Update](MicroEngine.World.md) the addition is deferred to the end of the update, so the new entity first updates in the next one.

```csharp
public TEntity Add<TEntity>(TEntity entity) where TEntity : T
```

#### Parameters

`entity` TEntity

#### Returns

 TEntity

<code class="paramref">entity</code>, for chaining or keeping a typed reference.

#### Type Parameters

`TEntity` 

### <a id="MicroEngine_World_EntityList_1_Clear"></a> Clear\(\)

Removes every entity at once, without calling [Removed](MicroEngine.World.Entity.md#MicroEngine_World_Entity_Removed). An entity may call it during
[Update](MicroEngine.World.md): the entities not yet updated are skipped, and those added after the call join at the end of
the update as usual.

```csharp
public void Clear()
```

### <a id="MicroEngine_World_EntityList_1_Draw_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_Camera2D_System_Single_"></a> Draw\(IRenderer, Camera2D?, float\)

Draws living entities sorted by [Depth](MicroEngine.World.Entity.md#MicroEngine_World_Entity_Depth) (stable for equal depth).

```csharp
public void Draw(IRenderer renderer, Camera2D? camera = null, float cullMargin = 32)
```

#### Parameters

`renderer` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`camera` [Camera2D](MicroEngine.Graphics.Camera2D.md)?

`cullMargin` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_World_EntityList_1_Flush"></a> Flush\(\)

Applies deferred additions and removes destroyed entities.

```csharp
public void Flush()
```

### <a id="MicroEngine_World_EntityList_1_GetEnumerator"></a> GetEnumerator\(\)

Enumerates the living entities in insertion order (deferred additions excluded until flushed). <code>foreach</code>
over the list allocates nothing.

```csharp
public EntityList<T>.Enumerator GetEnumerator()
```

#### Returns

 [EntityList](MicroEngine.World.EntityList\-1.md)<T\>.[Enumerator](MicroEngine.World.EntityList\-1.Enumerator.md)

### <a id="MicroEngine_World_EntityList_1_OfType__1"></a> OfType<TEntity\>\(\)

Living entities of type <code class="typeparamref">TEntity</code>, in insertion order; <code>foreach</code> over them allocates nothing.

```csharp
public EntityList<T>.TypedEntities<TEntity> OfType<TEntity>() where TEntity : T
```

#### Returns

 [EntityList](MicroEngine.World.EntityList\-1.md)<T\>.[TypedEntities](MicroEngine.World.EntityList\-1.TypedEntities\-1.md)<TEntity\>

#### Type Parameters

`TEntity` 

### <a id="MicroEngine_World_EntityList_1_Overlapping_MicroEngine_RectF_"></a> Overlapping\(RectF\)

Living entities whose [Bounds](MicroEngine.World.Entity.md#MicroEngine_World_Entity_Bounds) overlap <code class="paramref">area</code> (sharing an edge does not
count), in insertion order; <code>foreach</code> over them allocates nothing.

```csharp
public EntityList<T>.OverlappingEntities Overlapping(RectF area)
```

#### Parameters

`area` [RectF](MicroEngine.RectF.md)

#### Returns

 [EntityList](MicroEngine.World.EntityList\-1.md)<T\>.[OverlappingEntities](MicroEngine.World.EntityList\-1.OverlappingEntities.md)

### <a id="MicroEngine_World_EntityList_1_Update_System_Single_"></a> Update\(float\)

Updates every living entity in insertion order, then applies deferred additions and removes destroyed entities.

```csharp
public void Update(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

</div>
