# Struct RayHit

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics worlds](../manual/world.md#physics-worlds)  

The closest shape a ray hit ([RayCast](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_RayCast_System_Numerics_Vector2_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__MicroEngine_Physics_RayHit__)).

```csharp
public readonly struct RayHit : IEquatable<RayHit>
```

#### Implements

[IEquatable<RayHit\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Physics_RayHit__ctor_MicroEngine_Physics_ShapeId_MicroEngine_Scenes_EntityId_System_Numerics_Vector2_System_Numerics_Vector2_System_Single_"></a> RayHit\(ShapeId, EntityId, Vector2, Vector2, float\)

The closest shape a ray hit ([RayCast](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_RayCast_System_Numerics_Vector2_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__MicroEngine_Physics_RayHit__)).

```csharp
public RayHit(ShapeId Shape, EntityId Owner, Vector2 Point, Vector2 Normal, float Fraction)
```

#### Parameters

`Shape` [ShapeId](MicroEngine.Physics.ShapeId.md)

The shape hit.

`Owner` [EntityId](MicroEngine.Scenes.EntityId.md)

The entity owning that shape's body.

`Point` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Where the ray entered it.

`Normal` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

The surface normal there, unit length, facing the ray's origin.

`Fraction` [float](https://learn.microsoft.com/dotnet/api/system.single)

How far along the ray, from 0 (its start) to 1 (its end).

## Properties

### <a id="MicroEngine_Physics_RayHit_Fraction"></a> Fraction

How far along the ray, from 0 (its start) to 1 (its end).

```csharp
public float Fraction { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_RayHit_Normal"></a> Normal

The surface normal there, unit length, facing the ray's origin.

```csharp
public Vector2 Normal { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_RayHit_Owner"></a> Owner

The entity owning that shape's body.

```csharp
public EntityId Owner { get; init; }
```

#### Property Value

 [EntityId](MicroEngine.Scenes.EntityId.md)

### <a id="MicroEngine_Physics_RayHit_Point"></a> Point

Where the ray entered it.

```csharp
public Vector2 Point { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_RayHit_Shape"></a> Shape

The shape hit.

```csharp
public ShapeId Shape { get; init; }
```

#### Property Value

 [ShapeId](MicroEngine.Physics.ShapeId.md)

## Methods

### <a id="MicroEngine_Physics_RayHit_Deconstruct_MicroEngine_Physics_ShapeId__MicroEngine_Scenes_EntityId__System_Numerics_Vector2__System_Numerics_Vector2__System_Single__"></a> Deconstruct\(out ShapeId, out EntityId, out Vector2, out Vector2, out float\)

```csharp
public void Deconstruct(out ShapeId Shape, out EntityId Owner, out Vector2 Point, out Vector2 Normal, out float Fraction)
```

#### Parameters

`Shape` [ShapeId](MicroEngine.Physics.ShapeId.md)

`Owner` [EntityId](MicroEngine.Scenes.EntityId.md)

`Point` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`Normal` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`Fraction` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_RayHit_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_RayHit_Equals_MicroEngine_Physics_RayHit_"></a> Equals\(RayHit\)

```csharp
public bool Equals(RayHit other)
```

#### Parameters

`other` [RayHit](MicroEngine.Physics.RayHit.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_RayHit_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Physics_RayHit_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Physics_RayHit_op_Equality_MicroEngine_Physics_RayHit_MicroEngine_Physics_RayHit_"></a> operator ==\(RayHit, RayHit\)

```csharp
public static bool operator ==(RayHit left, RayHit right)
```

#### Parameters

`left` [RayHit](MicroEngine.Physics.RayHit.md)

`right` [RayHit](MicroEngine.Physics.RayHit.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_RayHit_op_Inequality_MicroEngine_Physics_RayHit_MicroEngine_Physics_RayHit_"></a> operator \!=\(RayHit, RayHit\)

```csharp
public static bool operator !=(RayHit left, RayHit right)
```

#### Parameters

`left` [RayHit](MicroEngine.Physics.RayHit.md)

`right` [RayHit](MicroEngine.Physics.RayHit.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
