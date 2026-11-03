# ADR 0028: One graphics device contract for every tier

## Status

Accepted (2026-11-03). Amends [ADR 0003](0003-scenes-entities-runtime.md) (the graphics model is no longer a closed 2D
immediate API), [ADR 0017](0017-sdl3-native-platform.md) (SDL's renderer becomes a device behind the contract, not the
contract) and [ADR 0024](0024-graphics-configuration.md) (the "capability-based renderer" it set apart as a separate
mission is this contract). Builds on [ADR 0026](0026-kernel-modules-and-schedule.md) and
[ADR 0037](0037-platform-host-contract.md) (`ISurface`). Evidence: ARCH-1 dossier §3 (S2-S5, D5, D7), §6.4 and §7;
annex [A2](../evaluations/arch-1/A2-render.md) (R1-R5, R12); the stage 0 rendering rules
([rendering contract](../evaluations/arch-1/rendering-contract.md)); P3
([`experiments/arch-1/p3-sdl-gpu-3d`](../../experiments/arch-1/p3-sdl-gpu-3d/README.md)) and the graphics contract spike
([`experiments/arch-1/graphics-spike`](../../experiments/arch-1/graphics-spike/README.md)), frozen by architect
directive 5 with E1-E14 and the `Textured` merge.

## Context

`IRenderer` was the only graphics contract: 2D, immediate, straight alpha, narrower than both backends under it. It had
no matrices, render targets, buffers, samplers, readback or shaders (S2); a texture was its own sampler and a CPU
RGBA8 copy (S5); the backend contract was unwritten and the backends disagreed (D5, S4); there was no seam to replace the
device (S3). The spike showed that one device contract modelled on SDL_GPU and WebGPU can be implemented without ad hoc
exceptions by a fixed-function tier-0 device (SDL_Renderer on Direct3D 11, OpenGL and Vulkan) and a programmable
tier-1 device (pure SDL_GPU on Direct3D 12 and Vulkan), both driving the same 2D batcher, with the same conformance
suite passing on all of them.

## Decision

- **One device contract, version 1, in `MicroEngine.Graphics`** (namespace `MicroEngine.Graphics.Devices`), an assembly
  that references only the kernel and the platform contracts (the architecture test checks it) and ships inside the
  `MicroEngine` package. Its text is the XML documentation of its types; `GraphicsContract.Version` is 1. It is the
  spike's frozen contract with E1-E14 applied and one change on top: the two built-in pipelines with identical rules
  (`Sprite`, `TexturedGeometry`) are one, **`BuiltInPipeline.Textured`**. Any later change of a rule or of a type's
  shape is a contract revision: the version rises and the evidence is recorded.
- **Tiers differ only in declared capabilities** (`GraphicsCapabilities`: tier, `GraphicsFeatures`, shader formats,
  formats per usage, built-in blend states, present modes, limits as guaranteed minimums, API and adapter as reported
  after creation). A request outside them throws **`GraphicsCapabilityException`** (`Operation`, `Device`, `Missing`,
  `Requirement`) when it is made, never at draw time, and nothing degrades silently.
- **Core validation in `GraphicsDeviceBase` and `CommandList`.** Every call is checked (ownership, disposal, sizes,
  ranges, formats, capabilities, recording order) before the backend's `*Core` member runs, so errors and the moment
  they are raised are identical on every device.
- **Resources are owned by their device and carry generations.** Core gives each resource a `Handle<GraphicsResource>`
  (kernel `HandlePool`): a disposed resource's handle stops resolving even when its slot is reused. Destruction is
  deferred: the backend release runs once the device has completed every submission made before the disposal
  (`CompletedSubmissions`; at once on devices that execute lists inside `Submit`). Samplers are immutable values cached
  by the device; built-in pipelines and a target's colour texture are device- and target-owned.
- **Premultiplied alpha** is the convention of every colour and texel the contract carries; the blend presets assume it.
  Straight-alpha sources are premultiplied when they are uploaded (`PremultipliedColor.PremultiplyInPlace` is the rule's
  reference). This fixes D7 at the contract level.
- **Written rules checked by a conformance suite** (`tests/Shared/Graphics/DeviceConformance.cs`): coverage by pixel
  centre with the top-left rule, built-in vertex space (target pixels relative to the viewport), clears that ignore
  viewport and scissor, absolute scissor, pass-scoped bindings, submit and recording order, fenced readback of offscreen
  targets only, the surface target's expiry at `Present`, the typed error for every undeclared feature. Exact properties
  are checked exactly; tolerances name their source of variation (8-bit quantization ±1/255, sub-texel filtering
  precision ±2/255, sub-pixel snapping at edges).
- **A CPU reference device** (`ReferenceGraphicsDevice`, backend "reference") executes the rules literally in managed
  code, needs no window and is identical on every machine. It is the oracle of the suite and the device headless runs
  draw with; it declares render targets, repeat and mirror addressing, custom blend and separate filters.
- **Deterministic counters** (`GraphicsDeviceBase.Counters`: submissions, passes, draw calls, pipeline changes, texture
  bindings, uploads, presents, resources) counted by core from the submitted lists, so tests hold scenes to exact
  budgets on every backend.
- **Not in this decision:** the tier-0 SDL and raylib devices, `Presentation` and views (ADR 0029) and the 2D batcher
  with `IRenderer` as its façade arrive in the same stage as separate changes; the tier-1 SDL_GPU device, custom shaders
  and depth are exercised in the 3D vertical slice (stage 6).

## Consequences

- A backend author implements one contract and runs one suite; a third-party device gets core's validation and the
  suite's verdict by deriving from `GraphicsDeviceBase` and subclassing `DeviceConformance`.
- The contract's types live in a sub-namespace because the old `IGraphicsDevice`, `Texture` and `TextureFilter` of
  `MicroEngine.Graphics` keep working for games until the 2D layer and the consumers migrate; the two never meet in a
  game's code.
- The surface is never read back: captures render offscreen, read the target, then draw it to the window. The
  reference device keeps its presented frame (`PresentedFrame`) for headless captures, outside the contract.
- Open risks recorded by the spike stay open and change no shape: SDL 3.5.0 is a development series; software and
  emulated rasterizers (SDL's software renderer is a diagnostic device that does not claim conformance; lavapipe and
  WARP untested for tier 1); Metal, phones and the web; mipmaps, sRGB and several colour attachments; `DeviceLost`
  through the host's event queue; no ETC2 in SDL_GPU; the tier-1 surface copy at `Present` (E12).

## Alternatives

- **Tier 1 through SDL_Renderer's `gpu` driver.** Rejected (P3): draw ordering deferred until present or readback, and
  custom render-state shaders dropped silently on Direct3D 12; P1 measured its start-up and memory cost.
- **Separate 2D and 3D device interfaces.** Rejected: they fork the resource model, and 2D composed over 3D would cross
  two APIs.
- **Queryable per-mechanism extensions.** Rejected: they fragment the contract into untested combinations.
- **Keep `IRenderer` as the only contract.** Rejected: it is the problem (S2).
- **One renderer per graphics API, WebGPU everywhere now, or a render graph now.** Rejected: industry reversals; web
  coverage and a Rust/C++ dependency overlapping SDL's windowing; no consumer for a graph yet.
- **Keep `Sprite` and `TexturedGeometry`.** Rejected (directive 5): identical rules, two names for one thing.
