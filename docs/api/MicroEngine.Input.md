# Namespace MicroEngine.Input

<div data-pagefind-weight="0.3">

Input: device events with ids (keys, text, pointers and touches, wheel, gamepads), players and device pairing, input layers, and actions over them: bindings and their persistence, analog values, on-screen controls, prompts, custom sources.

Guide: [Input](../manual/input.md)  

### Classes

 [ActionBindingData](MicroEngine.Input.ActionBindingData.md)

The bindings of one action as data, by name (keys, buttons and axes are their enum names), so saved bindings stay
valid when the engine adds keys. Part of [InputBindingSet](MicroEngine.Input.InputBindingSet.md).

 [ActionBindings](MicroEngine.Input.ActionBindings.md)

What an action is bound to, for display or a rebinding screen ([GetBindings](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_GetBindings_System_String_)).

 [ActionNames](MicroEngine.Input.ActionNames.md)

The action names the engine's own parts read: menus and dialogs (<code>SelectionList</code>, <code>DialogueBox</code>), the
achievements screen, the default on-screen controls and prompts. They are defaults the game may rename: build the
map with [CreateDefault](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_CreateDefault_MicroEngine_Input_ActionNames_) or set [Names](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Names), and every engine
part follows.

 [ActionPrompts](MicroEngine.Input.ActionPrompts.md)

Tells the player which control presses an action on the device they are using, so dialogues, tutorials, help
screens and hints never name a key the player does not have. The text follows the active device (the one that last
pressed an action, touch while the on-screen controls are shown), the action's current bindings (remapping included)
and the touch layout, and changes as soon as the player switches device: read it every frame, or when
[Version](MicroEngine.Input.ActionPrompts.md#MicroEngine_Input_ActionPrompts_Version) changes. Games get one in [Prompts](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Prompts).

 [DefaultTouchSkin](MicroEngine.Input.DefaultTouchSkin.md)

The built-in look, shaped by [TouchStyle](MicroEngine.Input.TouchStyle.md). Idle controls are an outlined shape at the style's opacity;
a pressed one is filled, outlined thicker and drawn slightly smaller (pressed in), so the press reads by shape as
well as colour, even at low opacity. A dark halo keeps outlines legible on light scenes. A d-pad lights the arrows
it presses and marks where the thumb is; a joystick shows its base, its dead zone, the knob at the thumb's
deflection and an arrow on the rim whose opacity follows the intensity. Images replace fills and outlines.

 [FrameLayout](MicroEngine.Input.FrameLayout.md)

Where the virtual canvas goes in a window: the shared letterbox rule of every backend.

 [InputBindingSet](MicroEngine.Input.InputBindingSet.md)

Every action's bindings as data, for a rebinding screen to save and load: through [SaveBindings](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_SaveBindings_MicroEngine_Platform_IStorage_System_String_)
and [LoadBindings](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_LoadBindings_MicroEngine_Platform_IStorage_System_String_) (the platform's storage), or as a value a game keeps in its own save
(<code>SaveStore.Save("bindings", map.ExportBindings())</code>; the classes have public properties).

 [InputDeviceInfo](MicroEngine.Input.InputDeviceInfo.md)

A connected device as its backend describes it.

 [InputDevices](MicroEngine.Input.InputDevices.md)

The state of every device, derived from the events no input layer took: which keys and buttons are held, where the
axes, the pointers and the wheel are, and which devices are connected. A press that started and ended between two
updates still reads as held until an update has sampled it ([EndSample](MicroEngine.Input.InputDevices.md#MicroEngine_Input_InputDevices_EndSample)): taps expire by updates, never
by drawn frames. Players read it through views limited to their devices ([View](MicroEngine.Input.InputDevices.md#MicroEngine_Input_InputDevices_View_System_Func_MicroEngine_Input_DeviceId_System_Boolean__)).

 [InputLayerOrder](MicroEngine.Input.InputLayerOrder.md)

The orders of the engine's own layers. Higher orders see events first; gameplay is below every layer.

 [InputLayers](MicroEngine.Input.InputLayers.md)

The input layers in routing order: highest <code>order</code> first, layers of equal order in the order they were added.
Events no layer takes become the polled state actions read ([InputDevices](MicroEngine.Input.InputDevices.md)) and the frame's
[Events](MicroEngine.Input.InputSystem.md#MicroEngine_Input_InputSystem_Events).

 [InputMap](MicroEngine.Input.InputMap.md)

Named actions bound to keys, gamepad buttons, stick directions and analog axes, plus any number of action sources
(touch controls, custom sources). Games query actions ("jump", "confirm"), never physical keys, so bindings and
scripted input stay in one place. Every action is both digital ([Down](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Down_System_String_), [Pressed](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Pressed_System_String_),
[Released](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Released_System_String_)) and analog ([Value](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Value_System_String_), [Vector](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Vector_System_String_System_String_System_String_System_String_)): a key is 0 or 1, a stick or a
virtual joystick gives the deflection beyond its dead zone.
Edge states (pressed/released) are computed per fixed update, so a tap is never lost or doubled.

 [InputSystem](MicroEngine.Input.InputSystem.md)

The engine's input: the devices and their events, the routing through input layers, the players and their action
maps, and the text channel. Each frame the events of the platform's input backend (and those sent with
[Send](MicroEngine.Input.InputSystem.md#MicroEngine_Input_InputSystem_Send_MicroEngine_Input_InputEvent__)) are routed through [Layers](MicroEngine.Input.InputSystem.md#MicroEngine_Input_InputSystem_Layers); what no layer takes becomes the devices' polled state
and [Events](MicroEngine.Input.InputSystem.md#MicroEngine_Input_InputSystem_Events). Each update samples every player's map from the devices they hear.

 [OverlayShapes](MicroEngine.Input.OverlayShapes.md)

Shapes built from [IOverlayCanvas](MicroEngine.Input.IOverlayCanvas.md) triangles and rectangles, which never overlap, so translucent colours stay even.

 [PlayerInput](MicroEngine.Input.PlayerInput.md)

One local player: an action map of their own ([Map](MicroEngine.Input.PlayerInput.md#MicroEngine_Input_PlayerInput_Map)) read from the devices paired with them.

 [Players](MicroEngine.Input.Players.md)

The local players. With no setup there is one player who owns every device, so a single-player game never deals
with players at all (<code>Game.Input</code> is player 1's map). For local multiplayer, pair devices explicitly: a
device paired with a player is heard only by that player, and once player 1 has a device of their own they stop
hearing the devices nobody was given.

 [TextInput](MicroEngine.Input.TextInput.md)

The text channel, separate from actions: while it is on, characters typed (and the text an input method is
composing) arrive here instead of as keys to remember. Turn it on for name entry, chat or a console; on phones it
shows the on-screen keyboard. A layer above gameplay (a text field) may take text events itself.

 [TouchButton](MicroEngine.Input.TouchButton.md)

A button that presses an action (and optionally more, as one key may: <code>Also = ["confirm"]</code> on a jump button).

 [TouchControl](MicroEngine.Input.TouchControl.md)

One on-screen control of a [TouchLayout](MicroEngine.Input.TouchLayout.md). Positions and sizes are density-independent pixels (dp, 1/160
inch), so a control keeps its physical size on any screen; the layout shrinks everything when a screen is too small.

 [TouchDPad](MicroEngine.Input.TouchDPad.md)

A d-pad: discrete directions with no analog value, for games where a precise press matters more than a smooth one
(platformers, grid movement, menus). The thumb anywhere around its centre presses the direction it points to.

 [TouchLayout](MicroEngine.Input.TouchLayout.md)

A game's on-screen controls: which controls, where, and how they look and feel. The same layout is used on phones
(SDL), in mobile browsers and in tests; games return theirs from <code>Game.CreateTouchLayout</code>.

 [TouchStick](MicroEngine.Input.TouchStick.md)

An analog joystick: a 2D value with intensity ([Vector](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Vector_System_String_System_String_System_String_System_String_)) and, beyond [Threshold](MicroEngine.Input.TouchStick.md#MicroEngine_Input_TouchStick_Threshold),
the four direction actions as well, so digital code keeps working.

 [TouchStyle](MicroEngine.Input.TouchStyle.md)

How touch controls look: shape, colours of each state, outline, press feedback and optional images. Only the
drawing changes: hit areas, finger tracking and actions stay those of the layout, so a restyled control behaves
exactly as before. Colours left null follow [Tint](MicroEngine.Input.TouchLayout.md#MicroEngine_Input_TouchLayout_Tint) and [PressedTint](MicroEngine.Input.TouchLayout.md#MicroEngine_Input_TouchLayout_PressedTint).
Set one style for every control kind ([ButtonStyle](MicroEngine.Input.TouchLayout.md#MicroEngine_Input_TouchLayout_ButtonStyle), [DPadStyle](MicroEngine.Input.TouchLayout.md#MicroEngine_Input_TouchLayout_DPadStyle),
[StickStyle](MicroEngine.Input.TouchLayout.md#MicroEngine_Input_TouchLayout_StickStyle)) or for one control ([Style](MicroEngine.Input.TouchControl.md#MicroEngine_Input_TouchControl_Style)).

 [VirtualControls](MicroEngine.Input.VirtualControls.md)

On-screen controls at run time: hit testing, finger tracking, d-pad and joystick values, tap latching, haptics and
drawing. It is an [IActionSource](MicroEngine.Input.IActionSource.md) (read by [InputMap](MicroEngine.Input.InputMap.md) with the keyboard and gamepad), an
[ITouchOverlay](MicroEngine.Input.ITouchOverlay.md) (drawn by the backend over the game) and an input layer ([IInputLayer](MicroEngine.Input.IInputLayer.md), at
[TouchControls](MicroEngine.Input.InputLayerOrder.md#MicroEngine_Input_InputLayerOrder_TouchControls)): a finger that lands on a control is the control's, every other finger
goes on to gameplay as a pointer. Each finger belongs to the control it started on until it lifts, so two thumbs
work independently and a thumb that slides a little stays on its control.

### Structs

 [ActionPrompt](MicroEngine.Input.ActionPrompt.md)

How to show one action to the player on one device: <code>"Z"</code> on a keyboard, <code>"A"</code> on an Xbox pad,
<code>"×"</code> on a PlayStation pad, the button's own label on a touch screen. [Text](MicroEngine.Input.ActionPrompt.md#MicroEngine_Input_ActionPrompt_Text) is what to print;
[Kind](MicroEngine.Input.ActionPrompt.md#MicroEngine_Input_ActionPrompt_Kind) and the other members let a game draw a glyph instead (a key cap, a coloured pad button).

 [DeviceId](MicroEngine.Input.DeviceId.md)

Identifies one input device for as long as it is connected: a keyboard, a mouse, a touch screen, one gamepad. Ids
are chosen by whoever reports the device (a backend's own ids are positive; the engine's polled sources and touch
screen use negative ones) and are never reused while the game runs.

 [InputEvent](MicroEngine.Input.InputEvent.md)

One thing that happened on an input device, in the order it happened, with the time it happened on the host's
monotonic clock. Backends deliver them ([IInputBackend](MicroEngine.Input.IInputBackend.md)); the engine routes them through the input
layers (UI, on-screen controls, then gameplay) and derives the polled state actions read from those no layer took.
Build one with the static methods ([KeyDown](MicroEngine.Input.InputEvent.md#MicroEngine_Input_InputEvent_KeyDown_MicroEngine_Input_DeviceId_MicroEngine_Input_Key_System_Double_System_Boolean_), [PointerDown](MicroEngine.Input.InputEvent.md#MicroEngine_Input_InputEvent_PointerDown_MicroEngine_Input_DeviceId_System_Int64_MicroEngine_Input_PointerKind_System_Numerics_Vector2_MicroEngine_Input_PointerButton_System_Double_)...).

 [PointerInfo](MicroEngine.Input.PointerInfo.md)

A pointer the game can read: the mouse while it is over the window, or a finger or pen on the screen.

 [PointerState](MicroEngine.Input.PointerState.md)

The mouse (or another pointer) over the game's virtual canvas.

 [TouchControlView](MicroEngine.Input.TouchControlView.md)

Everything a skin needs to draw one control as it is this frame, in screen pixels.

 [TouchEvent](MicroEngine.Input.TouchEvent.md)

One change of one finger, in the touch screen's pixels ([Size](MicroEngine.Input.ITouchScreen.md#MicroEngine_Input_ITouchScreen_Size)).

### Interfaces

 [IActionSource](MicroEngine.Input.IActionSource.md)

A source of actions rather than of keys: on-screen touch controls, a network peer, an accessibility switch or an AI.
Added with [AddSource](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_AddSource_MicroEngine_Input_IActionSource_), it is read every step together with the keyboard and gamepad, so a game
cannot tell which produced an action.

 [IGamepadRumble](MicroEngine.Input.IGamepadRumble.md)

Gamepad vibration, a capability of input backends whose pads can rumble.

 [IInputBackend](MicroEngine.Input.IInputBackend.md)

The input part of a platform: the devices and what happens on them, delivered as events. A capability of hosts with
input ([TryGetCapability``1](MicroEngine.Platform.IPlatformHost.md#MicroEngine_Platform_IPlatformHost_TryGetCapability__1___0__)); the engine drains it once per frame, at the start
of the frame's input phase.

 [IInputLayer](MicroEngine.Input.IInputLayer.md)

One stage of input routing: user interface, on-screen controls, a debug console, a rebinding screen. Events reach
the layers from the highest order down ([InputLayers](MicroEngine.Input.InputLayers.md)); a layer that returns true takes the event, and
nothing below it (other layers, the actions of gameplay) sees it.

 [IInputSource](MicroEngine.Input.IInputSource.md)

The raw device state a backend reports once per polled frame.

 [IKeyLabels](MicroEngine.Input.IKeyLabels.md)

The labels the player's keyboard layout prints on keys: "Q" for [A](MicroEngine.Input.Key.md) on AZERTY. A capability of input
backends that know the layout ([TryGetCapability``1](MicroEngine.Platform.IPlatformHost.md#MicroEngine_Platform_IPlatformHost_TryGetCapability__1___0__)).

 [IOverlayCanvas](MicroEngine.Input.IOverlayCanvas.md)

Primitive drawing in screen pixels, implemented by each backend for [ITouchOverlay](MicroEngine.Input.ITouchOverlay.md).

 [ITextInputControl](MicroEngine.Input.ITextInputControl.md)

Turns the system's text input on and off: the on-screen keyboard on phones, input methods (IME) on desktops. A
capability of input backends that deliver [Text](MicroEngine.Input.InputEventKind.md).

 [ITouchOverlay](MicroEngine.Input.ITouchOverlay.md)

Something drawn over the game on the touch screen, such as [VirtualControls](MicroEngine.Input.VirtualControls.md).

 [ITouchScreen](MicroEngine.Input.ITouchScreen.md)

A screen that may be touched, a capability a backend offers ([TryGetCapability``1](MicroEngine.Platform.IPlatformHost.md#MicroEngine_Platform_IPlatformHost_TryGetCapability__1___0__)): its drawable size, the
area free of notches, rounded corners and system bars, its density, the touches since the last frame, and a layer
drawn over the game for on-screen controls. Coordinates are drawable pixels with the origin at the top left.
SDL reports phones and touch screens, the browser host reports Pointer Events, the headless platform lets tests
touch it ([HeadlessTouchScreen](MicroEngine.Core.HeadlessTouchScreen.md)).

 [ITouchSkin](MicroEngine.Input.ITouchSkin.md)

Draws touch controls. The engine calls it for each shown control with its state; a game may replace the whole look
([Skin](MicroEngine.Input.TouchLayout.md#MicroEngine_Input_TouchLayout_Skin)) and still delegate kinds it does not change to [Instance](MicroEngine.Input.DefaultTouchSkin.md#MicroEngine_Input_DefaultTouchSkin_Instance).

### Enums

 [DeviceKind](MicroEngine.Input.DeviceKind.md)

The kind of an input device.

 [GamepadStyle](MicroEngine.Input.GamepadStyle.md)

The button names of a gamepad family, for prompts: [PadButton](MicroEngine.Input.PadButton.md) uses Xbox names, which PlayStation pads
show as shapes and Nintendo pads with A/B and X/Y swapped.

 [InputDevice](MicroEngine.Input.InputDevice.md)

The kind of device that last pressed an action (see [LastDevice](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_LastDevice)).

 [InputEventKind](MicroEngine.Input.InputEventKind.md)

What an [InputEvent](MicroEngine.Input.InputEvent.md) reports.

 [Key](MicroEngine.Input.Key.md)

Physical keys, named by their position on a US keyboard (a scancode, not a character): [A](MicroEngine.Input.Key.md) is the key
left of [S](MicroEngine.Input.Key.md) whatever the layout prints on it, so WASD movement stays in place on AZERTY or Dvorak. Ask
[IKeyLabels](MicroEngine.Input.IKeyLabels.md) (or <code>ActionPrompts</code>) for the label the player's layout shows. Backends map their own
codes onto these; the set covers a full-size keyboard.

 [PadAxis](MicroEngine.Input.PadAxis.md)

Analog gamepad axes. Sticks are -1..1 (right and down positive), triggers 0..1.

 [PadButton](MicroEngine.Input.PadButton.md)

Gamepad buttons by position, using Xbox names ([A](MicroEngine.Input.PadButton.md) is the bottom face button on every pad); backends map
their own codes. [GamepadStyle](MicroEngine.Input.GamepadStyle.md) says what the pad prints there.

 [PointerButton](MicroEngine.Input.PointerButton.md)

Pointer buttons. A finger or pen tip is [Primary](MicroEngine.Input.PointerButton.md).

 [PointerKind](MicroEngine.Input.PointerKind.md)

What kind of pointer moved or pressed: the mouse, a finger on a touch screen or a pen.

 [PromptKind](MicroEngine.Input.PromptKind.md)

What an [ActionPrompt](MicroEngine.Input.ActionPrompt.md) shows: a key, a gamepad control, an on-screen control, or a plain label.

 [InputMap.StickDirection](MicroEngine.Input.InputMap.StickDirection.md)

The direction of the left stick an action is bound to (<code>None</code>, <code>Left</code>, <code>Right</code>, <code>Up</code>,
<code>Down</code>). The action is down when the stick points that way beyond [StickThreshold](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_StickThreshold), and its
[Value](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Value_System_String_) is the deflection along that direction.

 [StickDirections](MicroEngine.Input.StickDirections.md)

How a 2D stick turns into the four direction actions.

 [StickPlacement](MicroEngine.Input.StickPlacement.md)

Where a virtual joystick's base is.

 [TouchAnchor](MicroEngine.Input.TouchAnchor.md)

Which corner or edge of the safe area a touch control is placed from.

 [TouchButtonMode](MicroEngine.Input.TouchButtonMode.md)

How a touch button drives its action.

 [TouchPhase](MicroEngine.Input.TouchPhase.md)

What happened to a finger in a [TouchEvent](MicroEngine.Input.TouchEvent.md): it touched the screen (<code>Began</code>), moved (<code>Moved</code>),
was lifted (<code>Ended</code>), or the system took it away, for example when the game left the screen (<code>Canceled</code>).

 [TouchShape](MicroEngine.Input.TouchShape.md)

The outline of a drawn touch control.

 [TouchVisibility](MicroEngine.Input.TouchVisibility.md)

When on-screen controls are drawn.

</div>
