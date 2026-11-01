# ADR 0020: Unified input and engine-owned touch controls

## Status

Accepted. Amends [ADR 0017](0017-sdl3-native-platform.md) (`TouchPad` is replaced). Evidence:
[INPUT-1](../evaluations/input-1.md).

## Context

Before INPUT-1, input was digital only: an `InputMap` read keys, pad buttons and left-stick directions past half
deflection. Phones got `TouchPad` on SDL only: nine fixed circles that pressed gamepad buttons, sized as fractions of
the screen height, with no safe area, no pressed state beyond a brighter fill, no joystick, a hit area that let a
sliding finger press its neighbours, and nothing at all in mobile browsers. Played on a phone, buttons were hard to hit
and gave no clear sign that a press was received. Games could not say which controls they need: Meadow Run, Neon Rift,
Courtyard Key and Trail Duel all got the same pad.

## Decision

- **Actions stay the only thing games read**, now both digital and analog. `InputMap` keeps `Down`, `Pressed`,
  `Released` and `Axis` unchanged, and adds `Value(action)` (0..1), `AnalogAxis` and `Vector(left, right, up, down)`
  (keys give unit vectors with normalised diagonals, sticks give their deflection). The left stick has a radial dead
  zone (`StickDeadZone`), a press threshold, and eight or four sectors (`StickMode`), so a grid game never steps
  diagonally and a platformer gets real diagonals. Triggers and the right stick bind with `BindAxis`; `Unbind` and
  `GetBindings` allow remapping. `LastDevice`, `PadConnected` and `Pointer` describe the devices.
- **Action sources** (`IActionSource`, `InputMap.AddSource`) are the extension point for input that is not a key:
  touch controls, a network peer, an accessibility switch. They are read every step with the devices.
- **The engine owns touch controls** (`VirtualControls`, configured by a game's `TouchLayout` from
  `Game.CreateTouchLayout`): d-pads (four or eight way), joysticks (fixed or floating, analog, with dead zone and
  digital threshold) and buttons (hold or toggle, pressing one or several actions). Positions are density-independent
  pixels from safe-area corners; the layout shrinks only when a screen cannot hold it. Each finger belongs to the
  control it started on until it lifts; buttons tolerate small slides and release beyond them (or roll onto a
  neighbour when the layout allows it); taps shorter than a frame are kept until a step reads them; rotation, focus
  loss and suspension release fingers. Hit testing, state, haptic requests and drawing are all in the core, so the
  same layout behaves the same everywhere and is tested headlessly.
- **Backends only report a touch screen and draw primitives.** `IPlatform.Touch` (`ITouchScreen`, default null)
  gives touches with stable finger ids, the size, the safe area, the density, a vibration call and an overlay layer
  drawn over the letterboxed frame through `IOverlayCanvas` (circle, ring, triangle, rectangle, text). SDL reads finger
  events, `SDL_GetWindowSafeArea`, `SDL_GetWindowDisplayScale` and drives the Android vibrator through its haptic API.
  The browser page forwards **Pointer Events** (with pointer capture and coalesced moves), CSS
  `env(safe-area-inset-*)` and `navigator.vibrate` to the raylib platform. The frame itself is fitted inside the safe
  area, and placed at the top in portrait so the controls get the space below (`FrameLayout`).
- **Scripts can touch** (`touch`, `tap`, `lift`): scripted fingers go through the same hit testing on the platform's
  real layout, which is how the phone checks exercise two thumbs at once.

## Alternatives considered

- **NippleJS for browser joysticks** (MIT, a few kilobytes, maintained: 1.0.x releases in 2026). It draws DOM elements over the
  page and reports its own events. It would give browsers a joystick but not d-pads or buttons, would need a second
  layout description beside the native one, could not run in SDL or in headless tests, and its DOM drawing would not
  match the native look. Rejected: one layout in the core, drawn by each backend, is less code than the bridge.
- **raylib's touch API in the browser** (`GetTouchPointCount`, `GetTouchPosition`). Per-frame state indexed by
  position, without cancellation and without stable identity when a finger lifts; it loses quick taps and cannot
  follow a thumb. Rejected for Pointer Events, which every current mobile browser implements.
- **SDL's virtual joystick** (`SDL_AttachVirtualJoystick`). It injects a fake controller but draws nothing and does
  no hit testing; touch controls would still be ours, behind a gamepad indirection that loses which control was used.
- **Keeping `TouchPad` and adding a joystick.** Its pad-button indirection made touch a gamepad (no labels per game,
  conflicts with a real pad, no toggles) and it existed on SDL only.
- **.NET touch libraries** (MonoGame's `TouchPanel`/virtual gamepad samples, Myra and other UI toolkits). Tied to their
  frameworks' renderers and loops; none is a standalone input layer for SDL or raylib.
- **iOS haptics through Core Haptics or `UIImpactFeedbackGenerator`.** Would need iOS-specific native glue the SDL host
  does not have; SDL's haptic API only drives controllers on iOS. Left out: iOS phones give visual feedback only.

## Consequences

- `TouchPad` is removed and touch no longer presses gamepad buttons; a game that relied on it gets the default layout,
  which presses the same default actions ([migration](../migrations.md#030-alpha)).
- `IInputSource` gains default members (`GetPadAxis`, `PadConnected`, `Pointer`) and `IPlatform` gains `Touch`; existing
  implementations compile unchanged.
- Each game chooses its controls in a few lines and is tested through touches; the next game reuses everything.
