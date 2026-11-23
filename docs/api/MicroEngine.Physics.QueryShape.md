# Struct QueryShape

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics](../manual/physics.md)  

A shape a query asks with ([ShapeCast](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_ShapeCast_MicroEngine_Physics_QueryShape__System_Numerics_Vector2_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__MicroEngine_Physics_RayHit__), [Overlap](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Overlap_MicroEngine_RectF__MicroEngine_Physics_QueryFilter__System_Span_MicroEngine_Physics_ShapeId__)):
it is not a shape of the world and is centred where the query says. Every backend that declares
[ShapeQueries](MicroEngine.Physics.PhysicsCapabilities.md) takes the three kinds, whatever shapes its bodies may have: an
explosion's circle works on the tile backend, which has no circle shapes.

```csharp
public readonly struct QueryShape : IEquatable<QueryShape>
```

#### Implements

[IEquatable<QueryShape\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Properties

### <a id="MicroEngine_Physics_QueryShape_Kind"></a> Kind

The geometry.

```csharp
public QueryShapeKind Kind { get; }
```

#### Property Value

 [QueryShapeKind](MicroEngine.Physics.QueryShapeKind.md)

### <a id="MicroEngine_Physics_QueryShape_Radius"></a> Radius

The radius of a circle or of a capsule's ends; 0 for a box.

```csharp
public float Radius { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_QueryShape_Rounded"></a> Rounded

The shape as a rounded box: the half width and half height of its core rectangle and the radius around it (a box
has no radius, a circle no core, a capsule a core with no width).

```csharp
public (Vector2 HalfExtents, float Radius) Rounded { get; }
```

#### Property Value

 \([Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2) [HalfExtents](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.numerics.vector2,system.single\-.halfextents), [float](https://learn.microsoft.com/dotnet/api/system.single) [Radius](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.numerics.vector2,system.single\-.radius)\)

### <a id="MicroEngine_Physics_QueryShape_Size"></a> Size

A box's full width and height; a capsule's width (twice its radius) and overall height; unused by a circle.

```csharp
public Vector2 Size { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

## Methods

### <a id="MicroEngine_Physics_QueryShape_Box_System_Numerics_Vector2_"></a> Box\(Vector2\)

An axis-aligned box of <code class="paramref">size</code>.

```csharp
public static QueryShape Box(Vector2 size)
```

#### Parameters

`size` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [QueryShape](MicroEngine.Physics.QueryShape.md)

### <a id="MicroEngine_Physics_QueryShape_Capsule_System_Single_System_Single_"></a> Capsule\(float, float\)

A capsule <code class="paramref">height</code> tall overall with ends of <code class="paramref">radius</code>, standing along the world's up direction.

```csharp
public static QueryShape Capsule(float radius, float height)
```

#### Parameters

`radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

`height` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [QueryShape](MicroEngine.Physics.QueryShape.md)

### <a id="MicroEngine_Physics_QueryShape_Circle_System_Single_"></a> Circle\(float\)

A circle of <code class="paramref">radius</code>.

```csharp
public static QueryShape Circle(float radius)
```

#### Parameters

`radius` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [QueryShape](MicroEngine.Physics.QueryShape.md)

### <a id="MicroEngine_Physics_QueryShape_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_QueryShape_Equals_MicroEngine_Physics_QueryShape_"></a> Equals\(QueryShape\)

```csharp
public bool Equals(QueryShape other)
```

#### Parameters

`other` [QueryShape](MicroEngine.Physics.QueryShape.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_QueryShape_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Physics_QueryShape_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Physics_QueryShape_Validate"></a> Validate\(\)

Throws when the shape is invalid.

```csharp
public void Validate()
```

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The kind is undefined (a <code>default</code> shape is a box with no size), or a size or radius is not positive and finite.

## Operators

### <a id="MicroEngine_Physics_QueryShape_op_Equality_MicroEngine_Physics_QueryShape_MicroEngine_Physics_QueryShape_"></a> operator ==\(QueryShape, QueryShape\)

```csharp
public static bool operator ==(QueryShape left, QueryShape right)
```

#### Parameters

`left` [QueryShape](MicroEngine.Physics.QueryShape.md)

`right` [QueryShape](MicroEngine.Physics.QueryShape.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_QueryShape_op_Inequality_MicroEngine_Physics_QueryShape_MicroEngine_Physics_QueryShape_"></a> operator \!=\(QueryShape, QueryShape\)

```csharp
public static bool operator !=(QueryShape left, QueryShape right)
```

#### Parameters

`left` [QueryShape](MicroEngine.Physics.QueryShape.md)

`right` [QueryShape](MicroEngine.Physics.QueryShape.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
