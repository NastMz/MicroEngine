# Evaluation: WEB-1, web support for the four demos

**Question.** Can Meadow Run, Courtyard Key, Trail Duel and Neon Rift be built, run and played in modern browsers
from the same gameplay code as desktop, through a reproducible build that a landing page can consume later?

**Answer.** Yes, in Chromium and Firefox on desktop computers with a keyboard. One shared host builds any of the four
games into a static package (`web/build.sh`), from a pinned toolchain that `web/setup.sh` installs without
touching the system .NET. Every game was run in Chrome 156 and Firefox 150 through its real mechanics, from the
title to its final boss and ending, and the frames it draws for the same input are byte-identical to the
desktop build (138 captures per browser). Mobile browsers and Safari were not tested and have no touch controls.

Validated on Windows 11, 2026-10-30, branch `feat/web-support`. Build and usage: [web/README.md](../../web/README.md).
Decisions: [ADR 0007](../decisions/0007-browser-host.md), [ADR 0008](../decisions/0008-web-packages.md). The
WEB-0 experiment that preceded this: [web-feasibility.md](web-feasibility.md).

## What WEB-0 left and what changed

| WEB-0 piece | Status after review | WEB-1 |
| --- | --- | --- |
| `GameLoop` (Start/Tick/Finish), browser guards in `RaylibPlatform` | Definitive | Kept; guards extended (fullscreen and minimum size belong to the page) |
| `ISaveStorage` + `BrowserSaveStorage` (localStorage) | Definitive | Kept; `SaveStore` now reports refused writes and never overwrites an unreadable slot |
| raylib rebuilt with the workload's emscripten, content manifest + preload | Definitive | Kept; scripted in `web/setup.sh` |
| `BrowserLauncher.Start` returning a `GameLoop` | Generalised | Returns a `GameSession` (start, end, restart, errors) |
| `web/NeonRift.Web` (one host per game) | Experimental | Replaced by `web/Host` + `web/games.props` (one entry per game) |
| Hand-installed SDK in `~/.dotnet-wasm`, README steps | Experimental | `web/toolchain.env` + `web/setup.sh` + `web/build.sh` |
| Page glue (text counter, no end screen) | Experimental | Loading, ready, end, error screens; gesture start; fullscreen; storage and sound notices |
| `experiments/web/RaylibProbe` | Experimental | Left as the WEB-0 record; not part of the build |

## Results per game

Kinds of evidence used below:

- **Script run**: the game's own input script replayed in the browser package (input forced through `InputMap`,
  as on desktop), captures compared byte for byte with the desktop build.
- **Keyboard run**: the same script typed through the browser's keyboard (trusted key events at the scripted
  update numbers), checked to reach the same scene, with page screenshots reviewed.
- **Platform suite**: the same checks for every game (below).

| Game | Mechanics asked for | Evidence in Chrome and Firefox |
| --- | --- | --- |
| Meadow Run | Movement, jumps, collisions, enemies, coins, transformations, transitions, saves, achievements | Script runs: menus (title, controls, map, level card, level, pause), smoke (new game, level 1 running and jumping), showcase (Bloom form: block bumps, coins, a stomp), end (credits after the Mole King). Keyboard runs: menus, smoke. New game save written to `localStorage` (and refused visibly when storage is full). |
| Courtyard Key | Exploration, combat, enemies, items, room navigation, interface, saves, achievements | Script runs: title, opening (elder dialogue, receiving the sword, swinging it, walking north to the road), fight (slime on the road), boss (Moss Warden intro, telegraphs and attacks, boomerang), ending (story, credits, statistics). Keyboard runs: opening, fight. |
| Trail Duel | Exploration, encounters, turn-based combat, capture, evolution, menus, saves, achievements | Script runs: title, new game (house, town, lab), master battle (the final hall battle, 81 captures through the turn sequence). Keyboard run: new game. |
| Neon Rift | Movement, shots, enemies, stages, pause, progression, audio, saves, achievements | Autopilot (the game's own, invulnerable to shots as in its desktop test) clears stages 1, 2 and 3 and reaches the ending in both browsers. Script runs: menus, stage 1 (run, aim, jump, fire), ending credits. Keyboard run: stage 1. Achievements: unlocked and kept across a reload in WEB-0; corrupt achievement data set aside in WEB-1 (both browsers). |

Every game also passed the platform suite in both browsers: ready screen, no audio before the start, a key held
while loading not reaching the game, audio running after the starting click, about 60 updates per second, held,
released and very short key presses, focus loss releasing keys, browser keys not scrolling the page, fullscreen
(button and Alt+Enter), storage across a reload, end screen instead of the last frame, a new run without
reloading, no page errors, and saves only under the game's own `microengine/<Game>/` keys.

**What "reaching the end" means here.** The endings were reached in the browser builds, but not in one
continuous human playthrough per game: Neon Rift's three stages were played by its autopilot; the other games'
final bosses and endings were reached from their developer checkpoints (`COURTYARD_START=boss`, `TRAIL_DEBUG=trainer`,
`MEADOW_START=credits`) with their scripts. Whole playthroughs from a new game to the credits are covered by
the games' headless tests (`PlaythroughTests`), and the byte-identical captures show the browser build runs the
same simulation. A person playing each game to the end in a browser, with a physical keyboard, is still to do.

## Input

- **The missed first presses of WEB-0 were real and had two causes.** (1) Before WEB-1 the game started as soon as
  it loaded, so a key pressed during loading could reach it late or not at all; now nothing reaches the game
  until the player starts it, and the starting key or click is consumed by the page. (2) raylib reports whether a
  key is down when a frame starts, so a press and release between two frames was never seen; this happens with
  quick taps during long frames (the first frames after loading) and, as testing showed, whenever a frame runs no
  simulation step (a display faster than 60 Hz). `RaylibInput` now reads raylib's pressed-key queue and keeps a
  press down until a simulation step has read it. This also applies to desktop.
- Held keys, releases, focus loss (raylib's GLFW layer releases held keys on `blur`) and browser keys are covered
  by the platform suite. The browser keeps Ctrl/Cmd shortcuts and function keys (F5, F11, F12); arrows, Space,
  Tab, `/` and `'` are kept from the browser while playing. F11 stays the browser's fullscreen (it cannot be
  taken by a page); the alternatives are Alt+Enter and the Fullscreen button.
- All keyboard input in testing came from automation (trusted CDP and WebDriver BiDi key events). No physical
  keyboard session was run. Gamepads were not tested.

## Audio

- The game starts inside the player's click or key press, so raylib creates its `AudioContext` while the
  browser allows audio. Measured on the audio output (an analyser on the node feeding the speakers): music and
  effects reach the output in all four games in both browsers, with autoplay blocked until a gesture (peak RMS
  0.12 to 0.44).
- If a browser still holds audio suspended, a "Sound is blocked: click to enable" button appears and any key or
  click resumes it (checked in both browsers by suspending the context). Firefox once kept audio suspended for
  a page started without a gesture (development `autostart=1`), and the button appeared; Chrome driven over CDP
  always allows audio (navigation counts as user activation), so its blocked path was not observed for real.
- raylib's web audio uses the deprecated `ScriptProcessorNode` (a console warning in Chrome) and is fed once per
  frame, so sound stops while the tab is hidden; the game pauses too, because `requestAnimationFrame` stops.

## Persistence

| Check | Result |
| --- | --- |
| Save and load after a reload | Pass: entries survive reloads; Neon Rift achievements (WEB-0) and a generic slot per game (platform suite) |
| Isolation between games | Pass: each game writes only `microengine/<Game>/...`; all four packages on one origin do not collide |
| Achievements | Pass: unlocks are written at once, progress about once a second and when the page is closed (`pagehide` ends the run) |
| Missing data | Pass: a fresh profile starts fresh in every game |
| Corrupt data | Pass: corrupt achievements were kept as `achievements.corrupt` and the game ran (both browsers) |
| Full storage | Pass: with the origin's quota filled, Meadow Run's new-game save failed, the page said "site storage is full", the game kept running and nothing was overwritten (both browsers) |
| Blocked storage | Pass in Firefox (`dom.storage.enabled=false`): the game ran with a session-only store and the page said progress will not be kept. Not tested in Chrome. |
| Catalogue versions | Engine behaviour, unit-tested and independent of the medium: a grown catalogue keeps old progress and adds new entries locked, entries removed from the catalogue survive a save, a lowered goal unlocks quietly on load |
| No overwrite after a read failure | Engine behaviour (unit tests `A_slot_that_could_not_be_read_is_never_overwritten`, `An_unreadable_file_is_never_overwritten`): a slot that could not be read is not written by that store |

Limits: `localStorage` holds about 5 MB per origin, shared with anything else on that origin (the games' saves
are a few kilobytes); browsers can evict site data under storage pressure, private windows discard it, and
Safari may delete script-written storage of sites not visited for seven days. IndexedDB would not remove these
limits, so `localStorage` stays.

## Lifecycle and presentation

Loading screen with progress (runtime, then content files), a ready screen ("Press any key or click to play"),
an end screen with "Play again" (a new run without reloading: raylib re-initialises on the same canvas), and an
error screen with the exception text. The canvas fills the page or iframe and the game is letterboxed at whole-
number scales when there is room (Neon Rift's 480x270 at 2x in 960x600) and scaled down without cropping when
the space is smaller (Meadow Run's 320x180 in a 240x160 iframe was checked in the Claude app's browser pane).
The embedding page receives `ready`, `starting`, `playing`, `ended` and `failed` messages.

The Claude app's browser pane never entered fullscreen (no event and no error); fullscreen was checked in
Chrome and Firefox instead.

## Chrome and Firefox

| | Chrome 156 | Firefox 150 |
| --- | --- | --- |
| Start, rendering, scene changes | Pass | Pass |
| Input | Pass | Pass |
| Audio | Pass | Pass |
| Persistence | Pass (blocked storage not tested) | Pass, including blocked storage |
| End and restart | Pass | Pass |
| Fullscreen | Pass (button, Alt+Enter) | Pass (button, Alt+Enter) |
| Captures equal to desktop | 138/138 | 138/138 |

No browser-specific fixes were needed. One run of the Firefox script scenarios, made while other browser tests
loaded the machine, drew some captures at other moments than desktop in three games (and missed a few); repeated
alone, all 138 matched again, twice. The cause was not found; it affects scripted capture comparisons under
heavy load, not play. Other differences seen were in automation, not in the games: Chrome counts
CDP navigation as user activation (audio is never blocked under test), and Firefox's WebDriver BiDi keyboard
does not know the key name "Space" (the test uses Z for jump).

**Not tested:** Safari (no macOS or iOS device here), Chromium-based browsers other than Chrome, mobile browsers.
The games have no touch controls, so phones and tablets cannot play them; Safari's WebGL 1 and Web Audio
support should be enough for raylib, but nothing was run.

## Size and performance

Transfer sizes when the server sends the `.br` copies (raw size in brackets):

| Game | Total | Runtime + raylib (`dotnet.native.wasm`) | .NET and game assemblies | Content |
| --- | --- | --- | --- | --- |
| Meadow Run | 1.7 MB (5.5) | 0.7 MB | 0.85 MB | none (procedural) |
| Courtyard Key | 1.7 MB (5.5) | 0.7 MB | 0.85 MB | one PNG |
| Trail Duel | 2.6 MB (7.0) | 0.7 MB | 0.91 MB | 0.85 MB (WAV, PNG) |
| Neon Rift | 11.9 MB (16.4) | 0.7 MB | 0.82 MB | 10.2 MB (OGG music 7.4 MB, WAV 2.6 MB, PNG 0.2 MB) |

Plus about 0.13 MB of JavaScript per package.

- **Invariant globalization** (no ICU data) took the framework from 2.7 MB to 1.7 MB per game; the games format
  only digits, so nothing visible changed (the script captures stayed identical).
- **Full trimming** was measured and saves nothing more: partial trimming already trims the framework, and the
  game assemblies are small. The reflection-based JSON in `SaveStore` therefore costs no download size.
- **Neon Rift's music** is the one large item: four OGG tracks, already compressed. Re-encoding them at a lower
  bitrate or loading music after the game starts would cut the first download, at the cost of changing the
  shipped assets or adding streaming; not done.
- WEB-0 reported Neon Rift at 6.4 MB with Brotli; that figure counted only files with `.br` copies. The package
  was about 13.8 MB then and is 11.9 MB now.
- Frame rate: the simulation stays at 60 updates per second when the page draws 100, 50 or 25 frames per second,
  in both browsers and all four games. Below 12 frames per second the five-steps-per-frame cap slows the game
  instead of skipping (same as desktop; measured in WEB-0 at 8 fps).

## Desktop

- 279 automated tests pass (engine 93, Meadow Run 44, Courtyard Key 27, Trail Duel 85, Neon Rift 30).
- The 15 script scenarios produce 138 desktop captures, all byte-identical between `main` and this branch.

## Limitations and open work

- Physical keyboard and gamepad play by a person, in both browsers, to the end of each game.
- The unexplained Firefox capture divergence under heavy machine load (above).
- Safari, other Chromium browsers and mobile browsers; touch controls do not exist.
- Neon Rift's first download (11.9 MB, mostly music).
- raylib's `ScriptProcessorNode` audio (deprecated API; sound pauses with hidden tabs).
- The toolchain scripts ran on Windows only; Linux and macOS paths are written but unrun.
- `localStorage` limits and eviction (above); no cloud copy, by design.
