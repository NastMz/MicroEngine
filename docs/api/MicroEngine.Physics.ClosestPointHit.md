# Struct ClosestPointHit

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics](../manual/physics.md)  

The surface nearest a point ([ClosestPoint](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_ClosestPoint_System_Numerics_Vector2_System_Single_MicroEngine_Physics_QueryFilter__MicroEngine_Physics_ClosestPointHit__)).

```csharp
public readonly struct ClosestPointHit : IEquatable<ClosestPointHit>
```

#### Implements

[IEquatable<ClosestPointHit\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Physics_ClosestPointHit__ctor_MicroEngine_Physics_ShapeId_MicroEngine_Scenes_EntityId_System_Numerics_Vector2_System_Single_"></a> ClosestPointHit\(ShapeId, EntityId, Vector2, float\)

The surface nearest a point ([ClosestPoint](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_ClosestPoint_System_Numerics_Vector2_System_Single_MicroEngine_Physics_QueryFilter__MicroEngine_Physics_ClosestPointHit__)).

```csharp
public ClosestPointHit(ShapeId Shape, EntityId Owner, Vector2 Point, float Distance)
```

#### Parameters

`Shape` [ShapeId](MicroEngine.Physics.ShapeId.md)

The nearest shape.

`Owner` [EntityId](MicroEngine.Scenes.EntityId.md)

The entity owning that shape's body.

`Point` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

The point of the shape nearest the one asked about; the point itself when it is inside the shape.

`Distance` [float](https://learn.microsoft.com/dotnet/api/system.single)

How far that is; 0 inside the shape.

## Properties

### <a id="MicroEngine_Physics_ClosestPointHit_Distance"></a> Distance

How far that is; 0 inside the shape.

```csharp
public float Distance { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_ClosestPointHit_Owner"></a> Owner

The entity owning that shape's body.

```csharp
public EntityId Owner { get; init; }
```

#### Property Value

 [EntityId](MicroEngine.Scenes.EntityId.md)

### <a id="MicroEngine_Physics_ClosestPointHit_Point"></a> Point

The point of the shape nearest the one asked about; the point itself when it is inside the shape.

```csharp
public Vector2 Point { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_ClosestPointHit_Shape"></a> Shape

The nearest shape.

```csharp
public ShapeId Shape { get; init; }
```

#### Property Value

 [ShapeId](MicroEngine.Physics.ShapeId.md)

## Methods

### <a id="MicroEngine_Physics_ClosestPointHit_Deconstruct_MicroEngine_Physics_ShapeId__MicroEngine_Scenes_EntityId__System_Numerics_Vector2__System_Single__"></a> Deconstruct\(out ShapeId, out EntityId, out Vector2, out float\)

```csharp
public void Deconstruct(out ShapeId Shape, out EntityId Owner, out Vector2 Point, out float Distance)
```

#### Parameters

`Shape` [ShapeId](MicroEngine.Physics.ShapeId.md)

`Owner` [EntityId](MicroEngine.Scenes.EntityId.md)

`Point` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`Distance` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_ClosestPointHit_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_ClosestPointHit_Equals_MicroEngine_Physics_ClosestPointHit_"></a> Equals\(ClosestPointHit\)

```csharp
public bool Equals(ClosestPointHit other)
```

#### Parameters

`other` [ClosestPointHit](MicroEngine.Physics.ClosestPointHit.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_ClosestPointHit_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Physics_ClosestPointHit_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Physics_ClosestPointHit_op_Equality_MicroEngine_Physics_ClosestPointHit_MicroEngine_Physics_ClosestPointHit_"></a> operator ==\(ClosestPointHit, ClosestPointHit\)

```csharp
public static bool operator ==(ClosestPointHit left, ClosestPointHit right)
```

#### Parameters

`left` [ClosestPointHit](MicroEngine.Physics.ClosestPointHit.md)

`right` [ClosestPointHit](MicroEngine.Physics.ClosestPointHit.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_ClosestPointHit_op_Inequality_MicroEngine_Physics_ClosestPointHit_MicroEngine_Physics_ClosestPointHit_"></a> operator \!=\(ClosestPointHit, ClosestPointHit\)

```csharp
public static bool operator !=(ClosestPointHit left, ClosestPointHit right)
```

#### Parameters

`left` [ClosestPointHit](MicroEngine.Physics.ClosestPointHit.md)

`right` [ClosestPointHit](MicroEngine.Physics.ClosestPointHit.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
