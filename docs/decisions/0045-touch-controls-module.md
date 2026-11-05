# 0045 — Touch controls as an opt-in input module

Status: Accepted (2026-11-04).

## Context

Every game got on-screen controls: `Game` created a `VirtualControls`, made it an action source and the touch input
layer, drew it over every platform's touch screen and ran `engine.touch` every frame, whether the game was meant for
touch or not (ARCH-1 S1). The engine is now composed from modules on an `EngineBuilder`
([0026](0026-kernel-modules-and-schedule.md)), and its frame schedule is fixed when the engine is built, before `Load`.
Every platform (SDL, the browser page, the headless platform) offers an `ITouchScreen`, because a desktop may have a
touch screen too; only its `TouchFirst` flag says that touch is the main way to play (phones, tablets, touch-only
browsers). Games and documented samples read and configure `Game.Touch` on every platform.

## Decision

- **An assembly of its own.** `MicroEngine.Input.Touch` holds the touch screen contract (`ITouchScreen`, `TouchEvent`,
  `TouchPhase`), the overlay contracts backends draw (`ITouchOverlay`, `IOverlayCanvas`), the letterbox rule
  (`FrameLayout`), the controls (`TouchLayout`, `TouchDPad`, `TouchStick`, `TouchButton`, `VirtualControls`) and their
  look (`TouchStyle`, `ITouchSkin`, `DefaultTouchSkin`, `OverlayShapes`). It references input, 2D graphics, the kernel and
  the platform contracts (text is allowed for labels). The namespace stays `MicroEngine.Input`; `MicroEngine.dll`
  forwards every type. `ITouchScreen` moves with the controls because its `Overlay` and the controls' methods use each
  other's types.
- **Active only when wanted.** The controls activate when the platform's touch screen is touch-first (with the default
  layout unless the game has its own) or when the game sets a layout of its own, on any platform: overriding
  `CreateTouchLayout` without calling the base method, setting `Game.Touch.Layout` in `Configure`, or setting it later
  (`Load`, a scene). Activation is one-way (`VirtualControls.IsActive`, `Activate`).
- **Inactive costs nothing.** `Game.Touch` always exists (from `Configure` on), so games and samples can read and
  configure it, but inactive controls have no frame system, no input layer, no action source and no overlay; every
  finger reaches gameplay as a pointer.
- **Where activation takes effect.** Controls active while the engine is composed are wired in then and run in
  `engine.touch`, scheduled right after `engine.input.events` as before. Controls activated after the build (the
  schedule is fixed) join at the start of the next frame, inside `engine.input.events` before its input is routed, and
  are updated at the end of that system, where `engine.touch` would have run: the same order within the frame.

## Consequences

- A game that never sets a layout, on a screen that is not touch-first, pays nothing for touch controls.
- Touch-first phones and browsers keep plug-and-play controls; the four demos, which declare their layouts, keep theirs
  on every platform, and so do games and samples that set a layout at run time.
- A touch-screen laptop or desktop browser no longer shows the default controls to a game that never set a layout.
- The per-step state hash is unchanged: inactive controls press nothing, and active ones behave as before.
