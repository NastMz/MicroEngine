# Struct MoveResult

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.World](MicroEngine.World.md)  
Assembly: MicroEngine.dll  
Guide: [Tile maps and collision](../manual/world.md)  

What happened during a [Move](MicroEngine.World.TileMotion.md#MicroEngine_World_TileMotion_Move_MicroEngine_RectF_System_Numerics_Vector2_MicroEngine_World_ICollisionGrid_System_Collections_Generic_IReadOnlyList_MicroEngine_RectF__System_Boolean_).

```csharp
public readonly struct MoveResult : IEquatable<MoveResult>
```

#### Implements

[IEquatable<MoveResult\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_World_MoveResult__ctor_MicroEngine_RectF_System_Boolean_System_Boolean_System_Boolean_System_Boolean_MicroEngine_World_TileFlags_"></a> MoveResult\(RectF, bool, bool, bool, bool, TileFlags\)

What happened during a [Move](MicroEngine.World.TileMotion.md#MicroEngine_World_TileMotion_Move_MicroEngine_RectF_System_Numerics_Vector2_MicroEngine_World_ICollisionGrid_System_Collections_Generic_IReadOnlyList_MicroEngine_RectF__System_Boolean_).

```csharp
public MoveResult(RectF Bounds, bool HitLeft, bool HitRight, bool HitCeiling, bool HitFloor, TileFlags Touched)
```

#### Parameters

`Bounds` RectF

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

### <a id="MicroEngine_World_MoveResult_Bounds"></a> Bounds

The box after the move.

```csharp
public RectF Bounds { get; init; }
```

#### Property Value

 RectF

### <a id="MicroEngine_World_MoveResult_HitCeiling"></a> HitCeiling

Upward movement was stopped.

```csharp
public bool HitCeiling { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_World_MoveResult_HitFloor"></a> HitFloor

Downward movement was stopped (the body landed).

```csharp
public bool HitFloor { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_World_MoveResult_HitLeft"></a> HitLeft

Movement to the left was stopped.

```csharp
public bool HitLeft { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_World_MoveResult_HitRight"></a> HitRight

Movement to the right was stopped.

```csharp
public bool HitRight { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_World_MoveResult_HitWall"></a> HitWall

Whether horizontal movement was stopped on either side.

```csharp
public bool HitWall { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_World_MoveResult_Position"></a> Position

The top-left of [Bounds](MicroEngine.World.MoveResult.md#MicroEngine_World_MoveResult_Bounds).

```csharp
public Vector2 Position { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_World_MoveResult_Touched"></a> Touched

Flags of every tile the box overlaps at the end (hazards, water...).

```csharp
public TileFlags Touched { get; init; }
```

#### Property Value

 [TileFlags](MicroEngine.World.TileFlags.md)

## Methods

### <a id="MicroEngine_World_MoveResult_Deconstruct_MicroEngine_RectF__System_Boolean__System_Boolean__System_Boolean__System_Boolean__MicroEngine_World_TileFlags__"></a> Deconstruct\(out RectF, out bool, out bool, out bool, out bool, out TileFlags\)

```csharp
public void Deconstruct(out RectF Bounds, out bool HitLeft, out bool HitRight, out bool HitCeiling, out bool HitFloor, out TileFlags Touched)
```

#### Parameters

`Bounds` RectF

`HitLeft` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`HitRight` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`HitCeiling` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`HitFloor` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

`Touched` [TileFlags](MicroEngine.World.TileFlags.md)

### <a id="MicroEngine_World_MoveResult_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_World_MoveResult_Equals_MicroEngine_World_MoveResult_"></a> Equals\(MoveResult\)

```csharp
public bool Equals(MoveResult other)
```

#### Parameters

`other` [MoveResult](MicroEngine.World.MoveResult.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_World_MoveResult_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_World_MoveResult_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_World_MoveResult_op_Equality_MicroEngine_World_MoveResult_MicroEngine_World_MoveResult_"></a> operator ==\(MoveResult, MoveResult\)

```csharp
public static bool operator ==(MoveResult left, MoveResult right)
```

#### Parameters

`left` [MoveResult](MicroEngine.World.MoveResult.md)

`right` [MoveResult](MicroEngine.World.MoveResult.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_World_MoveResult_op_Inequality_MicroEngine_World_MoveResult_MicroEngine_World_MoveResult_"></a> operator \!=\(MoveResult, MoveResult\)

```csharp
public static bool operator !=(MoveResult left, MoveResult right)
```

#### Parameters

`left` [MoveResult](MicroEngine.World.MoveResult.md)

`right` [MoveResult](MicroEngine.World.MoveResult.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
