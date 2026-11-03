# Class StepHash

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Kernel](MicroEngine.Kernel.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Modules and the frame schedule](../manual/lifecycle.md#modules-and-the-frame-schedule)  

The state hash after one fixed step.

```csharp
public sealed record StepHash : IEquatable<StepHash>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[StepHash](MicroEngine.Kernel.StepHash.md)

#### Implements

[IEquatable<StepHash\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Kernel_StepHash__ctor_System_Int64_System_UInt64_System_Collections_Generic_IReadOnlyList_MicroEngine_Kernel_SectionHash__"></a> StepHash\(long, ulong, IReadOnlyList<SectionHash\>\)

The state hash after one fixed step.

```csharp
public StepHash(long Update, ulong Hash, IReadOnlyList<SectionHash> Sections)
```

#### Parameters

`Update` [long](https://learn.microsoft.com/dotnet/api/system.int64)

The update the step ended (1 for the first).

`Hash` [ulong](https://learn.microsoft.com/dotnet/api/system.uint64)

The hash of the whole state.

`Sections` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[SectionHash](MicroEngine.Kernel.SectionHash.md)\>

The hash of each section, in order; empty unless the trace asked for them ([Sections](MicroEngine.Kernel.StateTrace.md#MicroEngine_Kernel_StateTrace_Sections)).

## Properties

### <a id="MicroEngine_Kernel_StepHash_Hash"></a> Hash

The hash of the whole state.

```csharp
public ulong Hash { get; init; }
```

#### Property Value

 [ulong](https://learn.microsoft.com/dotnet/api/system.uint64)

### <a id="MicroEngine_Kernel_StepHash_Sections"></a> Sections

The hash of each section, in order; empty unless the trace asked for them ([Sections](MicroEngine.Kernel.StateTrace.md#MicroEngine_Kernel_StateTrace_Sections)).

```csharp
public IReadOnlyList<SectionHash> Sections { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[SectionHash](MicroEngine.Kernel.SectionHash.md)\>

### <a id="MicroEngine_Kernel_StepHash_Update"></a> Update

The update the step ended (1 for the first).

```csharp
public long Update { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

## Methods

### <a id="MicroEngine_Kernel_StepHash__Clone__"></a> <Clone\>$\(\)

```csharp
public StepHash <Clone>$()
```

#### Returns

 [StepHash](MicroEngine.Kernel.StepHash.md)

### <a id="MicroEngine_Kernel_StepHash_Deconstruct_System_Int64__System_UInt64__System_Collections_Generic_IReadOnlyList_MicroEngine_Kernel_SectionHash___"></a> Deconstruct\(out long, out ulong, out IReadOnlyList<SectionHash\>\)

```csharp
public void Deconstruct(out long Update, out ulong Hash, out IReadOnlyList<SectionHash> Sections)
```

#### Parameters

`Update` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`Hash` [ulong](https://learn.microsoft.com/dotnet/api/system.uint64)

`Sections` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[SectionHash](MicroEngine.Kernel.SectionHash.md)\>

### <a id="MicroEngine_Kernel_StepHash_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Kernel_StepHash_Equals_MicroEngine_Kernel_StepHash_"></a> Equals\(StepHash?\)

```csharp
public bool Equals(StepHash? other)
```

#### Parameters

`other` [StepHash](MicroEngine.Kernel.StepHash.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Kernel_StepHash_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Kernel_StepHash_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Kernel_StepHash_op_Equality_MicroEngine_Kernel_StepHash_MicroEngine_Kernel_StepHash_"></a> operator ==\(StepHash?, StepHash?\)

```csharp
public static bool operator ==(StepHash? left, StepHash? right)
```

#### Parameters

`left` [StepHash](MicroEngine.Kernel.StepHash.md)?

`right` [StepHash](MicroEngine.Kernel.StepHash.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Kernel_StepHash_op_Inequality_MicroEngine_Kernel_StepHash_MicroEngine_Kernel_StepHash_"></a> operator \!=\(StepHash?, StepHash?\)

```csharp
public static bool operator !=(StepHash? left, StepHash? right)
```

#### Parameters

`left` [StepHash](MicroEngine.Kernel.StepHash.md)?

`right` [StepHash](MicroEngine.Kernel.StepHash.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
