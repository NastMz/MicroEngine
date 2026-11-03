# Struct MoveResult

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics worlds](../manual/world.md#physics-worlds)  

What [Move](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Move_MicroEngine_Physics_BodyId_System_Numerics_Vector2_MicroEngine_Physics_QueryFilter__) did: where the body ended, how far it went, and every surface it was stopped
by or touches at the end (within the backend's [Skin](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Skin)), each one with its normal. A
kinematic body's contacts and its ground come from here, never from [ContactEvents](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_ContactEvents).

```csharp
public readonly struct MoveResult
```

## Constructors

### <a id="MicroEngine_Physics_MoveResult__ctor_System_Numerics_Vector2_System_Numerics_Vector2_System_Numerics_Vector2_System_ReadOnlySpan_MicroEngine_Physics_MoveContact__"></a> MoveResult\(Vector2, Vector2, Vector2, ReadOnlySpan<MoveContact\>\)

Creates a result from the contacts the backend found (the first [MaxContacts](MicroEngine.Physics.MoveResult.md#MicroEngine_Physics_MoveResult_MaxContacts) are kept).

```csharp
public MoveResult(Vector2 position, Vector2 delta, Vector2 up, ReadOnlySpan<MoveContact> contacts)
```

#### Parameters

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

The body's origin after the move.

`delta` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

The displacement actually applied.

`up` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

The world's up direction, which tells ground, walls and ceilings apart.

`contacts` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[MoveContact](MicroEngine.Physics.MoveContact.md)\>

The contacts, in the order the backend found them.

## Fields

### <a id="MicroEngine_Physics_MoveResult_GroundThreshold"></a> GroundThreshold

The smallest dot product of a contact's normal with [Up](MicroEngine.Physics.IPhysicsWorld2D.md#MicroEngine_Physics_IPhysicsWorld2D_Up) that counts as ground (about
45 degrees); a dot product of at most its negation is a ceiling, anything between is a wall.

```csharp
public const float GroundThreshold = 0.7
```

#### Field Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Physics_MoveResult_MaxContacts"></a> MaxContacts

The most contacts one result holds; further ones are counted in [TotalContacts](MicroEngine.Physics.MoveResult.md#MicroEngine_Physics_MoveResult_TotalContacts) but not kept.

```csharp
public const int MaxContacts = 8
```

#### Field Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Properties

### <a id="MicroEngine_Physics_MoveResult_Contacts"></a> Contacts

The contacts kept, in the order the backend found them.

```csharp
[UnscopedRef]
public ReadOnlySpan<MoveContact> Contacts { get; }
```

#### Property Value

 [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[MoveContact](MicroEngine.Physics.MoveContact.md)\>

### <a id="MicroEngine_Physics_MoveResult_Delta"></a> Delta

The displacement actually applied (the requested one when nothing was in the way).

```csharp
public Vector2 Delta { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_MoveResult_Ground"></a> Ground

The ground shape ([GroundNormal](MicroEngine.Physics.MoveResult.md#MicroEngine_Physics_MoveResult_GroundNormal)'s); [None](MicroEngine.Physics.ShapeId.md#MicroEngine_Physics_ShapeId_None) when not on ground.

```csharp
public ShapeId Ground { get; }
```

#### Property Value

 [ShapeId](MicroEngine.Physics.ShapeId.md)

### <a id="MicroEngine_Physics_MoveResult_GroundNormal"></a> GroundNormal

The most upward ground normal; zero when [OnGround](MicroEngine.Physics.MoveResult.md#MicroEngine_Physics_MoveResult_OnGround) is false.

```csharp
public Vector2 GroundNormal { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_MoveResult_HitCeiling"></a> HitCeiling

Whether a contact is a ceiling (its normal points down).

```csharp
public bool HitCeiling { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_MoveResult_HitWall"></a> HitWall

Whether a contact is a wall (neither ground nor ceiling).

```csharp
public bool HitWall { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_MoveResult_OnGround"></a> OnGround

Whether a contact is ground: its normal points up within [GroundThreshold](MicroEngine.Physics.MoveResult.md#MicroEngine_Physics_MoveResult_GroundThreshold).

```csharp
public bool OnGround { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_MoveResult_Position"></a> Position

The body's origin after the move.

```csharp
public Vector2 Position { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Physics_MoveResult_TotalContacts"></a> TotalContacts

How many contacts the backend found; [Contacts](MicroEngine.Physics.MoveResult.md#MicroEngine_Physics_MoveResult_Contacts) holds at most [MaxContacts](MicroEngine.Physics.MoveResult.md#MicroEngine_Physics_MoveResult_MaxContacts) of them.

```csharp
public int TotalContacts { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Physics_MoveResult_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

</div>
