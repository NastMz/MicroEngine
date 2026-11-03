# ADR 0032: Input as device events with ids, players and layers

## Status

Accepted (2026-11-03). Amends [ADR 0020](0020-unified-input-and-touch-controls.md) (actions stay the only thing games
must read, but they are now derived from a device event stream; the on-screen controls become one input layer instead
of the owner of every touch). Builds on [ADR 0026](0026-kernel-modules-and-schedule.md) (the `Input` phase),
[ADR 0037](0037-platform-host-contract.md) (capabilities) and [ADR 0038](0038-one-composition-path.md). Evidence:
ARCH-1 §3 (S10, S11, D11 for QWERTY prompts), §6.4 (input sketch), §6.5 (input row), §7; annex
[A3](../evaluations/arch-1/A3-sim.md) IN-1..IN-7 and UI-1; annex [R2](../evaluations/arch-1/R2-engine-research.md)
§6 and §7.6; the browser determinism report
([`experiments/arch-1/browser-determinism`](../../experiments/arch-1/browser-determinism/README.md): unread taps
expired after eight drawn frames).

## Context

`IInputSource` was a polled snapshot with no device index: SDL opened the first gamepad, raylib read pad 0, and hosts
that added a source got a `CompositeInput` that merged everything by "any source down", so a keyboard holding Left and
a pad holding Right gave an axis of 0 (measured, A3 E5). The vocabulary was a 66-key enum with no text, wheel, rumble
or event order. Every touch was drained by the on-screen controls, so a game could not read a tap, a swipe or a drag.
Nothing arbitrated between a menu and the scene under it except `InputMap.Reset()` on scene changes. Prompts named
keys by the US enum (an AZERTY player was told "Press A" for the key labelled Q). Taps shorter than a frame were kept
for eight drawn frames, so a page drawing more than eight frames per update lost them. Rebinding had no persistence,
and the engine's widgets read hard-coded action names.

## Decision

- **A `MicroEngine.Input` assembly** referencing only the kernel and the platform contracts (the architecture test
  checks it), shipped inside the `MicroEngine` package. It holds the device vocabulary, the event model, the derived
  state, players, layers, `InputMap` and binding persistence. The on-screen controls (`VirtualControls`,
  `TouchLayout`, skins) and `ActionPrompts` stay in the `MicroEngine` assembly for now because they draw through the
  renderer there; they move once the 2D renderer and text are assemblies of their own.
- **Devices have ids and report events.** `DeviceId`, `DeviceKind` and `InputDeviceInfo` (name, pad style, the player
  number the pad shows, whether it can rumble) identify devices. `InputEvent` is one timestamped thing that happened:
  device added or removed, key down or up (with repeat), text and composition, pointer down, up, move, cancel and
  leave with a pointer id and kind (mouse, touch, pen) in canvas and screen pixels, wheel, gamepad button and axis.
  `IInputBackend.Drain` is the backend contract (order, announce-before-use, keys are positions, no synthesized
  mouse/touch, release on focus loss); `IKeyLabels`, `IGamepadRumble` and `ITextInputControl` are optional
  capabilities. `Key` covers a full-size keyboard and `PadButton` adds guide, misc, paddles and touchpad, appended so
  existing numbers never change.
- **Polled state is derived in the core.** `InputDevices` applies the events no layer took and answers the polled
  questions `InputMap.Sample` already asks, so `InputMap` keeps its API, its edge rules and its state hash. A press
  that started and ended between updates reads as held until an update has sampled it: **taps expire by updates, never
  by drawn frames.** Polled sources (`IInputSource`: a host's extra input, a test's hand-set keys, a backend not yet
  converted) remain devices of their own.
- **Players with explicit pairing and a zero-setup default.** `Players` holds one `PlayerInput` (an `InputMap` and its
  paired devices) per local player. A device paired with a player is heard only by that player; an unpaired device is
  heard by player 1 while player 1 has no device of their own, otherwise by nobody, and its presses raise
  `UnpairedPress` (the join hook). So single-player code is unchanged (`Game.Input` is player 1's map), keyboard and
  pad of two players never cancel, and an unassigned pad never drives anyone by accident (the leafwing and Godot
  pitfalls of R2). Other players and pairings join the state hash only when they exist, so single-player traces keep
  their hashes.
- **Input layers with consumption.** Events pass through `IInputLayer`s from the highest order down (UI 300, on-screen
  controls 200, game layers 100) before gameplay; a layer that takes a press also gets everything it starts (that
  pointer until it lifts, that key's release). The on-screen controls are a layer: fingers that land on a control are
  theirs, every other finger reaches gameplay as a pointer (`InputMap.Pointers`, `Pointer`, `InputSystem.Events`).
  Within one update, readers of the same action arbitrate with `InputMap.Consume`.
- **Text is a channel, not actions.** `TextInput.Start(caret)` / `Stop()` drive the backend's text input (IME
  candidates placed at the caret, the on-screen keyboard on phones); typed text and the composition are read per
  update.
- **Rebinding persistence and engine action names.** `InputMap.ExportBindings`/`ImportBindings` (by name, unknown names
  skipped, removed actions not restored) and `SaveBindings`/`LoadBindings` over `IStorage` (unreadable data set
  aside); `InputSystem.ListenForPress` and `InputMap.BindFrom` capture "the next press" without gameplay seeing it.
  The engine's own parts read action names from `InputMap.Names` (`ActionNames`), defaults the game may rename.
- **Scripts inject device events.** `InputSystem.Send` queues events that are routed with the next frame exactly like a
  backend's, so scripted runs and tests exercise the real input path; forcing actions (`SetForced`) stays for tests.
- **Key prompts from the layout.** Bindings stay positional; prompts ask `IKeyLabels` for the label the player's
  layout prints.

The work lands in steps: the assembly, the event model, players, layers, text, persistence and the headless backend
first; then the SDL and raylib backends as event backends (every pad with hot-plug, player index, type and rumble,
text and IME, wheel, layout labels; raylib taps by updates); then scripted device events, engine action names in the
widgets and prompts, and a two-player scenario. Window shortcuts (F11, Alt+Enter) becoming a rebindable engine action
belong to the graphics stage (directive 10, 2a).

## Alternatives considered

- **Keep merged polled state and grow the enum** (S10-S11): cheap, but no device identity, no second player, no event
  order, no text, and touches still owned by the controls.
- **Unity's full action-asset system** (action maps, control schemes, a project asset): its own team acknowledged the
  complexity, users found simple tasks harder, and Unity later added a default project-wide asset (R2 §6). The simple
  path here stays one line, with players, pairing and persistence behind optional APIs.
- **Device index on every binding** (Godot style): keeps one map but makes every binding per device and leaves players
  to the game; the R2 forum evidence shows that is where local multiplayer goes wrong.
- **Putting the backend contract (`IInputBackend`, the event types) in `MicroEngine.Platform`**, as the dossier's
  physical-structure table sketches: deferred. Today's backends reference the core, which references `MicroEngine.Input`,
  so the contract works from the input assembly; moving the event types to the platform contracts when backends stop
  referencing the core (stage 5) is a mechanical move of one file, and is recorded as an open point.

## Consequences

- `Game.InputSystem` exposes players, layers, devices, events and text; `Game.Input` is unchanged for single-player
  games. The frame schedule gains `engine.input.events` (routing) before `engine.touch` in the `Input` phase.
- `VirtualControls` gains `BeginFrame`, `HandleTouch`, `TakeScripted` and `EndFrame` (`Update` remains, as their
  composition); its taps expire by updates.
- A game whose class has a member named like a new `Game` member gets a hiding warning; `Game.Players` was therefore not
  added (players are `Game.InputSystem.Players`).
- Backend authors implement `IInputBackend` (and the optional capabilities) instead of the polled `IInputSource`; a
  polled source keeps working as one device with no pairing finer than "the whole source".
