# Struct SpatialHandle

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.Scene.dll  
Guide: [Scenes](../manual/scenes.md)  

An item of a [SpatialIndex2D`1](MicroEngine.Scenes.md): a slot index and the generation of the item that held the slot when
the handle was made. A removed item's slot is reused with a new generation, so an old handle never reaches the new
item. <code>default</code> is [None](MicroEngine.Scenes.SpatialHandle.md#MicroEngine_Scenes_SpatialHandle_None) (generation 0 is never issued).

```csharp
public readonly struct SpatialHandle : IEquatable<SpatialHandle>
```

#### Implements

[IEquatable<SpatialHandle\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Scenes_SpatialHandle__ctor_System_UInt32_System_UInt32_"></a> SpatialHandle\(uint, uint\)

An item of a [SpatialIndex2D`1](MicroEngine.Scenes.md): a slot index and the generation of the item that held the slot when
the handle was made. A removed item's slot is reused with a new generation, so an old handle never reaches the new
item. <code>default</code> is [None](MicroEngine.Scenes.SpatialHandle.md#MicroEngine_Scenes_SpatialHandle_None) (generation 0 is never issued).

```csharp
public SpatialHandle(uint Index, uint Generation)
```

#### Parameters

`Index` [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

The slot.

`Generation` [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

The generation of the slot's item; 0 only for [None](MicroEngine.Scenes.SpatialHandle.md#MicroEngine_Scenes_SpatialHandle_None).

## Properties

### <a id="MicroEngine_Scenes_SpatialHandle_Generation"></a> Generation

The generation of the slot's item; 0 only for [None](MicroEngine.Scenes.SpatialHandle.md#MicroEngine_Scenes_SpatialHandle_None).

```csharp
public uint Generation { get; init; }
```

#### Property Value

 [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

### <a id="MicroEngine_Scenes_SpatialHandle_Index"></a> Index

The slot.

```csharp
public uint Index { get; init; }
```

#### Property Value

 [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

### <a id="MicroEngine_Scenes_SpatialHandle_IsNone"></a> IsNone

Whether this is [None](MicroEngine.Scenes.SpatialHandle.md#MicroEngine_Scenes_SpatialHandle_None).

```csharp
public bool IsNone { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_SpatialHandle_None"></a> None

The handle of no item.

```csharp
public static SpatialHandle None { get; }
```

#### Property Value

 [SpatialHandle](MicroEngine.Scenes.SpatialHandle.md)

## Methods

### <a id="MicroEngine_Scenes_SpatialHandle_Deconstruct_System_UInt32__System_UInt32__"></a> Deconstruct\(out uint, out uint\)

```csharp
public void Deconstruct(out uint Index, out uint Generation)
```

#### Parameters

`Index` [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

`Generation` [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

### <a id="MicroEngine_Scenes_SpatialHandle_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_SpatialHandle_Equals_MicroEngine_Scenes_SpatialHandle_"></a> Equals\(SpatialHandle\)

```csharp
public bool Equals(SpatialHandle other)
```

#### Parameters

`other` [SpatialHandle](MicroEngine.Scenes.SpatialHandle.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_SpatialHandle_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Scenes_SpatialHandle_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Scenes_SpatialHandle_op_Equality_MicroEngine_Scenes_SpatialHandle_MicroEngine_Scenes_SpatialHandle_"></a> operator ==\(SpatialHandle, SpatialHandle\)

```csharp
public static bool operator ==(SpatialHandle left, SpatialHandle right)
```

#### Parameters

`left` [SpatialHandle](MicroEngine.Scenes.SpatialHandle.md)

`right` [SpatialHandle](MicroEngine.Scenes.SpatialHandle.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_SpatialHandle_op_Inequality_MicroEngine_Scenes_SpatialHandle_MicroEngine_Scenes_SpatialHandle_"></a> operator \!=\(SpatialHandle, SpatialHandle\)

```csharp
public static bool operator !=(SpatialHandle left, SpatialHandle right)
```

#### Parameters

`left` [SpatialHandle](MicroEngine.Scenes.SpatialHandle.md)

`right` [SpatialHandle](MicroEngine.Scenes.SpatialHandle.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
