# INPUT-1: unified input and the mobile experience

Date: 2026-11-01. Branch `claude/trusting-cannon-wb6k7z` (from `main` at 6115a70). Task record:
[`odd/tasks/input-1.md`](../../odd/tasks/input-1.md). Decision: [ADR 0020](../decisions/0020-unified-input-and-touch-controls.md).
Manual: [Input](../manual/input.md).

Evidence words are used strictly: **run** means executed in the development container and its output read; **CI**
means it ran on GitHub-hosted runners from this branch (platform workflow runs 41275968161 and the re-run listed in
§5, validate run 41275980931); **desk** means judged from documentation or source without a prototype.

## 1. Audit before this work

| Area | State found | Problem for players |
| --- | --- | --- |
| Public contract | `IInputSource` (keys, pad buttons, left stick), `InputMap` (digital actions: `Down`, `Pressed`, `Released`, `Axis`) | No analog value reached games; a stick was four arrows past half deflection per axis, with no dead zone |
| Mapping | `Bind` adds keys, buttons, one stick direction | No way to remove a binding or read bindings back; no triggers, no right stick |
| Gamepads | SDL and raylib read pad 0; SDL kept the first pad opened | Unplugging the active pad of two left the game without one |
| Focus | raylib releases keys; SDL relied on `SDL_GetKeyboardState` | SDL kept on-screen buttons and tapped keys on focus loss |
| Touch, native | `TouchPad` in the core, used by SDL only: nine fixed circles pressing gamepad buttons, sized from the screen height, a 1.35× hit radius, fingers sliding between controls | Same layout for all games, no labels, no joystick, ghost presses when a sliding thumb crossed a neighbour, no safe area (notches covered controls), the only pressed state was a brighter fill |
| Touch, web | None. raylib's own touch state was unused; the page had `touch-action: none` on the canvas only | The four web demos could not be played on a phone |
| Duplication | The tap-latching code existed three times (raylib keys, SDL keys, `TouchPad`) | — |
| Tests | 4 `TouchPadTests`, input edge tests, keyboard tests in browsers | Nothing for analog input, multitouch, safe areas, rotation, focus with touch, or touch reaching any game |
| Documentation | [Input](../manual/input.md) listed "no mouse, pointer or touch API", "one gamepad, left stick only, digital thresholds" | — |

## 2. Technologies evaluated

| Candidate | Licence, maintenance | What it offers | Verdict |
| --- | --- | --- | --- |
| SDL 3.5 (ppy.SDL3-CS) | zlib; active (in use) | Finger events with ids and cancel, `SDL_GetWindowSafeArea` and its change event, `SDL_GetWindowDisplayScale`, haptics (Android vibrator), gamepad axes/triggers/hot-plug, `SDL_RenderDebugText`, `SDL_RenderGeometry`; also `SDL_AttachVirtualJoystick` | **Used** for every native platform. Run: bindings checked for each function before use. The virtual-joystick API injects a fake controller and draws nothing: not used |
| raylib 6.0 touch (`GetTouchPointCount`/`Position`/`PointId`) | zlib; active | Per-frame state indexed by position; on the web, `touchstart/move/end/cancel` callbacks store points by index (`rcore_web.c`, read here) | **Not used**: no cancel phase in the API, identity by index, taps within a frame lost |
| Pointer Events (W3C) | Browser standard; Chrome, Firefox, Safari 13+ | Stable `pointerId` per finger, `pointercancel`, pointer capture, coalesced moves, `pointerType` to leave the mouse alone | **Used** in the page (`main.js`) |
| Touch Events | Browser standard | Same data without capture or a mouse/touch distinction | Not needed beside Pointer Events |
| CSS `env(safe-area-inset-*)`, `viewport-fit=cover`, `visualViewport`, `screen.orientation` | Standards; Safari and Chrome | Notch and home-indicator insets, browser-bar changes, rotation | **Used** |
| `navigator.vibrate` | Standard; Chrome/Android, not Safari | Short vibrations | **Used** where present |
| NippleJS | MIT; maintained (1.0.x in 2026), a few kB | DOM joystick for browsers (static, semi, dynamic modes) | **Rejected** (desk, plus the decision's reasons): browser-only, joystick-only, a second layout description beside the native one, DOM drawing that native and headless cannot share |
| MonoGame virtual gamepad samples, Myra and similar .NET UI toolkits | MIT/various | Touch UI tied to their own loop and renderer | **Rejected** (desk): not usable over SDL or raylib |
| Core Haptics / `UIImpactFeedbackGenerator` (iOS) | Apple | Taptic feedback | **Not done**: needs native glue the SDL host lacks; iOS gives visual feedback only |

The engine's own code is the integration: hit testing, finger ownership, layouts in dp from safe-area corners,
feedback drawing and haptic requests in the core (about 700 lines with documentation), backends reporting touches and
drawing five primitives.

## 3. What was built

- `InputMap`: `Value`, `AnalogAxis`, `Vector`; radial dead zone, threshold and 8/4 sectors for the stick; `BindAxis`
  (triggers, right stick); `Unbind`/`GetBindings`; `AddSource`/`IActionSource`; `LastDevice`, `PadConnected`, `Pointer`.
- `VirtualControls` + `TouchLayout`: `TouchDPad`, `TouchStick` (fixed/floating), `TouchButton` (hold/toggle, several
  actions); opacity, scale, hit scale, slide tolerance, optional sliding between buttons, haptics, visibility.
- `ITouchScreen` (`IPlatform.Touch`), `IOverlayCanvas`, `FrameLayout`; `SdlTouchScreen`, `RaylibTouchScreen` +
  `BrowserTouch`, `HeadlessTouchScreen`; SDL focus-loss release and pad reconnection.
- Input scripts `touch`, `tap`, `lift`; `Scene.Describe()` for probes; a `touch.script` per demo.
- Web page: Pointer Events, safe area, gesture blocking, focus/visibility, suspension, haptics, bottom-centre page
  buttons on touch screens, "Tap to play".
- Checks: `web/tests/mobile.mjs`, the Android device check's `touch`, `touch-drag` and `touch-two-thumbs` levels, the
  iOS `touch-script` level, a `web-mobile` job in the platform workflow.

## 4. Per game

| Game | Control choice and why | Behaviour shown |
| --- | --- | --- |
| Meadow Run | Four-way d-pad (a rolled thumb never becomes down-right: no accidental crouch or plank drop); A = jump + confirm, B = run + cancel, thumb roll B→A | Walk right, jump while right stays held, stop on release, B→A roll (headless tests; mobile browsers; Android real drag; Android and iOS scripted) |
| Courtyard Key | Evaluated both: the hero walks in eight directions with four facings, so a floating joystick, now analog (light push = slow walk next to pits and blocks); keys unchanged | Full push equals keyboard speed, half push slower, joystick diagonals equal keyboard diagonals, pause reachable (headless; mobile browsers: 0.5 s full vs light push) |
| Trail Duel | Four-way d-pad for grid, menus and the battle grid; B held runs; MENU; the gamepad stick set to four sectors | Grid steps, MENU opens the start menu and B closes it, stick never diagonal (headless; mobile browsers; Android real drag) |
| Neon Rift | Floating joystick (eight-way aim is easier on a stick than on a cross), FIRE and JUMP with roll, LOCK as a toggle so standing aim needs no third finger | Up-right aim with fire held produces only up-right shots; a third finger jumps while aiming and firing; LOCK on/off; locked stick aims without walking (headless; mobile browsers) |

No game rule changed. Courtyard Key reads `Input.Vector` instead of `Axis` + normalise (identical for keys); each
playing scene got a `Describe()` for probes.

## 5. Results per platform

| Target | Means | Result |
| --- | --- | --- |
| Engine (all platforms) | `dotnet test`, run | 151 engine tests pass (28 touch, 9 analog), 10 new game touch tests; all game suites pass (Meadow Run 47, Courtyard Key 30, Trail Duel 87, Neon Rift 32) |
| Linux x64 | validate run 41275980931 (CI): .NET tests and native rendering, keyboard and audio captures | Pass |
| Windows x64 | validate run 41275980931 (CI): .NET tests | Pass. Not played by hand |
| macOS | platform run 41275968161 (CI): tests and SDL captures against the macOS reference | Pass |
| Web, mobile emulation | `mobile.mjs`, run (Chromium 153) and CI (Google Chrome) | 256/256 checks, 4 devices × 4 games, before and after the last visual fixes |
| Android (x86_64 emulator, API 34) | platform runs (CI) | Neon Rift: every level passed in 41275968161. All games: real system taps on the confirm control pass the title; a real held drag on the movement control presses the direction and moves the player (Meadow Run x 50→96, Trail Duel y 9→16, Courtyard Key moved); the scripted two-thumb level first failed for three games because the scripts pressed the second control too briefly for the 100 ms probe at ~125 updates/s, fixed in 122fdd2 (see the re-run below) |
| iOS (simulator) | platform runs (CI) | Captures, audio, lifecycle as before; scripted touches on the simulator's layout walk and jump (Meadow Run x 50→164 with a jump to y 115); same two-thumb script fix |

Re-run after 122fdd2: _filled in below when it finishes._

## 6. Mobile web evidence

`mobile.mjs` emulates an Android phone (915×412, DPR 2.625), a notched iPhone (844×390, DPR 3, insets 47/47/21 — this
is Chromium with an iOS user agent, **not Safari**), a small phone in portrait (360×640) and a tablet (1024×768), and
sends real multi-touch through `Input.dispatchTouchEvent`, which Chrome delivers to the page as touch and Pointer
Events. For every device and game it checked: "Tap to play", controls drawn, the confirm control leaving the title,
no scroll or zoom, every control on screen, controls outside the notch insets (iPhone), the per-game behaviour in §4,
losing focus releasing held controls, a tap shorter than a frame, rotation with a finger down (released, re-laid out,
working after), a pinch neither zooming nor scrolling, no context menu. Screenshots (`dist/web-mobile`, also the
`web-mobile` CI artifact) show the pressed and idle states, the d-pad's lit arrow and thumb marker, the joystick knob
and rim arrow.

The first run exposed two problems that the checks did not fail on but the screenshots did: Neon Rift's 480×270 frame
drawn at 1× on a 412-pixel-high browser (a third of the screen unused) and the joystick's arrow hidden under its knob
at full push. Both were fixed (`FrameLayout` scales fractionally when whole factors fill less than 80 %; the arrow is
drawn beyond the knob) and the full matrix passed again.

## 7. Checking on a real phone

Automation cannot stand in for a thumb on glass. On a physical Android phone (Chrome) and an iPhone (Safari), open a web
demo and, for each game:

1. Tap Play. The controls appear; the page does not zoom or scroll when tapping repeatedly or pinching.
2. Hold the movement control with the left thumb and press the main button with the right: both act at once.
3. Slide the left thumb well off the d-pad or stick: the direction stays; lift it: the player stops.
4. Press a button and slide the finger far away: it releases; slide back: it presses again.
5. Long-press anywhere: no menu, no text selection, no magnifier.
6. Rotate the phone with a finger down: nothing stays pressed; the controls move to the new edges and work.
7. Swipe to the home screen and come back: the game was paused (music stopped) and nothing is pressed.
8. On Android, presses vibrate briefly (unless the phone's touch vibration is off); on iPhone there is no vibration.
9. On a notched phone in landscape, no control sits under the notch or the home indicator.

The same steps apply to the Android APKs. Record the phone, OS and browser version with the result.

## 8. Remaining limitations

- No physical phone was used: system-level multi-touch was exercised in emulated Chrome; on the Android emulator real
  taps and drags are single-pointer (`adb input`), and two thumbs were scripted through the controls; the iOS simulator
  cannot receive scripted system touches, so iOS touch is scripted only. Safari was not run.
- No haptics on iOS; no gamepad rumble.
- One gamepad at a time; the mouse is readable but not bindable; rebinding is code (no screen); layouts are code.
- Touch controls overlap the game on screens near 16:9 (the letterbox bars hold them only on wider phones); dialogs at
  the bottom of Courtyard Key and Trail Duel can sit under the lower corners' controls at reduced opacity. Games can hide
  controls per context with `Touch.SetVisible`; the demos do not yet.
- The in-game "Controls" screens of the demos describe keyboard and gamepad only; the touch controls are described in
  each game's README.
- Labels use the backends' built-in fonts (SDL's 8×8 debug font, raylib's default font).
