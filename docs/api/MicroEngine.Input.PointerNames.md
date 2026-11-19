# Class PointerNames

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

The names prompts show for the mouse's controls, in the same short style as key and gamepad names: <code>"LMB"</code>,
<code>"Wheel ↑"</code>, <code>"Mouse →"</code>.

```csharp
public static class PointerNames
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PointerNames](MicroEngine.Input.PointerNames.md)

## Methods

### <a id="MicroEngine_Input_PointerNames_Button_MicroEngine_Input_PointerButton_"></a> Button\(PointerButton\)

The name of a mouse button: "LMB", "RMB", "MMB", "Mouse 4" (back) and "Mouse 5" (forward).

```csharp
public static string Button(PointerButton button)
```

#### Parameters

`button` [PointerButton](MicroEngine.Input.PointerButton.md)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_PointerNames_Of_MicroEngine_Input_InputControl_"></a> Of\(InputControl\)

The name of a mouse control: a button ([Button](MicroEngine.Input.PointerNames.md#MicroEngine_Input_PointerNames_Button_MicroEngine_Input_PointerButton_)), the wheel turning one way ("Wheel ↑") or the
pointer moving one way ("Mouse →"). Null for a control that is not on the mouse.

```csharp
public static string? Of(InputControl control)
```

#### Parameters

`control` [InputControl](MicroEngine.Input.InputControl.md)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

</div>
