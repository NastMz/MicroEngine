# Enum InputEventKind

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

What an [InputEvent](MicroEngine.Input.InputEvent.md) reports.

```csharp
public enum InputEventKind
```

## Fields

`AxisMoved = 14` 

A gamepad axis moved ([Axis](MicroEngine.Input.InputEvent.md#MicroEngine_Input_InputEvent_Axis), [Value](MicroEngine.Input.InputEvent.md#MicroEngine_Input_InputEvent_Value)).



`ButtonDown = 12` 

A gamepad button went down ([Button](MicroEngine.Input.InputEvent.md#MicroEngine_Input_InputEvent_Button)).



`ButtonUp = 13` 

A gamepad button went up.



`DeviceAdded = 0` 

A device was connected ([Info](MicroEngine.Input.InputEvent.md#MicroEngine_Input_InputEvent_Info) describes it). Devices present at start are reported too.



`DeviceRemoved = 1` 

A device was disconnected; every key, button and pointer it held is released by the events before it.



`KeyDown = 2` 

A key went down ([Key](MicroEngine.Input.InputEvent.md#MicroEngine_Input_InputEvent_Key)); [Repeat](MicroEngine.Input.InputEvent.md#MicroEngine_Input_InputEvent_Repeat) for the system's auto-repeat.



`KeyUp = 3` 

A key went up.



`PointerCancel = 9` 

The system took a finger away (the game lost the screen); handle it as an up that must not act.



`PointerDown = 6` 

A pointer button went down, or a finger or pen touched ([Pointer](MicroEngine.Input.InputEvent.md#MicroEngine_Input_InputEvent_Pointer), [Button](MicroEngine.Input.InputEvent.md#MicroEngine_Input_InputEvent_Button)).



`PointerLeave = 10` 

The mouse left the window.



`PointerMove = 8` 

A pointer moved ([Position](MicroEngine.Input.InputEvent.md#MicroEngine_Input_InputEvent_Position)).



`PointerUp = 7` 

A pointer button went up, or a finger or pen left the screen.



`Text = 4` 

Text was entered while text input is on ([Text](MicroEngine.Input.InputEvent.md#MicroEngine_Input_InputEvent_Text)): characters, never keys.



`TextComposition = 5` 

The text being composed by an input method changed ([Text](MicroEngine.Input.InputEvent.md#MicroEngine_Input_InputEvent_Text), empty when composition ended).



`Wheel = 11` 

The wheel turned ([Wheel](MicroEngine.Input.InputEvent.md#MicroEngine_Input_InputEvent_Wheel)).

</div>
