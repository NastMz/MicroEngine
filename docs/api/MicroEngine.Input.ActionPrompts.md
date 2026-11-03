# Class ActionPrompts

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.dll  
Guide: [Input](../manual/input.md)  

Tells the player which control presses an action on the device they are using, so dialogues, tutorials, help
screens and hints never name a key the player does not have. The text follows the active device (the one that last
pressed an action, touch while the on-screen controls are shown), the action's current bindings (remapping included)
and the touch layout, and changes as soon as the player switches device: read it every frame, or when
[Version](MicroEngine.Input.ActionPrompts.md#MicroEngine_Input_ActionPrompts_Version) changes. Games get one in [Prompts](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Prompts).

```csharp
public sealed class ActionPrompts
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[ActionPrompts](MicroEngine.Input.ActionPrompts.md)

## Examples

```csharp
font.Draw(r, Prompts.Format("Press {attack} to swing the sword"), position, color);
// Keyboard: "Press Z to swing...", Xbox pad: "Press A...", PlayStation: "Press ×...", phone: "Press A..." (the touch button's label)
```

## Constructors

### <a id="MicroEngine_Input_ActionPrompts__ctor_MicroEngine_Input_InputMap_MicroEngine_Input_VirtualControls_"></a> ActionPrompts\(InputMap, VirtualControls?\)

Prompts for the actions of <code class="paramref">input</code> and the controls of <code class="paramref">touch</code> (optional).

```csharp
public ActionPrompts(InputMap input, VirtualControls? touch = null)
```

#### Parameters

`input` InputMap

`touch` [VirtualControls](MicroEngine.Input.VirtualControls.md)?

## Properties

### <a id="MicroEngine_Input_ActionPrompts_Device"></a> Device

The device prompts are shown for: [Override](MicroEngine.Input.ActionPrompts.md#MicroEngine_Input_ActionPrompts_Override), else the device that last pressed an action; before any
press (or after scripted input), touch when the on-screen controls are shown or the screen is touch-first,
otherwise the keyboard.

```csharp
public InputDevice Device { get; }
```

#### Property Value

 InputDevice

### <a id="MicroEngine_Input_ActionPrompts_Override"></a> Override

Forces prompts for one device (a "show keyboard controls" option, tests); null follows the player.

```csharp
public InputDevice? Override { get; set; }
```

#### Property Value

 InputDevice?

### <a id="MicroEngine_Input_ActionPrompts_PadStyle"></a> PadStyle

The gamepad family button names follow: [PadStyleOverride](MicroEngine.Input.ActionPrompts.md#MicroEngine_Input_ActionPrompts_PadStyleOverride), else the connected pad's.

```csharp
public GamepadStyle PadStyle { get; }
```

#### Property Value

 GamepadStyle

### <a id="MicroEngine_Input_ActionPrompts_PadStyleOverride"></a> PadStyleOverride

Forces button names of one gamepad family; null follows the connected pad.

```csharp
public GamepadStyle? PadStyleOverride { get; set; }
```

#### Property Value

 GamepadStyle?

### <a id="MicroEngine_Input_ActionPrompts_Version"></a> Version

Changes whenever the text of prompts may have changed (another device, another pad family), so a game that
lays text out once (wrapping, measuring) knows when to do it again.

```csharp
public int Version { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Input_ActionPrompts_All_System_String_System_Nullable_MicroEngine_Input_InputDevice__"></a> All\(string, InputDevice?\)

Every control that presses <code class="paramref">action</code> on the device, in binding order (an empty list when none does).

```csharp
public IReadOnlyList<ActionPrompt> All(string action, InputDevice? device = null)
```

#### Parameters

`action` [string](https://learn.microsoft.com/dotnet/api/system.string)

`device` InputDevice?

#### Returns

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[ActionPrompt](MicroEngine.Input.ActionPrompt.md)\>

### <a id="MicroEngine_Input_ActionPrompts_ButtonName_MicroEngine_Input_PadButton_MicroEngine_Input_GamepadStyle_"></a> ButtonName\(PadButton, GamepadStyle\)

The name printed on a gamepad button of <code class="paramref">style</code>. [PadButton](MicroEngine.Input.PadButton.md) names positions as on
an Xbox pad; PlayStation pads show shapes there (×, ○, □, △, all in the built-in font), Nintendo pads swap A/B and
X/Y.

```csharp
public static string ButtonName(PadButton button, GamepadStyle style = GamepadStyle.Xbox)
```

#### Parameters

`button` PadButton

`style` GamepadStyle

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_ActionPrompts_Directions_System_String_System_String_System_String_System_String_System_Nullable_MicroEngine_Input_InputDevice__System_String_"></a> Directions\(string?, string?, string?, string?, InputDevice?, string?\)

How to show a group of directions together ("move"): on a keyboard the first key of each ("←→↑↓", "WASD"), on a
gamepad "D-pad" or "Stick", on a touch screen the name of the d-pad or joystick that presses them all.

```csharp
public string Directions(string? left = null, string? right = null, string? up = null, string? down = null, InputDevice? device = null, string? separator = null)
```

#### Parameters

`left` [string](https://learn.microsoft.com/dotnet/api/system.string)?

`right` [string](https://learn.microsoft.com/dotnet/api/system.string)?

`up` [string](https://learn.microsoft.com/dotnet/api/system.string)?

`down` [string](https://learn.microsoft.com/dotnet/api/system.string)?

`device` InputDevice?

`separator` [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Remarks

Keys are listed up, left, down, right, so a WASD set reads "WASD". With <code class="paramref">separator</code> set, a
keyboard shows every key set the directions share ("↑←↓→ / WASD"), for a help screen.

### <a id="MicroEngine_Input_ActionPrompts_Format_System_String_System_Func_MicroEngine_Input_ActionPrompt_System_String__"></a> Format\(string, Func<ActionPrompt, string\>?\)

Replaces each <code>{action}</code> in <code class="paramref">template</code> with that action's prompt (<code>{{</code> and <code>}}</code>
are literal braces); <code>{move}</code> stands for [Directions](MicroEngine.Input.ActionPrompts.md#MicroEngine_Input_ActionPrompts_Directions_System_String_System_String_System_String_System_String_System_Nullable_MicroEngine_Input_InputDevice__System_String_). Names that are neither bound, pressed by a
touch control nor labelled are left as written. <code class="paramref">decorate</code> may wrap each prompt ("[Z]").

```csharp
public string Format(string template, Func<ActionPrompt, string>? decorate = null)
```

#### Parameters

`template` [string](https://learn.microsoft.com/dotnet/api/system.string)

`decorate` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[ActionPrompt](MicroEngine.Input.ActionPrompt.md), [string](https://learn.microsoft.com/dotnet/api/system.string)\>?

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_ActionPrompts_Get_System_String_System_Nullable_MicroEngine_Input_InputDevice__"></a> Get\(string, InputDevice?\)

How to show <code class="paramref">action</code> on <code class="paramref">device</code> (default: [Device](MicroEngine.Input.ActionPrompts.md#MicroEngine_Input_ActionPrompts_Device)): the first binding the device has.

```csharp
public ActionPrompt Get(string action, InputDevice? device = null)
```

#### Parameters

`action` [string](https://learn.microsoft.com/dotnet/api/system.string)

`device` InputDevice?

#### Returns

 [ActionPrompt](MicroEngine.Input.ActionPrompt.md)

### <a id="MicroEngine_Input_ActionPrompts_KeyName_MicroEngine_Input_Key_"></a> KeyName\(Key\)

The name players read on a key cap: "Z", "Space", "Esc", "←".

```csharp
public static string KeyName(Key key)
```

#### Parameters

`key` Key

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_ActionPrompts_Knows_System_String_"></a> Knows\(string\)

Whether <code class="paramref">name</code> is something a template may name.

```csharp
public bool Knows(string name)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_ActionPrompts_SetLabel_System_String_System_Nullable_MicroEngine_Input_InputDevice__System_String_"></a> SetLabel\(string, InputDevice?, string?\)

Replaces the prompt of <code class="paramref">action</code> with <code class="paramref">text</code> on <code class="paramref">device</code>, or on
every device when it is null ("tap", "the left side"). Null text removes the label.

```csharp
public void SetLabel(string action, InputDevice? device, string? text)
```

#### Parameters

`action` [string](https://learn.microsoft.com/dotnet/api/system.string)

`device` InputDevice?

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Input_ActionPrompts_Split_System_String_"></a> Split\(string\)

The pieces of a template: literal text, and actions with their prompts, for drawing prompts as glyphs between
runs of text (see <code>MicroEngine.UI.PromptText</code>).

```csharp
public IReadOnlyList<(string Text, ActionPrompt? Prompt)> Split(string template)
```

#### Parameters

`template` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<\([string](https://learn.microsoft.com/dotnet/api/system.string) [Text](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.string,system.nullable\-microengine.input.actionprompt\-\-.text), [ActionPrompt](MicroEngine.Input.ActionPrompt.md)? [Prompt](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.string,system.nullable\-microengine.input.actionprompt\-\-.prompt)\)\>

### <a id="MicroEngine_Input_ActionPrompts_StyleFromName_System_String_"></a> StyleFromName\(string?\)

The gamepad family a pad's name suggests (browsers and raylib report names, not types).

```csharp
public static GamepadStyle StyleFromName(string? name)
```

#### Parameters

`name` [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Returns

 GamepadStyle

### <a id="MicroEngine_Input_ActionPrompts_Text_System_String_System_Nullable_MicroEngine_Input_InputDevice__"></a> Text\(string, InputDevice?\)

The first control's text, as [Get](MicroEngine.Input.ActionPrompts.md#MicroEngine_Input_ActionPrompts_Get_System_String_System_Nullable_MicroEngine_Input_InputDevice__).

```csharp
public string Text(string action, InputDevice? device = null)
```

#### Parameters

`action` [string](https://learn.microsoft.com/dotnet/api/system.string)

`device` InputDevice?

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_ActionPrompts_TextAll_System_String_System_String_System_Nullable_MicroEngine_Input_InputDevice__"></a> TextAll\(string, string, InputDevice?\)

Every control's text joined with <code class="paramref">separator</code>: <code>"Z / Space / Enter"</code> for a help screen.

```csharp
public string TextAll(string action, string separator = " / ", InputDevice? device = null)
```

#### Parameters

`action` [string](https://learn.microsoft.com/dotnet/api/system.string)

`separator` [string](https://learn.microsoft.com/dotnet/api/system.string)

`device` InputDevice?

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

</div>
