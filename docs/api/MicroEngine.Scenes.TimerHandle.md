# Struct TimerHandle

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.dll  
Guide: [Scenes](../manual/scenes.md)  

A timer of a scene's [SceneTimers](MicroEngine.Scenes.SceneTimers.md); [None](MicroEngine.Scenes.TimerHandle.md#MicroEngine_Scenes_TimerHandle_None) names no timer.

```csharp
public readonly struct TimerHandle : IEquatable<TimerHandle>
```

#### Implements

[IEquatable<TimerHandle\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Scenes_TimerHandle__ctor_System_Int32_"></a> TimerHandle\(int\)

A timer of a scene's [SceneTimers](MicroEngine.Scenes.SceneTimers.md); [None](MicroEngine.Scenes.TimerHandle.md#MicroEngine_Scenes_TimerHandle_None) names no timer.

```csharp
public TimerHandle(int Id)
```

#### Parameters

`Id` [int](https://learn.microsoft.com/dotnet/api/system.int32)

The timer's number within its scene; 0 for [None](MicroEngine.Scenes.TimerHandle.md#MicroEngine_Scenes_TimerHandle_None).

## Properties

### <a id="MicroEngine_Scenes_TimerHandle_Id"></a> Id

The timer's number within its scene; 0 for [None](MicroEngine.Scenes.TimerHandle.md#MicroEngine_Scenes_TimerHandle_None).

```csharp
public int Id { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Scenes_TimerHandle_IsNone"></a> IsNone

Whether this is [None](MicroEngine.Scenes.TimerHandle.md#MicroEngine_Scenes_TimerHandle_None).

```csharp
public bool IsNone { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_TimerHandle_None"></a> None

No timer.

```csharp
public static TimerHandle None { get; }
```

#### Property Value

 [TimerHandle](MicroEngine.Scenes.TimerHandle.md)

## Methods

### <a id="MicroEngine_Scenes_TimerHandle_Deconstruct_System_Int32__"></a> Deconstruct\(out int\)

```csharp
public void Deconstruct(out int Id)
```

#### Parameters

`Id` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Scenes_TimerHandle_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_TimerHandle_Equals_MicroEngine_Scenes_TimerHandle_"></a> Equals\(TimerHandle\)

```csharp
public bool Equals(TimerHandle other)
```

#### Parameters

`other` [TimerHandle](MicroEngine.Scenes.TimerHandle.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_TimerHandle_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Scenes_TimerHandle_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Scenes_TimerHandle_op_Equality_MicroEngine_Scenes_TimerHandle_MicroEngine_Scenes_TimerHandle_"></a> operator ==\(TimerHandle, TimerHandle\)

```csharp
public static bool operator ==(TimerHandle left, TimerHandle right)
```

#### Parameters

`left` [TimerHandle](MicroEngine.Scenes.TimerHandle.md)

`right` [TimerHandle](MicroEngine.Scenes.TimerHandle.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Scenes_TimerHandle_op_Inequality_MicroEngine_Scenes_TimerHandle_MicroEngine_Scenes_TimerHandle_"></a> operator \!=\(TimerHandle, TimerHandle\)

```csharp
public static bool operator !=(TimerHandle left, TimerHandle right)
```

#### Parameters

`left` [TimerHandle](MicroEngine.Scenes.TimerHandle.md)

`right` [TimerHandle](MicroEngine.Scenes.TimerHandle.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
