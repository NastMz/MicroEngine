# Struct ActionPrompt

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.dll  
Guide: [Input](../manual/input.md)  

How to show one action to the player on one device: <code>"Z"</code> on a keyboard, <code>"A"</code> on an Xbox pad,
<code>"×"</code> on a PlayStation pad, the button's own label on a touch screen. [Text](MicroEngine.Input.ActionPrompt.md#MicroEngine_Input_ActionPrompt_Text) is what to print;
[Kind](MicroEngine.Input.ActionPrompt.md#MicroEngine_Input_ActionPrompt_Kind) and the other members let a game draw a glyph instead (a key cap, a coloured pad button).

```csharp
public readonly struct ActionPrompt : IEquatable<ActionPrompt>
```

#### Implements

[IEquatable<ActionPrompt\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Input_ActionPrompt__ctor_System_String_MicroEngine_Input_InputDevice_MicroEngine_Input_PromptKind_System_String_"></a> ActionPrompt\(string, InputDevice, PromptKind, string\)

How to show one action to the player on one device: <code>"Z"</code> on a keyboard, <code>"A"</code> on an Xbox pad,
<code>"×"</code> on a PlayStation pad, the button's own label on a touch screen. [Text](MicroEngine.Input.ActionPrompt.md#MicroEngine_Input_ActionPrompt_Text) is what to print;
[Kind](MicroEngine.Input.ActionPrompt.md#MicroEngine_Input_ActionPrompt_Kind) and the other members let a game draw a glyph instead (a key cap, a coloured pad button).

```csharp
public ActionPrompt(string Action, InputDevice Device, PromptKind Kind, string Text)
```

#### Parameters

`Action` [string](https://learn.microsoft.com/dotnet/api/system.string)

The action shown.

`Device` [InputDevice](MicroEngine.Input.InputDevice.md)

The device it is shown for.

`Kind` [PromptKind](MicroEngine.Input.PromptKind.md)

What [Text](MicroEngine.Input.ActionPrompt.md#MicroEngine_Input_ActionPrompt_Text) stands for.

`Text` [string](https://learn.microsoft.com/dotnet/api/system.string)

Short text for the control ("Z", "Space", "A", "←", "JUMP").

## Properties

### <a id="MicroEngine_Input_ActionPrompt_Action"></a> Action

The action shown.

```csharp
public string Action { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_ActionPrompt_Button"></a> Button

The button, for [PadButton](MicroEngine.Input.PromptKind.md).

```csharp
public PadButton Button { get; init; }
```

#### Property Value

 [PadButton](MicroEngine.Input.PadButton.md)

### <a id="MicroEngine_Input_ActionPrompt_Control"></a> Control

The touch control's id, for [Touch](MicroEngine.Input.PromptKind.md).

```csharp
public string? Control { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Input_ActionPrompt_Device"></a> Device

The device it is shown for.

```csharp
public InputDevice Device { get; init; }
```

#### Property Value

 [InputDevice](MicroEngine.Input.InputDevice.md)

### <a id="MicroEngine_Input_ActionPrompt_Key"></a> Key

The key, for [Key](MicroEngine.Input.PromptKind.md).

```csharp
public Key Key { get; init; }
```

#### Property Value

 [Key](MicroEngine.Input.Key.md)

### <a id="MicroEngine_Input_ActionPrompt_Kind"></a> Kind

What [Text](MicroEngine.Input.ActionPrompt.md#MicroEngine_Input_ActionPrompt_Text) stands for.

```csharp
public PromptKind Kind { get; init; }
```

#### Property Value

 [PromptKind](MicroEngine.Input.PromptKind.md)

### <a id="MicroEngine_Input_ActionPrompt_PadStyle"></a> PadStyle

The gamepad family the button is named for.

```csharp
public GamepadStyle PadStyle { get; init; }
```

#### Property Value

 [GamepadStyle](MicroEngine.Input.GamepadStyle.md)

### <a id="MicroEngine_Input_ActionPrompt_Text"></a> Text

Short text for the control ("Z", "Space", "A", "←", "JUMP").

```csharp
public string Text { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Input_ActionPrompt_Deconstruct_System_String__MicroEngine_Input_InputDevice__MicroEngine_Input_PromptKind__System_String__"></a> Deconstruct\(out string, out InputDevice, out PromptKind, out string\)

```csharp
public void Deconstruct(out string Action, out InputDevice Device, out PromptKind Kind, out string Text)
```

#### Parameters

`Action` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Device` [InputDevice](MicroEngine.Input.InputDevice.md)

`Kind` [PromptKind](MicroEngine.Input.PromptKind.md)

`Text` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_ActionPrompt_Equals_System_Object_"></a> Equals\(object\)

```csharp
public override bool Equals(object obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_ActionPrompt_Equals_MicroEngine_Input_ActionPrompt_"></a> Equals\(ActionPrompt\)

```csharp
public bool Equals(ActionPrompt other)
```

#### Parameters

`other` [ActionPrompt](MicroEngine.Input.ActionPrompt.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_ActionPrompt_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Input_ActionPrompt_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Input_ActionPrompt_op_Equality_MicroEngine_Input_ActionPrompt_MicroEngine_Input_ActionPrompt_"></a> operator ==\(ActionPrompt, ActionPrompt\)

```csharp
public static bool operator ==(ActionPrompt left, ActionPrompt right)
```

#### Parameters

`left` [ActionPrompt](MicroEngine.Input.ActionPrompt.md)

`right` [ActionPrompt](MicroEngine.Input.ActionPrompt.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_ActionPrompt_op_Inequality_MicroEngine_Input_ActionPrompt_MicroEngine_Input_ActionPrompt_"></a> operator \!=\(ActionPrompt, ActionPrompt\)

```csharp
public static bool operator !=(ActionPrompt left, ActionPrompt right)
```

#### Parameters

`left` [ActionPrompt](MicroEngine.Input.ActionPrompt.md)

`right` [ActionPrompt](MicroEngine.Input.ActionPrompt.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
