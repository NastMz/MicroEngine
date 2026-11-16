# GRAPHICS-1: developer-authored rendering techniques

Issue #109. Task record: [`odd/tasks/graphics-1.md`](../../odd/tasks/graphics-1.md). Decision:
[ADR 0057](../decisions/0057-materials-on-the-2d-batcher.md). Run on 2026-11-14 on Windows 11, on `main` after
GRAPHICS-2 ([its evaluation](graphics-2.md), whose §4 item 4 left "a custom shader on the 2D batcher or on the present
pass" to this issue).

**Result.** An external developer can build a light occluded by walls, a mask, a water-distortion shader with
parameters and a multi-pass post-processing chain with the SDK. The first two need nothing new and run on every
graphics device, browsers included. The two shader techniques were possible before this change only by writing a
render pass by hand against the device for each quad; one additive extension, a material on the 2D batcher
(`Material2D`), removes that, and makes shaders usable for sprites, geometry and the drawing API. Nothing was adopted
from outside, no built-in effect was added, and the device contract is unchanged. What stays out is listed in §5 with
its reason; three items need the owner (§7).

Every statement below is labelled where it matters: **demonstrated** (run here, with a test or a capture that was
looked at), **hypothesis** (reasoned from the code, not run) or **player-perceptible** (what someone looking at the
screen sees).

## 1. Method

The four scenarios the issue names were written as scenes of `samples/Examples`, first against the public API as it
was (commit `7d85257`), then looked at on each device. What failed or cost too much became a gap (§4); the smallest
generic extension was written test-first (commit `a1de913`) and the examples rewritten on it (`61ad9d3`, `83ac1d5`).

| Run | What |
| --- | --- |
| SDL, `sdl-gpu` device (the driver SDL_GPU chooses here, and Vulkan by name in the tests) | all four examples, scripted captures; `Material2DTests` on both drivers |
| SDL, default device (`sdl-renderer`) | light and mask show; water and post-processing draw their plain picture |
| SDL software renderer (`--platform sdl-software`) | light and mask show |
| raylib | light and mask show |
| Browser (WebAssembly package of the examples, served locally and opened in a browser pane) | light and mask show; water says it needs `sdl-gpu` |
| Headless | every example opens and returns (`ExampleTests`), drawing its plain path: there is no graphics device |

Captures looked at: [light](graphics-1/light.png), [mask](graphics-1/mask.png), [water](graphics-1/water.png),
[post-processing](graphics-1/post.png) (the `sdl-gpu` run of the examples' tour, 3× canvas pixels). Not run: Metal,
Linux, Android, iOS; frame times were not measured.

## 2. Capability map

What a game can author today, and how it reaches it. "Tier 0" is every device (SDL renderer, SDL software, raylib,
browsers); "tier 1" is `sdl-gpu`.

| Capability | Tier 0 | Tier 1 | How a game reaches it | Evidence |
| --- | --- | --- | --- | --- |
| The device from a scene | yes | yes | `Game.Graphics as DeviceGraphics` → `Device`; the scene's renderer `as DeviceRenderer` → `Flush()`, `Target` (ADR 0046) | demonstrated: all four examples |
| Render targets, drawn into and sampled | yes | yes | `CreateRenderTarget`, a `Renderer2D` of the game's own with `BatchSettings(target)` | demonstrated on five backends |
| The drawing API (`IRenderer`: sprites, shapes, fonts) into a target | yes | yes | `new DeviceRenderer(batcher, graphics, target)` | demonstrated: the mask example draws text and circles into targets |
| Composition with blend states | the four presets | any `BlendState` | `BatchSettings.Blend`, `RenderState.Blend` | demonstrated: multiply and additive on all five |
| A blend state outside the presets | only where `CustomBlend` is declared | yes | check `Capabilities.Has(CustomBlend)` | demonstrated: declared by SDL's default renderer and raylib here, **not** by SDL's software renderer (the first mask fell back there) |
| Multiple passes | yes | yes | one batch per pass, each reading the target before it | demonstrated: light (6 batches a frame for 2 lights), post-processing (4) |
| Masks | yes, by multiply-and-add (3 targets) | also one custom blend state | see the guide | demonstrated on all five, software and browser included |
| Lighting with occlusion | yes, shadow quads from wall edges | yes | CPU geometry through `DrawGeometry`, light map multiplied over the canvas | demonstrated on all five |
| Custom shaders | no (`GraphicsCapabilityException`) | yes, HLSL compiled ahead of time | `microengine shaders`, `CompiledShader.Parse(...).Create(device)` | demonstrated (ADR 0042 and the conformance suite already showed it at device level) |
| Shader parameters | — | yes, one block per uniform slot, matched by layout | `CommandList.SetUniforms<T>`; now `Material2D.SetUniforms<T>` | demonstrated: water (9 parameters), blur, look |
| Several textures in a shader | — | yes | `SetTexture(slot, ...)`; now `Material2D.SetTexture` | demonstrated: the last post-processing pass reads two |
| A shader on sprites, geometry and text | — | **was not possible without rewriting the batcher**; now `RenderState.Material` | `Material2D` | demonstrated: `Material2DTests`, `MaterialInGameTests` |
| A pass over one scene | — | yes | at the end of `Scene.Draw` | demonstrated: both shader examples |
| A pass over the whole frame (all scenes and transitions) | — | yes | `Game.DrawAboveScenes` | demonstrated: `MaterialInGameTests` |
| A pass at the window's resolution, or on the composition of the canvas into the window | — | **no portable hook** | the UI view's renderer is reachable only through the platform object (`SdlPlatform.Canvas.UiRenderer`); the compose step has no shader stage | read in `TierOneGameTests` and `DeviceCanvas.Compose`, not run |
| Stencil, several colour targets, multisampling, mip generation, compute | — | no | not in the device contract | read in `Descriptions.cs`, `Capabilities.cs` |
| A render target as a texture of the drawing API (`Sprite`, `IRenderer.Draw`) | no | no | `DeviceTexture` has no public constructor; a target is drawn with `Renderer2D.Draw` instead | demonstrated: every example draws targets through a batcher |
| Any of this on the headless platform | no | no | `HeadlessPlatform` draws with its own rasterizer, not a device | demonstrated: the examples' fallback in `ExampleTests` |

## 3. The four attempts

**Light occluded by walls** (`OccludedLightExample`). Worked at the first attempt with the existing API, on every
device. Each light is a glow drawn additively into its own target, the shadow quads of the walls' far edges drawn
black over it, the result added into a light map that starts at the ambient colour and multiplies the canvas.
*Player-perceptible:* hard-edged shadows that move with the torch; no penumbra. *Barrier found:* none. *Not built:*
soft shadows (a blur pass of the light target is a tier-1 extra a game can add with a material), normal-mapped light.

**Mask** (`MaskExample`). The first version used one custom blend state (destination × source alpha) and worked on the
SDL default renderer, `sdl-gpu` and raylib. **Demonstrated failure:** on SDL's software renderer the device does not
declare `CustomBlend` and the example drew its fallback. The second version uses only presets (picture × mask, scene ×
inverse mask, add) and shows the same picture on all five backends. *Barrier found:* `CustomBlend` is all-or-nothing
(the SDL device declares it only if every factor and operation composes), so a game cannot ask for one blend state.
*Hypothesis:* browsers' WebGL renderer also lacks it (min/max operations); not checked, because the preset version
made it irrelevant for masks.

**Water distortion** and **post-processing chain** (`WaterExample`, `PostProcessExample`). Both worked at the first
attempt on `sdl-gpu` with the existing API, but only through a helper the sample had to write against the device
(`RawShaderPass`, 62 lines in `7d85257`): two shaders, a pipeline fixed to one blend state and one target format, a
vertex and an index buffer for one quad, the pixel-to-clip matrix, the uniform block and the texture bindings, in a
command list of its own. **Demonstrated barriers:**

1. Every shader draw is a hand-written pass. The batcher (`Renderer2D`) drew with the built-in pipeline only, so a
   shader could not be applied to what the batcher draws: no sprites, no geometry, no transform stack or camera, no
   sorting, no text. Water as a quad is fine; water as a tile layer, a dissolving sprite or outlined text would mean
   rewriting the batcher in the game.
2. A pipeline is bound to its blend state and target format, so the helper needed one pipeline per combination.
3. The parameter block is bytes matched by layout. A mismatch gives wrong values, not an error. (Not closed; §5.)
4. A pass cannot sample its own target, so changing the canvas with itself needs a copy first. This is a rule of the
   contract, the same in every GPU API; the examples copy.

After the extension both examples lost the helper: the water is 2 batches, the chain 4, each a `Begin`/`Draw`/`End`
of the batcher.

## 4. What was implemented

`Material2D` (`src/MicroEngine.Graphics2D/Material2D.cs`), `RenderState.Material` and `BatchSettings.Material`:

- A material is a vertex shader and a fragment shader the game created, plus the fragment stage's uniform data
  (`SetUniforms<T>(in T, slot)`) and its textures from slot 1 on (`SetTexture`). The batcher gives the vertex stage
  the pixel-to-clip matrix in uniform slot 0 and the fragment stage the draw's texture in slot 0.
- It is part of the batcher's state, so everything else applies unchanged: blend state, sampler, scissor, transforms,
  view, viewport, sort. Draws with and without a material mix in one batch in recording order.
- A draw keeps the parameters the material had when the draw was recorded; draws with the same texture, state and
  parameters share a draw call.
- Refusals are at the call: an incomplete material at the draw, an undeclared slot when it is set, a material of
  another device, a disposed one. A device without shaders refuses earlier, when the shaders are created.
- Additive: both new members are `init` properties (the records' constructors and deconstructors are as they were);
  with no material the batcher takes the path it took (`MicroEngine.Graphics.Tests`, 100 tests on the reference device, pass).
- No reflection or dynamic code; `MicroEngine.Graphics2D` builds with `IsAotCompatible` and no warning.

Tests: `Material2DTests` (11 tests × 2 drivers on `sdl-gpu`: 7 of 11 failed before the batcher used the material),
`MaterialInGameTests` (a scene's sprite through the drawing API with a material; a whole-frame pass from
`DrawAboveScenes`), `RenderingExamplesTests` (the four examples in an SDL window on both devices, by their captures).

Documentation: the guide `docs/guide/render-targets-and-shaders.md` (all code quoted from the examples), the release
note, the API reference.

## 5. Gaps and recommendation

| Gap | Evidence | Recommendation |
| --- | --- | --- |
| Shaders on what the batcher draws | §3, barriers 1–2 | **Minimal SDK extension: done** (`Material2D`) |
| Light with occlusion, masks, multi-pass composition | §3 | **Use existing APIs**; documented as examples |
| Shaders in browsers | SDL for the web is built without SDL_GPU (`SdlPlatform.BrowserProfile`); SDL has no WebGPU or WebGL backend for SDL_GPU (§6) | **Defer.** It needs a second programmable device (WebGL 2 or WebGPU) and a second shader target (GLSL ES or WGSL): a new backend, not an extension. Revisit when SDL ships a web backend for SDL_GPU. Games keep a plain path, as the examples do |
| Shaders on the default desktop device | `sdl-renderer` is tier 0 by design (ADR 0028, 0046) | **Use existing APIs**: the game asks for `sdl-gpu`. Whether `sdl-gpu` becomes the default is the owner's (§7) |
| Parameters matched by layout, not by name | §3, barrier 3 | **Defer.** Reflection of block members needs `CompiledShader` to carry them (a format version) and SPIRV-Cross member reflection in the tool. It removes a class of silent errors but no technique is blocked; cost: a file-format change and tool work. Slang or sokol-shdc generate such layouts (§6) but would replace the toolchain |
| Compiling shaders outside Windows | ADR 0042 | **Adapt external, owner's decision** (§7): DXC has official Linux binaries; no macOS ones were found |
| A pass at the window's resolution (CRT, scanlines on real pixels) and a shader on the compose step | §2 | **Owner's decision** (§7): it is in `src/MicroEngine` and `src/MicroEngine.Sdl`, outside this change, and touches `Presentation` |
| A render target as a `Texture` of the drawing API | §2 | **Minimal extension, not done**: a public way to wrap a target's colour as a `DeviceTexture` is in `src/MicroEngine` (outside this change's surface). The batcher covers it meanwhile |
| One blend state where `CustomBlend` is not declared | §3, mask | **Defer**: the preset technique covers masks everywhere; a finer capability is in the SDL device (outside this surface) |
| Stencil, several colour targets, multisampling, mipmaps | §2 | **Defer**: each is a contract extension on every backend (`PassDescription` names several colour attachments as "a later, additive extension"); no scenario here needed one |
| These techniques on the headless platform | §2 | **Defer**: a device-backed headless platform is a platform change. The reference device has no shaders, so shader techniques would still need a window |
| Soft shadows, normal maps, 1D shadow maps, SDF lights | not attempted | **Use existing APIs** (tier 1, with materials); references in §6 |

## 6. Reusable options outside the engine

Researched on 2026-11-14 from the projects' own pages; "unverified" marks what those pages did not settle. Nothing
here was adopted.

**SDL_GPU and SDL.** No WebGPU or WebGL backend exists upstream: the tracking issue is open
(<https://github.com/libsdl-org/SDL/issues/10768>), a community pull request was closed unmerged
(<https://github.com/libsdl-org/SDL/pull/12046>), and ARCH-1's research records a later draft
([R1 §1.2](arch-1/R1-render-research.md)). SDL 3.4 added `SDL_CreateGPURenderState` (a custom fragment shader for
SDL_Renderer's GPU driver, <https://github.com/libsdl-org/SDL/releases/tag/release-3.4.0>): fragment stage only, no
blend or pipeline control (<https://wiki.libsdl.org/SDL3/SDL_GPURenderStateCreateInfo>), and only with the `gpu`
render driver, so it adds nothing over the engine's own tier-1 device and nothing for browsers.

**Shader toolchains.**

| Option | What it gives | Licence | Packaging, platforms | NativeAOT / WebAssembly | Integration cost | Verdict |
| --- | --- | --- | --- | --- | --- | --- |
| DXC + SPIRV-Cross, as today (ADR 0042) | HLSL → DXIL, SPIR-V, MSL; resource counts | NCSA; Apache 2.0 | DXC by NuGet, Windows binaries only | build time only: no effect on a game | none | keep |
| DXC for Linux | the same compiler | NCSA | official release archives with `dxc` and `libdxcompiler.so` (<https://github.com/microsoft/DirectXShaderCompiler/releases>); no macOS archive found | build time only | a download step in the tool, or a NuGet package (`Vortice.Dxc.Native`, MIT, runtime ids unverified, <https://www.nuget.org/packages/Vortice.Dxc.Native>) | candidate (§7) |
| SDL_shadercross | HLSL or SPIR-V → DXIL, SPIR-V, MSL; reflection of counts and of inputs with names (<https://github.com/libsdl-org/SDL_shadercross>) | zlib | no tagged release, no package; build it yourself; depends on DXC and SPIRV-Cross | a native library if used at run time | high for no gain: the same two components the tool already runs | no (same conclusion as ADR 0042) |
| Slang | its own language and HLSL → SPIR-V, DXIL, MSL (experimental), WGSL (experimental); full reflection with offsets (<https://github.com/shader-slang/slang>, <https://shader-slang.org/slang/user-guide/reflection.html>) | Apache 2.0 with LLVM exception | prebuilt for Windows, Linux, macOS; C# only through community bindings (unverified) | build time only | replaces the compiler and the reflection; DXIL may still need DXC (unverified) | watch: the only option that would give named parameters and WGSL from one source |
| sokol-shdc | annotated GLSL → GLSL, HLSL, MSL, WGSL, SPIR-V with generated layouts (<https://github.com/floooh/sokol-tools/blob/master/docs/sokol-shdc.md>) | zlib | prebuilt for three systems; no C# output | build time only | GLSL as the source language; no DXIL | no |
| naga | WGSL, SPIR-V or GLSL → SPIR-V, MSL, HLSL, GLSL, WGSL (<https://github.com/gfx-rs/wgpu/tree/trunk/naga>) | MIT / Apache 2.0 | a Rust CLI | build time only | no HLSL input, no DXIL | only as a SPIR-V → WGSL step if a WebGPU device ever exists |
| shaderc / glslang | GLSL and HLSL → SPIR-V; HLSL support deprecated (<https://github.com/google/shaderc>) | Apache 2.0 | — | — | — | no |
| Veldrid.SPIRV | SPIR-V cross-compilation for Veldrid | MIT | last package 2022 (<https://www.nuget.org/packages/Veldrid.SPIRV>) | — | — | no (unmaintained) |

**2D material models in other frameworks** (references for the API's shape, not dependencies).

| Framework | Device | How a developer writes a 2D shader with parameters | Licence | Notes |
| --- | --- | --- | --- | --- |
| Foster | SDL3 + SDL_GPU, .NET 10 | compiled shaders supplied by the game; `Material` with sampler and uniform-buffer slots set by index (<https://github.com/FosterFramework/Foster/blob/main/Framework/Graphics/Material.cs>); the batcher takes a material | MIT | the closest model to `Material2D`; no named parameters either |
| MoonWorks | SDL3 GPU | HLSL or SPIR-V loaded at run time through SDL_shadercross (<https://github.com/MoonsideGames/MoonWorks>) | zlib | run-time compilation: natives in every game |
| MonoGame | its own (DX, GL, Vulkan, DX12) | `.fx` effects built by the content pipeline, `SpriteBatch.Begin(effect:)` (<https://docs.monogame.net/articles/getting_started/content_pipeline/custom_effects.html>) | Ms-PL / MIT | named parameters by reflection in the effect format; no web target today (<https://docs.monogame.net/roadmap/>) |
| FNA / FNA3D | FNA3D, with an SDL_GPU backend | D3D9 effect binaries through MojoShader (<https://github.com/FNA-XNA/FNA3D>) | Ms-PL / zlib | shader model 2–3 ceiling |
| Veldrid | Vulkan, D3D11, Metal, GL | — | MIT | no public activity since 2023 (<https://github.com/veldrid/veldrid>) |
| raylib | OpenGL through rlgl | GLSL at run time, uniforms by name | zlib | works on WebGL; the raylib platform of this engine is tier 0 by contract and desktop-only (ADR 0047) |
| sokol_gfx, bgfx | their own, WebGL2 / WebGPU capable | offline compilers with generated bindings | zlib, BSD-2 | adopting either is a new renderer, which the issue excludes |

Adopting any of these as a dependency would replace the device (a new renderer) or add run-time native code; the
comparison confirms the engine's own shape (compiled shaders, slot-indexed parameters, a material on the batcher) is
the common one for SDL_GPU frameworks.

**2D lighting.** No backend-neutral C# library was found; Penumbra (MIT, MonoGame DirectX only, "no longer in
development", <https://github.com/discosultan/penumbra>) is the known one. The techniques are small enough to be
examples: hull or edge shadow geometry (<https://www.redblobgames.com/articles/visibility/>, what the example does),
1D polar shadow maps (<https://github.com/mattdesl/lwjgl-basics/wiki/2D-Pixel-Perfect-Shadows>) and signed-distance
soft shadows (<https://www.ronja-tutorials.com/post/037-2d-shadows/>), the last two with materials on tier 1.

**Performance** (not measured). The costs are countable: the light example submits 6 batches a frame for two lights
at 320 × 180, the mask 6, the water 2, the chain 4 (two at half size). *Hypothesis:* negligible on any GPU at these
sizes, as GRAPHICS-2 measured about 0.6 ms for one extra full-window pass; on the software renderer the multiply and
additive passes are CPU blits and would scale with the canvas area. No frame time was taken here.

## 7. Decisions for the owner

1. **`Material2D` itself.** It is additive and the smallest shape found, but it is a new public type on a published
   package. Vetoing it leaves the two shader examples needing a hand-written pass (`7d85257` shows them that way).
2. **Shaders compiled on Linux.** Recommended: let `microengine shaders` use the official Linux DXC archive when one
   is present (today the command stops off Windows before it looks for DXC, so this is a code change). It would let a
   Linux CI job check committed shaders. Adds a download or a dependency: not done.
3. **A hook at the window's resolution.** Either the UI view's `DeviceRenderer` reachable from `DrawUi` without the
   platform object, or a `Presentation` stage that composes the canvas through a material. It would open CRT-style
   filters on real pixels (GRAPHICS-2 §4 item 4). It changes `src/MicroEngine` and how captures compose; it needs a
   design, not a field.

Also worth a line each: wrapping a render target as a `DeviceTexture` (small, in `src/MicroEngine`); a link from the
Graphics manual page to the new guide (`docs/manual` was outside this change).

## 8. Reproducing

```bash
# The four examples, captured (the last four of the tour), on the programmable device and on the default one:
dotnet run --project samples/Examples -c Release -- --script samples/Examples/scripts/tour.script --hidden --mute --graphics sdl-gpu --captures captures-gpu
dotnet run --project samples/Examples -c Release -- --script samples/Examples/scripts/tour.script --hidden --mute --captures captures
# Other backends: --platform sdl-software, --platform raylib. In a browser:
dotnet run --project src/MicroEngine.Cli -c Release -- package web --project samples/Examples --out dist
node web/serve.mjs dist/Examples-1.0.0-web 8080
# The tests:
dotnet test tests/MicroEngine.Desktop.Tests -c Release --filter "FullyQualifiedName~Material|FullyQualifiedName~RenderingExamples"
# After editing a shader (Windows):
dotnet run --project src/MicroEngine.Cli -c Release -- shaders --source samples/Examples/Shaders --out samples/Examples/Content/shaders
```
