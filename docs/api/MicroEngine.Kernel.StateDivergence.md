# Class StateDivergence

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Kernel](MicroEngine.Kernel.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Modules and the frame schedule](../manual/lifecycle.md#modules-and-the-frame-schedule)  

Where two traces first differ ([FirstDivergence](MicroEngine.Kernel.StateTrace.md#MicroEngine_Kernel_StateTrace_FirstDivergence_MicroEngine_Kernel_StateTrace_MicroEngine_Kernel_StateTrace_)).

```csharp
public sealed record StateDivergence : IEquatable<StateDivergence>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[StateDivergence](MicroEngine.Kernel.StateDivergence.md)

#### Implements

[IEquatable<StateDivergence\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Kernel_StateDivergence__ctor_System_Int64_System_String_MicroEngine_Kernel_StepHash_MicroEngine_Kernel_StepHash_"></a> StateDivergence\(long, string?, StepHash?, StepHash?\)

Where two traces first differ ([FirstDivergence](MicroEngine.Kernel.StateTrace.md#MicroEngine_Kernel_StateTrace_FirstDivergence_MicroEngine_Kernel_StateTrace_MicroEngine_Kernel_StateTrace_)).

```csharp
public StateDivergence(long Update, string? Section, StepHash? First, StepHash? Second)
```

#### Parameters

`Update` [long](https://learn.microsoft.com/dotnet/api/system.int64)

The update of the first step that differs.

`Section` [string](https://learn.microsoft.com/dotnet/api/system.string)?

The first section whose hash differs, or null when the steps carry no section hashes or one trace has no such step.

`First` [StepHash](MicroEngine.Kernel.StepHash.md)?

The step of the first trace, or null when it ended earlier.

`Second` [StepHash](MicroEngine.Kernel.StepHash.md)?

The step of the second trace, or null when it ended earlier.

## Properties

### <a id="MicroEngine_Kernel_StateDivergence_First"></a> First

The step of the first trace, or null when it ended earlier.

```csharp
public StepHash? First { get; init; }
```

#### Property Value

 [StepHash](MicroEngine.Kernel.StepHash.md)?

### <a id="MicroEngine_Kernel_StateDivergence_Second"></a> Second

The step of the second trace, or null when it ended earlier.

```csharp
public StepHash? Second { get; init; }
```

#### Property Value

 [StepHash](MicroEngine.Kernel.StepHash.md)?

### <a id="MicroEngine_Kernel_StateDivergence_Section"></a> Section

The first section whose hash differs, or null when the steps carry no section hashes or one trace has no such step.

```csharp
public string? Section { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Kernel_StateDivergence_Update"></a> Update

The update of the first step that differs.

```csharp
public long Update { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

## Methods

### <a id="MicroEngine_Kernel_StateDivergence__Clone__"></a> <Clone\>$\(\)

```csharp
public StateDivergence <Clone>$()
```

#### Returns

 [StateDivergence](MicroEngine.Kernel.StateDivergence.md)

### <a id="MicroEngine_Kernel_StateDivergence_Deconstruct_System_Int64__System_String__MicroEngine_Kernel_StepHash__MicroEngine_Kernel_StepHash__"></a> Deconstruct\(out long, out string?, out StepHash?, out StepHash?\)

```csharp
public void Deconstruct(out long Update, out string? Section, out StepHash? First, out StepHash? Second)
```

#### Parameters

`Update` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`Section` [string](https://learn.microsoft.com/dotnet/api/system.string)?

`First` [StepHash](MicroEngine.Kernel.StepHash.md)?

`Second` [StepHash](MicroEngine.Kernel.StepHash.md)?

### <a id="MicroEngine_Kernel_StateDivergence_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Kernel_StateDivergence_Equals_MicroEngine_Kernel_StateDivergence_"></a> Equals\(StateDivergence?\)

```csharp
public bool Equals(StateDivergence? other)
```

#### Parameters

`other` [StateDivergence](MicroEngine.Kernel.StateDivergence.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Kernel_StateDivergence_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Kernel_StateDivergence_ToString"></a> ToString\(\)

A one-line description: the update, the section and both hashes.

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Kernel_StateDivergence_op_Equality_MicroEngine_Kernel_StateDivergence_MicroEngine_Kernel_StateDivergence_"></a> operator ==\(StateDivergence?, StateDivergence?\)

```csharp
public static bool operator ==(StateDivergence? left, StateDivergence? right)
```

#### Parameters

`left` [StateDivergence](MicroEngine.Kernel.StateDivergence.md)?

`right` [StateDivergence](MicroEngine.Kernel.StateDivergence.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Kernel_StateDivergence_op_Inequality_MicroEngine_Kernel_StateDivergence_MicroEngine_Kernel_StateDivergence_"></a> operator \!=\(StateDivergence?, StateDivergence?\)

```csharp
public static bool operator !=(StateDivergence? left, StateDivergence? right)
```

#### Parameters

`left` [StateDivergence](MicroEngine.Kernel.StateDivergence.md)?

`right` [StateDivergence](MicroEngine.Kernel.StateDivergence.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
