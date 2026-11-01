# INPUT-1: Unified input system and mobile experience

Objective: one public input system for digital and analog actions on the six official platforms, engine-owned touch
controls (d-pad, joystick, buttons) with clear feedback, and the four demos comfortable to play on desktops, phones
and mobile browsers. Branch `claude/trusting-cannon-wb6k7z` (the session's designated branch; the brief asked for
`feat/input-1`), from `main` at 6115a70. Delivery: autonomous, no PR or release without the owner. Test runner:
`dotnet test MicroEngine.slnx -c Release`; browser: `node web/tests/mobile.mjs`.
Evaluation: [`docs/evaluations/input-1.md`](../../docs/evaluations/input-1.md). Decision: [ADR 0020](../../docs/decisions/0020-unified-input-and-touch-controls.md).

## Specs

- S1 Audit before changing: contracts, sources, mappings, SDL and raylib, keyboard/mouse/pad/touch, virtual controls,
  browser vs native, duplication, documented limits, real interaction problems, tests and gaps.
- S2 Research and choose: SDL3, raylib/WebAssembly, Pointer/Touch Events, virtual-control libraries (NippleJS
  included as a candidate), .NET libraries, safe areas and orientation; compare by licence, maintenance,
  compatibility, maturity, size, integration and reuse; "buscar, evaluar y reutilizar antes de implementar".
- S3 Unified actions: keyboard, mouse/pointer, digital and analog pads, touch, simultaneous sources, pressed/held/
  released, axes, dead zones, remapping, connect/disconnect, focus, suspend/resume, custom sources; analog not reduced
  to four arrows, digital kept precise; no needless breaking changes, migrations documented.
- S4 Virtual controls: d-pad (4/8), analog joystick (fixed/dynamic), action buttons; per-game layouts, large areas,
  real multitouch, finger tracking, slide tolerance, cancel/release, safe-area positioning, scaling by size/density/
  aspect, opacity, visibility by device and context, orientation; no game rules in the engine.
- S5 Feedback and ergonomics: normal/pressed states, visible joystick displacement, direction and intensity,
  immediate, distinguishable under transparency, short transitions; optional haptics; not colour/sound/vibration only;
  hit areas larger than drawings without ambiguity; no stuck or phantom presses; two thumbs.
- S6 Mobile web is mandatory: Chrome Android, Safari iOS, others; Pointer Events, multitouch, held controls,
  gestures, scroll, zoom, context menus, focus, resize, orientation, dynamic bars, safe areas, canvas scaling.
- S7 Demos: Meadow Run (precise digital, jump while holding a direction), Courtyard Key (d-pad vs joystick), Trail
  Duel (exploration, menus, battle), Neon Rift (move, aim, fire, jump at once); no HUD hidden.
- S8 Validation: presses, holds, quick taps, multitouch, analog, diagonals, dead zones, two thumbs, sliding off,
  focus, suspend, reconnection, orientation, safe areas, small viewports, aspect ratios, coexistence, no regressions;
  six targets by the means available; real multi-touch in mobile emulation; emulators/simulators for phones; honest
  limits; behaviour, not rendering, as evidence.
- S9 Documentation and distribution: architecture, actions, axes, controls, feedback, layouts, safe areas, native vs
  web, custom sources, examples, limits; demo pages; migration; release artifacts prepared, nothing published.
- S10 Scope: no UI framework, editor, physics, renderer, audio, loop rewrite, SDK distribution or new platform
  architecture.

## Tasks

| Task | Specs | Mode | Status | Evidence |
| --- | --- | --- | --- | --- |
| T1 Audit and research | S1 S2 | inline | done | Evaluation §1–2, ADR 0020 alternatives |
| T2 Core input: analog, dead zones, sectors, axes, remapping, sources, devices | S3 | inline | done | b964d3f; `AnalogInputTests` (9) |
| T3 Virtual controls, touch screen contract, overlay, scripts, headless touch | S4 S5 | inline | done | b964d3f; `TouchControlsTests` (28) |
| T4 SDL touch screen (safe area, density, haptics, focus, pad reconnection) | S3 S4 S5 | inline | done | b964d3f; Android/iOS CI levels |
| T5 Demo layouts and behavioural tests | S7 | inline | done | 1462c83; `TouchTests` in each game (10) |
| T6 Browser: Pointer Events, safe areas, gestures, focus, visibility, haptics | S6 | inline | done | 6bef9d7; `mobile.mjs` 256/256 |
| T7 Phone checks: logged layout, real taps and drags, scripted two thumbs | S8 | inline | see evaluation | 80e2e98; platform workflow run |
| T8 Docs, ADR, migration, CI job | S9 | inline | done | ed7ea4a and later |

## Log

- L1 Audit: input was digital only; `TouchPad` (SDL only) pressed gamepad buttons; no touch at all in browsers; the
  left stick was digital past 0.5 per axis; no dead zone; one pad, no reconnection; no focus-loss release on SDL.
- L2 `web/build-raylib-wasm.sh` ended silently on Linux (`pipefail` with no Python pack): fixed, toolchain now set up
  and packages built on Linux x64.
- L3 First browser run showed Neon Rift's 480×270 frame at 1× on a 412-pixel-high phone browser (a third of the screen
  empty) and the joystick arrow hidden under its knob: frame scales fractionally when whole factors fill under 80 %,
  arrow drawn beyond the knob, labels never above the asked height.
