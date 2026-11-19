# Enum InputControlKind

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

What kind of physical control an [InputControl](MicroEngine.Input.InputControl.md) is. The order is the order a map reads an action's
controls in: the digital ones first (a key before a gamepad button before a mouse button), then the analog ones.

```csharp
public enum InputControlKind
```

## Fields

`Chord = 2` 

A key pressed while a modifier is held ([Chord](MicroEngine.Input.InputControl.md#MicroEngine_Input_InputControl_Chord)).



`Key = 1` 

A keyboard key ([Key](MicroEngine.Input.InputControl.md#MicroEngine_Input_InputControl_Key)).



`None = 0` 

No control.



`PadAxis = 7` 

One side of a gamepad axis ([Axis](MicroEngine.Input.InputControl.md#MicroEngine_Input_InputControl_Axis), [Positive](MicroEngine.Input.InputControl.md#MicroEngine_Input_InputControl_Positive)).



`PadButton = 3` 

A gamepad button ([Button](MicroEngine.Input.InputControl.md#MicroEngine_Input_InputControl_Button)).



`PointerButton = 4` 

A mouse button ([PointerButton](MicroEngine.Input.InputControl.md#MicroEngine_Input_InputControl_PointerButton)); the primary one is also a finger or pen on the screen.



`PointerMotion = 8` 

The pointer moving one way ([Direction](MicroEngine.Input.InputControl.md#MicroEngine_Input_InputControl_Direction)): a value, never a press.



`Stick = 6` 

A stick pointing one way ([Stick](MicroEngine.Input.InputControl.md#MicroEngine_Input_InputControl_Stick), [Direction](MicroEngine.Input.InputControl.md#MicroEngine_Input_InputControl_Direction)).



`Wheel = 5` 

The wheel turning one way ([Direction](MicroEngine.Input.InputControl.md#MicroEngine_Input_InputControl_Direction)): pressed for one update per notch.

</div>
