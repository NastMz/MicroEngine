# Struct InputEvent

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

One thing that happened on an input device, in the order it happened, with the time it happened on the host's
monotonic clock. Backends deliver them ([IInputBackend](MicroEngine.Input.IInputBackend.md)); the engine routes them through the input
layers (UI, on-screen controls, then gameplay) and derives the polled state actions read from those no layer took.
Build one with the static methods ([KeyDown](MicroEngine.Input.InputEvent.md#MicroEngine_Input_InputEvent_KeyDown_MicroEngine_Input_DeviceId_MicroEngine_Input_Key_System_Double_System_Boolean_), [PointerDown](MicroEngine.Input.InputEvent.md#MicroEngine_Input_InputEvent_PointerDown_MicroEngine_Input_DeviceId_System_Int64_MicroEngine_Input_PointerKind_System_Numerics_Vector2_MicroEngine_Input_PointerButton_System_Double_)...).

```csharp
public readonly struct InputEvent : IEquatable<InputEvent>
```

#### Implements

[IEquatable<InputEvent\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Properties

### <a id="MicroEngine_Input_InputEvent_Axis"></a> Axis

The gamepad axis of axis events.

```csharp
public PadAxis Axis { get; init; }
```

#### Property Value

 [PadAxis](MicroEngine.Input.PadAxis.md)

### <a id="MicroEngine_Input_InputEvent_Button"></a> Button

The gamepad button of button events.

```csharp
public PadButton Button { get; init; }
```

#### Property Value

 [PadButton](MicroEngine.Input.PadButton.md)

### <a id="MicroEngine_Input_InputEvent_Device"></a> Device

The device it happened on.

```csharp
public DeviceId Device { get; init; }
```

#### Property Value

 [DeviceId](MicroEngine.Input.DeviceId.md)

### <a id="MicroEngine_Input_InputEvent_Info"></a> Info

The device of [DeviceAdded](MicroEngine.Input.InputEventKind.md).

```csharp
public InputDeviceInfo? Info { get; init; }
```

#### Property Value

 [InputDeviceInfo](MicroEngine.Input.InputDeviceInfo.md)?

### <a id="MicroEngine_Input_InputEvent_IsPress"></a> IsPress

Whether this is a press: a key-down that is not a repeat, a pointer-down or a gamepad button-down.

```csharp
public bool IsPress { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_InputEvent_Key"></a> Key

The key of key events.

```csharp
public Key Key { get; init; }
```

#### Property Value

 [Key](MicroEngine.Input.Key.md)

### <a id="MicroEngine_Input_InputEvent_Kind"></a> Kind

What happened.

```csharp
public InputEventKind Kind { get; init; }
```

#### Property Value

 [InputEventKind](MicroEngine.Input.InputEventKind.md)

### <a id="MicroEngine_Input_InputEvent_Pointer"></a> Pointer

The pointer of pointer events: 0 for the mouse, the finger's id for touches.

```csharp
public long Pointer { get; init; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Input_InputEvent_PointerButton"></a> PointerButton

The pointer button of pointer-down and pointer-up events.

```csharp
public PointerButton PointerButton { get; init; }
```

#### Property Value

 [PointerButton](MicroEngine.Input.PointerButton.md)

### <a id="MicroEngine_Input_InputEvent_PointerKind"></a> PointerKind

The kind of pointer.

```csharp
public PointerKind PointerKind { get; init; }
```

#### Property Value

 [PointerKind](MicroEngine.Input.PointerKind.md)

### <a id="MicroEngine_Input_InputEvent_Position"></a> Position

The pointer's position in virtual canvas pixels (may lie outside the canvas, in the letterbox).

```csharp
public Vector2 Position { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Input_InputEvent_Repeat"></a> Repeat

Whether a key-down is the system's auto-repeat of a held key (it does not change what is held).

```csharp
public bool Repeat { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_InputEvent_ScreenPosition"></a> ScreenPosition

The pointer's position in the window's pixels, for overlays drawn in screen pixels (touches; the mouse when known).

```csharp
public Vector2 ScreenPosition { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Input_InputEvent_Text"></a> Text

The text of text and composition events.

```csharp
public string? Text { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Input_InputEvent_Time"></a> Time

When, in seconds of the host's monotonic clock (0 for scripted and polled events).

```csharp
public double Time { get; init; }
```

#### Property Value

 [double](https://learn.microsoft.com/dotnet/api/system.double)

### <a id="MicroEngine_Input_InputEvent_Value"></a> Value

The axis value: sticks -1..1 (right and down positive), triggers 0..1.

```csharp
public float Value { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Input_InputEvent_Wheel"></a> Wheel

How far the wheel turned: Y positive away from the player, X positive to the right, in notches.

```csharp
public Vector2 Wheel { get; init; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

## Methods

### <a id="MicroEngine_Input_InputEvent_Added_MicroEngine_Input_InputDeviceInfo_System_Double_"></a> Added\(InputDeviceInfo, double\)

A device was connected.

```csharp
public static InputEvent Added(InputDeviceInfo info, double time = 0)
```

#### Parameters

`info` [InputDeviceInfo](MicroEngine.Input.InputDeviceInfo.md)

`time` [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Returns

 [InputEvent](MicroEngine.Input.InputEvent.md)

### <a id="MicroEngine_Input_InputEvent_AxisMoved_MicroEngine_Input_DeviceId_MicroEngine_Input_PadAxis_System_Single_System_Double_"></a> AxisMoved\(DeviceId, PadAxis, float, double\)

A gamepad axis moved.

```csharp
public static InputEvent AxisMoved(DeviceId device, PadAxis axis, float value, double time = 0)
```

#### Parameters

`device` [DeviceId](MicroEngine.Input.DeviceId.md)

`axis` [PadAxis](MicroEngine.Input.PadAxis.md)

`value` [float](https://learn.microsoft.com/dotnet/api/system.single)

`time` [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Returns

 [InputEvent](MicroEngine.Input.InputEvent.md)

### <a id="MicroEngine_Input_InputEvent_ButtonDown_MicroEngine_Input_DeviceId_MicroEngine_Input_PadButton_System_Double_"></a> ButtonDown\(DeviceId, PadButton, double\)

A gamepad button went down.

```csharp
public static InputEvent ButtonDown(DeviceId device, PadButton button, double time = 0)
```

#### Parameters

`device` [DeviceId](MicroEngine.Input.DeviceId.md)

`button` [PadButton](MicroEngine.Input.PadButton.md)

`time` [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Returns

 [InputEvent](MicroEngine.Input.InputEvent.md)

### <a id="MicroEngine_Input_InputEvent_ButtonUp_MicroEngine_Input_DeviceId_MicroEngine_Input_PadButton_System_Double_"></a> ButtonUp\(DeviceId, PadButton, double\)

A gamepad button went up.

```csharp
public static InputEvent ButtonUp(DeviceId device, PadButton button, double time = 0)
```

#### Parameters

`device` [DeviceId](MicroEngine.Input.DeviceId.md)

`button` [PadButton](MicroEngine.Input.PadButton.md)

`time` [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Returns

 [InputEvent](MicroEngine.Input.InputEvent.md)

### <a id="MicroEngine_Input_InputEvent_Composition_MicroEngine_Input_DeviceId_System_String_System_Double_"></a> Composition\(DeviceId, string, double\)

The text being composed changed.

```csharp
public static InputEvent Composition(DeviceId device, string text, double time = 0)
```

#### Parameters

`device` [DeviceId](MicroEngine.Input.DeviceId.md)

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

`time` [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Returns

 [InputEvent](MicroEngine.Input.InputEvent.md)

### <a id="MicroEngine_Input_InputEvent_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_InputEvent_Equals_MicroEngine_Input_InputEvent_"></a> Equals\(InputEvent\)

```csharp
public bool Equals(InputEvent other)
```

#### Parameters

`other` [InputEvent](MicroEngine.Input.InputEvent.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_InputEvent_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Input_InputEvent_KeyDown_MicroEngine_Input_DeviceId_MicroEngine_Input_Key_System_Double_System_Boolean_"></a> KeyDown\(DeviceId, Key, double, bool\)

A key went down.

```csharp
public static InputEvent KeyDown(DeviceId device, Key key, double time = 0, bool repeat = false)
```

#### Parameters

`device` [DeviceId](MicroEngine.Input.DeviceId.md)

`key` [Key](MicroEngine.Input.Key.md)

`time` [double](https://learn.microsoft.com/dotnet/api/system.double)

`repeat` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [InputEvent](MicroEngine.Input.InputEvent.md)

### <a id="MicroEngine_Input_InputEvent_KeyUp_MicroEngine_Input_DeviceId_MicroEngine_Input_Key_System_Double_"></a> KeyUp\(DeviceId, Key, double\)

A key went up.

```csharp
public static InputEvent KeyUp(DeviceId device, Key key, double time = 0)
```

#### Parameters

`device` [DeviceId](MicroEngine.Input.DeviceId.md)

`key` [Key](MicroEngine.Input.Key.md)

`time` [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Returns

 [InputEvent](MicroEngine.Input.InputEvent.md)

### <a id="MicroEngine_Input_InputEvent_PointerDown_MicroEngine_Input_DeviceId_System_Int64_MicroEngine_Input_PointerKind_System_Numerics_Vector2_MicroEngine_Input_PointerButton_System_Double_"></a> PointerDown\(DeviceId, long, PointerKind, Vector2, PointerButton, double\)

A pointer button went down or a finger touched.

```csharp
public static InputEvent PointerDown(DeviceId device, long pointer, PointerKind kind, Vector2 position, PointerButton button = PointerButton.Primary, double time = 0)
```

#### Parameters

`device` [DeviceId](MicroEngine.Input.DeviceId.md)

`pointer` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`kind` [PointerKind](MicroEngine.Input.PointerKind.md)

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`button` [PointerButton](MicroEngine.Input.PointerButton.md)

`time` [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Returns

 [InputEvent](MicroEngine.Input.InputEvent.md)

### <a id="MicroEngine_Input_InputEvent_PointerMove_MicroEngine_Input_DeviceId_System_Int64_MicroEngine_Input_PointerKind_System_Numerics_Vector2_System_Double_"></a> PointerMove\(DeviceId, long, PointerKind, Vector2, double\)

A pointer moved.

```csharp
public static InputEvent PointerMove(DeviceId device, long pointer, PointerKind kind, Vector2 position, double time = 0)
```

#### Parameters

`device` [DeviceId](MicroEngine.Input.DeviceId.md)

`pointer` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`kind` [PointerKind](MicroEngine.Input.PointerKind.md)

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`time` [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Returns

 [InputEvent](MicroEngine.Input.InputEvent.md)

### <a id="MicroEngine_Input_InputEvent_PointerUp_MicroEngine_Input_DeviceId_System_Int64_MicroEngine_Input_PointerKind_System_Numerics_Vector2_MicroEngine_Input_PointerButton_System_Double_"></a> PointerUp\(DeviceId, long, PointerKind, Vector2, PointerButton, double\)

A pointer button went up or a finger lifted.

```csharp
public static InputEvent PointerUp(DeviceId device, long pointer, PointerKind kind, Vector2 position, PointerButton button = PointerButton.Primary, double time = 0)
```

#### Parameters

`device` [DeviceId](MicroEngine.Input.DeviceId.md)

`pointer` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`kind` [PointerKind](MicroEngine.Input.PointerKind.md)

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`button` [PointerButton](MicroEngine.Input.PointerButton.md)

`time` [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Returns

 [InputEvent](MicroEngine.Input.InputEvent.md)

### <a id="MicroEngine_Input_InputEvent_Removed_MicroEngine_Input_DeviceId_System_Double_"></a> Removed\(DeviceId, double\)

A device was disconnected.

```csharp
public static InputEvent Removed(DeviceId device, double time = 0)
```

#### Parameters

`device` [DeviceId](MicroEngine.Input.DeviceId.md)

`time` [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Returns

 [InputEvent](MicroEngine.Input.InputEvent.md)

### <a id="MicroEngine_Input_InputEvent_TextInput_MicroEngine_Input_DeviceId_System_String_System_Double_"></a> TextInput\(DeviceId, string, double\)

Text was entered.

```csharp
public static InputEvent TextInput(DeviceId device, string text, double time = 0)
```

#### Parameters

`device` [DeviceId](MicroEngine.Input.DeviceId.md)

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

`time` [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Returns

 [InputEvent](MicroEngine.Input.InputEvent.md)

### <a id="MicroEngine_Input_InputEvent_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_InputEvent_WheelMoved_MicroEngine_Input_DeviceId_System_Numerics_Vector2_System_Numerics_Vector2_System_Double_"></a> WheelMoved\(DeviceId, Vector2, Vector2, double\)

The wheel turned.

```csharp
public static InputEvent WheelMoved(DeviceId device, Vector2 wheel, Vector2 position = default, double time = 0)
```

#### Parameters

`device` [DeviceId](MicroEngine.Input.DeviceId.md)

`wheel` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`time` [double](https://learn.microsoft.com/dotnet/api/system.double)

#### Returns

 [InputEvent](MicroEngine.Input.InputEvent.md)

## Operators

### <a id="MicroEngine_Input_InputEvent_op_Equality_MicroEngine_Input_InputEvent_MicroEngine_Input_InputEvent_"></a> operator ==\(InputEvent, InputEvent\)

```csharp
public static bool operator ==(InputEvent left, InputEvent right)
```

#### Parameters

`left` [InputEvent](MicroEngine.Input.InputEvent.md)

`right` [InputEvent](MicroEngine.Input.InputEvent.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_InputEvent_op_Inequality_MicroEngine_Input_InputEvent_MicroEngine_Input_InputEvent_"></a> operator \!=\(InputEvent, InputEvent\)

```csharp
public static bool operator !=(InputEvent left, InputEvent right)
```

#### Parameters

`left` [InputEvent](MicroEngine.Input.InputEvent.md)

`right` [InputEvent](MicroEngine.Input.InputEvent.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
