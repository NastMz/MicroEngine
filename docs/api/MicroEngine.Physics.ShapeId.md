# Struct ShapeId

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics worlds](../manual/world.md#physics-worlds)  

A shape of an [IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md) (a box, a tile grid, or a shape of a declared capability), attached to one
body. Generation-checked like [BodyId](MicroEngine.Physics.BodyId.md); <code>default</code> is [None](MicroEngine.Physics.ShapeId.md#MicroEngine_Physics_ShapeId_None).

```csharp
public readonly struct ShapeId : IEquatable<ShapeId>
```

#### Implements

[IEquatable<ShapeId\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Physics_ShapeId__ctor_System_UInt32_System_UInt32_"></a> ShapeId\(uint, uint\)

A shape of an [IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md) (a box, a tile grid, or a shape of a declared capability), attached to one
body. Generation-checked like [BodyId](MicroEngine.Physics.BodyId.md); <code>default</code> is [None](MicroEngine.Physics.ShapeId.md#MicroEngine_Physics_ShapeId_None).

```csharp
public ShapeId(uint Index, uint Generation)
```

#### Parameters

`Index` [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

The slot.

`Generation` [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

The generation of the slot's shape; 0 only for [None](MicroEngine.Physics.ShapeId.md#MicroEngine_Physics_ShapeId_None).

## Properties

### <a id="MicroEngine_Physics_ShapeId_Generation"></a> Generation

The generation of the slot's shape; 0 only for [None](MicroEngine.Physics.ShapeId.md#MicroEngine_Physics_ShapeId_None).

```csharp
public uint Generation { get; init; }
```

#### Property Value

 [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

### <a id="MicroEngine_Physics_ShapeId_Index"></a> Index

The slot.

```csharp
public uint Index { get; init; }
```

#### Property Value

 [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

### <a id="MicroEngine_Physics_ShapeId_IsNone"></a> IsNone

Whether this is [None](MicroEngine.Physics.ShapeId.md#MicroEngine_Physics_ShapeId_None).

```csharp
public bool IsNone { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_ShapeId_None"></a> None

The id of no shape.

```csharp
public static ShapeId None { get; }
```

#### Property Value

 [ShapeId](MicroEngine.Physics.ShapeId.md)

## Methods

### <a id="MicroEngine_Physics_ShapeId_Deconstruct_System_UInt32__System_UInt32__"></a> Deconstruct\(out uint, out uint\)

```csharp
public void Deconstruct(out uint Index, out uint Generation)
```

#### Parameters

`Index` [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

`Generation` [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

### <a id="MicroEngine_Physics_ShapeId_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_ShapeId_Equals_MicroEngine_Physics_ShapeId_"></a> Equals\(ShapeId\)

```csharp
public bool Equals(ShapeId other)
```

#### Parameters

`other` [ShapeId](MicroEngine.Physics.ShapeId.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_ShapeId_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Physics_ShapeId_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Physics_ShapeId_op_Equality_MicroEngine_Physics_ShapeId_MicroEngine_Physics_ShapeId_"></a> operator ==\(ShapeId, ShapeId\)

```csharp
public static bool operator ==(ShapeId left, ShapeId right)
```

#### Parameters

`left` [ShapeId](MicroEngine.Physics.ShapeId.md)

`right` [ShapeId](MicroEngine.Physics.ShapeId.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_ShapeId_op_Inequality_MicroEngine_Physics_ShapeId_MicroEngine_Physics_ShapeId_"></a> operator \!=\(ShapeId, ShapeId\)

```csharp
public static bool operator !=(ShapeId left, ShapeId right)
```

#### Parameters

`left` [ShapeId](MicroEngine.Physics.ShapeId.md)

`right` [ShapeId](MicroEngine.Physics.ShapeId.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
