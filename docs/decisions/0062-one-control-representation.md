# 0062 — One control representation for bindings; the pointer in actions; the cursor as a capability

Status: Accepted (2026-11-19). The SDK is in Alpha: the changed public types carry no compatibility commitment.
Evidence: [INPUT-2](../evaluations/input-2.md). Amends [ADR 0020](0020-unified-input-and-touch-controls.md), whose
actions and layers stay as they are.

## Context

An action's binding was five fixed lists (keys, gamepad buttons, one left-stick direction, gamepad axes, chords), and
the saved format, the listener of a rebinding screen and `BindFrom` each knew exactly those. The mouse could be read
but not bound; the reason on record was that no game used it. The cross-cutting audit
([CONSOLIDATION-1](../evaluations/consolidation-1.md), section 2.4) named this a closed representation: strategy,
management, point-and-click, twin-stick and first-person games each needed a second input path, outside rebinding,
saving and prompts. The same audit listed what the pointer state lacked (button edges, the side buttons, motion, a
cursor that can be hidden or locked) and that the right stick had no radial dead zone and no direction bindings.

## Decision

- **A binding is one list of `InputControl`.** `InputControl` is a value with a closed set of kinds: key, chord,
  gamepad button, mouse button, wheel direction, stick direction (either stick), gamepad axis side, pointer motion
  direction. Keys, chords, gamepad buttons and mouse buttons convert to it implicitly, so the fluent `Bind` calls read
  as before; the rest have names. A control has one text form (`Key:Space`, `Mouse:Secondary`, `Wheel:Up`).
- **The list is kept in reading order** (by kind, then as bound), and the map reads it in one pass: the digital kinds
  first, the first one down deciding the device, then the analog ones, the largest value winning. That is the order
  the five lists were read in, so existing games sample exactly as before.
- **Mouse buttons are digital controls; the wheel is an impulse; pointer motion is a value.** A wheel direction
  presses its action for one update per whole notch, every notch a press of its own, and the action's value is the
  notches turned in that update; fractions (touchpads) accumulate to presses. Pointer motion gives a value in canvas
  pixels times `InputMap.PointerSensitivity` and never presses. `Value` may therefore exceed 1 for those two, and
  `InputMap.Motion` reads four actions without `Vector`'s limit.
- **Pointer controls are read from the device state, which only holds events no input layer took.** The guarantee
  that a click on a widget or an on-screen control presses no action comes from the routing, not from the map. Each
  mouse button other than the primary one is a capture of its own.
- **Listening for mouse presses is asked for** (`ListenForPress(pressed, PressKinds.All)`); the default stays keys
  and gamepad buttons, so a controls screen worked with the mouse does not bind the click on its own buttons.
- **Saved bindings have a second format**: one list of control texts per action, version 2, written and read without
  reflection. The first format's five lists are still read, from files and from sets a game kept in its own save.
- **The pointer's state is complete**: every button with pressed and released edges computed per update by the map,
  the side buttons, the motion since the previous update (the backend's own relative motion where it has it), and
  the pointer's kind.
- **The cursor is an optional platform capability, `ICursorControl`**: visible or hidden, free or locked (relative
  mode). SDL offers it on desktops and in browsers, raylib offers it, the headless platform remembers it; phones do
  not have it.
- **Either stick is read as a whole** by `BindStick`, with a radial dead zone of its own (`RightStickDeadZone`); an
  axis bound alone keeps `AxisDeadZone`.
- **State hashes** of a map with no mouse control bound and no side button held are byte for byte what they were;
  the pointer's later state enters the hash only otherwise.
- **Gestures are not controls.** A tap, a long press, a swipe or a pinch is a judgement over time and distance; it
  belongs in a recogniser above the events and is not part of this decision (INPUT-2, section J).

## Alternatives considered

- **More lists on the binding** (mouse buttons, wheel): the same closed shape, a branch per list in five places, and
  the next control is another list.
- **Open control classes a game can derive**: sampling must not allocate and a control must compare, save and order;
  `IActionSource` is already where a game adds a control the engine does not know.
- **A pointer API beside actions**: the fragmentation the review was asked to remove.
- **The wheel as a held state or as a value only**: the first merges consecutive notches into one press, the second
  leaves `Pressed` useless.
- **Listening for the mouse by default**: changes what existing controls screens do when the player clicks.

## Consequences

- A mouse button, the wheel and pointer motion are bound, rebound, saved and scripted like keys, in one API.
- `InputMap.ExportBindings` fills `ActionBindingData.Controls` only; code that edited an exported set by the five
  lists moves to `Controls` or to `Unbind(action, control)` (migration notes).
- A new kind of control is an enum member, a line in the sampler and a line in the parser; a saved file that names a
  kind an older version does not know loses that control and keeps the rest.
- `PointerState`, `InputEvent` (`Delta`), `IInputSource` (`RightStick`) and `InputDevice` (`Mouse`) grew.
- Not verified on this decision's date: a hand on a real mouse, a browser's pointer lock, and every system other than
  Windows (INPUT-2, section I).
