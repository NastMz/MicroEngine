# Class InputMap

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

Named actions bound to keys, gamepad buttons, stick directions and analog axes, plus any number of action sources
(touch controls, custom sources). Games query actions ("jump", "confirm"), never physical keys, so bindings and
scripted input stay in one place. Every action is both digital ([Down](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Down_System_String_), [Pressed](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Pressed_System_String_),
[Released](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Released_System_String_)) and analog ([Value](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Value_System_String_), [Vector](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Vector_System_String_System_String_System_String_System_String_)): a key is 0 or 1, a stick or a
virtual joystick gives the deflection beyond its dead zone.
Edge states (pressed/released) are computed per fixed update, so a tap is never lost or doubled.

```csharp
public sealed class InputMap : IStateHashed
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[InputMap](MicroEngine.Input.InputMap.md)

#### Implements

IStateHashed

## Examples

```csharp
// In a Game subclass: keep the defaults and add a "dash" action.
protected override InputMap CreateInput() => InputMap.CreateDefault()
    .Bind("dash", [Key.LeftShift, Key.N], [PadButton.RightShoulder]);

// In a scene's Update:
var move = Input.Vector("left", "right", "up", "down");
if (Input.Pressed("confirm")) Audio.Play("select");
```

## Constructors

### <a id="MicroEngine_Input_InputMap__ctor"></a> InputMap\(\)

```csharp
public InputMap()
```

## Fields

### <a id="MicroEngine_Input_InputMap_BindingsSlot"></a> BindingsSlot

The slot [SaveBindings](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_SaveBindings_MicroEngine_Platform_IStorage_System_String_) and [LoadBindings](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_LoadBindings_MicroEngine_Platform_IStorage_System_String_) use unless told otherwise.

```csharp
public const string BindingsSlot = "input-bindings"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Properties

### <a id="MicroEngine_Input_InputMap_Actions"></a> Actions

The names of every bound action (including actions unbound with [Unbind](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Unbind_System_String_)).

```csharp
public IEnumerable<string> Actions { get; }
```

#### Property Value

 [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Input_InputMap_AxisDeadZone"></a> AxisDeadZone

Dead zone of bound triggers and other single axes (default 0.1).

```csharp
public float AxisDeadZone { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Input_InputMap_LastDevice"></a> LastDevice

The device that most recently pressed an action. Touch controls use it to hide while a keyboard or gamepad plays.

```csharp
public InputDevice LastDevice { get; }
```

#### Property Value

 [InputDevice](MicroEngine.Input.InputDevice.md)

### <a id="MicroEngine_Input_InputMap_Names"></a> Names

The engine's own action names this map uses (menus, dialogs, on-screen controls and prompts read actions by
these names). Rename them here when the game calls its actions differently; the default names are
[Default](MicroEngine.Input.ActionNames.md#MicroEngine_Input_ActionNames_Default).

```csharp
public ActionNames Names { get; set; }
```

#### Property Value

 [ActionNames](MicroEngine.Input.ActionNames.md)

### <a id="MicroEngine_Input_InputMap_PadConnected"></a> PadConnected

Whether a gamepad was connected at the last sample.

```csharp
public bool PadConnected { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_InputMap_PadStyle"></a> PadStyle

The family of the gamepad at the last sample (Xbox when unknown), for prompts.

```csharp
public GamepadStyle PadStyle { get; }
```

#### Property Value

 [GamepadStyle](MicroEngine.Input.GamepadStyle.md)

### <a id="MicroEngine_Input_InputMap_Pointer"></a> Pointer

The pointer at the last sample: the mouse over the virtual canvas, or a finger on the screen that no on-screen
control took (whichever moved last), with the wheel's turn since the previous update.

```csharp
public PointerState Pointer { get; }
```

#### Property Value

 [PointerState](MicroEngine.Input.PointerState.md)

### <a id="MicroEngine_Input_InputMap_Pointers"></a> Pointers

Every pointer at the last sample: the mouse while over the window, and each finger or pen on the screen that no
input layer (such as the on-screen controls) took, in device and id order. Tap-to-move, drag and pinch read it.

```csharp
public IReadOnlyList<PointerInfo> Pointers { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[PointerInfo](MicroEngine.Input.PointerInfo.md)\>

### <a id="MicroEngine_Input_InputMap_Sources"></a> Sources

The action sources read with the devices ([AddSource](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_AddSource_MicroEngine_Input_IActionSource_)).

```csharp
public IReadOnlyList<IActionSource> Sources { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[IActionSource](MicroEngine.Input.IActionSource.md)\>

### <a id="MicroEngine_Input_InputMap_StickDeadZone"></a> StickDeadZone

Radial dead zone of the left stick, as a fraction of full deflection (default 0.2). Values beyond it are rescaled to 0..1.

```csharp
public float StickDeadZone { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Input_InputMap_StickMode"></a> StickMode

How the left stick presses direction actions: eight sectors (default) or four.

```csharp
public StickDirections StickMode { get; set; }
```

#### Property Value

 [StickDirections](MicroEngine.Input.StickDirections.md)

### <a id="MicroEngine_Input_InputMap_StickThreshold"></a> StickThreshold

Deflection (after the dead zone) at which a stick direction counts as down (default 0.35).

```csharp
public float StickThreshold { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_Input_InputMap_AddSource_MicroEngine_Input_IActionSource_"></a> AddSource\(IActionSource\)

Adds an action source read with the devices at every sample. Its actions need no binding, but unbound names are listed in [Actions](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Actions) only once bound.

```csharp
public InputMap AddSource(IActionSource source)
```

#### Parameters

`source` [IActionSource](MicroEngine.Input.IActionSource.md)

#### Returns

 [InputMap](MicroEngine.Input.InputMap.md)

### <a id="MicroEngine_Input_InputMap_AnalogAxis_System_String_System_String_"></a> AnalogAxis\(string, string\)

-1..1 from two opposing actions, analog when a stick or virtual joystick drives them.

```csharp
public float AnalogAxis(string negative, string positive)
```

#### Parameters

`negative` [string](https://learn.microsoft.com/dotnet/api/system.string)

`positive` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Input_InputMap_ApplyDeadZone_System_Numerics_Vector2_System_Single_"></a> ApplyDeadZone\(Vector2, float\)

Radial dead zone: inside it the stick is centred, beyond it the deflection is rescaled to 0..1.

```csharp
public static Vector2 ApplyDeadZone(Vector2 stick, float deadZone)
```

#### Parameters

`stick` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`deadZone` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Input_InputMap_Axis_System_String_System_String_"></a> Axis\(string, string\)

-1, 0 or 1 from two opposing actions.

```csharp
public int Axis(string negative, string positive)
```

#### Parameters

`negative` [string](https://learn.microsoft.com/dotnet/api/system.string)

`positive` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Input_InputMap_Bind_System_String_MicroEngine_Input_Key___"></a> Bind\(string, params Key\[\]\)

Binds keys to <code class="paramref">action</code>, adding to its existing bindings.

```csharp
public InputMap Bind(string action, params Key[] keys)
```

#### Parameters

`action` [string](https://learn.microsoft.com/dotnet/api/system.string)

The action name (case-sensitive, not empty).

`keys` [Key](MicroEngine.Input.Key.md)\[\]

Keys that press the action.

#### Returns

 [InputMap](MicroEngine.Input.InputMap.md)

This map, for chaining.

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">action</code> is empty.

### <a id="MicroEngine_Input_InputMap_Bind_System_String_MicroEngine_Input_Key___MicroEngine_Input_PadButton___MicroEngine_Input_InputMap_StickDirection_"></a> Bind\(string, Key\[\], PadButton\[\], StickDirection\)

Binds keys, gamepad buttons and optionally a left-stick direction to <code class="paramref">action</code>, adding to its existing bindings (use [Unbind](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Unbind_System_String_) first to replace them).

```csharp
public InputMap Bind(string action, Key[] keys, PadButton[] buttons, InputMap.StickDirection stick = StickDirection.None)
```

#### Parameters

`action` [string](https://learn.microsoft.com/dotnet/api/system.string)

The action name (case-sensitive, not empty).

`keys` [Key](MicroEngine.Input.Key.md)\[\]

Keys that press the action.

`buttons` [PadButton](MicroEngine.Input.PadButton.md)\[\]

Gamepad buttons that press the action.

`stick` [InputMap](MicroEngine.Input.InputMap.md).[StickDirection](MicroEngine.Input.InputMap.StickDirection.md)

A left-stick direction that presses the action; <code>None</code> keeps the current one.

#### Returns

 [InputMap](MicroEngine.Input.InputMap.md)

This map, for chaining.

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">action</code> is empty.

### <a id="MicroEngine_Input_InputMap_BindAxis_System_String_MicroEngine_Input_PadAxis_System_Boolean_"></a> BindAxis\(string, PadAxis, bool\)

Binds one side of an analog axis: <code>BindAxis("aim-right", PadAxis.RightX, positive: true)</code>, or a trigger
(<code>BindAxis("fire", PadAxis.RightTrigger)</code>). The action is down beyond half travel and its
[Value](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Value_System_String_) follows the axis.

```csharp
public InputMap BindAxis(string action, PadAxis axis, bool positive = true)
```

#### Parameters

`action` [string](https://learn.microsoft.com/dotnet/api/system.string)

`axis` [PadAxis](MicroEngine.Input.PadAxis.md)

`positive` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [InputMap](MicroEngine.Input.InputMap.md)

### <a id="MicroEngine_Input_InputMap_BindFrom_System_String_MicroEngine_Input_InputEvent__System_Boolean_"></a> BindFrom\(string, in InputEvent, bool\)

Binds the key or gamepad button of <code class="paramref">press</code> (a key-down or button-down event, such as the one
[ListenForPress](MicroEngine.Input.InputSystem.md#MicroEngine_Input_InputSystem_ListenForPress_System_Action_MicroEngine_Input_InputEvent__) hands over) to <code class="paramref">action</code>, adding to its bindings; with
<code class="paramref">replace</code>, the action's keys (for a key) or buttons (for a button) are replaced.

```csharp
public bool BindFrom(string action, in InputEvent press, bool replace = false)
```

#### Parameters

`action` [string](https://learn.microsoft.com/dotnet/api/system.string)

`press` [InputEvent](MicroEngine.Input.InputEvent.md)

`replace` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

False when the event is neither a key-down nor a button-down.

### <a id="MicroEngine_Input_InputMap_Consume_System_String_"></a> Consume\(string\)

Takes the action for whoever read it first in this update: from now until it is let go, it reads as up for
everyone (not [Down](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Down_System_String_), [Pressed](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Pressed_System_String_) or [Released](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Released_System_String_), value 0). A menu over a scene
that both read "confirm" consumes it, so the scene below does not act on the same press. Does nothing when the
action is not down.

```csharp
public bool Consume(string action)
```

#### Parameters

`action` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether the action was down and is now consumed.

### <a id="MicroEngine_Input_InputMap_CopyBindings"></a> CopyBindings\(\)

A new map with the same bindings, names and stick and axis settings, and no sampled state or action sources:
the starting point of another player's map.

```csharp
public InputMap CopyBindings()
```

#### Returns

 [InputMap](MicroEngine.Input.InputMap.md)

### <a id="MicroEngine_Input_InputMap_CreateDefault_MicroEngine_Input_ActionNames_"></a> CreateDefault\(ActionNames?\)

A conventional binding set shared by the sample games: arrows, WASD, the d-pad and the left stick move; confirm,
cancel, action and pause on keys and face buttons. <code class="paramref">names</code> renames the engine's actions (null
keeps [Default](MicroEngine.Input.ActionNames.md#MicroEngine_Input_ActionNames_Default)), and the map keeps them in [Names](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Names).

```csharp
public static InputMap CreateDefault(ActionNames? names = null)
```

#### Parameters

`names` [ActionNames](MicroEngine.Input.ActionNames.md)?

#### Returns

 [InputMap](MicroEngine.Input.InputMap.md)

### <a id="MicroEngine_Input_InputMap_Directions_System_Numerics_Vector2_System_Single_MicroEngine_Input_StickDirections_"></a> Directions\(Vector2, float, StickDirections\)

The directions a 2D input presses: none inside <code class="paramref">threshold</code>, else by 45° or 90° sectors.

```csharp
public static IReadOnlySet<InputMap.StickDirection> Directions(Vector2 v, float threshold, StickDirections mode)
```

#### Parameters

`v` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

`threshold` [float](https://learn.microsoft.com/dotnet/api/system.single)

`mode` [StickDirections](MicroEngine.Input.StickDirections.md)

#### Returns

 [IReadOnlySet](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlyset\-1)<[InputMap](MicroEngine.Input.InputMap.md).[StickDirection](MicroEngine.Input.InputMap.StickDirection.md)\>

### <a id="MicroEngine_Input_InputMap_Down_System_String_"></a> Down\(string\)

Whether the action is held in this update. Also true for a press that started and ended within the last frame, so a quick tap is never missed. Unknown actions are never down.

```csharp
public bool Down(string action)
```

#### Parameters

`action` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_InputMap_EndUpdate"></a> EndUpdate\(\)

Advances edge detection. Called by the runner after each fixed update.

```csharp
public void EndUpdate()
```

### <a id="MicroEngine_Input_InputMap_ExportBindings"></a> ExportBindings\(\)

Every action's bindings as data (see [InputBindingSet](MicroEngine.Input.InputBindingSet.md)), in ordinal order of the names.

```csharp
public InputBindingSet ExportBindings()
```

#### Returns

 [InputBindingSet](MicroEngine.Input.InputBindingSet.md)

### <a id="MicroEngine_Input_InputMap_GetBindings_System_String_"></a> GetBindings\(string\)

What the action is bound to; null for an unknown action.

```csharp
public ActionBindings? GetBindings(string action)
```

#### Parameters

`action` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [ActionBindings](MicroEngine.Input.ActionBindings.md)?

### <a id="MicroEngine_Input_InputMap_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds the sampled state to the per-step state hash: the actions down, down in the previous update, latched and
forced (each in ordinal order), every action's value, the last device, the gamepad's connection and style, and the
pointer. Bindings are configuration, not state, and are left out.

```csharp
public void HashState(StateHasher hash)
```

#### Parameters

`hash` StateHasher

### <a id="MicroEngine_Input_InputMap_ImportBindings_MicroEngine_Input_InputBindingSet_"></a> ImportBindings\(InputBindingSet\)

Replaces the bindings of the actions this map has with those in <code class="paramref">set</code>. Actions the set does not
mention keep theirs; actions the map does not have are ignored (a renamed or removed action does not come back),
and so are names of keys, buttons or axes this version does not know.

```csharp
public int ImportBindings(InputBindingSet set)
```

#### Parameters

`set` [InputBindingSet](MicroEngine.Input.InputBindingSet.md)

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

The number of actions whose bindings were replaced.

### <a id="MicroEngine_Input_InputMap_IsConsumed_System_String_"></a> IsConsumed\(string\)

Whether [Consume](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Consume_System_String_) took the action and it has not been let go yet.

```csharp
public bool IsConsumed(string action)
```

#### Parameters

`action` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_InputMap_LoadBindings_MicroEngine_Platform_IStorage_System_String_"></a> LoadBindings\(IStorage, string\)

Reads bindings saved with [SaveBindings](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_SaveBindings_MicroEngine_Platform_IStorage_System_String_) and applies them ([ImportBindings](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_ImportBindings_MicroEngine_Input_InputBindingSet_)). Returns false
and keeps the current bindings when nothing was saved or the data is unreadable (which is set aside, so the next
save does not destroy it).

```csharp
public bool LoadBindings(IStorage storage, string slot = "input-bindings")
```

#### Parameters

`storage` IStorage

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_InputMap_Pressed_System_String_"></a> Pressed\(string\)

True during the first update after the action went down, even if it was released within the same frame.

```csharp
public bool Pressed(string action)
```

#### Parameters

`action` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_InputMap_Released_System_String_"></a> Released\(string\)

True during the first update after the action was let go. A tap that started and ended within one frame reports [Pressed](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Pressed_System_String_) in its update and [Released](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Released_System_String_) in the next one.

```csharp
public bool Released(string action)
```

#### Parameters

`action` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_InputMap_RemoveSource_MicroEngine_Input_IActionSource_"></a> RemoveSource\(IActionSource\)

Stops reading <code class="paramref">source</code>; returns false when it was not added.

```csharp
public bool RemoveSource(IActionSource source)
```

#### Parameters

`source` [IActionSource](MicroEngine.Input.IActionSource.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_InputMap_Reset"></a> Reset\(\)

Clears every state, e.g. after a scene change, so held keys must be pressed again.

```csharp
public void Reset()
```

### <a id="MicroEngine_Input_InputMap_Sample_MicroEngine_Input_IInputSource_"></a> Sample\(IInputSource\)

Samples devices and action sources. Called by the runner before each fixed update.

```csharp
public void Sample(IInputSource source)
```

#### Parameters

`source` [IInputSource](MicroEngine.Input.IInputSource.md)

### <a id="MicroEngine_Input_InputMap_SaveBindings_MicroEngine_Platform_IStorage_System_String_"></a> SaveBindings\(IStorage, string\)

Writes every action's bindings to <code class="paramref">storage</code> (the slot <code class="paramref">slot</code>), for a rebinding screen.

```csharp
public void SaveBindings(IStorage storage, string slot = "input-bindings")
```

#### Parameters

`storage` IStorage

`slot` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Exceptions

 [IOException](https://learn.microsoft.com/dotnet/api/system.io.ioexception)

The storage failed.

### <a id="MicroEngine_Input_InputMap_SetForced_System_String_System_Boolean_"></a> SetForced\(string, bool\)

Forces an action down regardless of devices (scripted playback, tests).

```csharp
public void SetForced(string action, bool down)
```

#### Parameters

`action` [string](https://learn.microsoft.com/dotnet/api/system.string)

`down` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_InputMap_Unbind_System_String_"></a> Unbind\(string\)

Removes every key, button, stick and axis bound to the action (the action stays, unbound), to rebind it.

```csharp
public InputMap Unbind(string action)
```

#### Parameters

`action` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [InputMap](MicroEngine.Input.InputMap.md)

### <a id="MicroEngine_Input_InputMap_Value_System_String_"></a> Value\(string\)

The action's strength in 0..1: 1 for a key or button, the deflection beyond the dead zone for a stick, trigger
or virtual joystick. A stick that is deflected but below [StickThreshold](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_StickThreshold) has a value without being down.

```csharp
public float Value(string action)
```

#### Parameters

`action` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Input_InputMap_Vector_System_String_System_String_System_String_System_String_"></a> Vector\(string, string, string, string\)

A 2D direction from four actions, at most 1 long: keys give unit vectors (diagonals normalised), a stick or a
virtual joystick gives its deflection. <code>Input.Vector("left", "right", "up", "down")</code>.

```csharp
public Vector2 Vector(string left, string right, string up, string down)
```

#### Parameters

`left` [string](https://learn.microsoft.com/dotnet/api/system.string)

`right` [string](https://learn.microsoft.com/dotnet/api/system.string)

`up` [string](https://learn.microsoft.com/dotnet/api/system.string)

`down` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

</div>
