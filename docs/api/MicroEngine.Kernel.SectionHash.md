# Struct SectionHash

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Kernel](MicroEngine.Kernel.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Modules and the frame schedule](../manual/lifecycle.md#modules-and-the-frame-schedule)  

The hash of one named part of the state of a step ([Sections](MicroEngine.Kernel.StepHash.md#MicroEngine_Kernel_StepHash_Sections)).

```csharp
public readonly struct SectionHash : IEquatable<SectionHash>
```

#### Implements

[IEquatable<SectionHash\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Kernel_SectionHash__ctor_System_String_System_UInt64_"></a> SectionHash\(string, ulong\)

The hash of one named part of the state of a step ([Sections](MicroEngine.Kernel.StepHash.md#MicroEngine_Kernel_StepHash_Sections)).

```csharp
public SectionHash(string Name, ulong Hash)
```

#### Parameters

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

The section's name (<code>engine.time</code>, <code>scene[0]:PlayScene</code>, <code>game</code>...).

`Hash` [ulong](https://learn.microsoft.com/dotnet/api/system.uint64)

The 64-bit hash of the values added to it.

## Properties

### <a id="MicroEngine_Kernel_SectionHash_Hash"></a> Hash

The 64-bit hash of the values added to it.

```csharp
public ulong Hash { get; init; }
```

#### Property Value

 [ulong](https://learn.microsoft.com/dotnet/api/system.uint64)

### <a id="MicroEngine_Kernel_SectionHash_Name"></a> Name

The section's name (<code>engine.time</code>, <code>scene[0]:PlayScene</code>, <code>game</code>...).

```csharp
public string Name { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Kernel_SectionHash_Deconstruct_System_String__System_UInt64__"></a> Deconstruct\(out string, out ulong\)

```csharp
public void Deconstruct(out string Name, out ulong Hash)
```

#### Parameters

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Hash` [ulong](https://learn.microsoft.com/dotnet/api/system.uint64)

### <a id="MicroEngine_Kernel_SectionHash_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Kernel_SectionHash_Equals_MicroEngine_Kernel_SectionHash_"></a> Equals\(SectionHash\)

```csharp
public bool Equals(SectionHash other)
```

#### Parameters

`other` [SectionHash](MicroEngine.Kernel.SectionHash.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Kernel_SectionHash_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Kernel_SectionHash_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Kernel_SectionHash_op_Equality_MicroEngine_Kernel_SectionHash_MicroEngine_Kernel_SectionHash_"></a> operator ==\(SectionHash, SectionHash\)

```csharp
public static bool operator ==(SectionHash left, SectionHash right)
```

#### Parameters

`left` [SectionHash](MicroEngine.Kernel.SectionHash.md)

`right` [SectionHash](MicroEngine.Kernel.SectionHash.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Kernel_SectionHash_op_Inequality_MicroEngine_Kernel_SectionHash_MicroEngine_Kernel_SectionHash_"></a> operator \!=\(SectionHash, SectionHash\)

```csharp
public static bool operator !=(SectionHash left, SectionHash right)
```

#### Parameters

`left` [SectionHash](MicroEngine.Kernel.SectionHash.md)

`right` [SectionHash](MicroEngine.Kernel.SectionHash.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
