# ADR 0026: A kernel with modules, a typed service registry and a phased frame schedule; `Game` is a module façade

## Status

Accepted (2026-11-02). Amends [ADR 0003](0003-scenes-entities-runtime.md) (the fixed runner and its fixed sequence of
services) and [ADR 0016](0016-platform-profiles.md) ("no dependency-injection container: a catalog and a request cover
every case found"). Evidence: [ARCH-1](../evaluations/arch-1.md) §3 (S1, S9, S14, S17), §6.1, §6.3, §6.4, and annex
[A1](../evaluations/arch-1/A1-core.md) F2, F6, F8, F12, F13 and §4.2. Implemented in two steps: the kernel assembly,
its types, tests and packaging first; then the loop running the schedule and the `Game` façade, with ADR 0027
(loop-owned time).

## Context

`Game` created and owned every service in a fixed order and ran a fixed step (scenes, audio, toasts, achievements,
input edges). A physics world, an animation system, a debug overlay or a 3D renderer had no place in the frame except
inside a scene's `Update`/`Draw`, no subsystem could be replaced, and adding one changed the base class. There was no
frame phase at the display's rate, no engine events beyond suspension, and diagnostics went to `Console.Error`. ADR
0016 rejected a container because nothing then needed more than a catalog and a request; the target architecture
(physics, input layers, a graphics device, content handles as modules) does.

## Decision

- **A kernel assembly, `MicroEngine.Kernel`, that knows no domain.** It holds `EngineBuilder`, `Engine`,
  `IEngineModule`, `ServiceRegistry`, `CapabilityRegistry`, `FrameSchedule` and `FramePhase`, `FrameContext`, the
  time policy and clock (ADR 0027), `IEngineLifecycle`, `ILog`, `Handle<T>`/`HandlePool<T>`, and the backend-free math
  and colour types (`Color`, the new linear `ColorF` with premultiply helpers, `RectF`, `MathX`, `Ease`). It references
  no other engine assembly; `MicroEngine` references it.
- **Modules compose the engine.** An `IEngineModule` registers services, frame systems, capabilities, settings and
  lifecycle handlers from `Configure(EngineBuilder)`, once, when it is added. A module type is added at most once.
- **A typed registry, not a container.** `ServiceRegistry` stores one instance per exact type (`Add`, `Replace`,
  `Get`, `TryGet`, `Remove`); nothing is created on demand, resolved through interfaces it also implements or found by
  reflection, so composition works under trimming, NativeAOT and WebAssembly. A missing service fails with its name.
- **Settings are immutable records edited at composition.** `EngineBuilder.Configure<T>(Func<T, T>)` receives the
  current value (or the record's defaults) and returns the new one; the engine reads settings once, when it is built.
  The dossier's sketch had `Action<T>`; immutable records cannot change under a running loop, so the edit returns a
  new value instead.
- **A phased schedule.** Frames run twelve phases: `BeginFrame`, `Input`, then per fixed step `PreFixed`,
  `FixedUpdate`, `Physics`, `PostFixed`, then `Update`, `LateUpdate`, `Extract`, `Render`, `Present`, `EndFrame`.
  Systems have schedule-wide unique names; within a phase they run in insertion order, and `after`/`before` name
  another system of the same phase. The order is resolved depth first in insertion order, so a constraint moves only
  the systems it names. A constraint naming a missing system or one in another phase, and a cycle (reported as
  `a -> b -> c -> a`), fail when the engine is built. A built schedule is frozen; running it allocates nothing. Built-in
  systems are replaced by name (`Replace`).
- **Capabilities are queried, never cast.** Optional features are declared in a `CapabilityRegistry` and found with
  `Engine.TryGetCapability<T>`. The platform split (stage 1B) moves the platform's optional interfaces there.
- **Engine events and a log.** `IEngineLifecycle` raises resized, focus, suspending, resumed and quit requested (with
  veto) on the game's thread between frames; the loop raises suspension and quit today, the platform split adds the
  others. `ILog` writes `category: message`; the default `ConsoleLog` writes to the current standard error, which is
  today's output, and a host routes diagnostics by registering another sink.
- **`Game` is a module façade.** `Game` implements `IEngineModule`: its built-in services are registered in the
  registry and resolved from it, its fixed sequence became named systems in the schedule (same order as before), and
  `Game.Configure(EngineBuilder)` is the extension point. Games keep their one-line `Program.cs` and their API.
- **One package set.** The kernel is not a package: `MicroEngine.Kernel.dll`, its documentation and its symbols ship
  inside the `MicroEngine` package. The project reference is private (no package dependency) and `MicroEngine` hands
  the kernel's assembly to projects that reference it, so the repository's projects compile against it as before.
- **Source compatibility for the moved types.** `Color`, `RectF`, `MathX` and `Ease` keep their namespace
  (`MicroEngine`); `MicroEngine.dll` forwards them (`TypeForwardedTo`), so code compiled against an earlier version still
  finds them. The kernel's own new types live in `MicroEngine.Kernel`.
- **An architecture test.** `tests/MicroEngine.Tests/Kernel/ArchitectureTests.cs` holds the allowed references of every
  engine assembly of the target structure (and today's transitional ones) and fails when a project under `src/` is not
  in the table or references outside its row; it also checks the compiled kernel references no engine assembly.

## Alternatives considered

- **Keep `Game` and add `Components`/`Services` as MonoGame did.** Rejected: the service set would remain the base
  class's public API and there would still be no phases.
- **Microsoft.Extensions.DependencyInjection.** Rejected: reflection-based activation and lifetimes, trimming and AOT
  friction on WebAssembly and iOS, and far more than one instance per type needs.
- **A full ECS scheduler as the kernel.** Rejected: it would impose a data model on every module (Unity's dual-model
  cost); systems here are named callbacks.
- **A separate `MicroEngine.Kernel` package.** Rejected for now: a package is published only when a consumer needs to
  leave one out (one version, few packages).

## Consequences

- New subsystems (physics, input layers, a graphics device) are modules with their own phases and services; replacing
  a built-in service or system is a registry or schedule call, not a fork of `Game`.
- The two namespace cycles caused by `Game` knowing every service can be broken as modules leave `MicroEngine`.
- Platform capabilities, host-raised events and log routing are wired by the platform split and hosting units; until
  then `Resized` and `FocusChanged` are declared but not raised by the shipped backends.
