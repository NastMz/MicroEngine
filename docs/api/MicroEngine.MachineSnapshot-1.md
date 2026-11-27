# Struct MachineSnapshot<TState\>

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine](MicroEngine.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Graphics](../manual/rendering.md)  

A machine's state and how long it has been in it, to save and restore (`Restore`).

```csharp
public readonly struct MachineSnapshot<TState> : IEquatable<MachineSnapshot<TState>>
```

#### Type Parameters

`TState` 

#### Implements

[IEquatable<MachineSnapshot<TState\>\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_MachineSnapshot_1__ctor__0_System_Single_System_Int32_"></a> MachineSnapshot\(TState, float, int\)

A machine's state and how long it has been in it, to save and restore (`Restore`).

```csharp
public MachineSnapshot(TState State, float Time, int Steps = 0)
```

#### Parameters

`State` TState

The state.

`Time` [float](https://learn.microsoft.com/dotnet/api/system.single)

Seconds in that state.

`Steps` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Updates in that state (`Steps`).

## Properties

### <a id="MicroEngine_MachineSnapshot_1_State"></a> State

The state.

```csharp
public TState State { get; init; }
```

#### Property Value

 TState

### <a id="MicroEngine_MachineSnapshot_1_Steps"></a> Steps

Updates in that state (`Steps`).

```csharp
public int Steps { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_MachineSnapshot_1_Time"></a> Time

Seconds in that state.

```csharp
public float Time { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_MachineSnapshot_1_Deconstruct__0__System_Single__System_Int32__"></a> Deconstruct\(out TState, out float, out int\)

```csharp
public void Deconstruct(out TState State, out float Time, out int Steps)
```

#### Parameters

`State` TState

`Time` [float](https://learn.microsoft.com/dotnet/api/system.single)

`Steps` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_MachineSnapshot_1_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_MachineSnapshot_1_Equals_MicroEngine_MachineSnapshot__0__"></a> Equals\(MachineSnapshot<TState\>\)

```csharp
public bool Equals(MachineSnapshot<TState> other)
```

#### Parameters

`other` [MachineSnapshot](MicroEngine.MachineSnapshot\-1.md)<TState\>

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_MachineSnapshot_1_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_MachineSnapshot_1_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_MachineSnapshot_1_op_Equality_MicroEngine_MachineSnapshot__0__MicroEngine_MachineSnapshot__0__"></a> operator ==\(MachineSnapshot<TState\>, MachineSnapshot<TState\>\)

```csharp
public static bool operator ==(MachineSnapshot<TState> left, MachineSnapshot<TState> right)
```

#### Parameters

`left` [MachineSnapshot](MicroEngine.MachineSnapshot\-1.md)<TState\>

`right` [MachineSnapshot](MicroEngine.MachineSnapshot\-1.md)<TState\>

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_MachineSnapshot_1_op_Inequality_MicroEngine_MachineSnapshot__0__MicroEngine_MachineSnapshot__0__"></a> operator \!=\(MachineSnapshot<TState\>, MachineSnapshot<TState\>\)

```csharp
public static bool operator !=(MachineSnapshot<TState> left, MachineSnapshot<TState> right)
```

#### Parameters

`left` [MachineSnapshot](MicroEngine.MachineSnapshot\-1.md)<TState\>

`right` [MachineSnapshot](MicroEngine.MachineSnapshot\-1.md)<TState\>

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
