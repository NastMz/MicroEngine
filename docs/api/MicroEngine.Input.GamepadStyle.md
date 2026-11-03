# Enum GamepadStyle

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

The button names of a gamepad family, for prompts: [PadButton](MicroEngine.Input.PadButton.md) uses Xbox names, which PlayStation pads
show as shapes and Nintendo pads with A/B and X/Y swapped.

```csharp
public enum GamepadStyle
```

## Fields

`Nintendo = 2` 

Nintendo names, where the button in [A](MicroEngine.Input.PadButton.md)'s place is labelled B.



`PlayStation = 1` 

PlayStation shapes (cross, circle, square, triangle).



`Xbox = 0` 

Xbox names (A, B, X, Y), also for pads whose family is unknown.

</div>
