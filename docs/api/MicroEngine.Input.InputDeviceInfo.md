# Class InputDeviceInfo

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

A connected device as its backend describes it.

```csharp
public sealed record InputDeviceInfo : IEquatable<InputDeviceInfo>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[InputDeviceInfo](MicroEngine.Input.InputDeviceInfo.md)

#### Implements

[IEquatable<InputDeviceInfo\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Input_InputDeviceInfo__ctor_MicroEngine_Input_DeviceId_MicroEngine_Input_DeviceKind_System_String_"></a> InputDeviceInfo\(DeviceId, DeviceKind, string\)

A connected device as its backend describes it.

```csharp
public InputDeviceInfo(DeviceId Id, DeviceKind Kind, string Name)
```

#### Parameters

`Id` [DeviceId](MicroEngine.Input.DeviceId.md)

Its id.

`Kind` [DeviceKind](MicroEngine.Input.DeviceKind.md)

Keyboard, mouse, touch screen, gamepad or other.

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

The name the system reports ("Xbox Series X Controller"), or a generic one.

## Properties

### <a id="MicroEngine_Input_InputDeviceInfo_CanRumble"></a> CanRumble

For gamepads: whether [Rumble](MicroEngine.Input.IGamepadRumble.md#MicroEngine_Input_IGamepadRumble_Rumble_MicroEngine_Input_DeviceId_System_Single_System_Single_System_Double_) can do anything on it.

```csharp
public bool CanRumble { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_InputDeviceInfo_Id"></a> Id

Its id.

```csharp
public DeviceId Id { get; init; }
```

#### Property Value

 [DeviceId](MicroEngine.Input.DeviceId.md)

### <a id="MicroEngine_Input_InputDeviceInfo_Kind"></a> Kind

Keyboard, mouse, touch screen, gamepad or other.

```csharp
public DeviceKind Kind { get; init; }
```

#### Property Value

 [DeviceKind](MicroEngine.Input.DeviceKind.md)

### <a id="MicroEngine_Input_InputDeviceInfo_Name"></a> Name

The name the system reports ("Xbox Series X Controller"), or a generic one.

```csharp
public string Name { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_InputDeviceInfo_PlayerIndex"></a> PlayerIndex

For gamepads: the player number the system or the pad shows (its LEDs), 0-based, or -1 when it has none.

```csharp
public int PlayerIndex { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Input_InputDeviceInfo_Style"></a> Style

For gamepads: the family whose names its buttons carry (Xbox when unknown).

```csharp
public GamepadStyle Style { get; init; }
```

#### Property Value

 [GamepadStyle](MicroEngine.Input.GamepadStyle.md)

## Methods

### <a id="MicroEngine_Input_InputDeviceInfo__Clone__"></a> <Clone\>$\(\)

```csharp
public InputDeviceInfo <Clone>$()
```

#### Returns

 [InputDeviceInfo](MicroEngine.Input.InputDeviceInfo.md)

### <a id="MicroEngine_Input_InputDeviceInfo_Deconstruct_MicroEngine_Input_DeviceId__MicroEngine_Input_DeviceKind__System_String__"></a> Deconstruct\(out DeviceId, out DeviceKind, out string\)

```csharp
public void Deconstruct(out DeviceId Id, out DeviceKind Kind, out string Name)
```

#### Parameters

`Id` [DeviceId](MicroEngine.Input.DeviceId.md)

`Kind` [DeviceKind](MicroEngine.Input.DeviceKind.md)

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_InputDeviceInfo_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_InputDeviceInfo_Equals_MicroEngine_Input_InputDeviceInfo_"></a> Equals\(InputDeviceInfo?\)

```csharp
public bool Equals(InputDeviceInfo? other)
```

#### Parameters

`other` [InputDeviceInfo](MicroEngine.Input.InputDeviceInfo.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_InputDeviceInfo_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Input_InputDeviceInfo_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Input_InputDeviceInfo_op_Equality_MicroEngine_Input_InputDeviceInfo_MicroEngine_Input_InputDeviceInfo_"></a> operator ==\(InputDeviceInfo?, InputDeviceInfo?\)

```csharp
public static bool operator ==(InputDeviceInfo? left, InputDeviceInfo? right)
```

#### Parameters

`left` [InputDeviceInfo](MicroEngine.Input.InputDeviceInfo.md)?

`right` [InputDeviceInfo](MicroEngine.Input.InputDeviceInfo.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_InputDeviceInfo_op_Inequality_MicroEngine_Input_InputDeviceInfo_MicroEngine_Input_InputDeviceInfo_"></a> operator \!=\(InputDeviceInfo?, InputDeviceInfo?\)

```csharp
public static bool operator !=(InputDeviceInfo? left, InputDeviceInfo? right)
```

#### Parameters

`left` [InputDeviceInfo](MicroEngine.Input.InputDeviceInfo.md)?

`right` [InputDeviceInfo](MicroEngine.Input.InputDeviceInfo.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
