# Enum InputDevice

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

The kind of device that last pressed an action (see [LastDevice](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_LastDevice)).

```csharp
public enum InputDevice
```

## Fields

`Gamepad = 2` 

`Keyboard = 1` 

`Mouse = 5` 

`None = 0` 

`Other = 4` 

`Touch = 3` 

## Remarks

<code>Other</code> covers scripted input and custom [IActionSource](MicroEngine.Input.IActionSource.md)s. <code>Mouse</code> is a mouse button, the wheel
or pointer motion bound to an action; prompts show the keyboard and the mouse together.

</div>
