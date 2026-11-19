# INPUT-2: the pointer in actions and rebinding

Date: 2026-11-19. Branch `feat/input-2` (from `main` at 834833a). Part of
[CONSOLIDATION-1](consolidation-1.md) (section 2.4, closed representations); issue #175. Decision:
[ADR 0062](../decisions/0062-one-control-representation.md). Manual: [Input](../manual/input.md).

Evidence words are used strictly: **run** means executed on the development machine (Windows 11, x64) and its output
read; **desk** means judged from documentation or source without running it.

## A. Need

A strategy game orders with a click and scrolls a list of units with the wheel; a management game zooms and drags its
map; a point-and-click adventure has two buttons and nothing else; a twin-stick shooter aims with the right stick or
the mouse; a first-person view turns with the mouse's motion. All of them need what every other game already has for
keys: the player chooses the control, the choice is saved, prompts name it, a menu on top takes the press, and a test
can script it. None of them could have it.

## B. Current experience (before this work)

A binding was five fixed lists: keys, gamepad buttons, one left-stick direction, gamepad axes, chords
(`InputMap.cs:55-62` on `main`). The saved format wrote exactly those five. `ListenForPress` and `BindFrom` accepted a
key-down or a gamepad button-down and nothing else. The mouse could be read (`Input.Pointer`: position, three buttons
held, the wheel's sum) but not bound, and reading it was a second input path beside actions: no edges (the game kept
last frame's state itself), no back or forward button (SDL delivered them, the polled state dropped them), no motion,
no way to hide or lock the cursor. The right stick had only per-axis bindings with a per-axis dead zone, so a
twin-stick aim was square near the centre and the right stick could not press directions.

The manual gave the reason in one line: "The mouse is readable (`Input.Pointer`) but cannot be bound to actions; no
game uses it." That is one of the statements the project no longer takes as a conclusion: the four demos do not use
it because none of them is one of the genres above.

What a developer wrote by hand for a click-to-move game with rebinding (**desk**, from the `Pathfinding` sample,
which reads `Input.Pointer` directly, and the controls screen of `samples/Examples`): the previous button state to
find a click; a check that the pointer is not
over a widget (the UI layer took the event, but the game read `Input.Pointer` in the same update and could not tell
"no click" from "a click the UI took" without knowing the layers); a second saved file for mouse bindings, with its
own format; a second listening path; a second table of names for prompts.

Reading the code also found a defect nobody had hit because nothing bound the mouse: a secondary, middle or side
button press that an input layer took (a right click on a panel) was recorded as a capture of the whole pointer, and
only a release of the *primary* button ended it. Until then every mouse event went to that layer, so the next left
click anywhere was lost (**run**: the test
`A_secondary_click_a_layer_took_does_not_take_the_next_primary_click_with_it` fails with the old rule).

## C. Alternatives

| Alternative | What it gives | Why not, or why |
| --- | --- | --- |
| A sixth and seventh list (`PointerButtons`, `Wheel`) on the binding | The smallest diff | The same closed shape with two more members: the saved format, `ListenForPress`, `BindFrom`, prompts and the conflict logic of a controls screen each grow a branch per list, and the next control (a pen's barrel button, a second pointer) is another list. It also keeps "one left-stick direction" as a special case |
| A class hierarchy (`KeyControl`, `MouseButtonControl`...) open to the game | Extensible by a game | Sampling runs for every action at every update and must not allocate; a control must be a value that compares, saves as text and has a stable reading order. A game that needs a control the engine does not know has `IActionSource`, which is already the extension point |
| **One value type, `InputControl`, with a closed set of kinds** | One list per action; one text form; one listener; one conflict rule | Chosen. Closed on purpose: the set is what the engine's backends deliver. A new kind is one enum member, one line in the sampler and one in the parser |
| A separate "pointer actions" layer beside `InputMap` | No change to bindings | It is the fragmentation the question was about: two APIs, two saves, two prompt tables |
| Gestures as controls (`Tap`, `Swipe`) | Touch games bind like the rest | See section J: a gesture is a recogniser over time and position, not a control that is down or up |

For the wheel, three semantics were weighed. A held state ("down while turning") merges two notches in consecutive
updates into one press, so "next weapon" skips. A pure value leaves `Pressed` useless. **An impulse** (pressed for one
update per notch, every notch a new press, the value being the notches turned in that update) serves both "next
weapon" and "zoom by this much", and a fractional wheel accumulates to whole notches for presses while its value stays
smooth.

For pointer motion: it is an amount per update, not a position in 0..1. Clamping it would make it useless and
pressing an action from it would be arbitrary, so it gives a value and never a press, and `InputMap.Motion` reads
four actions without `Vector`'s limit of 1.

For the cursor: `SDL_HideCursor`, `SDL_SetWindowRelativeMouseMode` and raylib's `HideCursor`/`DisableCursor` exist;
a phone has no cursor; a browser locks only after a gesture. That is the shape of an optional platform capability, the
pattern `IClipboard`, `IGamepadRumble` and `ITextInputControl` already follow.

## D. Target experience

```csharp
protected override InputMap CreateInput() => InputMap.CreateDefault()
    .Bind("order", PointerButton.Primary)                       // a click; a tap on a phone
    .Bind("grab", PointerButton.Secondary)
    .Bind("zoom-in", InputControl.WheelUp, Key.Equals)
    .Bind("zoom-out", InputControl.WheelDown, Key.Minus)
    .Bind("look-right", InputControl.PointerRight)
    .BindStick("look-right", PadStick.Right, InputMap.StickDirection.Right);

// In a scene's Update:
if (Input.Pressed("order")) SendUnitsTo(camera.ScreenToWorld(Input.Pointer.Position));   // never a click a widget took
if (Input.Pressed("zoom-in")) camera.Zoom *= 1.25f;                                      // once per notch
if (Input.Down("grab")) camera.Position -= Input.Pointer.Delta / camera.Zoom;
if (Game.TryGetCapability<ICursorControl>(out var cursor) && Input.Pressed("grab")) cursor.SetRelative(true);

// A controls screen: the next mouse button or wheel turn becomes the order, and is saved with the keys.
Game.InputSystem.ListenForPress(press => { Input.BindFrom("order", press, replace: true); Input.SaveBindings(storage); }, PressKinds.All);
```

## E. Responsibility boundary

The engine owns: what a control is, how a map reads it, the routing that lets a layer take a click before any action
sees it, listening, the saved format, the names of the mouse's controls, the pointer's edges and motion, and the
cursor capability on the platforms that have a cursor. The game owns: which actions exist and what a click means,
which controls may be rebound and what happens on a conflict (the examples swap), the sensitivity it offers the
player, and when the cursor is locked. Nothing genre-specific entered the engine: there is no "selection box" and no
"camera drag".

## F. Integration

- **Layers and UI.** Pointer controls are read from the device state, which is built only from events no layer took.
  `UiRoot` takes presses and wheel turns over its widgets, so a click on a widget presses no action and no code in
  the map knows about widgets (**run**: `A_click_or_a_wheel_turn_on_a_widget_presses_no_action_bound_to_the_mouse`).
  Captures are per mouse button.
- **Players.** A mouse is a device; a player's view filters devices, so pairing works unchanged (**run**).
- **Touch.** A finger no on-screen control took is the primary pointer, so `PointerButton.Primary` is also a tap and
  `LastDevice` says `Touch`. On-screen controls hide for the mouse as for the keyboard.
- **Determinism.** The hash of a map's state is unchanged for a map with no mouse control bound and no side button
  held (**run**: nine recorded hashes of a sequence with keys, a gamepad, mouse moves, clicks and the wheel are
  identical before and after). With a mouse control bound, the hash also covers the side buttons, the motion, the
  previous buttons and the wheel's accumulated fractions.
- **Saved bindings.** Format 2 is one list of control texts per action, written with `Utf8JsonWriter` and read with
  `JsonDocument`: no reflection, as before (ADR 0054). Format 1 loads through the same reader (**run**: a literal
  version 1 file).
- **Scripts.** `mouse X,Y down|up [button]`, `click X,Y [button]` and `wheel D [H]`; the script's wheel turns where
  its mouse is.
- **Backends.** SDL: five buttons (already delivered), motion with SDL's own relative motion scaled to canvas pixels,
  a locked cursor's position frozen where it was locked, the cursor capability except on phones. raylib: the two side
  buttons added, the cursor capability, a locked cursor's motion from `GetMouseDelta`. Headless: a cursor that
  remembers.

## G. Trade-offs

- `ActionBindingData`'s five lists stay as the first format's fields, empty on export. A game that edited an exported
  set by those lists must move to `Controls` or to `Unbind(action, control)`; the engine's own example did, and came
  out shorter. Recorded in the migration notes.
- The set of control kinds is closed. A game cannot add "my pedal" as an `InputControl`; it adds an `IActionSource`.
- A wheel direction cannot be held, and three notches in one update are one press with value 3. A game that must
  count them reads the value.
- `Value` is no longer bounded by 1 for actions bound to the wheel or to pointer motion; the documentation says so
  and `Vector` still limits its result.
- Listening for mouse buttons is opt-in. A controls screen worked with the mouse would otherwise bind the click on
  its own "cancel" button; existing screens keep their behaviour.
- `PointerState` grew; code that compared a whole `PointerState` with one it built must compare members.

## H. Decision

**Remodel** the binding representation (ADR 0062): one list of `InputControl` per action, used by binding, sampling,
listening, the saved format and the conflict logic. **Extend** the pointer state (edges, side buttons, motion, kind),
the stick settings (right stick) and the platform contract (`ICursorControl`). **Do not add** gesture recognition in
this unit (section J).

## I. Validation

All **run** on the development machine unless marked.

| What | Result |
| --- | --- |
| `tests/MicroEngine.Tests` (`PointerActionTests`, 21 tests; `BindingCompatibilityTests`, 4; script and widget tests) | pass |
| The same tests with the old capture rule and without the wheel impulse | three fail, as they should |
| State hashes of a map with no mouse control, recorded on `main` | identical |
| `tests/MicroEngine.Desktop.Tests` input conformance: side buttons; cursor hidden, locked, a locked mouse's motion | pass on SDL (events pushed into SDL's queue, hidden window) and headless; on raylib only what needs no injected event (the cursor capability) |
| `samples/Examples` tour, played hidden, capture `33-mouse.png` looked at | the scout walks to the click, zoom 1.25, the view dragged, "ORDER: LMB" |
| `Game.Prompts` naming the mouse's controls | done: the keyboard's prompts list an action's mouse controls after its keys (`PromptKind.Pointer`), `LastDevice == Mouse` counts as the keyboard for prompts, and the right stick shows as "R→"; `ActionPromptTests` covers it |
| A real mouse on a visible SDL or raylib window | **not run**: no person at the machine. raylib's mapping of its two side buttons to back and forward follows GLFW's order and is **desk** |
| A browser (pointer lock after a click, Escape) | **not run** here; the behaviour documented is SDL's on Emscripten and the browsers' rule, **desk** |
| Android, iOS, macOS, Linux | **not run** here |

## J. Gestures: evaluated, not added

**Need.** Touch-first games read a tap, a long press, a swipe and a pinch. Today a game gets every finger
(`Input.Pointers`, with ids and positions) and every event (`InputSystem.Events`) and writes the recogniser.

**What a recogniser is.** Not a control: a control is down or up at an instant, a gesture is a judgement over a
window of time and distance, with thresholds in physical units (millimetres, so the screen's density), arbitration
between candidates (a tap or the start of a swipe; a long press or a slow drag; one finger of a pinch lifting), and
an answer that sometimes comes late (a tap is known at release, a double tap after a timeout). It belongs above the
events, as a small layer, and its results are events with a position, not actions.

**Alternatives.** (1) Bindable gesture controls: rejected, for the reason above; a swipe direction as an impulse
control is tempting but drags the arbitration into the sampler. (2) A `GestureRecognizer` type fed from
`InputSystem.Events`, with tap, long press, swipe and pinch and thresholds in millimetres: the right shape, about the
size of the on-screen stick's logic. (3) Nothing.

**Decision: not in this unit.** The reason is not that a game can write it. It is that the thresholds and the
arbitration are the whole capability, and they are tuned on a touch screen in a hand: a recogniser whose defaults were
never felt on a phone is a prototype, and this unit could not put a finger on a screen (no device, no person at the
machine). Scripted fingers prove the logic, not the feel. The representation chosen here does not need to change for
it: the recogniser reads the same events and pointers. It is recorded as its own initiative, to be done with a device
at hand, with option (2) as the starting design.
