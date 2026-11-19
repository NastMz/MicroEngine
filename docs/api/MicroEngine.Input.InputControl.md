# Struct InputControl

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

One physical control an action can be bound to: a key, a key combination, a gamepad button, one side of a gamepad
axis, a stick direction, a mouse button, the wheel turning one way or the pointer moving one way. Bindings, rebinding
by listening, saved bindings and prompts all work on this one value, so a mouse button is rebound and saved exactly
like a key.

```csharp
public readonly struct InputControl : IEquatable<InputControl>
```

#### Implements

[IEquatable<InputControl\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Examples

```csharp
protected override InputMap CreateInput() => InputMap.CreateDefault()
    .Bind("select", PointerButton.Primary)
    .Bind("order", PointerButton.Secondary, Key.E)
    .Bind("zoom-in", InputControl.WheelUp, Key.Equals)
    .Bind("zoom-out", InputControl.WheelDown, Key.Minus);
```

## Remarks

A key, a chord, a gamepad button and a mouse button convert to a control by themselves, so
<code>map.Bind("select", PointerButton.Primary, Key.Enter)</code> reads as it should. The other kinds have a name:
[WheelUp](MicroEngine.Input.InputControl.md#MicroEngine_Input_InputControl_WheelUp), [PointerRight](MicroEngine.Input.InputControl.md#MicroEngine_Input_InputControl_PointerRight), [Of](MicroEngine.Input.InputControl.md#MicroEngine_Input_InputControl_Of_MicroEngine_Input_Key_),
[Of](MicroEngine.Input.InputControl.md#MicroEngine_Input_InputControl_Of_MicroEngine_Input_Key_). As text ([ToString](MicroEngine.Input.InputControl.md#MicroEngine_Input_InputControl_ToString), [TryParse](MicroEngine.Input.InputControl.md#MicroEngine_Input_InputControl_TryParse_System_String_MicroEngine_Input_InputControl__)) a control is its kind and
its name: <code>Key:Space</code>, <code>Mouse:Secondary</code>, <code>Wheel:Up</code>, <code>Stick:Right.Up</code>.

## Properties

### <a id="MicroEngine_Input_InputControl_Axis"></a> Axis

The axis of a gamepad-axis control.

```csharp
public PadAxis Axis { get; }
```

#### Property Value

 [PadAxis](MicroEngine.Input.PadAxis.md)

### <a id="MicroEngine_Input_InputControl_Button"></a> Button

The button of a gamepad-button control.

```csharp
public PadButton Button { get; }
```

#### Property Value

 [PadButton](MicroEngine.Input.PadButton.md)

### <a id="MicroEngine_Input_InputControl_Chord"></a> Chord

The combination of a chord control, else null.

```csharp
public KeyChord? Chord { get; }
```

#### Property Value

 [KeyChord](MicroEngine.Input.KeyChord.md)?

### <a id="MicroEngine_Input_InputControl_Device"></a> Device

The kind of device the control is on: the keyboard, a gamepad, or the mouse (buttons, wheel and motion).

```csharp
public InputDevice Device { get; }
```

#### Property Value

 [InputDevice](MicroEngine.Input.InputDevice.md)

### <a id="MicroEngine_Input_InputControl_Direction"></a> Direction

The direction of a stick, wheel or pointer-motion control (up is away from the player for the wheel).

```csharp
public InputMap.StickDirection Direction { get; }
```

#### Property Value

 [InputMap](MicroEngine.Input.InputMap.md).[StickDirection](MicroEngine.Input.InputMap.StickDirection.md)

### <a id="MicroEngine_Input_InputControl_Key"></a> Key

The key of a key control, or the key a chord presses.

```csharp
public Key Key { get; }
```

#### Property Value

 [Key](MicroEngine.Input.Key.md)

### <a id="MicroEngine_Input_InputControl_Kind"></a> Kind

What kind of control this is; [None](MicroEngine.Input.InputControlKind.md) for the default value.

```csharp
public InputControlKind Kind { get; }
```

#### Property Value

 [InputControlKind](MicroEngine.Input.InputControlKind.md)

### <a id="MicroEngine_Input_InputControl_PointerButton"></a> PointerButton

The button of a mouse-button control.

```csharp
public PointerButton PointerButton { get; }
```

#### Property Value

 [PointerButton](MicroEngine.Input.PointerButton.md)

### <a id="MicroEngine_Input_InputControl_PointerDown"></a> PointerDown

The pointer moving down.

```csharp
public static InputControl PointerDown { get; }
```

#### Property Value

 [InputControl](MicroEngine.Input.InputControl.md)

### <a id="MicroEngine_Input_InputControl_PointerLeft"></a> PointerLeft

The pointer moving left.

```csharp
public static InputControl PointerLeft { get; }
```

#### Property Value

 [InputControl](MicroEngine.Input.InputControl.md)

### <a id="MicroEngine_Input_InputControl_PointerRight"></a> PointerRight

The pointer moving right.

```csharp
public static InputControl PointerRight { get; }
```

#### Property Value

 [InputControl](MicroEngine.Input.InputControl.md)

### <a id="MicroEngine_Input_InputControl_PointerUp"></a> PointerUp

The pointer moving up.

```csharp
public static InputControl PointerUp { get; }
```

#### Property Value

 [InputControl](MicroEngine.Input.InputControl.md)

### <a id="MicroEngine_Input_InputControl_Positive"></a> Positive

For a gamepad-axis control: whether the positive side (right, down, a trigger's travel) presses.

```csharp
public bool Positive { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_InputControl_Stick"></a> Stick

The stick of a stick control.

```csharp
public PadStick Stick { get; }
```

#### Property Value

 [PadStick](MicroEngine.Input.PadStick.md)

### <a id="MicroEngine_Input_InputControl_WheelDown"></a> WheelDown

The wheel turning towards the player (scrolling down).

```csharp
public static InputControl WheelDown { get; }
```

#### Property Value

 [InputControl](MicroEngine.Input.InputControl.md)

### <a id="MicroEngine_Input_InputControl_WheelLeft"></a> WheelLeft

The wheel turning left (wheels that tilt, touchpads).

```csharp
public static InputControl WheelLeft { get; }
```

#### Property Value

 [InputControl](MicroEngine.Input.InputControl.md)

### <a id="MicroEngine_Input_InputControl_WheelRight"></a> WheelRight

The wheel turning right (wheels that tilt, touchpads).

```csharp
public static InputControl WheelRight { get; }
```

#### Property Value

 [InputControl](MicroEngine.Input.InputControl.md)

### <a id="MicroEngine_Input_InputControl_WheelUp"></a> WheelUp

The wheel turning away from the player (scrolling up).

```csharp
public static InputControl WheelUp { get; }
```

#### Property Value

 [InputControl](MicroEngine.Input.InputControl.md)

## Methods

### <a id="MicroEngine_Input_InputControl_Equals_MicroEngine_Input_InputControl_"></a> Equals\(InputControl\)

```csharp
public bool Equals(InputControl other)
```

#### Parameters

`other` [InputControl](MicroEngine.Input.InputControl.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_InputControl_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_InputControl_FromPress_MicroEngine_Input_InputEvent__"></a> FromPress\(in InputEvent\)

The control a press is on, for a rebinding screen: the key of a key-down, the button of a gamepad button-down, the
button of a mouse button-down, or the way a wheel event turned (its larger component). Null for anything else,
and for a finger or pen (a touch is not a control to rebind).

```csharp
public static InputControl? FromPress(in InputEvent press)
```

#### Parameters

`press` [InputEvent](MicroEngine.Input.InputEvent.md)

#### Returns

 [InputControl](MicroEngine.Input.InputControl.md)?

### <a id="MicroEngine_Input_InputControl_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Input_InputControl_Of_MicroEngine_Input_Key_"></a> Of\(Key\)

A keyboard key.

```csharp
public static InputControl Of(Key key)
```

#### Parameters

`key` [Key](MicroEngine.Input.Key.md)

#### Returns

 [InputControl](MicroEngine.Input.InputControl.md)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">key</code> is [None](MicroEngine.Input.Key.md).

### <a id="MicroEngine_Input_InputControl_Of_MicroEngine_Input_KeyChord_"></a> Of\(KeyChord\)

A key combination.

```csharp
public static InputControl Of(KeyChord chord)
```

#### Parameters

`chord` [KeyChord](MicroEngine.Input.KeyChord.md)

#### Returns

 [InputControl](MicroEngine.Input.InputControl.md)

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">chord</code> is null.

### <a id="MicroEngine_Input_InputControl_Of_MicroEngine_Input_PadButton_"></a> Of\(PadButton\)

A gamepad button.

```csharp
public static InputControl Of(PadButton button)
```

#### Parameters

`button` [PadButton](MicroEngine.Input.PadButton.md)

#### Returns

 [InputControl](MicroEngine.Input.InputControl.md)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">button</code> is [None](MicroEngine.Input.PadButton.md).

### <a id="MicroEngine_Input_InputControl_Of_MicroEngine_Input_PointerButton_"></a> Of\(PointerButton\)

A mouse button. The primary button is also a finger or a pen on the screen.

```csharp
public static InputControl Of(PointerButton button)
```

#### Parameters

`button` [PointerButton](MicroEngine.Input.PointerButton.md)

#### Returns

 [InputControl](MicroEngine.Input.InputControl.md)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">button</code> is [None](MicroEngine.Input.PointerButton.md).

### <a id="MicroEngine_Input_InputControl_Of_MicroEngine_Input_PadAxis_System_Boolean_"></a> Of\(PadAxis, bool\)

One side of a gamepad axis: a trigger, or half of a stick axis.

```csharp
public static InputControl Of(PadAxis axis, bool positive = true)
```

#### Parameters

`axis` [PadAxis](MicroEngine.Input.PadAxis.md)

`positive` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [InputControl](MicroEngine.Input.InputControl.md)

### <a id="MicroEngine_Input_InputControl_Of_MicroEngine_Input_PadStick_MicroEngine_Input_InputMap_StickDirection_"></a> Of\(PadStick, StickDirection\)

A stick pointing one way.

```csharp
public static InputControl Of(PadStick stick, InputMap.StickDirection direction)
```

#### Parameters

`stick` [PadStick](MicroEngine.Input.PadStick.md)

`direction` [InputMap](MicroEngine.Input.InputMap.md).[StickDirection](MicroEngine.Input.InputMap.StickDirection.md)

#### Returns

 [InputControl](MicroEngine.Input.InputControl.md)

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">direction</code> is <code>None</code>.

### <a id="MicroEngine_Input_InputControl_ToString"></a> ToString\(\)

The control as text, the form saved bindings use: <code>Key:Space</code>, <code>Chord:LeftAlt|RightAlt+Enter</code>,
<code>Pad:A</code>, <code>Mouse:Secondary</code>, <code>Wheel:Up</code>, <code>Stick:Right.Up</code>, <code>Axis:RightTrigger+</code>,
<code>Motion:Left</code>. Names are the enum names, so the text stays valid when the engine adds keys.

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_InputControl_TryParse_System_String_MicroEngine_Input_InputControl__"></a> TryParse\(string?, out InputControl\)

Reads a control written by [ToString](MicroEngine.Input.InputControl.md#MicroEngine_Input_InputControl_ToString). False for text that is not a control, or that names a kind,
key, button or axis this version does not know (saved bindings skip those).

```csharp
public static bool TryParse(string? text, out InputControl control)
```

#### Parameters

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)?

`control` [InputControl](MicroEngine.Input.InputControl.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Operators

### <a id="MicroEngine_Input_InputControl_op_Equality_MicroEngine_Input_InputControl_MicroEngine_Input_InputControl_"></a> operator ==\(InputControl, InputControl\)

Whether two controls are the same physical control.

```csharp
public static bool operator ==(InputControl left, InputControl right)
```

#### Parameters

`left` [InputControl](MicroEngine.Input.InputControl.md)

`right` [InputControl](MicroEngine.Input.InputControl.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_InputControl_op_Implicit_MicroEngine_Input_Key__MicroEngine_Input_InputControl"></a> implicit operator InputControl\(Key\)

A key as a control.

```csharp
public static implicit operator InputControl(Key key)
```

#### Parameters

`key` [Key](MicroEngine.Input.Key.md)

#### Returns

 [InputControl](MicroEngine.Input.InputControl.md)

### <a id="MicroEngine_Input_InputControl_op_Implicit_MicroEngine_Input_KeyChord__MicroEngine_Input_InputControl"></a> implicit operator InputControl\(KeyChord\)

A key combination as a control.

```csharp
public static implicit operator InputControl(KeyChord chord)
```

#### Parameters

`chord` [KeyChord](MicroEngine.Input.KeyChord.md)

#### Returns

 [InputControl](MicroEngine.Input.InputControl.md)

### <a id="MicroEngine_Input_InputControl_op_Implicit_MicroEngine_Input_PadButton__MicroEngine_Input_InputControl"></a> implicit operator InputControl\(PadButton\)

A gamepad button as a control.

```csharp
public static implicit operator InputControl(PadButton button)
```

#### Parameters

`button` [PadButton](MicroEngine.Input.PadButton.md)

#### Returns

 [InputControl](MicroEngine.Input.InputControl.md)

### <a id="MicroEngine_Input_InputControl_op_Implicit_MicroEngine_Input_PointerButton__MicroEngine_Input_InputControl"></a> implicit operator InputControl\(PointerButton\)

A mouse button as a control.

```csharp
public static implicit operator InputControl(PointerButton button)
```

#### Parameters

`button` [PointerButton](MicroEngine.Input.PointerButton.md)

#### Returns

 [InputControl](MicroEngine.Input.InputControl.md)

### <a id="MicroEngine_Input_InputControl_op_Inequality_MicroEngine_Input_InputControl_MicroEngine_Input_InputControl_"></a> operator \!=\(InputControl, InputControl\)

Whether two controls differ.

```csharp
public static bool operator !=(InputControl left, InputControl right)
```

#### Parameters

`left` [InputControl](MicroEngine.Input.InputControl.md)

`right` [InputControl](MicroEngine.Input.InputControl.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
