# Class InputScript

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Testing your game](../examples/testing.md)  

A deterministic input script, keyed by update number. Text format, one command per line:

```csharp
# comment
30 press confirm          # down for 2 updates
60 hold right 90          # down for 90 updates
60 down jump / 75 up jump # explicit edges
200 capture title.png
240 touch 1 dpad 1 0      # finger 1 down on the d-pad, one radius to the right (or moves there)
250 tap 2 jump            # finger 2 taps the "jump" control (lifted 2 updates later)
260 touch 3 0.5,0.5       # finger 3 at the middle of the screen (fractions of its size)
300 lift 1
320 prompts gamepad playstation  # name controls as on a PlayStation pad (keyboard, gamepad, touch or auto)
400 key Space down        # the script's keyboard: a key by position (a Key name) goes down, or up
410 keytap Z              # down and up within one frame
420 pad 2 add playstation # the script's gamepad 2 connects (xbox, playstation or nintendo)
430 pad 2 A down          # a button of pad 2 (a PadButton name) goes down, or up
440 pad 2 LeftX -0.8      # an axis of pad 2 (a PadAxis name) moves
450 pad 2 remove
460 mouse 160,90 down     # the script's mouse moves to a point in canvas pixels (and its button goes down or up)
470 wheel 1               # the wheel turns one notch away from the player
480 text Ana              # text typed (the rest of the line), read while text input is on
900 quit
```

"@+N" instead of an absolute number means N updates after the previous command. Touch commands go through the
game's [VirtualControls](MicroEngine.Input.VirtualControls.md) with the same hit testing as real fingers. The device commands send events
from the script's own devices ([ScriptKeyboard](MicroEngine.Core.InputScript.md#MicroEngine_Core_InputScript_ScriptKeyboard), [ScriptMouse](MicroEngine.Core.InputScript.md#MicroEngine_Core_InputScript_ScriptMouse), [ScriptPad](MicroEngine.Core.InputScript.md#MicroEngine_Core_InputScript_ScriptPad_System_Int32_)) through
the engine's input layers, exactly as a backend's would; each device is announced before its first event.

```csharp
public sealed class InputScript
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[InputScript](MicroEngine.Core.InputScript.md)

## Constructors

### <a id="MicroEngine_Core_InputScript__ctor"></a> InputScript\(\)

```csharp
public InputScript()
```

## Properties

### <a id="MicroEngine_Core_InputScript_QuitUpdate"></a> QuitUpdate

The update after which the script stops the run, or null.

```csharp
public long? QuitUpdate { get; }
```

#### Property Value

 [long](https://learn.microsoft.com/dotnet/api/system.int64)?

### <a id="MicroEngine_Core_InputScript_ScriptKeyboard"></a> ScriptKeyboard

The script's keyboard.

```csharp
public static DeviceId ScriptKeyboard { get; }
```

#### Property Value

 [DeviceId](MicroEngine.Input.DeviceId.md)

### <a id="MicroEngine_Core_InputScript_ScriptMouse"></a> ScriptMouse

The script's mouse.

```csharp
public static DeviceId ScriptMouse { get; }
```

#### Property Value

 [DeviceId](MicroEngine.Input.DeviceId.md)

## Methods

### <a id="MicroEngine_Core_InputScript_AddPad_System_Int64_System_Int32_MicroEngine_Input_GamepadStyle_"></a> AddPad\(long, int, GamepadStyle\)

The script's gamepad <code class="paramref">number</code> connects at update <code class="paramref">at</code>.

```csharp
public InputScript AddPad(long at, int number, GamepadStyle style = GamepadStyle.Xbox)
```

#### Parameters

`at` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`number` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`style` [GamepadStyle](MicroEngine.Input.GamepadStyle.md)

#### Returns

 [InputScript](MicroEngine.Core.InputScript.md)

### <a id="MicroEngine_Core_InputScript_Apply_System_Int64_MicroEngine_Input_InputMap_"></a> Apply\(long, InputMap\)

Applies every edge scheduled for the update about to run.

```csharp
public void Apply(long update, InputMap input)
```

#### Parameters

`update` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`input` [InputMap](MicroEngine.Input.InputMap.md)

### <a id="MicroEngine_Core_InputScript_ApplyDevices_System_Int64_MicroEngine_Input_InputSystem_"></a> ApplyDevices\(long, InputSystem\)

Sends the device events scheduled for the update about to run to <code class="paramref">input</code> (routed with the frame's events).

```csharp
public void ApplyDevices(long update, InputSystem input)
```

#### Parameters

`update` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`input` [InputSystem](MicroEngine.Input.InputSystem.md)

### <a id="MicroEngine_Core_InputScript_ApplyPrompts_System_Int64_MicroEngine_Input_ActionPrompts_"></a> ApplyPrompts\(long, ActionPrompts\)

Applies the <code>prompts</code> commands scheduled for the update about to run: which device <code class="paramref">prompts</code> names controls for.

```csharp
public void ApplyPrompts(long update, ActionPrompts prompts)
```

#### Parameters

`update` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`prompts` [ActionPrompts](MicroEngine.Input.ActionPrompts.md)

### <a id="MicroEngine_Core_InputScript_ApplyTouches_System_Int64_MicroEngine_Input_VirtualControls_"></a> ApplyTouches\(long, VirtualControls\)

Hands the touches scheduled for the update about to run to the game's touch controls.

```csharp
public void ApplyTouches(long update, VirtualControls controls)
```

#### Parameters

`update` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`controls` [VirtualControls](MicroEngine.Input.VirtualControls.md)

### <a id="MicroEngine_Core_InputScript_Axis_System_Int64_System_Int32_MicroEngine_Input_PadAxis_System_Single_"></a> Axis\(long, int, PadAxis, float\)

An axis of the script's gamepad <code class="paramref">number</code> moves to <code class="paramref">value</code> (the pad connects first if it has not).

```csharp
public InputScript Axis(long at, int number, PadAxis axis, float value)
```

#### Parameters

`at` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`number` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`axis` [PadAxis](MicroEngine.Input.PadAxis.md)

`value` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [InputScript](MicroEngine.Core.InputScript.md)

### <a id="MicroEngine_Core_InputScript_Button_System_Int64_System_Int32_MicroEngine_Input_PadButton_System_Boolean_"></a> Button\(long, int, PadButton, bool\)

A button of the script's gamepad <code class="paramref">number</code> goes down or up (the pad connects first if it has not).

```csharp
public InputScript Button(long at, int number, PadButton button, bool down)
```

#### Parameters

`at` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`number` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`button` [PadButton](MicroEngine.Input.PadButton.md)

`down` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [InputScript](MicroEngine.Core.InputScript.md)

### <a id="MicroEngine_Core_InputScript_Capture_System_Int64_System_String_"></a> Capture\(long, string\)

Saves a screenshot named <code class="paramref">fileName</code> after update <code class="paramref">at</code>.

```csharp
public InputScript Capture(long at, string fileName)
```

#### Parameters

`at` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`fileName` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [InputScript](MicroEngine.Core.InputScript.md)

### <a id="MicroEngine_Core_InputScript_CaptureAt_System_Int64_"></a> CaptureAt\(long\)

The screenshot file name scheduled after <code class="paramref">update</code>, or null.

```csharp
public string? CaptureAt(long update)
```

#### Parameters

`update` [long](https://learn.microsoft.com/dotnet/api/system.int64)

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Core_InputScript_Hold_System_Int64_System_String_System_Int64_"></a> Hold\(long, string, long\)

Holds <code class="paramref">action</code> down from update <code class="paramref">at</code> for <code class="paramref">duration</code> updates (at least 1).

```csharp
public InputScript Hold(long at, string action, long duration)
```

#### Parameters

`at` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`action` [string](https://learn.microsoft.com/dotnet/api/system.string)

`duration` [long](https://learn.microsoft.com/dotnet/api/system.int64)

#### Returns

 [InputScript](MicroEngine.Core.InputScript.md)

### <a id="MicroEngine_Core_InputScript_Key_System_Int64_MicroEngine_Input_Key_System_Boolean_"></a> Key\(long, Key, bool\)

The script's keyboard presses (<code class="paramref">down</code>) or releases <code class="paramref">key</code> at update <code class="paramref">at</code>.

```csharp
public InputScript Key(long at, Key key, bool down)
```

#### Parameters

`at` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`key` [Key](MicroEngine.Input.Key.md)

`down` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [InputScript](MicroEngine.Core.InputScript.md)

### <a id="MicroEngine_Core_InputScript_Lift_System_Int64_System_Int32_"></a> Lift\(long, int\)

Lifts finger <code class="paramref">finger</code> at update <code class="paramref">at</code>.

```csharp
public InputScript Lift(long at, int finger)
```

#### Parameters

`at` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`finger` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [InputScript](MicroEngine.Core.InputScript.md)

### <a id="MicroEngine_Core_InputScript_Mouse_System_Int64_System_Numerics_Vector2_System_Nullable_System_Boolean__"></a> Mouse\(long, Vector2, bool?\)

The script's mouse moves to <code class="paramref">position</code> (canvas pixels) and, when <code class="paramref">down</code> is set, its primary button goes down or up.

```csharp
public InputScript Mouse(long at, Vector2 position, bool? down = null)
```

#### Parameters

`at` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`down` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)?

#### Returns

 [InputScript](MicroEngine.Core.InputScript.md)

### <a id="MicroEngine_Core_InputScript_Parse_System_String_"></a> Parse\(string\)

Parses the text format described above.

```csharp
public static InputScript Parse(string text)
```

#### Parameters

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [InputScript](MicroEngine.Core.InputScript.md)

#### Exceptions

 [FormatException](https://learn.microsoft.com/dotnet/api/system.formatexception)

A line is invalid; the message names its line number.

### <a id="MicroEngine_Core_InputScript_Press_System_Int64_System_String_"></a> Press\(long, string\)

Presses <code class="paramref">action</code> at update <code class="paramref">at</code> for 2 updates.

```csharp
public InputScript Press(long at, string action)
```

#### Parameters

`at` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`action` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [InputScript](MicroEngine.Core.InputScript.md)

### <a id="MicroEngine_Core_InputScript_QuitAfter_System_Int64_"></a> QuitAfter\(long\)

Stops the run after update <code class="paramref">at</code>.

```csharp
public InputScript QuitAfter(long at)
```

#### Parameters

`at` [long](https://learn.microsoft.com/dotnet/api/system.int64)

#### Returns

 [InputScript](MicroEngine.Core.InputScript.md)

### <a id="MicroEngine_Core_InputScript_QuitAt_System_Int64_"></a> QuitAt\(long\)

Whether the run must stop after <code class="paramref">update</code>.

```csharp
public bool QuitAt(long update)
```

#### Parameters

`update` [long](https://learn.microsoft.com/dotnet/api/system.int64)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_InputScript_ScriptPad_System_Int32_"></a> ScriptPad\(int\)

The script's gamepad <code class="paramref">number</code> (1 and up).

```csharp
public static DeviceId ScriptPad(int number)
```

#### Parameters

`number` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [DeviceId](MicroEngine.Input.DeviceId.md)

### <a id="MicroEngine_Core_InputScript_Send_System_Int64_MicroEngine_Input_InputEvent_"></a> Send\(long, InputEvent\)

Sends <code class="paramref">e</code> at update <code class="paramref">at</code>, through the input layers like a device's event (its device must be announced, as a backend's must).

```csharp
public InputScript Send(long at, InputEvent e)
```

#### Parameters

`at` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`e` [InputEvent](MicroEngine.Input.InputEvent.md)

#### Returns

 [InputScript](MicroEngine.Core.InputScript.md)

### <a id="MicroEngine_Core_InputScript_Touch_System_Int64_System_Int32_System_String_System_Numerics_Vector2_"></a> Touch\(long, int, string, Vector2\)

Puts finger <code class="paramref">finger</code> on a touch control (offset in its radii) at update <code class="paramref">at</code>, or moves it there.

```csharp
public InputScript Touch(long at, int finger, string control, Vector2 offset = default)
```

#### Parameters

`at` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`finger` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`control` [string](https://learn.microsoft.com/dotnet/api/system.string)

`offset` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

#### Returns

 [InputScript](MicroEngine.Core.InputScript.md)

</div>
