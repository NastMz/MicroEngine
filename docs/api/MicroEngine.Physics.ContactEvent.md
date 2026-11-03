# Struct ContactEvent

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics worlds](../manual/world.md#physics-worlds)  

Two shapes began or stopped touching during the latest [Step](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Step_System_Single_); only pairs with at least
one dynamic body report them (a kinematic body's contacts come from [MoveResult](MicroEngine.Physics.MoveResult.md)).

```csharp
public readonly struct ContactEvent : IEquatable<ContactEvent>
```

#### Implements

[IEquatable<ContactEvent\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Physics_ContactEvent__ctor_MicroEngine_Physics_ContactEventKind_MicroEngine_Physics_ShapeId_MicroEngine_Physics_ShapeId_MicroEngine_Scenes_EntityId_MicroEngine_Scenes_EntityId_System_Numerics_Vector2_"></a> ContactEvent\(ContactEventKind, ShapeId, ShapeId, EntityId, EntityId, Vector2\)

Two shapes began or stopped touching during the latest [Step](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Step_System_Single_); only pairs with at least
one dynamic body report them (a kinematic body's contacts come from [MoveResult](MicroEngine.Physics.MoveResult.md)).

```csharp
public ContactEvent(ContactEventKind Kind, ShapeId A, ShapeId B, EntityId OwnerA, EntityId OwnerB, Vector2 Normal)
```

#### Parameters

`Kind` [ContactEventKind](MicroEngine.Physics.ContactEventKind.md)

Begin or end.

`A` [ShapeId](MicroEngine.Physics.ShapeId.md)

The first shape.

`B` [ShapeId](MicroEngine.Physics.ShapeId.md)

The second shape.

`OwnerA` EntityId

The entity owning <code class="paramref">A</code>'s body.

`OwnerB` EntityId

The entity owning <code class="paramref">B</code>'s body.

`Normal` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

For a begin, the contact normal, unit length, pointing from <code class="paramref">A</code> to <code class="paramref">B</code>; zero for an end.

## Properties

### <a id="MicroEngine_Physics_ContactEvent_A"></a> A

The first shape.

```csharp
public ShapeId A { get; init; }
```

#### Property Value

 [ShapeId](MicroEngine.Physics.ShapeId.md)

### <a id="MicroEngine_Physics_ContactEvent_B"></a> B

The second shape.

```csharp
public ShapeId B { get; init; }
```

#### Property Value

 [ShapeId](MicroEngine.Physics.ShapeId.md)

### <a id="MicroEngine_Physics_ContactEvent_Kind"></a> Kind

Begin or end.

```csharp
public ContactEventKind Kind { get; init; }
```

#### Property Value

 [ContactEventKind](MicroEngine.Physics.ContactEventKind.md)

### <a id="MicroEngine_Physics_ContactEvent_Normal"></a> Normal

For a begin, the contact normal, unit length, pointing from <code class="paramref">A</code> to <code class="paramref">B</code>; zero for an end.

```csharp
public Vector2 Normal { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_ContactEvent_OwnerA"></a> OwnerA

The entity owning <code class="paramref">A</code>'s body.

```csharp
public EntityId OwnerA { get; init; }
```

#### Property Value

 EntityId

### <a id="MicroEngine_Physics_ContactEvent_OwnerB"></a> OwnerB

The entity owning <code class="paramref">B</code>'s body.

```csharp
public EntityId OwnerB { get; init; }
```

#### Property Value

 EntityId

## Methods

### <a id="MicroEngine_Physics_ContactEvent_Deconstruct_MicroEngine_Physics_ContactEventKind__MicroEngine_Physics_ShapeId__MicroEngine_Physics_ShapeId__MicroEngine_Scenes_EntityId__MicroEngine_Scenes_EntityId__System_Numerics_Vector2__"></a> Deconstruct\(out ContactEventKind, out ShapeId, out ShapeId, out EntityId, out EntityId, out Vector2\)

```csharp
public void Deconstruct(out ContactEventKind Kind, out ShapeId A, out ShapeId B, out EntityId OwnerA, out EntityId OwnerB, out Vector2 Normal)
```

#### Parameters

`Kind` [ContactEventKind](MicroEngine.Physics.ContactEventKind.md)

`A` [ShapeId](MicroEngine.Physics.ShapeId.md)

`B` [ShapeId](MicroEngine.Physics.ShapeId.md)

`OwnerA` EntityId

`OwnerB` EntityId

`Normal` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_ContactEvent_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_ContactEvent_Equals_MicroEngine_Physics_ContactEvent_"></a> Equals\(ContactEvent\)

```csharp
public bool Equals(ContactEvent other)
```

#### Parameters

`other` [ContactEvent](MicroEngine.Physics.ContactEvent.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_ContactEvent_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Physics_ContactEvent_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Physics_ContactEvent_op_Equality_MicroEngine_Physics_ContactEvent_MicroEngine_Physics_ContactEvent_"></a> operator ==\(ContactEvent, ContactEvent\)

```csharp
public static bool operator ==(ContactEvent left, ContactEvent right)
```

#### Parameters

`left` [ContactEvent](MicroEngine.Physics.ContactEvent.md)

`right` [ContactEvent](MicroEngine.Physics.ContactEvent.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_ContactEvent_op_Inequality_MicroEngine_Physics_ContactEvent_MicroEngine_Physics_ContactEvent_"></a> operator \!=\(ContactEvent, ContactEvent\)

```csharp
public static bool operator !=(ContactEvent left, ContactEvent right)
```

#### Parameters

`left` [ContactEvent](MicroEngine.Physics.ContactEvent.md)

`right` [ContactEvent](MicroEngine.Physics.ContactEvent.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
