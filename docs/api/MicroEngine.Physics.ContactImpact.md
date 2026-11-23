# Struct ContactImpact

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics worlds](../manual/world.md#physics-worlds)  

Two shapes hit each other during the latest [Step](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Step_System_Single_) faster than the backend's impact
speed ([ContactDetail](MicroEngine.Physics.PhysicsCapabilities.md)): what an impact sound or damage needs. Pairs with a dynamic
body only; a pair that keeps touching reports again whenever it hits again (a bouncing or rolling body).

```csharp
public readonly struct ContactImpact : IEquatable<ContactImpact>
```

#### Implements

[IEquatable<ContactImpact\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Physics_ContactImpact__ctor_MicroEngine_Physics_ShapeId_MicroEngine_Physics_ShapeId_MicroEngine_Scenes_EntityId_MicroEngine_Scenes_EntityId_System_Numerics_Vector2_System_Numerics_Vector2_System_Single_System_Single_"></a> ContactImpact\(ShapeId, ShapeId, EntityId, EntityId, Vector2, Vector2, float, float\)

Two shapes hit each other during the latest [Step](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Step_System_Single_) faster than the backend's impact
speed ([ContactDetail](MicroEngine.Physics.PhysicsCapabilities.md)): what an impact sound or damage needs. Pairs with a dynamic
body only; a pair that keeps touching reports again whenever it hits again (a bouncing or rolling body).

```csharp
public ContactImpact(ShapeId A, ShapeId B, EntityId OwnerA, EntityId OwnerB, Vector2 Point, Vector2 Normal, float Speed, float Impulse)
```

#### Parameters

`A` [ShapeId](MicroEngine.Physics.ShapeId.md)

The first shape.

`B` [ShapeId](MicroEngine.Physics.ShapeId.md)

The second shape.

`OwnerA` [EntityId](MicroEngine.Scenes.EntityId.md)

The entity owning <code class="paramref">A</code>'s body.

`OwnerB` [EntityId](MicroEngine.Scenes.EntityId.md)

The entity owning <code class="paramref">B</code>'s body.

`Point` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Where they hit, a world point between the two surfaces.

`Normal` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

The contact normal, unit length, pointing from <code class="paramref">A</code> to <code class="paramref">B</code>.

`Speed` [float](https://learn.microsoft.com/dotnet/api/system.single)

How fast the two surfaces approached each other along the normal, in units per second; always positive.

`Impulse` [float](https://learn.microsoft.com/dotnet/api/system.single)

The impulse the contact applied along the normal in that step (mass × units per second): it grows with the masses
as well as the speed, so a heavy crate hits harder than a light one at the same speed. 0 when the contact no longer
exists.

## Properties

### <a id="MicroEngine_Physics_ContactImpact_A"></a> A

The first shape.

```csharp
public ShapeId A { get; init; }
```

#### Property Value

 [ShapeId](MicroEngine.Physics.ShapeId.md)

### <a id="MicroEngine_Physics_ContactImpact_B"></a> B

The second shape.

```csharp
public ShapeId B { get; init; }
```

#### Property Value

 [ShapeId](MicroEngine.Physics.ShapeId.md)

### <a id="MicroEngine_Physics_ContactImpact_Impulse"></a> Impulse

The impulse the contact applied along the normal in that step (mass × units per second): it grows with the masses
as well as the speed, so a heavy crate hits harder than a light one at the same speed. 0 when the contact no longer
exists.

```csharp
public float Impulse { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_ContactImpact_Normal"></a> Normal

The contact normal, unit length, pointing from <code class="paramref">A</code> to <code class="paramref">B</code>.

```csharp
public Vector2 Normal { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_ContactImpact_OwnerA"></a> OwnerA

The entity owning <code class="paramref">A</code>'s body.

```csharp
public EntityId OwnerA { get; init; }
```

#### Property Value

 [EntityId](MicroEngine.Scenes.EntityId.md)

### <a id="MicroEngine_Physics_ContactImpact_OwnerB"></a> OwnerB

The entity owning <code class="paramref">B</code>'s body.

```csharp
public EntityId OwnerB { get; init; }
```

#### Property Value

 [EntityId](MicroEngine.Scenes.EntityId.md)

### <a id="MicroEngine_Physics_ContactImpact_Point"></a> Point

Where they hit, a world point between the two surfaces.

```csharp
public Vector2 Point { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_ContactImpact_Speed"></a> Speed

How fast the two surfaces approached each other along the normal, in units per second; always positive.

```csharp
public float Speed { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Physics_ContactImpact_Deconstruct_MicroEngine_Physics_ShapeId__MicroEngine_Physics_ShapeId__MicroEngine_Scenes_EntityId__MicroEngine_Scenes_EntityId__System_Numerics_Vector2__System_Numerics_Vector2__System_Single__System_Single__"></a> Deconstruct\(out ShapeId, out ShapeId, out EntityId, out EntityId, out Vector2, out Vector2, out float, out float\)

```csharp
public void Deconstruct(out ShapeId A, out ShapeId B, out EntityId OwnerA, out EntityId OwnerB, out Vector2 Point, out Vector2 Normal, out float Speed, out float Impulse)
```

#### Parameters

`A` [ShapeId](MicroEngine.Physics.ShapeId.md)

`B` [ShapeId](MicroEngine.Physics.ShapeId.md)

`OwnerA` [EntityId](MicroEngine.Scenes.EntityId.md)

`OwnerB` [EntityId](MicroEngine.Scenes.EntityId.md)

`Point` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`Normal` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`Speed` [float](https://learn.microsoft.com/dotnet/api/system.single)

`Impulse` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_ContactImpact_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_ContactImpact_Equals_MicroEngine_Physics_ContactImpact_"></a> Equals\(ContactImpact\)

```csharp
public bool Equals(ContactImpact other)
```

#### Parameters

`other` [ContactImpact](MicroEngine.Physics.ContactImpact.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_ContactImpact_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Physics_ContactImpact_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Physics_ContactImpact_op_Equality_MicroEngine_Physics_ContactImpact_MicroEngine_Physics_ContactImpact_"></a> operator ==\(ContactImpact, ContactImpact\)

```csharp
public static bool operator ==(ContactImpact left, ContactImpact right)
```

#### Parameters

`left` [ContactImpact](MicroEngine.Physics.ContactImpact.md)

`right` [ContactImpact](MicroEngine.Physics.ContactImpact.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_ContactImpact_op_Inequality_MicroEngine_Physics_ContactImpact_MicroEngine_Physics_ContactImpact_"></a> operator \!=\(ContactImpact, ContactImpact\)

```csharp
public static bool operator !=(ContactImpact left, ContactImpact right)
```

#### Parameters

`left` [ContactImpact](MicroEngine.Physics.ContactImpact.md)

`right` [ContactImpact](MicroEngine.Physics.ContactImpact.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
