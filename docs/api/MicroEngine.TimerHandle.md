# Struct TimerHandle

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine](MicroEngine.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Graphics](../manual/rendering.md)  

A timer of a [Timers](MicroEngine.Timers.md); [None](MicroEngine.TimerHandle.md#MicroEngine_TimerHandle_None) names no timer.

```csharp
public readonly struct TimerHandle : IEquatable<TimerHandle>
```

#### Implements

[IEquatable<TimerHandle\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_TimerHandle__ctor_System_Int32_"></a> TimerHandle\(int\)

A timer of a [Timers](MicroEngine.Timers.md); [None](MicroEngine.TimerHandle.md#MicroEngine_TimerHandle_None) names no timer.

```csharp
public TimerHandle(int Id)
```

#### Parameters

`Id` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The timer's number within its [Timers](MicroEngine.Timers.md); 0 for [None](MicroEngine.TimerHandle.md#MicroEngine_TimerHandle_None).

## Properties

### <a id="MicroEngine_TimerHandle_Id"></a> Id

The timer's number within its [Timers](MicroEngine.Timers.md); 0 for [None](MicroEngine.TimerHandle.md#MicroEngine_TimerHandle_None).

```csharp
public int Id { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_TimerHandle_IsNone"></a> IsNone

Whether this is [None](MicroEngine.TimerHandle.md#MicroEngine_TimerHandle_None).

```csharp
public bool IsNone { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_TimerHandle_None"></a> None

No timer.

```csharp
public static TimerHandle None { get; }
```

#### Property Value

 [TimerHandle](MicroEngine.TimerHandle.md)

## Methods

### <a id="MicroEngine_TimerHandle_Deconstruct_System_Int32__"></a> Deconstruct\(out int\)

```csharp
public void Deconstruct(out int Id)
```

#### Parameters

`Id` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_TimerHandle_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_TimerHandle_Equals_MicroEngine_TimerHandle_"></a> Equals\(TimerHandle\)

```csharp
public bool Equals(TimerHandle other)
```

#### Parameters

`other` [TimerHandle](MicroEngine.TimerHandle.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_TimerHandle_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_TimerHandle_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_TimerHandle_op_Equality_MicroEngine_TimerHandle_MicroEngine_TimerHandle_"></a> operator ==\(TimerHandle, TimerHandle\)

```csharp
public static bool operator ==(TimerHandle left, TimerHandle right)
```

#### Parameters

`left` [TimerHandle](MicroEngine.TimerHandle.md)

`right` [TimerHandle](MicroEngine.TimerHandle.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_TimerHandle_op_Inequality_MicroEngine_TimerHandle_MicroEngine_TimerHandle_"></a> operator \!=\(TimerHandle, TimerHandle\)

```csharp
public static bool operator !=(TimerHandle left, TimerHandle right)
```

#### Parameters

`left` [TimerHandle](MicroEngine.TimerHandle.md)

`right` [TimerHandle](MicroEngine.TimerHandle.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
