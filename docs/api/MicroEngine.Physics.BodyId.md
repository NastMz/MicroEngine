# Struct BodyId

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics worlds](../manual/world.md#physics-worlds)  

A body of an [IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md): a slot index and the generation of the body that held the slot when the
id was made. A destroyed body's slot is reused with a new generation, so an old id never reaches the new body.
<code>default</code> is [None](MicroEngine.Physics.BodyId.md#MicroEngine_Physics_BodyId_None) (generation 0 is never issued).

```csharp
public readonly struct BodyId : IEquatable<BodyId>
```

#### Implements

[IEquatable<BodyId\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Physics_BodyId__ctor_System_UInt32_System_UInt32_"></a> BodyId\(uint, uint\)

A body of an [IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md): a slot index and the generation of the body that held the slot when the
id was made. A destroyed body's slot is reused with a new generation, so an old id never reaches the new body.
<code>default</code> is [None](MicroEngine.Physics.BodyId.md#MicroEngine_Physics_BodyId_None) (generation 0 is never issued).

```csharp
public BodyId(uint Index, uint Generation)
```

#### Parameters

`Index` [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

The slot.

`Generation` [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

The generation of the slot's body; 0 only for [None](MicroEngine.Physics.BodyId.md#MicroEngine_Physics_BodyId_None).

## Properties

### <a id="MicroEngine_Physics_BodyId_Generation"></a> Generation

The generation of the slot's body; 0 only for [None](MicroEngine.Physics.BodyId.md#MicroEngine_Physics_BodyId_None).

```csharp
public uint Generation { get; init; }
```

#### Property Value

 [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

### <a id="MicroEngine_Physics_BodyId_Index"></a> Index

The slot.

```csharp
public uint Index { get; init; }
```

#### Property Value

 [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

### <a id="MicroEngine_Physics_BodyId_IsNone"></a> IsNone

Whether this is [None](MicroEngine.Physics.BodyId.md#MicroEngine_Physics_BodyId_None).

```csharp
public bool IsNone { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_BodyId_None"></a> None

The id of no body.

```csharp
public static BodyId None { get; }
```

#### Property Value

 [BodyId](MicroEngine.Physics.BodyId.md)

## Methods

### <a id="MicroEngine_Physics_BodyId_Deconstruct_System_UInt32__System_UInt32__"></a> Deconstruct\(out uint, out uint\)

```csharp
public void Deconstruct(out uint Index, out uint Generation)
```

#### Parameters

`Index` [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

`Generation` [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

### <a id="MicroEngine_Physics_BodyId_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_BodyId_Equals_MicroEngine_Physics_BodyId_"></a> Equals\(BodyId\)

```csharp
public bool Equals(BodyId other)
```

#### Parameters

`other` [BodyId](MicroEngine.Physics.BodyId.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_BodyId_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Physics_BodyId_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Physics_BodyId_op_Equality_MicroEngine_Physics_BodyId_MicroEngine_Physics_BodyId_"></a> operator ==\(BodyId, BodyId\)

```csharp
public static bool operator ==(BodyId left, BodyId right)
```

#### Parameters

`left` [BodyId](MicroEngine.Physics.BodyId.md)

`right` [BodyId](MicroEngine.Physics.BodyId.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_BodyId_op_Inequality_MicroEngine_Physics_BodyId_MicroEngine_Physics_BodyId_"></a> operator \!=\(BodyId, BodyId\)

```csharp
public static bool operator !=(BodyId left, BodyId right)
```

#### Parameters

`left` [BodyId](MicroEngine.Physics.BodyId.md)

`right` [BodyId](MicroEngine.Physics.BodyId.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
