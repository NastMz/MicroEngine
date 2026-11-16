# Class SpatialIndex2D<T\>

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.Scene.dll  
Guide: [Scenes](../manual/scenes.md)  

Finds what is near a place: items of the game's own (entity ids, objects, numbers), each filed under a point or a
rectangle of the 2D world, asked for by rectangle or by circle without walking every item. A game creates one for
what it needs to find (enemies for the towers, neighbours for a swarm, what is in view in a world much larger than
the screen), moves an item's place when the item moves and removes it when it goes. A world has none unless the
game makes one, and the physics world is not involved: items need no body.

```csharp
public sealed class SpatialIndex2D<T>
```

#### Type Parameters

`T` 

What a query gives back: [EntityId](MicroEngine.Scenes.EntityId.md), an attachment, an index into the game's arrays.

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SpatialIndex2D<T\>](MicroEngine.Scenes.SpatialIndex2D\-1.md)

## Remarks

The rules:

<ol><li><b>Places.</b> An item is a point or a rectangle with its edges included: a rectangle query finds every item
that overlaps or touches the area, a circle query every item with a point within the radius of the centre (a point
item: its distance is at most the radius).</li><li><b>Order.</b> Results come in an order that depends only on the calls made on the index: the same additions,
moves, removals and queries give the same results in the same order on every run and machine. It is not sorted by
distance or by handle.</li><li><b>Cost.</b> The index is a grid of square cells of [CellSize](MicroEngine.Scenes.md) units: a query looks at the cells
its area reaches, so choose a cell about the size of the usual query (the radius a tower sees, the view of a
camera divided by a few). An item that covers more than [MaxCells](MicroEngine.Scenes.md) cells is kept apart and tested by
every query. A move within the item's cells changes no cell.</li><li><b>Memory.</b> Nothing is allocated by a query, a move or a removal; adding items allocates only when the
index grows.</li><li><b>Threads.</b> Not thread-safe. A query changes nothing, so several threads may query while none adds,
moves or removes.</li></ol>

## Constructors

### <a id="MicroEngine_Scenes_SpatialIndex2D_1__ctor_System_Single_"></a> SpatialIndex2D\(float\)

An empty index with cells of <code class="paramref">cellSize</code> world units a side.

```csharp
public SpatialIndex2D(float cellSize)
```

#### Parameters

`cellSize` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">cellSize</code> is not positive and finite.

## Fields

### <a id="MicroEngine_Scenes_SpatialIndex2D_1_MaxCells"></a> MaxCells

The most cells an item is filed in; a larger one is kept in a list every query tests.

```csharp
public const int MaxCells = 64
```

#### Field Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Properties

### <a id="MicroEngine_Scenes_SpatialIndex2D_1_CellSize"></a> CellSize

The side of a cell, in world units.

```csharp
public float CellSize { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Scenes_SpatialIndex2D_1_Count"></a> Count

The items in the index.

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Scenes_SpatialIndex2D_1_Add__0_System_Numerics_Vector2_"></a> Add\(T, Vector2\)

Adds <code class="paramref">item</code> at the point <code class="paramref">position</code>.

```csharp
public SpatialHandle Add(T item, Vector2 position)
```

#### Parameters

`item` T

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [SpatialHandle](MicroEngine.Scenes.SpatialHandle.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">position</code> is not finite.

### <a id="MicroEngine_Scenes_SpatialIndex2D_1_Add__0_MicroEngine_RectF__"></a> Add\(T, in RectF\)

Adds <code class="paramref">item</code> under the rectangle <code class="paramref">bounds</code>.

```csharp
public SpatialHandle Add(T item, in RectF bounds)
```

#### Parameters

`item` T

`bounds` [RectF](MicroEngine.RectF.md)

#### Returns

 [SpatialHandle](MicroEngine.Scenes.SpatialHandle.md)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">bounds</code> is not finite or has a negative size.

### <a id="MicroEngine_Scenes_SpatialIndex2D_1_BoundsOf_MicroEngine_Scenes_SpatialHandle_"></a> BoundsOf\(SpatialHandle\)

Where the item is filed: its rectangle, of size zero for a point.

```csharp
public RectF BoundsOf(SpatialHandle handle)
```

#### Parameters

`handle` [SpatialHandle](MicroEngine.Scenes.SpatialHandle.md)

#### Returns

 [RectF](MicroEngine.RectF.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The handle is stale or none.

### <a id="MicroEngine_Scenes_SpatialIndex2D_1_Clear"></a> Clear\(\)

Removes every item; every handle becomes stale. The index keeps its storage.

```csharp
public void Clear()
```

### <a id="MicroEngine_Scenes_SpatialIndex2D_1_Contains_MicroEngine_Scenes_SpatialHandle_"></a> Contains\(SpatialHandle\)

Whether <code class="paramref">handle</code> is an item of this index (added and not removed since).

```csharp
public bool Contains(SpatialHandle handle)
```

#### Parameters

`handle` [SpatialHandle](MicroEngine.Scenes.SpatialHandle.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_SpatialIndex2D_1_ItemOf_MicroEngine_Scenes_SpatialHandle_"></a> ItemOf\(SpatialHandle\)

The item added under <code class="paramref">handle</code>.

```csharp
public T ItemOf(SpatialHandle handle)
```

#### Parameters

`handle` [SpatialHandle](MicroEngine.Scenes.SpatialHandle.md)

#### Returns

 T

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The handle is stale or none.

### <a id="MicroEngine_Scenes_SpatialIndex2D_1_Move_MicroEngine_Scenes_SpatialHandle_System_Numerics_Vector2_"></a> Move\(SpatialHandle, Vector2\)

Moves the item to the point <code class="paramref">position</code>.

```csharp
public void Move(SpatialHandle handle, Vector2 position)
```

#### Parameters

`handle` [SpatialHandle](MicroEngine.Scenes.SpatialHandle.md)

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The handle is stale or none.

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">position</code> is not finite.

### <a id="MicroEngine_Scenes_SpatialIndex2D_1_Move_MicroEngine_Scenes_SpatialHandle_MicroEngine_RectF__"></a> Move\(SpatialHandle, in RectF\)

Moves the item to the rectangle <code class="paramref">bounds</code>.

```csharp
public void Move(SpatialHandle handle, in RectF bounds)
```

#### Parameters

`handle` [SpatialHandle](MicroEngine.Scenes.SpatialHandle.md)

`bounds` [RectF](MicroEngine.RectF.md)

#### Exceptions

 [InvalidOperationException](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

The handle is stale or none.

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">bounds</code> is not finite or has a negative size.

### <a id="MicroEngine_Scenes_SpatialIndex2D_1_Query_MicroEngine_RectF__System_Span__0__"></a> Query\(in RectF, Span<T\>\)

Writes into <code class="paramref">results</code> the items that overlap or touch <code class="paramref">area</code> and returns how
many do (which may exceed the span's length: only the first ones are written).

```csharp
public int Query(in RectF area, Span<T> results)
```

#### Parameters

`area` [RectF](MicroEngine.RectF.md)

`results` [Span](https://learn.microsoft.com/dotnet/api/system.span\-1)<T\>

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">area</code> is not finite or has a negative size.

### <a id="MicroEngine_Scenes_SpatialIndex2D_1_Query_MicroEngine_RectF__System_Collections_Generic_List__0__"></a> Query\(in RectF, List<T\>\)

Adds to <code class="paramref">results</code> (without clearing it) the items that overlap or touch <code class="paramref">area</code> and returns how many were added.

```csharp
public int Query(in RectF area, List<T> results)
```

#### Parameters

`area` [RectF](MicroEngine.RectF.md)

`results` [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<T\>

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">area</code> is not finite or has a negative size.

### <a id="MicroEngine_Scenes_SpatialIndex2D_1_QueryCircle_System_Numerics_Vector2_System_Single_System_Span__0__"></a> QueryCircle\(Vector2, float, Span<T\>\)

Writes into <code class="paramref">results</code> the items with a point within <code class="paramref">radius</code> of
<code class="paramref">center</code> and returns how many there are (which may exceed the span's length: only the first
ones are written).

```csharp
public int QueryCircle(Vector2 center, float radius, Span<T> results)
```

#### Parameters

`center` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

`results` [Span](https://learn.microsoft.com/dotnet/api/system.span\-1)<T\>

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">center</code> is not finite, or <code class="paramref">radius</code> is negative or not finite.

### <a id="MicroEngine_Scenes_SpatialIndex2D_1_QueryCircle_System_Numerics_Vector2_System_Single_System_Collections_Generic_List__0__"></a> QueryCircle\(Vector2, float, List<T\>\)

Adds to <code class="paramref">results</code> (without clearing it) the items with a point within <code class="paramref">radius</code> of <code class="paramref">center</code> and returns how many were added.

```csharp
public int QueryCircle(Vector2 center, float radius, List<T> results)
```

#### Parameters

`center` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

`results` [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<T\>

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">center</code> is not finite, or <code class="paramref">radius</code> is negative or not finite.

### <a id="MicroEngine_Scenes_SpatialIndex2D_1_Remove_MicroEngine_Scenes_SpatialHandle_"></a> Remove\(SpatialHandle\)

Takes the item out; false for a stale or none handle (already removed).

```csharp
public bool Remove(SpatialHandle handle)
```

#### Parameters

`handle` [SpatialHandle](MicroEngine.Scenes.SpatialHandle.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
