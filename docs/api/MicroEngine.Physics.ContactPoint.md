# Struct ContactPoint

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics](../manual/physics.md)  

One point at which a body touches another shape now ([ContactsOf](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_ContactsOf_MicroEngine_Physics_BodyId_System_Span_MicroEngine_Physics_ContactPoint__)).

```csharp
public readonly struct ContactPoint : IEquatable<ContactPoint>
```

#### Implements

[IEquatable<ContactPoint\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Physics_ContactPoint__ctor_MicroEngine_Physics_ShapeId_MicroEngine_Physics_ShapeId_MicroEngine_Scenes_EntityId_System_Numerics_Vector2_System_Numerics_Vector2_System_Single_System_Single_"></a> ContactPoint\(ShapeId, ShapeId, EntityId, Vector2, Vector2, float, float\)

One point at which a body touches another shape now ([ContactsOf](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_ContactsOf_MicroEngine_Physics_BodyId_System_Span_MicroEngine_Physics_ContactPoint__)).

```csharp
public ContactPoint(ShapeId Shape, ShapeId Other, EntityId OtherOwner, Vector2 Point, Vector2 Normal, float Separation, float NormalImpulse)
```

#### Parameters

`Shape` [ShapeId](MicroEngine.Physics.ShapeId.md)

The body's own shape.

`Other` [ShapeId](MicroEngine.Physics.ShapeId.md)

The shape it touches.

`OtherOwner` [EntityId](MicroEngine.Scenes.EntityId.md)

The entity owning the other shape's body.

`Point` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

The contact point, a world point.

`Normal` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

The contact normal, unit length, pointing from the other shape towards the body: up for a body resting on a floor.

`Separation` [float](https://learn.microsoft.com/dotnet/api/system.single)

The distance between the two surfaces there: negative when they overlap.

`NormalImpulse` [float](https://learn.microsoft.com/dotnet/api/system.single)

The impulse the solver applied at this point along the normal in the latest step (mass × units per second): how hard the two press on each other.

## Properties

### <a id="MicroEngine_Physics_ContactPoint_Normal"></a> Normal

The contact normal, unit length, pointing from the other shape towards the body: up for a body resting on a floor.

```csharp
public Vector2 Normal { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_ContactPoint_NormalImpulse"></a> NormalImpulse

The impulse the solver applied at this point along the normal in the latest step (mass × units per second): how hard the two press on each other.

```csharp
public float NormalImpulse { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_ContactPoint_Other"></a> Other

The shape it touches.

```csharp
public ShapeId Other { get; init; }
```

#### Property Value

 [ShapeId](MicroEngine.Physics.ShapeId.md)

### <a id="MicroEngine_Physics_ContactPoint_OtherOwner"></a> OtherOwner

The entity owning the other shape's body.

```csharp
public EntityId OtherOwner { get; init; }
```

#### Property Value

 [EntityId](MicroEngine.Scenes.EntityId.md)

### <a id="MicroEngine_Physics_ContactPoint_Point"></a> Point

The contact point, a world point.

```csharp
public Vector2 Point { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_ContactPoint_Separation"></a> Separation

The distance between the two surfaces there: negative when they overlap.

```csharp
public float Separation { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_ContactPoint_Shape"></a> Shape

The body's own shape.

```csharp
public ShapeId Shape { get; init; }
```

#### Property Value

 [ShapeId](MicroEngine.Physics.ShapeId.md)

## Methods

### <a id="MicroEngine_Physics_ContactPoint_Deconstruct_MicroEngine_Physics_ShapeId__MicroEngine_Physics_ShapeId__MicroEngine_Scenes_EntityId__System_Numerics_Vector2__System_Numerics_Vector2__System_Single__System_Single__"></a> Deconstruct\(out ShapeId, out ShapeId, out EntityId, out Vector2, out Vector2, out float, out float\)

```csharp
public void Deconstruct(out ShapeId Shape, out ShapeId Other, out EntityId OtherOwner, out Vector2 Point, out Vector2 Normal, out float Separation, out float NormalImpulse)
```

#### Parameters

`Shape` [ShapeId](MicroEngine.Physics.ShapeId.md)

`Other` [ShapeId](MicroEngine.Physics.ShapeId.md)

`OtherOwner` [EntityId](MicroEngine.Scenes.EntityId.md)

`Point` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`Normal` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`Separation` [float](https://learn.microsoft.com/dotnet/api/system.single)

`NormalImpulse` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_ContactPoint_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_ContactPoint_Equals_MicroEngine_Physics_ContactPoint_"></a> Equals\(ContactPoint\)

```csharp
public bool Equals(ContactPoint other)
```

#### Parameters

`other` [ContactPoint](MicroEngine.Physics.ContactPoint.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_ContactPoint_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Physics_ContactPoint_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Physics_ContactPoint_op_Equality_MicroEngine_Physics_ContactPoint_MicroEngine_Physics_ContactPoint_"></a> operator ==\(ContactPoint, ContactPoint\)

```csharp
public static bool operator ==(ContactPoint left, ContactPoint right)
```

#### Parameters

`left` [ContactPoint](MicroEngine.Physics.ContactPoint.md)

`right` [ContactPoint](MicroEngine.Physics.ContactPoint.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_ContactPoint_op_Inequality_MicroEngine_Physics_ContactPoint_MicroEngine_Physics_ContactPoint_"></a> operator \!=\(ContactPoint, ContactPoint\)

```csharp
public static bool operator !=(ContactPoint left, ContactPoint right)
```

#### Parameters

`left` [ContactPoint](MicroEngine.Physics.ContactPoint.md)

`right` [ContactPoint](MicroEngine.Physics.ContactPoint.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
