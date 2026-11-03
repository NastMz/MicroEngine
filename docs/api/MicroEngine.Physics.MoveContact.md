# Struct MoveContact

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics worlds](../manual/world.md#physics-worlds)  

A surface a moving body touched ([Contacts](MicroEngine.Physics.MoveResult.md#MicroEngine_Physics_MoveResult_Contacts)).

```csharp
public readonly struct MoveContact : IEquatable<MoveContact>
```

#### Implements

[IEquatable<MoveContact\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Physics_MoveContact__ctor_MicroEngine_Physics_ShapeId_MicroEngine_Scenes_EntityId_System_Numerics_Vector2_"></a> MoveContact\(ShapeId, EntityId, Vector2\)

A surface a moving body touched ([Contacts](MicroEngine.Physics.MoveResult.md#MicroEngine_Physics_MoveResult_Contacts)).

```csharp
public MoveContact(ShapeId Shape, EntityId Owner, Vector2 Normal)
```

#### Parameters

`Shape` [ShapeId](MicroEngine.Physics.ShapeId.md)

The shape touched.

`Owner` EntityId

The entity owning that shape's body.

`Normal` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

The surface normal, unit length, pointing from the surface towards the moving body.

## Properties

### <a id="MicroEngine_Physics_MoveContact_Normal"></a> Normal

The surface normal, unit length, pointing from the surface towards the moving body.

```csharp
public Vector2 Normal { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_MoveContact_Owner"></a> Owner

The entity owning that shape's body.

```csharp
public EntityId Owner { get; init; }
```

#### Property Value

 EntityId

### <a id="MicroEngine_Physics_MoveContact_Shape"></a> Shape

The shape touched.

```csharp
public ShapeId Shape { get; init; }
```

#### Property Value

 [ShapeId](MicroEngine.Physics.ShapeId.md)

## Methods

### <a id="MicroEngine_Physics_MoveContact_Deconstruct_MicroEngine_Physics_ShapeId__MicroEngine_Scenes_EntityId__System_Numerics_Vector2__"></a> Deconstruct\(out ShapeId, out EntityId, out Vector2\)

```csharp
public void Deconstruct(out ShapeId Shape, out EntityId Owner, out Vector2 Normal)
```

#### Parameters

`Shape` [ShapeId](MicroEngine.Physics.ShapeId.md)

`Owner` EntityId

`Normal` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_MoveContact_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_MoveContact_Equals_MicroEngine_Physics_MoveContact_"></a> Equals\(MoveContact\)

```csharp
public bool Equals(MoveContact other)
```

#### Parameters

`other` [MoveContact](MicroEngine.Physics.MoveContact.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_MoveContact_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Physics_MoveContact_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Physics_MoveContact_op_Equality_MicroEngine_Physics_MoveContact_MicroEngine_Physics_MoveContact_"></a> operator ==\(MoveContact, MoveContact\)

```csharp
public static bool operator ==(MoveContact left, MoveContact right)
```

#### Parameters

`left` [MoveContact](MicroEngine.Physics.MoveContact.md)

`right` [MoveContact](MicroEngine.Physics.MoveContact.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_MoveContact_op_Inequality_MicroEngine_Physics_MoveContact_MicroEngine_Physics_MoveContact_"></a> operator \!=\(MoveContact, MoveContact\)

```csharp
public static bool operator !=(MoveContact left, MoveContact right)
```

#### Parameters

`left` [MoveContact](MicroEngine.Physics.MoveContact.md)

`right` [MoveContact](MicroEngine.Physics.MoveContact.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
