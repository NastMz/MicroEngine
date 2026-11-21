# GRAPHICS-3: post-processing, the window stage, mip levels and draw order

Evaluation of 2026-11-20, on `main` at 2143542f, for NastMz/MicroEngine#181. It follows the method of
[CONSOLIDATION-1](consolidation-1.md), section 1 (A need, B current experience, C alternatives, D target experience,
E responsibility limit, F integration, G trade-offs, H decision, I validation). Decision record:
[ADR 0071](../decisions/0071-post-processing-window-stage-mip-levels-draw-order.md), which amends
[ADR 0057](../decisions/0057-materials-on-the-2d-batcher.md). Feature record: `odd/tasks/graphics-3.md`.

Machine: Ryzen 5 7600X, Windows 11, .NET SDK 10.0.400, NVIDIA GPU. **No time was measured**: other work was building
on the machine, and no decision below rests on a frame time. Devices run: the headless rasterizer, the CPU reference
device, SDL's default device and its Direct3D 11, Direct3D 12, OpenGL, Vulkan, "gpu" and software render drivers,
`sdl-gpu` (default, Direct3D 12, Vulkan) and raylib (OpenGL), all on Windows. **Not run**: any browser build, Linux,
macOS, Android, iOS, Metal.

## 0. The audit, checked against the code

The audit (consolidation-1, 2.2) said: "no device declares `Mipmaps`, there is no post-processing helper and no hook
at window resolution", and that the draw API has no depth although the batcher does. All four were true at 2143542f:

| Claim | Found |
| --- | --- |
| No device declares `Mipmaps` | `SdlGpuDevice.DeclaredFeatures`, `RaylibDevice`, `SdlRendererDevice`, `ReferenceGraphicsDevice`: none listed it. The contract accepted `MipLevels > 1` but had no way to fill the levels (data is level 0 only) |
| No post-processing helper | `samples/Examples/Examples/PostProcessExample.cs`: three render targets, two materials, a `Pass` helper and 14 lines of parameters per frame, written in the game; ADR 0057 "ships no post-processing stack" |
| No hook at window resolution | `DeviceCanvas.Compose` and `CanvasPresenter.Present` drew canvas and UI view straight into the surface target; nothing public ran between them and `Present` |
| Depth in the batcher only | `Renderer2D.Draw(..., float depth)` and `BatchSettings.Sort` exist; `DeviceRenderer` opens one `Deferred` batch per frame and passes depth 0; `IRenderer` has no depth |

One thing the audit did not say: the default desktop device (`sdl-renderer`) and every browser build are tier 0, so
an effect that needs a shader is, by default, not seen. That shaped the design more than anything else here.

## 1. Post-processing

**A. Need.** Bloom on lights and projectiles, a vignette, colour grading (a night tint, a flash, a desaturated
"paused" world), a blur behind a menu, a CRT look for a retro game, a custom shader over the whole picture. Nearly
every finished 2D game has at least one. They are full-screen, they chain, and they need intermediate targets.

**B. Current experience.** The developer wrote the chain: create and size the targets, redirect the scene into the
first, order the passes, ping-pong, set shader parameters per pass, handle the canvas resizing, dispose everything,
and compile two shaders. About 60 lines and two HLSL files for bloom, vignette and scanlines; on the default device
all of it drew nothing (the materials' fallback). Nothing could process more than one scene's own targets.

**C. Alternatives.** No library exists for this stack (an `IRenderer` over the engine's device contract), so the
alternatives are designs, compared for their shape. They were not run, and the versions and licences in this table
are as the writer knows them, not re-read for this evaluation:

| Reference | Version, licence | Shape | Taken from it |
| --- | --- | --- | --- |
| moonshine (LÖVE) | 2023 master, MIT | a chain object, `chain(effect).chain(effect)`, effects with named parameters, disable per effect | the chain as an object with ordered, switchable effects |
| gdx-vfx (libGDX) | 0.5.4, Apache-2.0 | `VfxManager` owns ping-pong buffers; `ChainVfxEffect`; begin/end capture | the chain owns the buffers; begin/end around ordinary drawing |
| Godot 4 | 4.5, MIT | `WorldEnvironment` (glow, adjustments) plus a shader on a full-screen `ColorRect`/`BackBufferCopy` | ready effects and a custom shader pass side by side |
| MonoGame / FNA | 3.8.4, MS-PL / 25.x, MS-PL | none built in: render target plus `Effect` by hand | this is experience B |
| Hand-written, per game | — | what the example did | kept possible: the chain is made of public calls |

**D. Target experience** (written before the API):

```csharp
public override void Enter() =>
    PostProcess.Add(new Bloom { Threshold = 0.6f, Strength = 1.2f }).Add(new Vignette { Strength = 0.6f });
```

and the scene's `Draw` unchanged. For the whole game, `Game.PostProcess.Add(...)`; over the interface too,
`Game.ScreenPostProcess.Add(...)` (section 2). A shader of the game's own is one more effect:
`Add(new ShaderEffect(Assets.Material("shaders/fringe.frag.shader")))`. An effect written in the game derives from
`PostEffect` and gets the chain's targets from a `PostPass`.

**E. Responsibility.** The engine owns: the picture, the ping-pong, scratch targets and textures sized to the
picture and released with the scene or game, the ready effects and their shaders, and what each effect does where no
shader runs. The game owns: which effects, their values, its own shaders and effects. Not the engine's: a lighting
system, tone mapping or HDR targets (the canvas is 8-bit), temporal effects that need the previous frame.

**F. Integration.** The chain is built on `IRenderer` and `IGraphicsDevice` (targets, states, materials: ADR 0064),
so it runs on every renderer, the headless one included, without a line per backend. Scenes: `Scene.PostProcess`,
begun before the scene's camera and ended after it, released when the scene leaves the stack. Assets: a
`ShaderEffect`'s material is an ordinary material asset. Saves, input: none. State hash: nothing; post-processing
only draws, and a chain holds no simulation state. Determinism: effects without a shader are byte-identical across
nine of the ten renderers run (the software renderer differs within its declared tolerance).

**G. Trade-offs.**

- *On the drawing API, not on the device.* One implementation everywhere and effects a game can write with the calls
  it knows; the cost is one extra full-picture copy per chain (the result is drawn into the destination rather than
  the last effect writing there) and no access to things the drawing API lacks (several colour targets).
- *Tier 0 gets real effects, not fallbacks.* `Vignette` and `Scanlines` are made of a generated texture and the
  multiply blend state on **every** device, so they are the same picture everywhere. `Blur` and `Bloom` have a
  second implementation made of halving with linear sampling and the multiply and additive presets; `ColorGrade`
  keeps tint and brightness; `Crt` keeps its lines. Each reports `Full`, `Reduced` or `None`
  (`PostEffect.SupportOn`). Two implementations of three effects is more code to keep, accepted because otherwise
  the default device and browsers show nothing.
- *Pictures keep their transparency.* A scene's picture is transparent where the scene draws nothing, so a scene
  over another is processed alone. That is why vignette and scanlines multiply instead of blending a colour over:
  the first version darkened the scene underneath (seen in the conformance capture, fixed, tested).
- *Built-in shaders are created on first use* on `sdl-gpu`, in the frame that first runs the effect. Not measured;
  noted as a limit.
- *A chain's resources are released between frames*, like any texture: a disposal inside a frame fails on a batched
  renderer. The engine's own chains are released at scene exit and game shutdown.

**H. Decision: Extend.** `PostProcess`, `PostEffect`, `PostPass`, seven ready effects, `Scene.PostProcess`,
`Game.PostProcess`. ADR 0057's "ships no post-processing stack" is reversed.

**I. Validation.** `tests/MicroEngine.Tests/Graphics/PostProcessTests.cs` (27: chain, ownership, each effect
without shaders on the headless rasterizer and the reference device, scene and game integration);
`RendererConformance` gained three rules run on ten renderers (30 tests): effects made of blending draw the same
everywhere; effects with a shader draw it where shaders run and their stated form elsewhere; a game's shader runs
as a pass. Looked at: the conformance captures of `sdl-gpu`, SDL's default device, raylib and headless side by side,
and the example on `sdl-gpu` and SDL's default device (off, on, with the game's own effect).

## 2. The window stage

**A. Need.** Effects that belong to the screen: a CRT curve that bends the HUD with the world, a fade or flash over
everything, a final shader at the window's pixels (a canvas of 320 × 180 has too few for a curved tube or a phosphor
mask). Plus the choice of scaling filter.

**B. Current experience.** The scaling was already the game's: `Presentation` chooses rounding, aspect, the upscale
sampler and the prescale (ADR 0029, GRAPHICS-2). After that, nothing: effects ran before scaling and never included
the UI view, which platforms composite at the window's resolution.

**C. Alternatives.** (1) A material on the presentation (`Presentation.Material`): one shader, no chain, no ready
effects, and nothing on tier 0. (2) Draw the UI into the canvas when effects are on: loses the window-resolution
interface. (3) The same chain type, run by the platform on the composed window: chosen.

**D. Target.** `ScreenPostProcess.Add(new Crt { Lines = Height }).Add(new Vignette());` in `Load`.

**E. Responsibility.** The platform owns the window and so runs the stage; the game owns the chain. A platform says
it has the stage with a capability (`IScreenEffects`), like `IScreenView`.

**F. Integration.** `DeviceCanvas.Compose(presentation, frame, letterbox, effects)`: the window's picture (letterbox,
canvas with its prescale, UI view) is drawn into an off-screen target of the window's size by the same
`CanvasPresenter` code, run through the chain with a `DeviceRenderer`, and copied to the surface with nearest
sampling. With no active chain the previous path runs, untouched. The headless platform has no window, so it runs the
chain when a capture is taken, on the window composed on the CPU. Captures of such a frame are the window as
presented (`DeviceCanvas.CapturePresented`). A platform without the capability: the game runs the chain on the
canvas. The on-screen touch controls are drawn after the stage, as before.

**G. Trade-offs.** Two window-sized targets and one extra copy while the chain is active; nothing when it is not.
`Scanlines.Lines` and `Crt.Lines` default to half the picture's height, which on a window means the window's rows:
the game gives the canvas's height (documented, shown in the example).

**H. Decision: Extend.** `Game.ScreenPostProcess`, `IScreenEffects`, the `Compose` and `Present` overloads.

**I. Validation.** `ScreenPostProcessTests` (6): the chain covers world, interface and letterbox at half a canvas
pixel; an inactive chain and a chain that changes nothing give the byte-identical window, prescale included; game to
platform hand-over; the canvas fallback. Looked at: the example on `sdl-gpu` (curved tube bending the heading) and
on SDL's default device and raylib (flat, lined, vignetted), on and off.

## 3. Mip levels

**A. Need.** HD art drawn below its size: a camera that zooms out, one art set for several canvas sizes, a large
map or portrait shown small. Without levels, thin details alias and shimmer in motion.

**B. Current experience.** None possible through the drawing API. The manual told developers to "keep art within
about twice the size it is drawn at".

**C. Alternatives.**

| Option | Result |
| --- | --- |
| `SDL_GenerateMipmapsForGPUTexture` (SDL 3.5.0 as pinned by ppy.SDL3-CS 2026.1002.1, zlib) | used on `sdl-gpu`; the texture needs colour-target usage |
| `rlGenTextureMipmaps` (raylib through Raylib-cs 8.1.0, zlib) | used on raylib desktop, with `GL_LINEAR_MIPMAP_LINEAR` minification |
| SDL_Renderer (the default device) | has no mip API in SDL 3.5; nothing to call |
| Levels made on the CPU and uploaded | needs per-level upload in the contract on every backend and still cannot help SDL_Renderer; rejected for the generate command |
| A CPU-prefiltered smaller texture where there are no levels | a different picture at every scale but one; not equivalent, not shipped |

**D. Target.** `Assets.Texture("big.png", TextureFilter.LinearMipmap)`: a third filter, so it flows through every
place a filter already goes (assets, atlases, the default filter) without a new overload.

**E. Responsibility.** The engine makes and remakes the levels; the developer chooses the filter per texture.

**F. Integration.** Device contract: `CommandList.GenerateMipmaps` (refused for one-level textures, render targets'
colour and inside a pass); `GraphicsFeatures.Mipmaps` declared by `sdl-gpu` and raylib desktop. Drawing API:
`TextureFilter.LinearMipmap`, `Texture.MipLevels`; `Texture.Update` remakes the levels; `SizeInBytes` counts them.
Assets: the filter is already part of an asset's key. Where the device has no levels the texture has one and is
sampled as linear: a stated degradation, visible in `MipLevels`.

**G. Trade-offs.** The default device and browsers do not get them: the limit is SDL_Renderer's, stated in the
manual, the limitations page and the example. On `sdl-gpu` a nearest draw of a texture with levels picks the nearest
level (the sampler has no level cap in the contract); stated rather than extending `SamplerDescription`. In an atlas
the smallest levels mix neighbouring sprites (one pixel of border); stated.

**H. Decision: Extend.**

**I. Validation.** `DeviceConformance.Mipmaps_...` on twelve devices: a texel checkerboard at an eighth of its size
is one colour with one level and their average (128 ± 6) with levels, the levels follow an update, and devices
without the feature refuse with the typed error. `RendererConformance` rule on ten renderers; `MipLevelTests`.
Looked at: the example on `sdl-gpu`, raylib (levels: even grey lines) and SDL's default device (both pictures alias,
labelled "no mip levels on this device").

**Compressed textures: not done, delimited.** The need is memory and load time for large HD art (a 4096² picture is
64 MB, 85 MB with levels), not picture quality. The device contract already names BC7, ETC2 and ASTC and
`DeviceGraphics.Wrap` draws such a texture (ADR 0064), so the drawing side exists. What is missing is content: an
encoder in the build (KTX-Software or Basis Universal, both Apache-2.0 native tools as the writer knows them; no
managed encoder is known, and none was searched for here), a container reader, and per-platform variants of a file, which the content pipeline does not have
(ADR 0066). And only `sdl-gpu` could load them: SDL_Renderer uploads uncompressed pixels. It is its own unit of
content work, larger than this initiative's graphics work, and it does not block a game from being built.

## 4. Draw order

**A. Need.** Top-down and isometric games sort what stands on the ground by its feet; many games separate layers
(shadows, actors, effects) without ordering their code by them.

**B. Current experience.** Call order. A developer kept a list, sorted it by Y every frame and drew from it, or
wrote the draw code in layer order. The batcher's depth and sort were unreachable from `IRenderer`.

**C. Alternatives.** (1) Expose the batcher's `SortMode` and depth on `IRenderer`: `Draw` overloads with depth on
every renderer, the headless one and test doubles included, and a sort that only exists per batch on a device
renderer. (2) A depth buffer: tier 1 only, wrong for translucent sprites. (3) A renderer that records draws and
replays them in order: works over any `IRenderer`, so everywhere, and everything that draws into a renderer (fonts,
tiles, particles) can be sorted without knowing. Chosen. The scene world (`SceneWorld`) draws nothing itself, so
there is no engine-side object list to sort instead.

**D. Target.**

```csharp
var world = _sorted.Begin(r);
foreach (var tree in _trees) { world.Depth = tree.Feet.Y; tree.Draw(world); }
world.Depth = _player.Feet.Y; _player.Draw(world);
_sorted.End();
```

**E/F.** The engine orders (layer, depth, call order: stable and the same on every run) and replays each draw under
the camera, blend mode, clip and states it was recorded with; the game says what the depth is. No state hash.

**G. Trade-offs.** Recording costs a struct per draw and a sort; sorted draws can break batches that call order kept
together. `Clear` and render targets are refused inside a block. Not measured.

**H. Decision: Extend.** `SortedRenderer`.

**I. Validation.** `SortedRendererTests` (5; the three ordering tests were seen failing with the sort removed);
example and its test; looked at sorted against call order side by side, identical on `sdl-gpu` and SDL's default.

## 5. Re-evaluated and not added

| Capability | Need | Finding | Decision |
| --- | --- | --- | --- |
| Stencil | masks and clipping to a shape | The capability a developer wants is the mask, and it exists on every device with a render target and the multiply state (`MaskExample`). A stencil buffer needs a depth-stencil attachment: `sdl-gpu` only; SDL_Renderer has none | Keep. A second, one-device way to the same picture is not added. A ready mask type would be the useful step, as its own unit |
| Several colour targets | colour and normals in one pass, for lit sprites | Needs shaders with several outputs (`sdl-gpu` only) and something that consumes them; the engine has no normal-mapped lighting. `PassDescription` has one colour attachment | Keep. Belongs with a lighting initiative that decides what the second target holds |
| Multisampling | smooth edges of shapes and rotated sprites on HD canvases | A real deficiency: circles, lines and triangles have hard edges on an HD canvas. Device MSAA on a render target exists on `sdl-gpu` only; SDL_Renderer and raylib offer none per target. The fix that reaches every device is antialiased shape drawing or a supersampled canvas, not MSAA | Keep the contract; the deficiency is recorded as open (smooth shapes) |

## 6. Shader compilation on Linux and macOS: delimited, not done

The issue assigns it here. It was not done in this unit: it cannot be verified on this machine (Windows only), and
it needs `src/MicroEngine.Cli/Shaders.cs` and CI workflows, which were outside this writer's edit surface. What was
read on 2026-11-20:

- DirectXShaderCompiler's latest release is v1.9.2609 ("DX Compiler Release for September 2026"); its notes mention
  a Linux release archive and Windows ARM64 libraries, and v1.8.2505 lists `dxc` and `libdxcompiler.so` for Linux. No
  macOS binary was found in either. The tool here uses the `Microsoft.Direct3D.DXC` package (Windows binaries;
  it reported dxcompiler 1.8.2505.32).
- SDL_shadercross (zlib) takes HLSL or SPIR-V and writes DXBC, DXIL, SPIR-V, MSL or HLSL, through SPIRV-Cross and
  `dxcompiler`; its repository has no release and publishes no binaries.

So Linux x64 looks reachable with the official DXC archive and the SPIRV-Cross the tool already uses; macOS needs a
DXC built from source or an unofficial build, a redistribution question for the owner. Both need a host to prove
byte-identical output against the Windows files. Left as its own unit.

## 7. Open after this unit

- Smooth (antialiased) shapes on HD canvases (section 5).
- Compressed textures and per-platform variants (section 3).
- The shader tool on Linux and macOS (section 6).
- Mip levels on SDL's default device and in browsers: not possible with SDL_Renderer 3.5.
- Not run: browsers, Linux, macOS, phones. A browser build draws through SDL's default device (tier 0), so it is
  expected to show what that device showed here (`Vignette`, `Scanlines`, the reduced forms, no mip levels); that is
  an inference from the shared code path, not an observation.
