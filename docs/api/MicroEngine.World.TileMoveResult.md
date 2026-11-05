# Struct TileMoveResult

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.World](MicroEngine.World.md)  
Assembly: MicroEngine.Physics.Tiles.dll  
Guide: [Tile maps and collision](../manual/world.md)  

What happened during a [Move](MicroEngine.World.TileMotion.md#MicroEngine_World_TileMotion_Move_MicroEngine_RectF_System_Numerics_Vector2_MicroEngine_World_ICollisionGrid_System_Collections_Generic_IReadOnlyList_MicroEngine_RectF__System_Boolean_). A body of the physics contract reports its move in
[MoveResult](MicroEngine.Physics.MoveResult.md) instead.

```csharp
public readonly struct TileMoveResult : IEquatable<TileMoveResult>
```

#### Implements

[IEquatable<TileMoveResult\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_World_TileMoveResult__ctor_MicroEngine_RectF_System_Boolean_System_Boolean_System_Boolean_System_Boolean_MicroEngine_World_TileFlags_"></a> TileMoveResult\(RectF, bool, bool, bool, bool, TileFlags\)

What happened during a [Move](MicroEngine.World.TileMotion.md#MicroEngine_World_TileMotion_Move_MicroEngine_RectF_System_Numerics_Vector2_MicroEngine_World_ICollisionGrid_System_Collections_Generic_IReadOnlyList_MicroEngine_RectF__System_Boolean_). A body of the physics contract reports its move in
[MoveResult](MicroEngine.Physics.MoveResult.md) instead.

```csharp
public TileMoveResult(RectF Bounds, bool HitLeft, bool HitRight, bool HitCeiling, bool HitFloor, TileFlags Touched)
```

#### Parameters

`Bounds` [RectF](MicroEngine.RectF.md)

The box after the move.

`HitLeft` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Movement to the left was stopped.

`HitRight` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Movement to the right was stopped.

`HitCeiling` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Upward movement was stopped.

`HitFloor` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Downward movement was stopped (the body landed).

`Touched` [TileFlags](MicroEngine.World.TileFlags.md)

Flags of every tile the box overlaps at the end (hazards, water...).

## Properties

### <a id="MicroEngine_World_TileMoveResult_Bounds"></a> Bounds

The box after the move.

```csharp
public RectF Bounds { get; init; }
```

#### Property Value

 [RectF](MicroEngine.RectF.md)

### <a id="MicroEngine_World_TileMoveResult_HitCeiling"></a> HitCeiling

Upward movement was stopped.

```csharp
public bool HitCeiling { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_World_TileMoveResult_HitFloor"></a> HitFloor

Downward movement was stopped (the body landed).

```csharp
public bool HitFloor { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_World_TileMoveResult_HitLeft"></a> HitLeft

Movement to the left was stopped.

```csharp
public bool HitLeft { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_World_TileMoveResult_HitRight"></a> HitRight

Movement to the right was stopped.

```csharp
public bool HitRight { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_World_TileMoveResult_HitWall"></a> HitWall

Whether horizontal movement was stopped on either side.

```csharp
public bool HitWall { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_World_TileMoveResult_Position"></a> Position

The top-left of [Bounds](MicroEngine.World.TileMoveResult.md#MicroEngine_World_TileMoveResult_Bounds).

```csharp
public Vector2 Position { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_World_TileMoveResult_Touched"></a> Touched

Flags of every tile the box overlaps at the end (hazards, water...).

```csharp
public TileFlags Touched { get; init; }
```

#### Property Value

 [TileFlags](MicroEngine.World.TileFlags.md)

## Methods

### <a id="MicroEngine_World_TileMoveResult_Deconstruct_MicroEngine_RectF__System_Boolean__System_Boolean__System_Boolean__System_Boolean__MicroEngine_World_TileFlags__"></a> Deconstruct\(out RectF, out bool, out bool, out bool, out bool, out TileFlags\)

```csharp
public void Deconstruct(out RectF Bounds, out bool HitLeft, out bool HitRight, out bool HitCeiling, out bool HitFloor, out TileFlags Touched)
```

#### Parameters

`Bounds` [RectF](MicroEngine.RectF.md)

`HitLeft` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`HitRight` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`HitCeiling` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`HitFloor` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`Touched` [TileFlags](MicroEngine.World.TileFlags.md)

### <a id="MicroEngine_World_TileMoveResult_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_World_TileMoveResult_Equals_MicroEngine_World_TileMoveResult_"></a> Equals\(TileMoveResult\)

```csharp
public bool Equals(TileMoveResult other)
```

#### Parameters

`other` [TileMoveResult](MicroEngine.World.TileMoveResult.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_World_TileMoveResult_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_World_TileMoveResult_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_World_TileMoveResult_op_Equality_MicroEngine_World_TileMoveResult_MicroEngine_World_TileMoveResult_"></a> operator ==\(TileMoveResult, TileMoveResult\)

```csharp
public static bool operator ==(TileMoveResult left, TileMoveResult right)
```

#### Parameters

`left` [TileMoveResult](MicroEngine.World.TileMoveResult.md)

`right` [TileMoveResult](MicroEngine.World.TileMoveResult.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_World_TileMoveResult_op_Inequality_MicroEngine_World_TileMoveResult_MicroEngine_World_TileMoveResult_"></a> operator \!=\(TileMoveResult, TileMoveResult\)

```csharp
public static bool operator !=(TileMoveResult left, TileMoveResult right)
```

#### Parameters

`left` [TileMoveResult](MicroEngine.World.TileMoveResult.md)

`right` [TileMoveResult](MicroEngine.World.TileMoveResult.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
