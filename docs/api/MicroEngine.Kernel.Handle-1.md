# Struct Handle<T\>

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Kernel](MicroEngine.Kernel.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Modules and the frame schedule](../manual/lifecycle.md#modules-and-the-frame-schedule)  

A reference to an item of a [HandlePool`1](MicroEngine.Kernel.md): a slot index and the generation of the item that held the
slot when the handle was made. When the item is removed its slot's generation changes, so an old handle no longer
finds anything (instead of finding whatever reuses the slot). <code>default</code> is [None](MicroEngine.Kernel.md).

```csharp
public readonly struct Handle<T> : IEquatable<Handle<T>>
```

#### Type Parameters

`T` 

The kind of item, so handles of different pools cannot be mixed up.

#### Implements

[IEquatable<Handle<T\>\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Kernel_Handle_1__ctor_System_Int32_System_UInt32_"></a> Handle\(int, uint\)

A reference to an item of a [HandlePool`1](MicroEngine.Kernel.md): a slot index and the generation of the item that held the
slot when the handle was made. When the item is removed its slot's generation changes, so an old handle no longer
finds anything (instead of finding whatever reuses the slot). <code>default</code> is [None](MicroEngine.Kernel.md).

```csharp
public Handle(int Index, uint Generation)
```

#### Parameters

`Index` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The slot.

`Generation` [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

The generation of the slot's item; 0 only for [None](MicroEngine.Kernel.md).

## Properties

### <a id="MicroEngine_Kernel_Handle_1_Generation"></a> Generation

The generation of the slot's item; 0 only for [None](MicroEngine.Kernel.md).

```csharp
public uint Generation { get; init; }
```

#### Property Value

 [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

### <a id="MicroEngine_Kernel_Handle_1_Index"></a> Index

The slot.

```csharp
public int Index { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Kernel_Handle_1_IsNone"></a> IsNone

Whether this is [None](MicroEngine.Kernel.md).

```csharp
public bool IsNone { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Kernel_Handle_1_None"></a> None

The handle of nothing.

```csharp
public static Handle<T> None { get; }
```

#### Property Value

 [Handle](MicroEngine.Kernel.Handle\-1.md)<T\>

## Methods

### <a id="MicroEngine_Kernel_Handle_1_Deconstruct_System_Int32__System_UInt32__"></a> Deconstruct\(out int, out uint\)

```csharp
public void Deconstruct(out int Index, out uint Generation)
```

#### Parameters

`Index` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`Generation` [uint](https://learn.microsoft.com/dotnet/api/system.uint32)

### <a id="MicroEngine_Kernel_Handle_1_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Kernel_Handle_1_Equals_MicroEngine_Kernel_Handle__0__"></a> Equals\(Handle<T\>\)

```csharp
public bool Equals(Handle<T> other)
```

#### Parameters

`other` [Handle](MicroEngine.Kernel.Handle\-1.md)<T\>

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Kernel_Handle_1_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Kernel_Handle_1_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Kernel_Handle_1_op_Equality_MicroEngine_Kernel_Handle__0__MicroEngine_Kernel_Handle__0__"></a> operator ==\(Handle<T\>, Handle<T\>\)

```csharp
public static bool operator ==(Handle<T> left, Handle<T> right)
```

#### Parameters

`left` [Handle](MicroEngine.Kernel.Handle\-1.md)<T\>

`right` [Handle](MicroEngine.Kernel.Handle\-1.md)<T\>

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Kernel_Handle_1_op_Inequality_MicroEngine_Kernel_Handle__0__MicroEngine_Kernel_Handle__0__"></a> operator \!=\(Handle<T\>, Handle<T\>\)

```csharp
public static bool operator !=(Handle<T> left, Handle<T> right)
```

#### Parameters

`left` [Handle](MicroEngine.Kernel.Handle\-1.md)<T\>

`right` [Handle](MicroEngine.Kernel.Handle\-1.md)<T\>

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
