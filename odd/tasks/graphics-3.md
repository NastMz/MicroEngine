# GRAPHICS-3: post-processing, present stage, mipmaps and draw order

Objective: what a developer still assembles by hand on top of RENDER-1 (ADR 0064): a chain of full-screen effects on a
scene, the canvas or the window; mip levels for textures drawn smaller than their size; an order of drawing that does
not depend on call order. Branch `feat/graphics-3`, issue #181, one pull request. Evaluation:
[`docs/evaluations/graphics-3.md`](../../docs/evaluations/graphics-3.md); decision: ADR 0071.
Test runner: `dotnet test MicroEngine.slnx -c Release`; visual checks are scripted captures looked at.

## Specs

- S1 (issue, acceptance) "A game enables a full-screen effect in a few lines and it covers the UI when asked."
- S2 (issue, acceptance) "Behaviour on tier-0 devices and browsers is declared and tested."
- S3 (issue, acceptance) "Manual, guide, ADR 0057 amended."
- S4 (issue, target) "a post-process chain owned by the scene or camera, a present stage that can take a material at
  window resolution, mipmaps on devices that can and through `Assets.Texture`, draw depth and sorting on the normal
  path". "A small set of ready effects (colour grade, blur, CRT, vignette) is evaluated as shipped content rather than
  examples."
- S5 (issue, target) "Stencil, multiple render targets and MSAA are re-evaluated with the same method, not assumed
  either way."
- S6 (issue) "the shader tool on Linux and macOS where the toolchain allows" and the section "Cross-platform shader
  compilation ownership". Assumption (the writer's): this part needs hosts this machine does not have and edits
  outside the writer's surface (`src/MicroEngine.Cli/Shaders.cs`, `.github/`), so it is delimited in the evaluation
  and left to its own unit.
- S7 (brief) "A capability a backend cannot do must be declared and fail with a typed error or degrade in a stated
  way." "state what post-processing does in the browser today, honestly."
- S8 (brief) "for every effect and for mipmaps and sorting, run a scripted capture of an example and LOOK at the PNG".
- S9 (brief) "CONTENT-1 (ADR 0066) left "compressed textures and mipmaps" to this initiative: mipmaps are yours;
  compressed textures only if the method justifies them, otherwise delimit them with reasons."

## Tasks

| ID | Task | Specs | Route | Status |
| --- | --- | --- | --- | --- |
| T1 | Draw order: `SortedRenderer` (layer, depth, call order) over any renderer | S4 | inline | [x] c03481bc; `SortedRendererTests` 5/5 (RED seen with the sort removed: 3 failed) |
| T2 | Post-processing chain and ready effects; `Scene.PostProcess`, `Game.PostProcess` | S1, S2, S4, S7 | inline | [x] 6ac98318; `PostProcessTests` 27/27; three conformance rules on 10 renderers, 30/30 |
| T3 | Window stage: `Game.ScreenPostProcess` at the window's resolution, over the UI | S1, S4 | inline | [x] 7791b7f6; `ScreenPostProcessTests` 6/6 |
| T4 | Mip levels: `TextureFilter.LinearMipmap`, device feature on `sdl-gpu` and raylib | S4, S7, S9 | inline | [x] 7a070665; device conformance on 12 devices, renderer rule on 10, `MipLevelTests` 2/2 |
| T5 | Evaluation, ADR 0071, ADR 0057 amended, manual, guide, release notes, limitations, API reference | S3, S5, S6 | inline | [x] the commit after T4 |

Not done, each delimited in the evaluation: the shader tool on Linux and macOS (S6), compressed textures (S9),
stencil, several colour targets and multisampling (S5: re-evaluated, kept out with reasons).

## Log

- L1 (2026-11-20) Issue #181, verbatim. **Problem**: "Post-processing is a hand-written chain in an example, there is
  no hook at window resolution (effects run before prescale and never include the UI), no device declares mipmaps
  (minified HD art shimmers), the draw API has no depth or sort although the batcher does, and the shader compiler
  runs only on Windows. ADR 0057 (GRAPHICS-1 #109) recorded "no post-processing stack" because "no scenario asked for
  one"." **Target**: "On top of RENDER-1: a post-process chain owned by the scene or camera, a present stage that can
  take a material at window resolution, mipmaps on devices that can and through `Assets.Texture`, draw depth and
  sorting on the normal path, and the shader tool on Linux and macOS where the toolchain allows. A small set of ready
  effects (colour grade, blur, CRT, vignette) is evaluated as shipped content rather than examples. Stencil, multiple
  render targets and MSAA are re-evaluated with the same method, not assumed either way." **Cross-platform shader
  compilation ownership**: "The current `microengine shaders` implementation explicitly gates on Windows and locates
  Windows `dxc.exe` from `Microsoft.Direct3D.DXC`. Investigate **maintained DXC releases/builds and alternatives
  (including SDL_shadercross)** on Windows, Linux and macOS, with redistribution/licensing, native architecture, CI
  and per-host reproducibility evidence; remove the host-OS gate where validated. Ensure one host can produce all
  required native output formats (DXIL, SPIR-V and MSL) without requiring the developer to own every target machine.
  Include compilation error parity, `--check` and regression tests per supported host." "**Distinct
  responsibilities:** GRAPHICS-3 owns cross-platform native shader compilation; CONTENT-1 #113 owns
  automatic/incremental content-build integration; SHADER-GRAPH-1 #190 owns the recommended C# Fluent authoring
  experience; WEBGPU-1 #192 owns browser programmable rendering and WGSL integration. Preserve raw HLSL + the existing
  CLI as an advanced path." **Acceptance**: "A game enables a full-screen effect in a few lines and it covers the UI
  when asked." "Behaviour on tier-0 devices and browsers is declared and tested." "Manual, guide, ADR 0057 amended."
  "Depends on RENDER-1."
- L2 (2026-11-20) The audit's four claims checked against the code and found true (evaluation, section 0). Found
  besides: the default desktop device and browser builds run no shaders, so every ready effect got a form made of
  render targets and blend presets, or says it is left out (`PostEffect.SupportOn`).
- L3 (2026-11-20) T2, found by looking at the conformance capture: a vignette and scanlines blended over a scene's
  transparent picture darkened the scene under it. Both now use the multiply state, which keeps the picture's alpha;
  the conformance rule draws a transparent band to pin it.
- L4 (2026-11-20) T2, found by the reference-device tests: releasing a chain inside a frame fails on a batched
  renderer (the batch still names its textures). It is the rule for any texture; the engine releases its chains at
  scene exit and game shutdown, and the documentation says "between frames".
- L5 (2026-11-20) T4: SDL's software renderer shrinks with an averaging scaler, so a one-level texture is already
  smooth there; the device conformance suite names it (`MinificationAverages`) instead of loosening the rule.
- L6 (2026-11-20) Looked at (scripted captures, Windows): draw order sorted against call order (`sdl-gpu`, SDL
  default: identical files); the post-processing example off, on and with the game's own effect (`sdl-gpu`, SDL
  default); the conformance captures of the effects on `sdl-gpu`, SDL default, raylib and headless; the window
  stage on and off (`sdl-gpu`: curved tube over the heading; SDL default: flat, lined); mip levels (`sdl-gpu`,
  raylib: smooth; SDL default: aliased and labelled); the post-processing and window-stage examples on raylib (the
  post-processing captures are byte-identical to SDL default's). Not run: browsers, Linux, macOS, phones.
- L7 (2026-11-20) Outside the writer's listed paths but inside their intent, changed: `src/MicroEngine.Sdl/SdlPlatform.cs`
  and `src/MicroEngine.Raylib/RaylibPlatform.cs` (the window stage: about fifteen lines each),
  `src/MicroEngine.Raylib/Graphics/RaylibDevice.cs` and `src/MicroEngine.Graphics.SdlGpu/SdlGpuDevice.cs` (mip
  levels), `src/MicroEngine.Graphics2D/**` (where the drawing API lives), `src/MicroEngine/Core/GameViews.cs` and
  `HeadlessPlatform.cs`, `tests/Shared/Graphics/DeviceConformance.cs`, `docs/guide/render-targets-and-shaders.md`,
  `docs/manual/assets.md` (one limitation line). The four demo games were not touched.
