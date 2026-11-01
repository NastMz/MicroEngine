# Struct TouchEvent

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.dll  
Guide: [Input](../manual/input.md)  

One change of one finger, in the touch screen's pixels ([Size](MicroEngine.Input.ITouchScreen.md#MicroEngine_Input_ITouchScreen_Size)).

```csharp
public readonly struct TouchEvent : IEquatable<TouchEvent>
```

#### Implements

[IEquatable<TouchEvent\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Input_TouchEvent__ctor_System_Int64_MicroEngine_Input_TouchPhase_System_Numerics_Vector2_"></a> TouchEvent\(long, TouchPhase, Vector2\)

One change of one finger, in the touch screen's pixels ([Size](MicroEngine.Input.ITouchScreen.md#MicroEngine_Input_ITouchScreen_Size)).

```csharp
public TouchEvent(long Finger, TouchPhase Phase, Vector2 Position)
```

#### Parameters

`Finger` [long](https://learn.microsoft.com/dotnet/api/system.int64)

Identifies the finger from [Began](MicroEngine.Input.TouchPhase.md) until it ends or is cancelled.

`Phase` [TouchPhase](MicroEngine.Input.TouchPhase.md)

`Position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

## Properties

### <a id="MicroEngine_Input_TouchEvent_Finger"></a> Finger

Identifies the finger from [Began](MicroEngine.Input.TouchPhase.md) until it ends or is cancelled.

```csharp
public long Finger { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Input_TouchEvent_Phase"></a> Phase

```csharp
public TouchPhase Phase { get; init; }
```

#### Property Value

 [TouchPhase](MicroEngine.Input.TouchPhase.md)

### <a id="MicroEngine_Input_TouchEvent_Position"></a> Position

```csharp
public Vector2 Position { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

## Methods

### <a id="MicroEngine_Input_TouchEvent_Deconstruct_System_Int64__MicroEngine_Input_TouchPhase__System_Numerics_Vector2__"></a> Deconstruct\(out long, out TouchPhase, out Vector2\)

```csharp
public void Deconstruct(out long Finger, out TouchPhase Phase, out Vector2 Position)
```

#### Parameters

`Finger` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`Phase` [TouchPhase](MicroEngine.Input.TouchPhase.md)

`Position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Input_TouchEvent_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchEvent_Equals_MicroEngine_Input_TouchEvent_"></a> Equals\(TouchEvent\)

```csharp
public bool Equals(TouchEvent other)
```

#### Parameters

`other` [TouchEvent](MicroEngine.Input.TouchEvent.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchEvent_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Input_TouchEvent_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Input_TouchEvent_op_Equality_MicroEngine_Input_TouchEvent_MicroEngine_Input_TouchEvent_"></a> operator ==\(TouchEvent, TouchEvent\)

```csharp
public static bool operator ==(TouchEvent left, TouchEvent right)
```

#### Parameters

`left` [TouchEvent](MicroEngine.Input.TouchEvent.md)

`right` [TouchEvent](MicroEngine.Input.TouchEvent.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TouchEvent_op_Inequality_MicroEngine_Input_TouchEvent_MicroEngine_Input_TouchEvent_"></a> operator \!=\(TouchEvent, TouchEvent\)

```csharp
public static bool operator !=(TouchEvent left, TouchEvent right)
```

#### Parameters

`left` [TouchEvent](MicroEngine.Input.TouchEvent.md)

`right` [TouchEvent](MicroEngine.Input.TouchEvent.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
