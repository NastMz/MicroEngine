# Enum PromptKind

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.dll  
Guide: [Input](../manual/input.md)  

What an [ActionPrompt](MicroEngine.Input.ActionPrompt.md) shows: a key, a gamepad control, an on-screen control, or a plain label.

```csharp
public enum PromptKind
```

## Fields

`Key = 0` 

A keyboard key ([Key](MicroEngine.Input.ActionPrompt.md#MicroEngine_Input_ActionPrompt_Key)).



`Label = 4` 

A label set with [SetLabel](MicroEngine.Input.ActionPrompts.md#MicroEngine_Input_ActionPrompts_SetLabel_System_String_System_Nullable_MicroEngine_Input_InputDevice__System_String_), or the action's name when nothing presses it on the device.



`PadAxis = 2` 

A direction of the left stick, a trigger or another gamepad axis.



`PadButton = 1` 

A gamepad button ([Button](MicroEngine.Input.ActionPrompt.md#MicroEngine_Input_ActionPrompt_Button), named for [PadStyle](MicroEngine.Input.ActionPrompt.md#MicroEngine_Input_ActionPrompt_PadStyle)).



`Touch = 3` 

An on-screen touch control: a button's label, or a d-pad or joystick direction.

</div>
