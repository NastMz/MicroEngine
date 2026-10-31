# 0003 — Scene stack, plain entities, fixed-step runner, raylib behind IPlatform

Status: Accepted (2026-10-29)

## Decision

- **Runtime:** `GameRunner` advances the simulation at a fixed 60 Hz (max five steps per frame) and
  renders once per frame. Headless platforms advance one step per frame, making runs deterministic.
- **Scenes:** a stack managed by `SceneManager`. Overlays draw over (and by default freeze) the scene
  below. Transitions cover the screen, swap at their midpoint and reveal the new scene.
- **Entities:** abstract `Entity` classes in `EntityList<T>` with deferred add/remove and depth-sorted
  drawing. No ECS: the games have tens to hundreds of objects, and explicit classes read better.
- **Backend boundary:** `IPlatform` (renderer, graphics device, audio device, input source). raylib
  (Raylib-cs 8.1.0) is the only desktop backend and lives in `MicroEngine.Raylib`.
- **Input:** named actions only; scripts and tests drive the same actions as devices.

## Alternatives considered

- ECS (e.g. archetype storage): more indirection than these games need; can be layered on later.
- MonoGame/FNA: heavier content pipeline; raylib is already validated on the target machine and keeps
  the backend small.
- Variable timestep: simpler, but breaks reproducible scripted playthroughs and jump consistency.
