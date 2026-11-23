# Struct JointId

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics](../manual/physics.md)  

A joint of an [IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md) ([Joints](MicroEngine.Physics.PhysicsCapabilities.md)). Generation-checked like
[BodyId](MicroEngine.Physics.BodyId.md); <code>default</code> is [None](MicroEngine.Physics.JointId.md#MicroEngine_Physics_JointId_None).

```csharp
public readonly struct JointId : IEquatable<JointId>
```

#### Implements

[IEquatable<JointId\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Physics_JointId__ctor_System_UInt32_System_UInt32_"></a> JointId\(uint, uint\)

A joint of an [IPhysicsWorld2D](MicroEngine.Physics.IPhysicsWorld2D.md) ([Joints](MicroEngine.Physics.PhysicsCapabilities.md)). Generation-checked like
[BodyId](MicroEngine.Physics.BodyId.md); <code>default</code> is [None](MicroEngine.Physics.JointId.md#MicroEngine_Physics_JointId_None).

```csharp
public JointId(uint Index, uint Generation)
```

#### Parameters

`Index` [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

The slot.

`Generation` [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

The generation of the slot's joint; 0 only for [None](MicroEngine.Physics.JointId.md#MicroEngine_Physics_JointId_None).

## Properties

### <a id="MicroEngine_Physics_JointId_Generation"></a> Generation

The generation of the slot's joint; 0 only for [None](MicroEngine.Physics.JointId.md#MicroEngine_Physics_JointId_None).

```csharp
public uint Generation { get; init; }
```

#### Property Value

 [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

### <a id="MicroEngine_Physics_JointId_Index"></a> Index

The slot.

```csharp
public uint Index { get; init; }
```

#### Property Value

 [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

### <a id="MicroEngine_Physics_JointId_IsNone"></a> IsNone

Whether this is [None](MicroEngine.Physics.JointId.md#MicroEngine_Physics_JointId_None).

```csharp
public bool IsNone { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_JointId_None"></a> None

The id of no joint.

```csharp
public static JointId None { get; }
```

#### Property Value

 [JointId](MicroEngine.Physics.JointId.md)

## Methods

### <a id="MicroEngine_Physics_JointId_Deconstruct_System_UInt32__System_UInt32__"></a> Deconstruct\(out uint, out uint\)

```csharp
public void Deconstruct(out uint Index, out uint Generation)
```

#### Parameters

`Index` [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

`Generation` [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

### <a id="MicroEngine_Physics_JointId_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_JointId_Equals_MicroEngine_Physics_JointId_"></a> Equals\(JointId\)

```csharp
public bool Equals(JointId other)
```

#### Parameters

`other` [JointId](MicroEngine.Physics.JointId.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_JointId_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Physics_JointId_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Physics_JointId_op_Equality_MicroEngine_Physics_JointId_MicroEngine_Physics_JointId_"></a> operator ==\(JointId, JointId\)

```csharp
public static bool operator ==(JointId left, JointId right)
```

#### Parameters

`left` [JointId](MicroEngine.Physics.JointId.md)

`right` [JointId](MicroEngine.Physics.JointId.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Physics_JointId_op_Inequality_MicroEngine_Physics_JointId_MicroEngine_Physics_JointId_"></a> operator \!=\(JointId, JointId\)

```csharp
public static bool operator !=(JointId left, JointId right)
```

#### Parameters

`left` [JointId](MicroEngine.Physics.JointId.md)

`right` [JointId](MicroEngine.Physics.JointId.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
