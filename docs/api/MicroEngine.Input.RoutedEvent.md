# Struct RoutedEvent

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

One event of the frame and where it went ([Routed](MicroEngine.Input.InputSystem.md#MicroEngine_Input_InputSystem_Routed)).

```csharp
public readonly struct RoutedEvent : IEquatable<RoutedEvent>
```

#### Implements

[IEquatable<RoutedEvent\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Input_RoutedEvent__ctor_MicroEngine_Input_InputEvent_MicroEngine_Input_IInputLayer_"></a> RoutedEvent\(InputEvent, IInputLayer?\)

One event of the frame and where it went ([Routed](MicroEngine.Input.InputSystem.md#MicroEngine_Input_InputSystem_Routed)).

```csharp
public RoutedEvent(InputEvent Event, IInputLayer? Layer)
```

#### Parameters

`Event` [InputEvent](MicroEngine.Input.InputEvent.md)

The event.

`Layer` [IInputLayer](MicroEngine.Input.IInputLayer.md)?

The input layer that took it (or that took the press it ends), or null when it reached the devices and gameplay.

## Properties

### <a id="MicroEngine_Input_RoutedEvent_Event"></a> Event

The event.

```csharp
public InputEvent Event { get; init; }
```

#### Property Value

 [InputEvent](MicroEngine.Input.InputEvent.md)

### <a id="MicroEngine_Input_RoutedEvent_Layer"></a> Layer

The input layer that took it (or that took the press it ends), or null when it reached the devices and gameplay.

```csharp
public IInputLayer? Layer { get; init; }
```

#### Property Value

 [IInputLayer](MicroEngine.Input.IInputLayer.md)?

## Methods

### <a id="MicroEngine_Input_RoutedEvent_Deconstruct_MicroEngine_Input_InputEvent__MicroEngine_Input_IInputLayer__"></a> Deconstruct\(out InputEvent, out IInputLayer?\)

```csharp
public void Deconstruct(out InputEvent Event, out IInputLayer? Layer)
```

#### Parameters

`Event` [InputEvent](MicroEngine.Input.InputEvent.md)

`Layer` [IInputLayer](MicroEngine.Input.IInputLayer.md)?

### <a id="MicroEngine_Input_RoutedEvent_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_RoutedEvent_Equals_MicroEngine_Input_RoutedEvent_"></a> Equals\(RoutedEvent\)

```csharp
public bool Equals(RoutedEvent other)
```

#### Parameters

`other` [RoutedEvent](MicroEngine.Input.RoutedEvent.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_RoutedEvent_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Input_RoutedEvent_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Input_RoutedEvent_op_Equality_MicroEngine_Input_RoutedEvent_MicroEngine_Input_RoutedEvent_"></a> operator ==\(RoutedEvent, RoutedEvent\)

```csharp
public static bool operator ==(RoutedEvent left, RoutedEvent right)
```

#### Parameters

`left` [RoutedEvent](MicroEngine.Input.RoutedEvent.md)

`right` [RoutedEvent](MicroEngine.Input.RoutedEvent.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_RoutedEvent_op_Inequality_MicroEngine_Input_RoutedEvent_MicroEngine_Input_RoutedEvent_"></a> operator \!=\(RoutedEvent, RoutedEvent\)

```csharp
public static bool operator !=(RoutedEvent left, RoutedEvent right)
```

#### Parameters

`left` [RoutedEvent](MicroEngine.Input.RoutedEvent.md)

`right` [RoutedEvent](MicroEngine.Input.RoutedEvent.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
