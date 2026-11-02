# Namespace MicroEngine.Input

<div data-pagefind-weight="0.3">

Input actions over keyboard, gamepad, mouse and touch: bindings, analog values, on-screen controls, custom sources.

Guide: [Input](../manual/input.md)  

### Classes

 [ActionBindings](MicroEngine.Input.ActionBindings.md)

What an action is bound to, for display or a rebinding screen ([GetBindings](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_GetBindings_System_String_)).

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

 [InputMap](MicroEngine.Input.InputMap.md)

Named actions bound to keys, gamepad buttons, stick directions and analog axes, plus any number of action sources
(touch controls, custom sources). Games query actions ("jump", "confirm"), never physical keys, so bindings and
scripted input stay in one place. Every action is both digital ([Down](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Down_System_String_), [Pressed](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Pressed_System_String_),
[Released](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Released_System_String_)) and analog ([Value](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Value_System_String_), [Vector](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Vector_System_String_System_String_System_String_System_String_)): a key is 0 or 1, a stick or a
virtual joystick gives the deflection beyond its dead zone.
Edge states (pressed/released) are computed per fixed update, so a tap is never lost or doubled.

 [OverlayShapes](MicroEngine.Input.OverlayShapes.md)

Shapes built from [IOverlayCanvas](MicroEngine.Input.IOverlayCanvas.md) triangles and rectangles, which never overlap, so translucent colours stay even.

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
drawing. It is an [IActionSource](MicroEngine.Input.IActionSource.md) (read by [InputMap](MicroEngine.Input.InputMap.md) with the keyboard and gamepad) and an
[ITouchOverlay](MicroEngine.Input.ITouchOverlay.md) (drawn by the backend over the game). Each finger belongs to the control it started on
until it lifts, so two thumbs work independently and a thumb that slides a little stays on its control.

### Structs

 [ActionPrompt](MicroEngine.Input.ActionPrompt.md)

How to show one action to the player on one device: <code>"Z"</code> on a keyboard, <code>"A"</code> on an Xbox pad,
<code>"×"</code> on a PlayStation pad, the button's own label on a touch screen. [Text](MicroEngine.Input.ActionPrompt.md#MicroEngine_Input_ActionPrompt_Text) is what to print;
[Kind](MicroEngine.Input.ActionPrompt.md#MicroEngine_Input_ActionPrompt_Kind) and the other members let a game draw a glyph instead (a key cap, a coloured pad button).

 [PointerState](MicroEngine.Input.PointerState.md)

The mouse (or another pointer) over the game's virtual canvas.

 [TouchControlView](MicroEngine.Input.TouchControlView.md)

Everything a skin needs to draw one control as it is this frame, in screen pixels.

 [TouchEvent](MicroEngine.Input.TouchEvent.md)

One change of one finger, in the touch screen's pixels ([Size](MicroEngine.Input.ITouchScreen.md#MicroEngine_Input_ITouchScreen_Size)).

### Interfaces

 [IActionSource](MicroEngine.Input.IActionSource.md)

A source of actions rather than of keys: on-screen touch controls ([VirtualControls](MicroEngine.Input.VirtualControls.md)), a network peer,
an accessibility switch or an AI. Added with [AddSource](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_AddSource_MicroEngine_Input_IActionSource_), it is read every step together with
the keyboard and gamepad, so a game cannot tell which produced an action.

 [IInputSource](MicroEngine.Input.IInputSource.md)

The raw device state a backend reports once per polled frame.

 [IOverlayCanvas](MicroEngine.Input.IOverlayCanvas.md)

Primitive drawing in screen pixels, implemented by each backend for [ITouchOverlay](MicroEngine.Input.ITouchOverlay.md).

 [ITouchOverlay](MicroEngine.Input.ITouchOverlay.md)

Something drawn over the game on the touch screen, such as [VirtualControls](MicroEngine.Input.VirtualControls.md).

 [ITouchScreen](MicroEngine.Input.ITouchScreen.md)

A screen that may be touched, reported by a backend ([Touch](MicroEngine.Core.IPlatform.md#MicroEngine_Core_IPlatform_Touch)): its drawable size, the
area free of notches, rounded corners and system bars, its density, the touches since the last frame, and a layer
drawn over the game for on-screen controls. Coordinates are drawable pixels with the origin at the top left.
SDL reports phones and touch screens, the browser host reports Pointer Events, the headless platform lets tests
touch it ([HeadlessTouchScreen](MicroEngine.Core.HeadlessTouchScreen.md)).

 [ITouchSkin](MicroEngine.Input.ITouchSkin.md)

Draws touch controls. The engine calls it for each shown control with its state; a game may replace the whole look
([Skin](MicroEngine.Input.TouchLayout.md#MicroEngine_Input_TouchLayout_Skin)) and still delegate kinds it does not change to [Instance](MicroEngine.Input.DefaultTouchSkin.md#MicroEngine_Input_DefaultTouchSkin_Instance).

### Enums

 [GamepadStyle](MicroEngine.Input.GamepadStyle.md)

The button names of a gamepad family, for prompts ([ActionPrompts](MicroEngine.Input.ActionPrompts.md)): [PadButton](MicroEngine.Input.PadButton.md) uses Xbox
names, which PlayStation pads show as shapes and Nintendo pads with A/B and X/Y swapped.

 [InputDevice](MicroEngine.Input.InputDevice.md)

The kind of device that last pressed an action (see [LastDevice](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_LastDevice)).

 [Key](MicroEngine.Input.Key.md)

Physical keys the engine understands. Backends map their own codes onto these.

 [PadAxis](MicroEngine.Input.PadAxis.md)

Analog gamepad axes. Sticks are -1..1 (right and down positive), triggers 0..1.

 [PadButton](MicroEngine.Input.PadButton.md)

Gamepad buttons using Xbox layout names; backends map their own codes.

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
