# Class StateHasher

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Kernel](MicroEngine.Kernel.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Modules and the frame schedule](../manual/lifecycle.md#modules-and-the-frame-schedule)  

Accumulates a 64-bit hash (FNV-1a) of simulation state in named sections. Values are encoded in a fixed binary form
(little-endian; floats by their bits, so <code>-0</code> and NaN payloads count; strings length-prefixed), so the same
values in the same order give the same hash on every machine and run. Nothing is read by reflection: the engine and
the game add what they choose ([IStateHashed](MicroEngine.Kernel.IStateHashed.md)).

```csharp
public sealed class StateHasher
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[StateHasher](MicroEngine.Kernel.StateHasher.md)

## Remarks

A section lasts until the next [Section](MicroEngine.Kernel.StateHasher.md#MicroEngine_Kernel_StateHasher_Section_System_String_); values added before the first one form a section with an empty
name. The whole hash covers every value and every section name; each section's hash covers only its own values, so
two runs that differ can be told apart by the section that changed.

## Constructors

### <a id="MicroEngine_Kernel_StateHasher__ctor"></a> StateHasher\(\)

```csharp
public StateHasher()
```

## Properties

### <a id="MicroEngine_Kernel_StateHasher_Sections"></a> Sections

The hash of each section so far, in the order they were started (the current one included); a new list each time.

```csharp
public IReadOnlyList<SectionHash> Sections { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[SectionHash](MicroEngine.Kernel.SectionHash.md)\>

### <a id="MicroEngine_Kernel_StateHasher_Value"></a> Value

The hash of everything added so far.

```csharp
public ulong Value { get; }
```

#### Property Value

 [ulong](https://learn.microsoft.com/dotnet/api/system.uint64)

## Methods

### <a id="MicroEngine_Kernel_StateHasher_Add_System_Byte_"></a> Add\(byte\)

Adds a byte.

```csharp
public void Add(byte value)
```

#### Parameters

`value` [byte](https://learn.microsoft.com/dotnet/api/system.byte)

### <a id="MicroEngine_Kernel_StateHasher_Add_System_Boolean_"></a> Add\(bool\)

Adds a boolean (one byte, 0 or 1).

```csharp
public void Add(bool value)
```

#### Parameters

`value` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Kernel_StateHasher_Add_System_Int32_"></a> Add\(int\)

Adds a 32-bit integer.

```csharp
public void Add(int value)
```

#### Parameters

`value` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Kernel_StateHasher_Add_System_Int64_"></a> Add\(long\)

Adds a 64-bit integer.

```csharp
public void Add(long value)
```

#### Parameters

`value` [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Kernel_StateHasher_Add_System_UInt64_"></a> Add\(ulong\)

Adds an unsigned 64-bit integer.

```csharp
public void Add(ulong value)
```

#### Parameters

`value` [ulong](https://learn.microsoft.com/dotnet/api/system.uint64)

### <a id="MicroEngine_Kernel_StateHasher_Add_System_Single_"></a> Add\(float\)

Adds a float by its bits: <code>0</code> and <code>-0</code>, or two NaNs with different payloads, are different values.

```csharp
public void Add(float value)
```

#### Parameters

`value` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Kernel_StateHasher_Add_System_Double_"></a> Add\(double\)

Adds a double by its bits.

```csharp
public void Add(double value)
```

#### Parameters

`value` [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Kernel_StateHasher_Add_System_Numerics_Vector2_"></a> Add\(Vector2\)

Adds both components of a vector, x first.

```csharp
public void Add(Vector2 value)
```

#### Parameters

`value` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Kernel_StateHasher_Add_System_String_"></a> Add\(string?\)

Adds a string as its length then its UTF-16 code units; null is told apart from empty.

```csharp
public void Add(string? value)
```

#### Parameters

`value` [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Kernel_StateHasher_Add_System_ReadOnlySpan_System_Byte__"></a> Add\(ReadOnlySpan<byte\>\)

Adds raw bytes, preceded by their count (tile maps, grids and other bulk state).

```csharp
public void Add(ReadOnlySpan<byte> bytes)
```

#### Parameters

`bytes` [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[byte](https://learn.microsoft.com/dotnet/api/system.byte)\>

### <a id="MicroEngine_Kernel_StateHasher_Add_MicroEngine_Kernel_IStateHashed_"></a> Add\(IStateHashed?\)

Adds an object's own state ([HashState](MicroEngine.Kernel.IStateHashed.md#MicroEngine_Kernel_IStateHashed_HashState_MicroEngine_Kernel_StateHasher_)), preceded by whether it is there.

```csharp
public void Add(IStateHashed? value)
```

#### Parameters

`value` [IStateHashed](MicroEngine.Kernel.IStateHashed.md)?

### <a id="MicroEngine_Kernel_StateHasher_Reset"></a> Reset\(\)

Forgets everything added, to hash another step with the same instance.

```csharp
public void Reset()
```

### <a id="MicroEngine_Kernel_StateHasher_Section_System_String_"></a> Section\(string\)

Starts the section <code class="paramref">name</code>: the values added next are hashed under it.

```csharp
public void Section(string name)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">name</code> is null.

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">name</code> holds a tab or a line break (traces are written one step per line).

</div>
