# Struct QueryFilter

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics](../manual/physics.md)  

Which shapes a query ([RayCast](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_RayCast_System_Numerics_Vector2_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__MicroEngine_Physics_RayHit__), [Overlap](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Overlap_MicroEngine_RectF__MicroEngine_Physics_QueryFilter__System_Span_MicroEngine_Physics_ShapeId__)) or a
[Move](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Move_MicroEngine_Physics_BodyId_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__) considers. A shape matches when its category is in the filter's
[Mask](MicroEngine.Physics.QueryFilter.md#MicroEngine_Physics_QueryFilter_Mask) and the filter's [Category](MicroEngine.Physics.QueryFilter.md#MicroEngine_Physics_QueryFilter_Category) is in the shape's mask. <code>default</code> is
[Default](MicroEngine.Physics.QueryFilter.md#MicroEngine_Physics_QueryFilter_Default): category [Default](MicroEngine.Physics.CollisionLayers.md#MicroEngine_Physics_CollisionLayers_Default), every layer, sensors left out.

```csharp
public readonly struct QueryFilter : IEquatable<QueryFilter>
```

#### Implements

[IEquatable<QueryFilter\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Physics_QueryFilter__ctor_System_UInt64_System_UInt64_"></a> QueryFilter\(ulong, ulong\)

A filter in <code class="paramref">category</code> that sees <code class="paramref">mask</code>.

```csharp
public QueryFilter(ulong category, ulong mask)
```

#### Parameters

`category` [ulong](https://learn.microsoft.com/dotnet/api/system.uint64)

`mask` [ulong](https://learn.microsoft.com/dotnet/api/system.uint64)

## Properties

### <a id="MicroEngine_Physics_QueryFilter_Category"></a> Category

The layers the query is in (shapes whose mask lacks them ignore it).

```csharp
public ulong Category { get; init; }
```

#### Property Value

 [ulong](https://learn.microsoft.com/dotnet/api/system.uint64)

### <a id="MicroEngine_Physics_QueryFilter_Default"></a> Default

The default filter: category [Default](MicroEngine.Physics.CollisionLayers.md#MicroEngine_Physics_CollisionLayers_Default), every layer, no sensors.

```csharp
public static QueryFilter Default { get; }
```

#### Property Value

 [QueryFilter](MicroEngine.Physics.QueryFilter.md)

### <a id="MicroEngine_Physics_QueryFilter_IncludeSensors"></a> IncludeSensors

Whether ray casts and overlaps return sensor shapes (false by default). Sensors never block
[Move](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Move_MicroEngine_Physics_BodyId_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__), whatever this says.

```csharp
public bool IncludeSensors { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_QueryFilter_Mask"></a> Mask

The layers the query sees.

```csharp
public ulong Mask { get; init; }
```

#### Property Value

 [ulong](https://learn.microsoft.com/dotnet/api/system.uint64)

### <a id="MicroEngine_Physics_QueryFilter_PassOneWay"></a> PassOneWay

Whether one-way platforms are passed through (dropping down through a platform); false by default.

```csharp
public bool PassOneWay { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_QueryFilter_Where"></a> Where

Leaves single shapes out, whatever their layers: a move or a query considers a shape only when this returns
true for it (null, the default, considers every shape the layers accept). For what layers cannot say: a ray
that must skip the body that fires it, a block that is solid only for a character that comes from below. It
needs [FilterPredicates](MicroEngine.Physics.PhysicsCapabilities.md), which both of the engine's backends declare. It is
called before the move or the query runs, once per shape near it; it may read the world
([BodyOf](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_BodyOf_MicroEngine_Physics_ShapeId_), [PositionOf](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_PositionOf_MicroEngine_Physics_BodyId_),
[BoundsOf](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_BoundsOf_MicroEngine_Physics_ShapeId_)) but must not query or change it. Keep the delegate in a field: one made
in every step allocates in every step.

```csharp
public Func<ShapeId, bool>? Where { get; init; }
```

#### Property Value

 [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[ShapeId](MicroEngine.Physics.ShapeId.md), [bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>?

## Methods

### <a id="MicroEngine_Physics_QueryFilter_Accepts_System_UInt64_System_UInt64_"></a> Accepts\(ulong, ulong\)

Whether a shape in <code class="paramref">category</code> colliding with <code class="paramref">mask</code> matches the filter's layers.

```csharp
public bool Accepts(ulong category, ulong mask)
```

#### Parameters

`category` [ulong](https://learn.microsoft.com/dotnet/api/system.uint64)

`mask` [ulong](https://learn.microsoft.com/dotnet/api/system.uint64)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_QueryFilter_Considers_MicroEngine_Physics_ShapeId_"></a> Considers\(ShapeId\)

Whether [Where](MicroEngine.Physics.QueryFilter.md#MicroEngine_Physics_QueryFilter_Where) leaves <code class="paramref">shape</code> in (true without a predicate).

```csharp
public bool Considers(ShapeId shape)
```

#### Parameters

`shape` [ShapeId](MicroEngine.Physics.ShapeId.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_QueryFilter_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_QueryFilter_Equals_MicroEngine_Physics_QueryFilter_"></a> Equals\(QueryFilter\)

```csharp
public bool Equals(QueryFilter other)
```

#### Parameters

`other` [QueryFilter](MicroEngine.Physics.QueryFilter.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_QueryFilter_For_MicroEngine_Physics_ShapeDescription__"></a> For\(in ShapeDescription\)

The filter that moves or queries as <code class="paramref">shape</code> would collide: its category and mask.

```csharp
public static QueryFilter For(in ShapeDescription shape)
```

#### Parameters

`shape` [ShapeDescription](MicroEngine.Physics.ShapeDescription.md)

#### Returns

 [QueryFilter](MicroEngine.Physics.QueryFilter.md)

### <a id="MicroEngine_Physics_QueryFilter_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Physics_QueryFilter_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Physics_QueryFilter_op_Equality_MicroEngine_Physics_QueryFilter_MicroEngine_Physics_QueryFilter_"></a> operator ==\(QueryFilter, QueryFilter\)

```csharp
public static bool operator ==(QueryFilter left, QueryFilter right)
```

#### Parameters

`left` [QueryFilter](MicroEngine.Physics.QueryFilter.md)

`right` [QueryFilter](MicroEngine.Physics.QueryFilter.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_QueryFilter_op_Inequality_MicroEngine_Physics_QueryFilter_MicroEngine_Physics_QueryFilter_"></a> operator \!=\(QueryFilter, QueryFilter\)

```csharp
public static bool operator !=(QueryFilter left, QueryFilter right)
```

#### Parameters

`left` [QueryFilter](MicroEngine.Physics.QueryFilter.md)

`right` [QueryFilter](MicroEngine.Physics.QueryFilter.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
