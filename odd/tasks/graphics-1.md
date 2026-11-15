# GRAPHICS-1: extensible graphics and reusable rendering infrastructure

Objective: establish, by building them as a consumer, which rendering techniques of their own a developer can author
with the SDK today, close the gaps the attempts show with the smallest generic extension, and compare the reusable
options outside the engine. Issue NastMz/MicroEngine#109. Branch `feat/graphics-1`. Test runner:
`dotnet test MicroEngine.slnx -c Release`. Evaluation: [`docs/evaluations/graphics-1.md`](../../docs/evaluations/graphics-1.md).
Decision: [ADR 0057](../../docs/decisions/0057-materials-on-the-2d-batcher.md). Delivery: local commits only.

## Specs

- S1 Opportunity: "MicroEngine's rebuilt graphics foundation exposes GPU devices, shaders, pipelines and render
  targets, but developers still need an accessible, flexible way to author **their own** 2D rendering techniques (and
  eventually share foundations with 3D). This is an investigation, **not** approval to implement a new renderer or
  ship pre-made visual effects."
- S2 Capability map: "Map current SDK capabilities and barriers to developer-authored shaders, materials, parameters,
  multiple passes, composition, masks, and lighting/occlusion techniques."
- S3 Options: "Evaluate mature reusable options in C#, C, C++, and other ecosystems, including tools built on
  SDL3/SDL_GPU, shader compilation/reflection and offline tooling."
- S4 Comparison: "Compare compatibility with graphics contracts, NativeAOT, WebAssembly, platform packaging, licenses,
  performance, integration cost, and maintenance."
- S5 Examples: "Validate gaps through small consumer-side examples (e.g. custom water distortion, occluded torch
  lighting, post-processing) **without** making these built-in effects."
- S6 Done when: "Provide a grounded capability map and recommendation: use existing APIs, adapt an external
  implementation, make a minimal SDK extension, or defer. Implementation needs a separate decision."
- S7 Judge by an external developer (owner's standing rule for research issues, as TILE-1 S6 and GRAPHICS-2 S7; given
  with this task): the research is the starting point; the bar is whether an external developer could build that with
  the SDK today. What works becomes a documented example; what fails identifies a gap, closed with the smallest generic
  SDK extension, with tests and an example. No built-in effects, no new renderer, no redesign, no breaking change, no
  new native or NuGet dependency: any of those is described as a decision for the owner instead of implemented.

## Tasks

| Task | Specs | Route | Status | Commit / evidence |
| --- | --- | --- | --- | --- |
| T1 Map the device contract, the 2D batcher, the tier-1 device, the shader tool and what GRAPHICS-2 left | S2 | inline | done | evaluation §2 |
| T2 The four techniques as scenes of `samples/Examples` on the API as it was; captures on each backend | S5 | inline | done | `7d85257`; captures looked at (L3) |
| T3 `Material2D`, `RenderState.Material`, `BatchSettings.Material`; tests on `sdl-gpu` | S6 S7 | inline | done | `a1de913`; RED: 7 of 11 `Material2DTests` failed with the API present and the batcher not using it; then 11/11 on the default driver and on Vulkan, 2/2 `MaterialInGameTests` |
| T4 The shader examples on materials; the four examples checked in a window on both SDL devices | S5 S7 | inline | done | `61ad9d3`; `RenderingExamplesTests` 2/2 |
| T5 The mask without a custom blend state, after it fell back on SDL's software renderer | S5 | inline | done | `83ac1d5`; captures on five backends (L4) |
| T6 Guide page, release note, API reference, architecture and status lines, site registration | S7 | inline | done | `bff1019`; `snippets.mjs` and `api-reference.mjs --check` clean |
| T7 External options with sources | S3 S4 | delegated (web research, read-only) | done | evaluation §6 |
| T8 Evaluation, ADR 0057, this record, `AGENTS.md` | S2 S6 | inline | done | this commit |

## Log

- L1 (2026-11-14) Issue #109, verbatim:

  > ## Opportunity
  > MicroEngine's rebuilt graphics foundation exposes GPU devices, shaders, pipelines and render targets, but developers still need an accessible, flexible way to author **their own** 2D rendering techniques (and eventually share foundations with 3D). This is an investigation, **not** approval to implement a new renderer or ship pre-made visual effects.
  >
  > ## Investigate
  > - Map current SDK capabilities and barriers to developer-authored shaders, materials, parameters, multiple passes, composition, masks, and lighting/occlusion techniques.
  > - Evaluate mature reusable options in C#, C, C++, and other ecosystems, including tools built on SDL3/SDL_GPU, shader compilation/reflection and offline tooling.
  > - Compare compatibility with graphics contracts, NativeAOT, WebAssembly, platform packaging, licenses, performance, integration cost, and maintenance.
  > - Validate gaps through small consumer-side examples (e.g. custom water distortion, occluded torch lighting, post-processing) **without** making these built-in effects.
  >
  > ## Done when
  > Provide a grounded capability map and recommendation: use existing APIs, adapt an external implementation, make a minimal SDK extension, or defer. Implementation needs a separate decision.
  >
  > **Status:** Research candidate · **Timing:** after the consolidated release.
- L2 (2026-11-14) Mapping. The device contract already has render targets, blend states, custom shaders with uniform
  slots and several textures (tier 1), and a scene reaches the device and the canvas (`DeviceGraphics.Device`,
  `DeviceRenderer.Flush`/`Target`). The batcher draws with the built-in pipeline only (GRAPHICS-2 §2, §4 item 4). The
  headless platform has no device; browsers have SDL_Renderer only. The examples go in `samples/Examples` (not a new
  project: `MicroEngine.slnx` is outside this change's surface), with a plain path where a device lacks a feature.
- L3 (2026-11-14) First attempt, existing API only. Light and mask: worked on `sdl-gpu`, the default SDL device and
  raylib. Water and the chain: worked on `sdl-gpu` through a 62-line pass written against the device
  (`RawShaderPass`): shaders, a pipeline per blend and format, buffers for one quad, the projection, uniforms,
  textures. The four captures were looked at on `sdl-gpu`; light and mask also on the default device and raylib.
  Gap: a shader cannot be applied to what the batcher draws.
- L4 (2026-11-14) Decision: a material on the batcher's state (two `init` properties, no change to the device
  contract), parameters captured per draw, pipelines per blend and format inside the material. Tests on the real
  device, because the reference device has no shaders. Mask: on `--platform sdl-software` the example drew its
  fallback (`CustomBlend` not declared); rewritten with multiply and additive only, then looked at on `sdl-gpu`, the
  default device, the software renderer, raylib and, from the WebAssembly package served locally, in a browser pane
  (light and mask show, water says it needs `sdl-gpu`).
- L5 (2026-11-14) Not done, with reasons, in evaluation §5 and §7: named parameters (a compiled-shader format change),
  shaders in browsers (a second backend), Linux shader compilation (a dependency or download), a pass at the window's
  resolution and a target as a drawing-API texture (`src/MicroEngine`, outside the surface), finer blend capability
  (`src/MicroEngine.Sdl`, outside the surface). `docs/manual/rendering.md` still says no shaders are exposed through
  `IRenderer`, which stays true; a link from it to the new guide is outside the surface. Not run: Metal, Linux,
  phones, frame times, the site build (`site/node_modules` is absent here).
- L6 (2026-11-14) Full `dotnet test MicroEngine.slnx -c Release`: every project passed except
  `MicroEngine.Desktop.Tests` (690 passed, 2 failed, 16 skipped of 708; no host abort). One failure was this change's
  `RenderingExamplesTests` on `sdl-gpu`: its capture showed the list of examples, not the example (it opened the
  examples through the list with scripted presses, and passed three times alone). Rewritten to run each example as the
  only scene of a game and read the canvas back, with no input. The other, `Losing_the_focus_releases_held_keys`
  ("time went back at AxisMoved"), is an existing keyboard test this change does not touch; it passed three times
  alone.
