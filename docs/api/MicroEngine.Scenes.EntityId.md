# Struct EntityId

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.Scene.dll  
Guide: [Scenes](../manual/scenes.md)  

An entity of an [IWorld](MicroEngine.Scenes.IWorld.md): a slot index and the generation of the entity that held the slot when the id
was made. A destroyed entity's slot is reused with a new generation, so an old id never reaches the new entity.
<code>default</code> is [None](MicroEngine.Scenes.EntityId.md#MicroEngine_Scenes_EntityId_None) (generation 0 is never issued).

```csharp
public readonly struct EntityId : IEquatable<EntityId>
```

#### Implements

[IEquatable<EntityId\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Scenes_EntityId__ctor_System_UInt32_System_UInt32_"></a> EntityId\(uint, uint\)

An entity of an [IWorld](MicroEngine.Scenes.IWorld.md): a slot index and the generation of the entity that held the slot when the id
was made. A destroyed entity's slot is reused with a new generation, so an old id never reaches the new entity.
<code>default</code> is [None](MicroEngine.Scenes.EntityId.md#MicroEngine_Scenes_EntityId_None) (generation 0 is never issued).

```csharp
public EntityId(uint Index, uint Generation)
```

#### Parameters

`Index` [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

The slot.

`Generation` [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

The generation of the slot's entity; 0 only for [None](MicroEngine.Scenes.EntityId.md#MicroEngine_Scenes_EntityId_None).

## Properties

### <a id="MicroEngine_Scenes_EntityId_Generation"></a> Generation

The generation of the slot's entity; 0 only for [None](MicroEngine.Scenes.EntityId.md#MicroEngine_Scenes_EntityId_None).

```csharp
public uint Generation { get; init; }
```

#### Property Value

 [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

### <a id="MicroEngine_Scenes_EntityId_Index"></a> Index

The slot.

```csharp
public uint Index { get; init; }
```

#### Property Value

 [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

### <a id="MicroEngine_Scenes_EntityId_IsNone"></a> IsNone

Whether this is [None](MicroEngine.Scenes.EntityId.md#MicroEngine_Scenes_EntityId_None).

```csharp
public bool IsNone { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_EntityId_None"></a> None

The id of no entity.

```csharp
public static EntityId None { get; }
```

#### Property Value

 [EntityId](MicroEngine.Scenes.EntityId.md)

## Methods

### <a id="MicroEngine_Scenes_EntityId_Deconstruct_System_UInt32__System_UInt32__"></a> Deconstruct\(out uint, out uint\)

```csharp
public void Deconstruct(out uint Index, out uint Generation)
```

#### Parameters

`Index` [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

`Generation` [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

### <a id="MicroEngine_Scenes_EntityId_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_EntityId_Equals_MicroEngine_Scenes_EntityId_"></a> Equals\(EntityId\)

```csharp
public bool Equals(EntityId other)
```

#### Parameters

`other` [EntityId](MicroEngine.Scenes.EntityId.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_EntityId_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Scenes_EntityId_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Scenes_EntityId_op_Equality_MicroEngine_Scenes_EntityId_MicroEngine_Scenes_EntityId_"></a> operator ==\(EntityId, EntityId\)

```csharp
public static bool operator ==(EntityId left, EntityId right)
```

#### Parameters

`left` [EntityId](MicroEngine.Scenes.EntityId.md)

`right` [EntityId](MicroEngine.Scenes.EntityId.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_EntityId_op_Inequality_MicroEngine_Scenes_EntityId_MicroEngine_Scenes_EntityId_"></a> operator \!=\(EntityId, EntityId\)

```csharp
public static bool operator !=(EntityId left, EntityId right)
```

#### Parameters

`left` [EntityId](MicroEngine.Scenes.EntityId.md)

`right` [EntityId](MicroEngine.Scenes.EntityId.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
